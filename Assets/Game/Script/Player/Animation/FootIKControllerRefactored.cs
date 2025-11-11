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

    // Strategy Pattern
    private IFootIKStrategy _currentStrategy;
    private GroundFootIKHandler _groundHandler;
    private ClimbingFootIKHandler _climbingHandler;
    private PelvisAdjuster _pelvisAdjuster;

    // IK weights (smoothed)
    private float _leftFootIKWeight;
    private float _rightFootIKWeight;

    // State tracking
    private bool _isClimbing;

    private void Start()
    {
        // Auto-assign if not set
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerController == null)
            playerController = GetComponent<PlayerControllerRefactored>();

        if (config == null)
        {
            Debug.LogWarning("FootIKControllerRefactored: No config assigned! Using default settings.");
            config = ScriptableObject.CreateInstance<FootIKConfig>();
        }

        // Initialize strategies
        _groundHandler = new GroundFootIKHandler(config);
        _climbingHandler = new ClimbingFootIKHandler(config);
        _pelvisAdjuster = new PelvisAdjuster(config);

        // Start with ground strategy
        SetStrategy(_groundHandler);

        if (animator == null)
        {
            Debug.LogError("FootIKControllerRefactored: No Animator found!");
            enabled = false;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !enableFootIK) return;

        // Check climbing state
        UpdateClimbingState();

        // Switch strategy based on state
        UpdateStrategy();

        // Apply IK weights
        ApplyIKWeights();

        // Process foot IK using current strategy
        float leftFootOffset = _currentStrategy.ProcessFootIK(AvatarIKGoal.LeftFoot, animator, transform);
        float rightFootOffset = _currentStrategy.ProcessFootIK(AvatarIKGoal.RightFoot, animator, transform);

        // Adjust pelvis (only for ground movement)
        if (!_isClimbing)
        {
            _pelvisAdjuster.AdjustPelvisHeight(leftFootOffset, rightFootOffset, animator);
        }
    }

    private void UpdateClimbingState()
    {
        if (playerController != null)
        {
            _isClimbing = playerController.GetCurrentState() is ClimbingState;
        }
        else
        {
            // Fallback to animator parameter
            _isClimbing = animator.GetBool("isClimbing");
        }
    }

    private void UpdateStrategy()
    {
        IFootIKStrategy newStrategy = _isClimbing ? 
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
            
            // Reapply current strategy
            SetStrategy(_isClimbing ? (IFootIKStrategy)_climbingHandler : _groundHandler);
        }
    }
}
