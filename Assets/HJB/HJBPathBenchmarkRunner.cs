using System.Collections;
using System.Collections.Generic;
using Game.Player;
using UnityEngine;

public class HJBPathBenchmarkRunner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerControllerRefactored playerController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private CinemachinePlayerCamera playerCamera;

    [Header("Benchmark Options")]
    [SerializeField] private LayerMask excludeLayers = 0;
    [SerializeField] private bool statImmune = true;
    [SerializeField] private bool ignoreSlope = true;
    [SerializeField] private bool disableNaturalEvents = true;
    [SerializeField] private NaturalEventDirector naturalEventDirector;

    [Header("Travel Tuning")]
    [SerializeField] private float arrivalRadius = 0.6f;
    [SerializeField] private float stuckEpsilon = 0.05f;
    [SerializeField] private float stuckSeconds = 2f;
    [SerializeField] private float maxTrialSeconds = 600f;
    [SerializeField] private float calculationTimeoutSeconds = 3600f;

    [Header("Latest Result")]
    [SerializeField] private HJBPathBenchmarkResult latestResult = new HJBPathBenchmarkResult();

    private Coroutine activeBenchmark;
    private int benchmarkVersion;
    private int originalExcludeLayers;
    private float originalSlopeLimit;
    private bool originalNaturalEventDirectorEnabled;
    private bool savedControllerState;
    private System.Action<float> activeStaminaDrainHandler;
    private float activeStaminaUsed;

    public HJBPathBenchmarkResult LatestResult => latestResult;

    public void StartBenchmark(
        HJBMeshDataProvider provider,
        HJBPathCalculationRunner calculationRunner,
        Vector2Int start,
        Vector2Int goal,
        HJBPathVisualizer hjbVisualizer,
        HJBPathVisualizer aStarVisualizer)
    {
        if (provider == null || calculationRunner == null)
        {
            Debug.LogWarning("[HJBBenchmark] Cannot start benchmark because provider or calculation runner is missing.");
            return;
        }

        ResolvePlayerReferences();
        if (playerController == null || characterController == null)
        {
            Debug.LogWarning("[HJBBenchmark] Cannot start benchmark because player controller or character controller is missing.");
            return;
        }

        if (activeBenchmark != null)
        {
            StopCoroutine(activeBenchmark);
            RestoreBenchmarkEnvironment();
        }

        benchmarkVersion++;
        PrepareBenchmarkEnvironment();
        activeBenchmark = StartCoroutine(RunBenchmark(
            benchmarkVersion,
            calculationRunner,
            start,
            goal,
            hjbVisualizer,
            aStarVisualizer));
    }

    private IEnumerator RunBenchmark(
        int version,
        HJBPathCalculationRunner calculationRunner,
        Vector2Int start,
        Vector2Int goal,
        HJBPathVisualizer hjbVisualizer,
        HJBPathVisualizer aStarVisualizer)
    {
        latestResult = new HJBPathBenchmarkResult();

        bool calculationDone = false;
        HJBBenchmarkPathCalculationResult calculationResult = null;

        calculationRunner.CalculateFreshBenchmarkPath(start, goal, result =>
        {
            if (version != benchmarkVersion)
            {
                return;
            }

            calculationResult = result;
            calculationDone = true;
        });

        float calculationTimer = 0f;
        while (!calculationDone && calculationTimer < calculationTimeoutSeconds)
        {
            calculationTimer += Time.deltaTime;
            yield return null;
        }

        if (!calculationDone || calculationResult == null)
        {
            MarkNoPath(latestResult.hjb, "HJB", calculationResult?.HjbTotalMilliseconds ?? -1d);
            MarkNoPath(latestResult.aStar, "A*", calculationResult?.AStarMilliseconds ?? -1d);
            Debug.LogWarning("[HJBBenchmark] Fresh path calculation failed or timed out.");
            RestoreBenchmarkEnvironment();
            activeBenchmark = null;
            yield break;
        }

        PopulateCalculationMetrics(latestResult.hjb, "HJB", calculationResult.HjbPath, calculationResult.HjbTotalMilliseconds);
        PopulateCalculationMetrics(latestResult.aStar, "A*", calculationResult.AStarPath, calculationResult.AStarMilliseconds);

        hjbVisualizer?.DrawPathWorld(calculationResult.HjbPath);
        aStarVisualizer?.DrawPathWorld(calculationResult.AStarPath);

        var startPose = new PlayerBenchmarkPose(playerController.transform.position, playerController.transform.rotation);

        yield return RunTravelTrial(calculationResult.HjbPath, latestResult.hjb, startPose);

        ResetPlayerForTrial(startPose);
        yield return null;

        yield return RunTravelTrial(calculationResult.AStarPath, latestResult.aStar, startPose);

        RestoreBenchmarkEnvironment();
        LogBenchmarkResult(latestResult);
        activeBenchmark = null;
    }

    private IEnumerator RunTravelTrial(
        List<Vector3> path,
        HJBAlgorithmBenchmarkResult result,
        PlayerBenchmarkPose startPose)
    {
        if (path == null || path.Count < 2)
        {
            result.status = HJBBenchmarkCompletionStatus.NoPath;
            result.travelSeconds = 0f;
            result.travelTimeDisplay = FormatSeconds(0f);
            result.actualTravelDistance = 0f;
            result.staminaUsed = 0f;
            yield break;
        }

        ResetPlayerForTrial(startPose);

        BeginStaminaCapture();

        int waypointIndex = 0;
        float elapsed = 0f;
        float actualDistance = 0f;
        float stuckTimer = 0f;
        Vector3 lastPosition = playerController.transform.position;
        Vector3 lastProgressPosition = lastPosition;
        var fixedWait = new WaitForFixedUpdate();

        ForceRunningState();

        while (waypointIndex < path.Count && elapsed < maxTrialSeconds)
        {
            Vector3 currentPosition = playerController.transform.position;
            Vector3 target = path[waypointIndex];
            Vector3 toTarget = new Vector3(target.x - currentPosition.x, 0f, target.z - currentPosition.z);
            float distanceXZ = toTarget.magnitude;

            while (distanceXZ < arrivalRadius)
            {
                waypointIndex++;
                stuckTimer = 0f;
                lastProgressPosition = currentPosition;

                if (waypointIndex >= path.Count)
                {
                    CompleteTrial();
                    yield break;
                }

                target = path[waypointIndex];
                toTarget = new Vector3(target.x - currentPosition.x, 0f, target.z - currentPosition.z);
                distanceXZ = toTarget.magnitude;
            }

            Vector3 worldDir = toTarget / Mathf.Max(distanceXZ, 0.001f);
            ApplyRunningInput(worldDir);

            yield return fixedWait;

            elapsed += Time.fixedDeltaTime;

            Vector3 nextPosition = playerController.transform.position;
            actualDistance += Vector3.Distance(lastPosition, nextPosition);
            lastPosition = nextPosition;

            float progressDistance = Vector2.Distance(
                new Vector2(nextPosition.x, nextPosition.z),
                new Vector2(lastProgressPosition.x, lastProgressPosition.z));

            if (progressDistance > stuckEpsilon)
            {
                lastProgressPosition = nextPosition;
                stuckTimer = 0f;
            }
            else
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= stuckSeconds)
                {
                    result.status = HJBBenchmarkCompletionStatus.Stuck;
                    result.travelSeconds = elapsed;
                    result.travelTimeDisplay = FormatSeconds(elapsed);
                    result.actualTravelDistance = actualDistance;
                    result.staminaUsed = activeStaminaUsed;
                    ClearRunningInput();
                    EndStaminaCapture();
                    yield break;
                }
            }

            ForceRunningState();
        }

        result.status = HJBBenchmarkCompletionStatus.Timeout;
        result.travelSeconds = elapsed;
        result.travelTimeDisplay = FormatSeconds(elapsed);
        result.actualTravelDistance = actualDistance;
        result.staminaUsed = activeStaminaUsed;
        ClearRunningInput();
        EndStaminaCapture();

        void CompleteTrial()
        {
            result.status = HJBBenchmarkCompletionStatus.Completed;
            result.travelSeconds = elapsed;
            result.travelTimeDisplay = FormatSeconds(elapsed);
            result.actualTravelDistance = actualDistance;
            result.staminaUsed = activeStaminaUsed;
            ClearRunningInput();
            EndStaminaCapture();
        }
    }

    private void PrepareBenchmarkEnvironment()
    {
        if (savedControllerState)
        {
            return;
        }

        savedControllerState = true;
        originalExcludeLayers = characterController.excludeLayers;
        originalSlopeLimit = characterController.slopeLimit;

        characterController.excludeLayers = excludeLayers;
        if (ignoreSlope)
        {
            characterController.slopeLimit = 90f;
        }

        if (statImmune)
        {
            playerStats?.SetSurvivalDrainSuppressed(true);
            playerStats?.SetStaminaDrainSuppressed(true);
            playerStats?.SetImmunity(true);
        }

        if (disableNaturalEvents && naturalEventDirector != null)
        {
            originalNaturalEventDirectorEnabled = naturalEventDirector.enabled;
            naturalEventDirector.enabled = false;
        }

        playerController.SetInputBlocked(true);
        playerController.SetInventoryToggleBlocked(true);
        playerCamera?.EnableCameraInput(false);
    }

    private void RestoreBenchmarkEnvironment()
    {
        EndStaminaCapture();
        ClearRunningInput();

        if (savedControllerState && characterController != null)
        {
            characterController.excludeLayers = originalExcludeLayers;
            characterController.slopeLimit = originalSlopeLimit;
        }

        if (statImmune)
        {
            playerStats?.SetImmunity(false);
            playerStats?.SetStaminaDrainSuppressed(false);
            playerStats?.SetSurvivalDrainSuppressed(false);
        }

        if (disableNaturalEvents && naturalEventDirector != null)
        {
            naturalEventDirector.enabled = originalNaturalEventDirectorEnabled;
        }

        if (playerController != null)
        {
            playerController.SetInputBlocked(false);
            playerController.SetInventoryToggleBlocked(false);
            playerController.TransitionTo(new WalkingState(playerController));
        }

        playerCamera?.EnableCameraInput(true);
        savedControllerState = false;
    }

    private void ResetPlayerForTrial(PlayerBenchmarkPose pose)
    {
        ClearRunningInput();

        bool controllerWasEnabled = characterController.enabled;
        characterController.enabled = false;
        playerController.transform.SetPositionAndRotation(pose.position, pose.rotation);
        characterController.enabled = controllerWasEnabled;

        if (playerStats != null)
        {
            playerStats.SetRunning(false);
            playerStats.SetWalking(false);
            playerStats.RestoreStamina(Mathf.Max(0f, playerStats.MaxStamina - playerStats.Stamina));
            playerStats.FullRest();

            if (statImmune)
            {
                playerStats.SetSurvivalDrainSuppressed(true);
                playerStats.SetStaminaDrainSuppressed(true);
            }
        }

        playerController.TransitionTo(new WalkingState(playerController));
    }

    private void ApplyRunningInput(Vector3 worldDir)
    {
        playerController.transform.rotation = Quaternion.RotateTowards(
            playerController.transform.rotation,
            Quaternion.LookRotation(worldDir, Vector3.up),
            720f * Time.fixedDeltaTime);

        playerController.SetWorldMoveDirOverride(worldDir);
        playerController.InputHandler?.OverrideMoveInput(Vector2.up);
        playerController.InputHandler?.SetSprintOverride(true);
    }

    private void ForceRunningState()
    {
        if (!(playerController.GetCurrentState() is RunningState))
        {
            playerController.TransitionTo(new RunningState(playerController));
        }
    }

    private void BeginStaminaCapture()
    {
        EndStaminaCapture();
        activeStaminaUsed = 0f;

        if (playerStats == null)
        {
            return;
        }

        activeStaminaDrainHandler = amount => activeStaminaUsed += amount;
        playerStats.OnStaminaDrained += activeStaminaDrainHandler;
    }

    private void EndStaminaCapture()
    {
        if (playerStats != null && activeStaminaDrainHandler != null)
        {
            playerStats.OnStaminaDrained -= activeStaminaDrainHandler;
        }

        activeStaminaDrainHandler = null;
    }

    private void ClearRunningInput()
    {
        if (playerController == null)
        {
            return;
        }

        playerController.SetWorldMoveDirOverride(null);
        playerController.InputHandler?.OverrideMoveInput(null);
        playerController.InputHandler?.SetSprintOverride(null);
    }

    private void ResolvePlayerReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerControllerRefactored>();
        }

        if (characterController == null && playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
        }

        if (playerStats == null && playerController != null)
        {
            playerStats = playerController.GetComponent<PlayerStats>();
        }
        
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<CinemachinePlayerCamera>();
        }

        if (naturalEventDirector == null)
        {
            naturalEventDirector = FindFirstObjectByType<NaturalEventDirector>();
        }
    }

    private void PopulateCalculationMetrics(
        HJBAlgorithmBenchmarkResult result,
        string algorithm,
        List<Vector3> path,
        double calculationMilliseconds)
    {
        result.algorithm = algorithm;
        result.calculationMilliseconds = calculationMilliseconds;
        result.calculationTimeDisplay = FormatMilliseconds(calculationMilliseconds);
        result.waypointCount = path?.Count ?? 0;
        result.plannedPathDistance = CalculatePathDistance(path);
        result.status = result.waypointCount >= 2
            ? HJBBenchmarkCompletionStatus.NotRun
            : HJBBenchmarkCompletionStatus.NoPath;
    }

    private void MarkNoPath(HJBAlgorithmBenchmarkResult result, string algorithm, double calculationMilliseconds)
    {
        result.algorithm = algorithm;
        result.calculationMilliseconds = calculationMilliseconds;
        result.calculationTimeDisplay = FormatMilliseconds(calculationMilliseconds);
        result.status = HJBBenchmarkCompletionStatus.NoPath;
        result.waypointCount = 0;
        result.plannedPathDistance = 0f;
        result.travelSeconds = 0f;
        result.travelTimeDisplay = FormatSeconds(0f);
        result.actualTravelDistance = 0f;
        result.staminaUsed = 0f;
    }

    private static float CalculatePathDistance(List<Vector3> path)
    {
        if (path == null || path.Count < 2)
        {
            return 0f;
        }

        float distance = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            distance += Vector3.Distance(path[i - 1], path[i]);
        }

        return distance;
    }

    private static void LogBenchmarkResult(HJBPathBenchmarkResult result)
    {
        Debug.Log(
            "[HJBBenchmark] HJB vs A* result\n" +
            "Algorithm | Calc time | Travel time | Planned m | Actual m | Stamina | Status | Waypoints\n" +
            FormatResultLine(result.hjb) + "\n" +
            FormatResultLine(result.aStar));
    }

    private static string FormatResultLine(HJBAlgorithmBenchmarkResult result)
    {
        return $"{result.algorithm} | {result.calculationTimeDisplay} | {result.travelTimeDisplay} | " +
               $"{result.plannedPathDistance:F2} | {result.actualTravelDistance:F2} | " +
               $"{result.staminaUsed:F2} | {result.status} | {result.waypointCount}";
    }

    private static string FormatMilliseconds(double milliseconds)
    {
        if (milliseconds < 0d)
        {
            return "--:--.---";
        }

        return FormatSeconds((float)(milliseconds / 1000d));
    }

    private static string FormatSeconds(float seconds)
    {
        if (seconds < 0f)
        {
            return "--:--.---";
        }

        int totalMilliseconds = Mathf.FloorToInt(seconds * 1000f);
        int minutes = totalMilliseconds / 60000;
        int remainingSeconds = totalMilliseconds / 1000 % 60;
        int milliseconds = totalMilliseconds % 1000;

        return $"{minutes:00}:{remainingSeconds:00}.{milliseconds:000}";
    }

    private readonly struct PlayerBenchmarkPose
    {
        public readonly Vector3 position;
        public readonly Quaternion rotation;

        public PlayerBenchmarkPose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }
}
