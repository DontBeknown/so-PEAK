using UnityEngine;
using Game.Player.Animation;
using Game.Player;
using Game.Player.Interfaces;

/// <summary>
/// Refactored Foot IK Controller using Strategy Pattern.
/// Coordinates between different IK strategies and pelvis adjustment.
/// Follows Single Responsibility and Open/Closed principles.
/// </summary>
public class FootIKControllerRefactored : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerControllerRefactored playerController;
    [SerializeField] private FootIKConfig config;

    [Header("Runtime Control")]
    [SerializeField] private bool enableFootIK = true;
    [SerializeField] private float initialIKDelaySeconds = 0.15f;

    // Strategy Pattern
    private IFootIKStrategy _currentStrategy;
    private GroundFootIKHandler _groundHandler;
    private ClimbingFootIKHandler _climbingHandler;
    private PelvisAdjuster _pelvisAdjuster;

    // IK weights (smoothed)
    private float _leftFootIKWeight;
    private float _rightFootIKWeight;

    // State tracking
    private bool _isAirborne;
    private bool _wasAirborne;
    private bool _hasSeededPelvis;
    private float _ikStartTime;

    private void Start()
    {
        // Auto-assign if not set
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("FootIKControllerRefactored: No Animator found!");
            enabled = false;
            return;
        }

        if (playerController == null)
            playerController = GetComponent<PlayerControllerRefactored>();

        if (config == null)
        {
            config = Resources.Load<FootIKConfig>("FootIKConfig");
            if (config == null)
            {
                Debug.LogError("FootIKControllerRefactored: No FootIKConfig assigned or found in Resources/FootIKConfig. Disabling Foot IK to avoid unstable startup behavior.");
                enabled = false;
                return;
            }
        }

        // Initialize strategies
        _groundHandler = new GroundFootIKHandler(config);
        _climbingHandler = new ClimbingFootIKHandler(config);
        _pelvisAdjuster = new PelvisAdjuster(config);

        // Start with ground strategy
        SetStrategy(_groundHandler);

        // Force a reliable first landing transition and seed values on the first IK frame.
        _wasAirborne = true;
        _hasSeededPelvis = false;
        _leftFootIKWeight = 0f;
        _rightFootIKWeight = 0f;
        _ikStartTime = Time.time;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !enableFootIK) return;

        if (!_hasSeededPelvis)
        {
            _pelvisAdjuster.Reset(animator.bodyPosition.y);
            _hasSeededPelvis = true;
        }

        if (Time.time - _ikStartTime < initialIKDelaySeconds)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            return;
        }

        // Check if player is in a non-grounded state
        UpdateAirborneState();

        // Detect landing: was airborne, now grounded
        if (_wasAirborne && !_isAirborne)
        {
            // Seed with current body Y so the lerp doesn't start from 0 and sink the character
            _pelvisAdjuster.Reset(animator.bodyPosition.y);
            
            // Zero out IK weights so they fade in smoothly
            _leftFootIKWeight = 0f;
            _rightFootIKWeight = 0f;
        }
        _wasAirborne = _isAirborne;

        // Switch strategy based on state
        UpdateStrategy();

        // Apply IK weights
        ApplyIKWeights();

        // Process foot IK using current strategy
        float leftFootOffset = _currentStrategy.ProcessFootIK(AvatarIKGoal.LeftFoot, animator, transform);
        float rightFootOffset = _currentStrategy.ProcessFootIK(AvatarIKGoal.RightFoot, animator, transform);

        // Adjust pelvis (only for ground movement)
        if (!_isAirborne)
        {
            _pelvisAdjuster.AdjustPelvisHeight(leftFootOffset, rightFootOffset, animator);
        }
    }

    private void UpdateAirborneState()
    {
        if (playerController != null)
        {
            var state = playerController.GetCurrentState();
            if (state == null)
            {
                _isAirborne = false;
                return;
            }

            _isAirborne = state is ClimbingState
                       || state is MantlingState
                       || state is FallingState;
        }
        else
        {
            // Fallback to animator parameter
            _isAirborne = animator.GetBool("isClimbing");
        }
    }

    private void UpdateStrategy()
    {
        IFootIKStrategy newStrategy = _isAirborne ? 
            (IFootIKStrategy)_climbingHandler : _groundHandler;

        if (newStrategy != _currentStrategy)
        {
            SetStrategy(newStrategy);
        }
    }

    private void SetStrategy(IFootIKStrategy newStrategy)
    {
        _currentStrategy?.OnExit();
        _currentStrategy = newStrategy;
        _currentStrategy?.OnEnter();
    }

    private void ApplyIKWeights()
    {
        // Smooth weight transitions
        float targetWeight = enableFootIK ? config.ikWeight : 0f;
        _leftFootIKWeight = Mathf.Lerp(_leftFootIKWeight, targetWeight, 
            Time.deltaTime * config.smoothSpeed);
        _rightFootIKWeight = Mathf.Lerp(_rightFootIKWeight, targetWeight, 
            Time.deltaTime * config.smoothSpeed);

        // Set IK weights
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, _leftFootIKWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _leftFootIKWeight * config.rotationWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, _rightFootIKWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _rightFootIKWeight * config.rotationWeight);
    }

    /// <summary>
    /// Enable or disable foot IK at runtime
    /// </summary>
    public void SetFootIKEnabled(bool enabled)
    {
        enableFootIK = enabled;
    }

    /// <summary>
    /// Change the configuration at runtime
    /// </summary>
    public void SetConfig(FootIKConfig newConfig)
    {
        if (newConfig != null)
        {
            config = newConfig;
            
            // Recreate strategies with new config
            _groundHandler = new GroundFootIKHandler(config);
            _climbingHandler = new ClimbingFootIKHandler(config);
            _pelvisAdjuster = new PelvisAdjuster(config);
            _hasSeededPelvis = false;
            _leftFootIKWeight = 0f;
            _rightFootIKWeight = 0f;
            _ikStartTime = Time.time;
            
            // Reapply current strategy
            SetStrategy(_isAirborne ? (IFootIKStrategy)_climbingHandler : _groundHandler);
        }
    }
}
