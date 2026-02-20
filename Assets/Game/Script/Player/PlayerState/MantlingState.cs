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
    private const float MaxMantleTime = 2.0f;

    /// <summary>
    /// How far through the animation to snap the player to the ledge top.
    /// 0.7 = 70% through the climb-up animation.
    /// </summary>
    private const float SnapNormalizedTime = 0.7f;

    /// <summary>
    /// The name of the climb-up animation state in the Animator Controller.
    /// Change this to match the exact state name in your Animator.
    /// </summary>
    private const string ClimbUpStateName = "ClimbUp";


    public MantlingState(IStateTransitioner stateTransitioner, Vector3 topPoint)
    {
        _stateTransitioner = stateTransitioner;
        _targetTopPoint = topPoint;
    }

    public void Enter(PlayerModelRefactored model)
    {
        _animator = model.Transform.GetComponentInChildren<Animator>();
        _animationStarted = false;
        _hasSnapped = false;

        // Fire the ClimbUp trigger FIRST, before disabling climbing
        _animator.SetTrigger(ClimbUpStateName);

        // NOW disable climbing animation and stamina drain
        // (moved here from ClimbingState.Exit to prevent animation conflicts)
        model.GetAnimationService().SetClimbing(false);
        model.Stats?.SetClimbing(false);

        // Lock movement while mantling
        model.Velocity = Vector3.zero;
        _timer = 0f;
    }

    public void Exit(PlayerModelRefactored model) { }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        _timer += Time.fixedDeltaTime;

        // Failsafe: if animation takes too long or state name doesn't match
        if (_timer > MaxMantleTime)
        {
            Debug.LogWarning($"[MantlingState] Failsafe triggered! Either animation took longer than {MaxMantleTime}s, or the Animator State is not exactly named '{ClimbUpStateName}'. Check your Animator Controller.");
            if (_targetTopPoint != Vector3.zero)
                model.SnapToTop(_targetTopPoint);
            _stateTransitioner?.TransitionTo(new WalkingState(_stateTransitioner));
            return;
        }

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // Wait for the Animator to actually enter the ClimbUp state
        if (!_animationStarted)
        {
            if (stateInfo.IsName(ClimbUpStateName))
                _animationStarted = true;
            return;
        }

        // Phase 1: Snap the player to the top partway through the animation
        if (!_hasSnapped && stateInfo.normalizedTime >= SnapNormalizedTime)
        {
            if (_targetTopPoint != Vector3.zero)
            {
                model.SnapToTop(_targetTopPoint);
            }
            _hasSnapped = true;
        }

        // Phase 2: Once the animation has fully played, transition to walking
        if (stateInfo.normalizedTime >= 0.95f && !_animator.IsInTransition(0))
        {
            // Ensure snap happened even if we somehow skipped phase 1
            if (!_hasSnapped)
            {
                if (_targetTopPoint != Vector3.zero)
                    model.SnapToTop(_targetTopPoint);
            }

            _stateTransitioner?.TransitionTo(new WalkingState(_stateTransitioner));
        }
    }

    public void OnJump(PlayerModelRefactored model, Vector2 input) { }
    public void OnClimb(PlayerModelRefactored model) { }
}
