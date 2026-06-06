using System.Collections.Generic;
using Game.UI;
using UnityEngine;

public class HJBClickPathController : MonoBehaviour
{
    public Camera cam;

    public HJBMeshDataProvider provider;
    public HJBPathSolver solver;
    public HJBBacktracker backtracker;
    public HJBPathVisualizer visualizer;

    [Header("A* Optional")]
    public bool includeAStar = true;
    public HJB.Pathfind.AStarPathSolver aStarSolver;
    public HJBPathVisualizer aStarVisualizer;
    public HJBPathBenchmarkRunner benchmarkRunner;

    bool AStarEnabled => includeAStar && aStarSolver != null;
    bool AStarVisualEnabled => includeAStar && aStarVisualizer != null;
    public bool RequiresAStarPath => AStarEnabled;

    [Header("Markers")]
    public GameObject startMarkerPrefab;
    public GameObject goalMarkerPrefab;

    GameObject startMarker;
    GameObject goalMarker;

    Vector2Int? start = null;
    Vector2Int? goal = null;

    [Header("Player Reference")]
    public Transform playerTransform;

    [Header("Path Fade Settings")]
    public bool useFadeWhenDrawing = true;
    [Min(0f)] public float fadeInDuration = 0.4f;
    [Min(0f)] public float holdDuration = 3f;
    [Min(0f)] public float fadeOutDuration = 0.8f;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    public static HJBClickPathController Instance { get; private set; }

    [HideInInspector] public Dictionary<WorldLevel, List<Vector3>> savedPathsByLevel = new Dictionary<WorldLevel, List<Vector3>>();
    [HideInInspector] public Dictionary<WorldLevel, List<Vector3>> savedAStarPathsByLevel = new Dictionary<WorldLevel, List<Vector3>>();

    HJBPathCacheStore cacheStore;
    HJBCachedPathPresenter cachedPathPresenter;
    HJBPathCalculationRunner calculationRunner;

    void OnEnable()
    {
        InitializeHelpers();

        Instance = this;

        if (SaveLoadService.Instance != null)
        {
            SaveLoadService.Instance.OnWorldLoaded += HandleWorldLoaded;
        }

        LoadPathsFromCurrentSave();
    }

    void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (SaveLoadService.Instance != null)
        {
            SaveLoadService.Instance.OnWorldLoaded -= HandleWorldLoaded;
        }

        cachedPathPresenter?.CancelFadeAnimations(visualizer, aStarVisualizer, AStarVisualEnabled);
    }


    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            SyncProviderHeightMultiplier();
            SetStartToPlayer();
            SetGoalToPeak();
            TrySolvePath();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleCachedPathDisplay(fadeInDuration, 0f, fadeOutDuration);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            StartBenchmarkFromPlayerToPeak();
        }
#endif
    }

    public void HidePath()
    {
        EnsureHelpers();
        cachedPathPresenter.HidePath(visualizer);
    }

    public void ToggleCachedPathDisplay(float fadeInSeconds = -1f, float displaySeconds = -1f, float fadeOutSeconds = -1f)
    {
        EnsureHelpers();
        cachedPathPresenter.ToggleCachedPathDisplay(
            provider,
            visualizer,
            aStarVisualizer,
            AStarVisualEnabled,
            useFadeWhenDrawing,
            fadeInDuration,
            holdDuration,
            fadeOutDuration,
            fadeInSeconds,
            displaySeconds,
            fadeOutSeconds);
    }

    public void SetStartToPlayer(Vector3? overrideWorldPos = null)
    {
        if (provider == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot set start position because provider is missing.");
            return;
        }

        Vector3 world;
        if (overrideWorldPos.HasValue)
        {
            world = overrideWorldPos.Value;
        }
        else if (!TryResolvePlayerPosition(out world))
        {
            Debug.LogWarning("[HJBClickPath] Cannot find player to set start position.");
            return;
        }

        Vector2Int gridPosition;
        if (TryGetCurrentSnapshot(out var snapshot))
        {
            gridPosition = snapshot.pathStart;
            world = provider.GridToWorld(gridPosition.x, gridPosition.y);
            DebugLog($"[HJBClickPath] Start set to snapshot pathStart at {gridPosition}");
        }
        else
        {
            gridPosition = provider.WorldToGrid(world);
            DebugLog($"[HJBClickPath] Start set from world position at {gridPosition}");
        }

        start = gridPosition;
        SpawnMarker(ref startMarker, startMarkerPrefab, world);
    }

    public System.Collections.IEnumerator CalculatePathFromSpawnToPeak(Vector3 spawnWorldPos)
    {
        EnsureHelpers();
        SyncProviderHeightMultiplier();
        SetStartToPlayer(spawnWorldPos);
        SetGoalToPeak();
        TrySolvePath();

        WorldLevel currentLevel = provider.worldDataManager.currentLevel;
        yield return calculationRunner.WaitForRequiredPaths(currentLevel, 3600f);
    }

    public void PreloadLevelSnapshots() => provider.PreloadLevelSnapshots();

    public System.Collections.IEnumerator PreloadLevelSnapshotsCoroutine(NextLevelLoadingScreen loadingScreen)
    {
        EnsureHelpers();
        return calculationRunner.PreloadLevelSnapshotsCoroutine(loadingScreen);
    }

    public void StartBackgroundPathCalculationForAllLevels()
    {
        EnsureHelpers();
        StartCoroutine(calculationRunner.BackgroundCalculateAllLevelPaths());
    }

    public bool HasMissingRequiredCachedPaths()
    {
        EnsureHelpers();
        return cacheStore.HasMissingRequiredCachedPaths(AStarEnabled);
    }

    public void DrawCachedPath()
    {
        EnsureHelpers();
        cachedPathPresenter.DrawCachedPath(provider, visualizer, aStarVisualizer, AStarVisualEnabled);
    }

    public void DrawCachedPathWithFade(float fadeInSeconds = -1f, float holdSeconds = -1f, float fadeOutSeconds = -1f)
    {
        EnsureHelpers();
        cachedPathPresenter.DrawCachedPathWithFade(
            provider,
            visualizer,
            aStarVisualizer,
            AStarVisualEnabled,
            fadeInDuration,
            holdDuration,
            fadeOutDuration,
            fadeInSeconds,
            holdSeconds,
            fadeOutSeconds);
    }

    public bool HasRequiredCachedPathsForLevel(WorldLevel level)
    {
        EnsureHelpers();
        return cacheStore.HasRequiredCachedPathsForLevel(level, AStarEnabled);
    }

    [ContextMenu("Benchmark HJB vs A* From Player To Peak")]
    public void StartBenchmarkFromPlayerToPeak()
    {
        EnsureHelpers();
        SyncProviderHeightMultiplier();
        SetStartToPlayer();
        SetGoalToPeak();

        if (start == null || goal == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot start benchmark because start or goal is missing.");
            return;
        }

        if (aStarSolver == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot start benchmark because A* solver is missing.");
            return;
        }

        if (benchmarkRunner == null)
        {
            benchmarkRunner = GetComponent<HJBPathBenchmarkRunner>();
        }

        if (benchmarkRunner == null)
        {
            benchmarkRunner = FindFirstObjectByType<HJBPathBenchmarkRunner>();
        }

        if (benchmarkRunner == null)
        {
            benchmarkRunner = gameObject.AddComponent<HJBPathBenchmarkRunner>();
        }

        benchmarkRunner.StartBenchmark(
            provider,
            calculationRunner,
            start.Value,
            goal.Value,
            visualizer,
            aStarVisualizer);
    }

    void TrySolvePath()
    {
        EnsureHelpers();
        calculationRunner.TrySolveCurrentPath(start, goal);
    }

    void InitializeHelpers()
    {
        cacheStore = new HJBPathCacheStore(savedPathsByLevel, savedAStarPathsByLevel);
        cachedPathPresenter = new HJBCachedPathPresenter(cacheStore, DebugLog);
        calculationRunner = new HJBPathCalculationRunner(
            provider,
            solver,
            backtracker,
            aStarSolver,
            cacheStore,
            () => AStarEnabled,
            DebugLog);
    }

    void EnsureHelpers()
    {
        if (cacheStore == null || cachedPathPresenter == null || calculationRunner == null)
        {
            InitializeHelpers();
        }
    }

    void SyncProviderHeightMultiplier()
    {
        if (provider != null && provider.worldDataManager != null && provider.worldDataManager.activeGen != null)
        {
            provider.heightMultiplier = provider.worldDataManager.activeGen.meshHeightMultiplier;
        }
    }

    bool TryResolvePlayerPosition(out Vector3 world)
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else if (provider != null && provider.renderController != null && provider.renderController.player != null)
            {
                playerTransform = provider.renderController.player;
            }
        }

        if (playerTransform == null)
        {
            world = default;
            return false;
        }

        world = playerTransform.position;
        return true;
    }

    bool TryGetCurrentSnapshot(out LevelTerrainSnapshot snapshot)
    {
        snapshot = null;
        if (provider == null || provider.worldDataManager == null)
        {
            return false;
        }

        if (provider.levelSnapshots == null)
        {
            return false;
        }

        WorldLevel level = provider.worldDataManager.currentLevel;
        if (!provider.levelSnapshots.ContainsKey(level))
        {
            provider.PreloadSnapshotForLevel(level);
        }

        return provider.levelSnapshots.TryGetValue(level, out snapshot) && snapshot != null;
    }

    void SetGoalToPeak()
    {
        if (provider == null || provider.worldDataManager == null || provider.worldDataManager.activeGen == null)
        {
            Debug.LogWarning("[HJBClickPath] WorldDataManager or activeGen not ready.");
            return;
        }

        Vector2Int peakCoord = provider.worldDataManager.activeGen.mainPeak;
        goal = peakCoord;

        Vector3 worldPos = provider.GridToWorld(peakCoord.x, peakCoord.y);
        SpawnMarker(ref goalMarker, goalMarkerPrefab, worldPos);
        DebugLog($"[HJBClickPath] Goal set to Mountain Peak at {peakCoord}");
    }

    void SpawnMarker(ref GameObject marker, GameObject prefab, Vector3 pos)
    {
        if (prefab == null)
        {
            return;
        }

        pos.y += 1f;

        if (marker == null)
        {
            marker = Instantiate(prefab, pos, Quaternion.identity);
        }
        else
        {
            marker.transform.position = pos;
        }
    }

    void HandleWorldLoaded(WorldSaveData loadedSave)
    {
        EnsureHelpers();
        cachedPathPresenter.ResetToggleState();
        LoadPathsFromCurrentSave();
    }

    void PersistPathsToCurrentSave(bool saveToFile = false)
    {
        EnsureHelpers();
        cacheStore.PersistToCurrentSave(saveToFile);
    }

    void LoadPathsFromCurrentSave()
    {
        EnsureHelpers();
        cacheStore.LoadFromCurrentSave();
    }

    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
}
