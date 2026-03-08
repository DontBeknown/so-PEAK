using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Transient state that plays the climb-up/mantle animation,
/// waits for it to finish, then transitions to WalkingState.
/// </summary>
public class MantlingState : IPlayerState
{
    private IStateTransitioner _stateTransitioner;
    private Animator _animator;
    private bool _animationStarted;
    private bool _hasSnapped;
    private Vector3 _targetTopPoint;
    private float _timer;

    /// <summary>
    /// Maximum time to wait in MantlingState before force-exiting.
    /// Acts as a failsafe if the Animator state name doesn't match perfectly.
    /// </summary>
    private const float MaxMantleTime = 3.0f;

    /// <summary>
    /// How far through the animation to snap the player to the ledge top.
    /// 0.7 = 70% through the climb-up animation.
    /// </summary>
    private const float SnapNormalizedTime = 0.7f;

    /// <summary>
    /// The trigger parameter name that initiates the climb-up animation.
    /// </summary>
    private const string ClimbUpTrigger = "ClimbUp";

    /// <summary>
    /// The exact name of the Animator state that plays during the climb-up.
    /// Must match the state name in the Animator Controller exactly.
    /// </summary>
    private const string ClimbUpStateName = "Climb_Ledge";


    public MantlingState(IStateTransitioner stateTransitioner, Vector3 topPoint)
    {
        _stateTransitioner = stateTransitioner;
        _targetTopPoint = topPoint;
    }

    public void Enter(PlayerModelRefactored model)
    {
        // TODO: re-enable animation once Climb_Ledge state is confirmed working
        model.GetAnimationService().SetClimbing(false);
        model.Stats?.SetClimbing(false);
        model.Velocity = Vector3.zero;

        if (_targetTopPoint != Vector3.zero)
            model.SnapToTop(_targetTopPoint);

        _stateTransitioner?.TransitionTo(new WalkingState(_stateTransitioner));
    }

    public void Exit(PlayerModelRefactored model) { }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input) { }

    public void OnJump(PlayerModelRefactored model, Vector2 input) { }
    public void OnClimb(PlayerModelRefactored model) { }
}
