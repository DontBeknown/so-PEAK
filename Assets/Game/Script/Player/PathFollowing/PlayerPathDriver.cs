using System.Collections.Generic;
using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player.Services;
using Game.Player.Stat.Assessment;
using Game.UI;
using UnityEngine.TextCore.Text;

namespace Game.Player.PathFollowing
{
    /// <summary>
    /// Drives the player along a list of world-space waypoints supplied via SetPath().
    /// Supports WalkOnly mode and Run mode (run until stamina low → stand still to regen → resume).
    /// Detects stuck state, attempts one jump to clear obstacles, then aborts with assessment if still stuck.
    /// Call GenerateAssessment is fired automatically at path end/abort.
    /// </summary>
    public class PlayerPathDriver : MonoBehaviour
    {
        public enum DriveMode { WalkOnly, Run }

        [SerializeField] private DriveMode driveMode = DriveMode.WalkOnly;
        [SerializeField] bool statImmue = false;
        [SerializeField] bool ignoreSlope = true;

        [SerializeField] private PlayerControllerRefactored playerController;
        [SerializeField] private CinemachinePlayerCamera playerCamera;
        [SerializeField] private HJBClickPathController hjbClickPathController;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerStats playerStats;

        [Header("Tuning")]
        [SerializeField] private float arrivalRadius = 0.6f;
        [SerializeField] [Range(0f, 1f)] private float lowStaminaThreshold = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float resumeStaminaThreshold = 0.70f;
        [SerializeField] private float stuckEpsilon = 0.05f;
        [SerializeField] private float stuckSeconds = 1.0f;
        [SerializeField] private float postJumpStuckSeconds = 1.0f;
        [SerializeField] private LayerMask ignoreLayers = ~0;

        private List<Vector3> _waypoints;
        private int _waypointIndex;
        private DriveMode _mode;
        private bool _isActive;
        private bool _staminaPaused;
        private bool _jumpAttempted;
        private float _stuckTimer;
        private Vector3 _lastProgressPos;

        private PlayerStats _stats;
        private LearningAssessmentService _assessmentService;
        private IEventBus _eventBus;

        private float oldSlopelimit;

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerControllerRefactored>();
            _stats = GetComponent<PlayerStats>();

            if (hjbClickPathController == null)
                hjbClickPathController = FindFirstObjectByType<HJBClickPathController>();

            if (playerCamera == null)
                playerCamera = ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
            if (playerCamera == null)
                playerCamera = FindFirstObjectByType<CinemachinePlayerCamera>();

            if(characterController == null)
                characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                StartCachedPathForCurrentLevel();
            }
        }

        /// <summary>Supply the path before calling StartPath.</summary>
        public void SetPath(IList<Vector3> waypoints)
        {
            _waypoints = new List<Vector3>(waypoints);
        }

        private void StartCachedPathForCurrentLevel()
        {
            if (!TryGetCachedPathForCurrentLevel(out var currentLevel, out var cachedPath))
            {
                return;
            }
            
            SetPath(cachedPath);
            StartPath(driveMode);

            Debug.Log($"[PlayerPathDriver] Started cached path for {currentLevel} with {cachedPath.Count} waypoints.");
        }

        private bool TryGetCachedPathForCurrentLevel(out WorldLevel currentLevel, out List<Vector3> cachedPath)
        {
            currentLevel = default;
            cachedPath = null;

            if (hjbClickPathController == null)
            {
                hjbClickPathController = FindFirstObjectByType<HJBClickPathController>();
            }

            if (hjbClickPathController == null)
            {
                Debug.LogWarning("[PlayerPathDriver] Cannot start cached path because the HJB path controller is missing.");
                return false;
            }

            if (hjbClickPathController.provider == null || hjbClickPathController.provider.worldDataManager == null)
            {
                Debug.LogWarning("[PlayerPathDriver] Cannot start cached path because world data is missing.");
                return false;
            }

            currentLevel = hjbClickPathController.provider.worldDataManager.currentLevel;
            if (!hjbClickPathController.savedPathsByLevel.TryGetValue(currentLevel, out cachedPath) || cachedPath == null || cachedPath.Count == 0)
            {
                Debug.LogWarning($"[PlayerPathDriver] No cached path available for {currentLevel}. Press P to calculate first.");
                cachedPath = null;
                return false;
            }

            return true;
        }

        /// <summary>Begin driving the player along the previously set path.</summary>
        public void StartPath(DriveMode mode)
        {
            if (_waypoints == null || _waypoints.Count == 0)
            {
                Debug.LogWarning("[PlayerPathDriver] No waypoints set. Call SetPath first.");
                return;
            }

            if (_isActive)
                StopPath(generateAssessment: false);

            _mode = mode;
            _waypointIndex = 0;
            _isActive = true;
            _staminaPaused = false;
            _jumpAttempted = false;
            _stuckTimer = 0f;
            _lastProgressPos = transform.position;

            _assessmentService = ServiceContainer.Instance.TryGet<LearningAssessmentService>();
            if (_assessmentService == null)
                _assessmentService = FindFirstObjectByType<LearningAssessmentService>();

            SetPlayerControlsBlocked(true);
            playerCamera?.EnableCameraInput(false);
            playerController.TransitionTo(new WalkingState(playerController));

            characterController.excludeLayers = ignoreLayers;

            if (ignoreSlope)
            {
                oldSlopelimit = characterController.slopeLimit;
                characterController.slopeLimit = 90f;
            }

            if(statImmue)
            {
                playerStats.SetImmunity(true);
            }
        }

        /// <summary>Abort or finish the path, optionally triggering the assessment report.</summary>
        public void StopPath(bool generateAssessment = true)
        {
            if (!_isActive) return;

            _isActive = false;
            _staminaPaused = false;

            var inputHandler = playerController.InputHandler;
            if (inputHandler != null)
            {
                inputHandler.OverrideMoveInput(null);
                inputHandler.SetSprintOverride(null);
            }

            SetPlayerControlsBlocked(false);
            playerCamera?.EnableCameraInput(true);
            playerController.TransitionTo(new WalkingState(playerController));

            if(ignoreSlope){
                characterController.slopeLimit = oldSlopelimit;
                characterController.excludeLayers = 0;
            }

            if(statImmue)
            {
                playerStats.SetImmunity(false);
            }

            if (generateAssessment && _assessmentService != null)
                _assessmentService.GenerateAssessment();

            if (generateAssessment)
                OpenAssessmentStatsUIInProgressionMode();

        }

        private void OpenAssessmentStatsUIInProgressionMode()
        {
            var uiServiceProvider = ServiceContainer.Instance.TryGet<UIServiceProvider>();
            uiServiceProvider?.OpenPanel("PlayerStatsTracker");

            _eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Publish(new AssessmentUIOpenedEvent("PlayerStatsTracker", true));
        }

        private void SetPlayerControlsBlocked(bool blocked)
        {
            playerController.SetInputBlocked(blocked);
            playerController.SetInventoryToggleBlocked(blocked);
        }

        private void FixedUpdate()
        {
            if (!_isActive || _waypoints == null) return;

            if (_waypointIndex >= _waypoints.Count)
            {
                StopPath(generateAssessment: true);
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 target = _waypoints[_waypointIndex];
            Vector3 toTarget = new Vector3(target.x - currentPos.x, 0f, target.z - currentPos.z);
            float distXZ = toTarget.magnitude;

            // Advance through waypoints that are already within arrival radius
            while (distXZ < arrivalRadius)
            {
                _waypointIndex++;
                _stuckTimer = 0f;
                _lastProgressPos = currentPos;
                _jumpAttempted = false;

                if (_waypointIndex >= _waypoints.Count)
                {
                    StopPath(generateAssessment: true);
                    return;
                }

                target = _waypoints[_waypointIndex];
                toTarget = new Vector3(target.x - currentPos.x, 0f, target.z - currentPos.z);
                distXZ = toTarget.magnitude;
            }

            var handler = playerController.InputHandler;
            if (handler == null) return;

            // ── Stamina pause (Run mode only) ──────────────────────────────
            if (_mode == DriveMode.Run && _stats != null)
            {
                float staminaPct = _stats.StaminaPercent;

                if (!_staminaPaused && staminaPct <= lowStaminaThreshold)
                {
                    _staminaPaused = true;
                    handler.OverrideMoveInput(Vector2.zero);
                    handler.SetSprintOverride(false);
                    playerController.TransitionTo(new WalkingState(playerController));
                    return;
                }

                if (_staminaPaused)
                {
                    if (staminaPct < resumeStaminaThreshold)
                    {
                        // Stand still — let WalkingState passive regen do its work
                        handler.OverrideMoveInput(Vector2.zero);
                        return;
                    }

                    _staminaPaused = false;
                    handler.SetSprintOverride(true);
                    playerController.TransitionTo(new RunningState(playerController));
                }
            }

            // ── Drive input toward waypoint ────────────────────────────────
            Vector3 worldDir = toTarget / distXZ;
            Vector2 driveInput = WorldDirToCameraInput(worldDir);
            handler.OverrideMoveInput(driveInput);

            if (_mode == DriveMode.Run && !_staminaPaused)
                handler.SetSprintOverride(true);
            else if (_mode == DriveMode.WalkOnly)
                handler.SetSprintOverride(false);

            // ── Stuck detection ────────────────────────────────────────────
            float movedXZ = Vector2.Distance(
                new Vector2(currentPos.x, currentPos.z),
                new Vector2(_lastProgressPos.x, _lastProgressPos.z));

            if (movedXZ > stuckEpsilon)
            {
                _lastProgressPos = currentPos;
                _stuckTimer = 0f;
            }
            else
            {
                _stuckTimer += Time.fixedDeltaTime;

                if (!_jumpAttempted && _stuckTimer >= stuckSeconds)
                {
                    _jumpAttempted = true;
                    _stuckTimer = 0f;
                    handler.TriggerJump();
                }
                else if (_jumpAttempted && _stuckTimer >= postJumpStuckSeconds)
                {
                    // Jump didn't free us — abort and show assessment
                    StopPath(generateAssessment: true);
                }
            }
        }

        /// <summary>
        /// Converts a world-space XZ direction into the camera-relative Vector2 input
        /// the player states expect (input.x = right, input.y = forward relative to camera).
        /// </summary>
        private static Vector2 WorldDirToCameraInput(Vector3 worldDir)
        {
            var cam = Camera.main;
            if (cam == null)
                return new Vector2(worldDir.x, worldDir.z);

            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            Vector3 camRight   = Vector3.ProjectOnPlane(cam.transform.right,   Vector3.up);

            if (camForward.sqrMagnitude > 0.001f) camForward.Normalize();
            if (camRight.sqrMagnitude   > 0.001f) camRight.Normalize();

            return new Vector2(
                Vector3.Dot(worldDir, camRight),
                Vector3.Dot(worldDir, camForward));
        }
    }
}
