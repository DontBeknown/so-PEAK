using UnityEngine;
using UnityEngine.Animations.Rigging;
using Game.Player;
using Game.Player.Animation;
using Game.Player.Interfaces;

/// <summary>
/// Refactored Hand IK Controller using Strategy Pattern.
/// Controls hand IK rig weight and hand positioning during climbing.
/// Follows Single Responsibility Principle.
/// </summary>
public class HandIKControllerRefactored : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerControllerRefactored playerController;
    [SerializeField] private Rig handIKRig;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Configuration")]
    [SerializeField] private HandIKConfig config;

    // Strategy
    private IHandPositioningStrategy _positioningStrategy;

    // State
    private float _currentRigWeight;
    private bool _isClimbing;

    private void Start()
    {
        // Auto-assign PlayerController
        if (playerController == null)
            playerController = GetComponent<PlayerControllerRefactored>();

        // Auto-assign Rig
        if (handIKRig == null)
            handIKRig = GetComponentInChildren<Rig>();

        if (config == null)
        {
            Debug.LogWarning("HandIKControllerRefactored: No config assigned! Creating default.");
            config = ScriptableObject.CreateInstance<HandIKConfig>();
        }

        // Initialize strategy
        _positioningStrategy = new RaycastHandPositioning(config);

        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (playerController == null)
            Debug.LogWarning("HandIKControllerRefactored: No PlayerControllerRefactored found!");

        if (handIKRig == null)
        {
            Debug.LogWarning("HandIKControllerRefactored: No HandIK Rig found! Hand IK disabled.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!config.enableHandIK || handIKRig == null) return;

        // Check if character is climbing
        UpdateClimbingState();

        // Control rig weight based on climbing state
        UpdateRigWeight();

        // Position hands if enabled and climbing
        if (_isClimbing && config.autoPositionHands)
        {
            PositionHands();
        }
    }

    private void UpdateClimbingState()
    {
        if (playerController != null)
        {
            _isClimbing = playerController.GetCurrentState() is ClimbingState;
        }
    }

    private void UpdateRigWeight()
    {
        float targetWeight = _isClimbing ? config.handIKWeight : 0f;
        _currentRigWeight = Mathf.Lerp(_currentRigWeight, targetWeight, 
            Time.deltaTime * config.handRigBlendSpeed);
        handIKRig.weight = _currentRigWeight;
    }

    private void PositionHands()
    {
        if (leftHandTarget != null)
        {
            _positioningStrategy.PositionHand(leftHandTarget, transform, -config.handHorizontalSpread);
        }

        if (rightHandTarget != null)
        {
            _positioningStrategy.PositionHand(rightHandTarget, transform, config.handHorizontalSpread);
        }
    }

    /// <summary>
    /// Change the hand positioning strategy at runtime
    /// </summary>
    public void SetPositioningStrategy(IHandPositioningStrategy newStrategy)
    {
        if (newStrategy != null)
        {
            _positioningStrategy = newStrategy;
        }
    }

    /// <summary>
    /// Change configuration at runtime
    /// </summary>
    public void SetConfig(HandIKConfig newConfig)
    {
        if (newConfig != null)
        {
            config = newConfig;
            _positioningStrategy = new RaycastHandPositioning(config);
        }
    }

    /// <summary>
    /// Manually set rig weight (for animations or special states)
    /// </summary>
    public void SetRigWeight(float weight)
    {
        _currentRigWeight = Mathf.Clamp01(weight);
        handIKRig.weight = _currentRigWeight;
    }
}
