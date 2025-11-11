using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Climbing state - handles wall climbing and mantling.
/// Refactored to use dependency injection for state transitions.
/// </summary>
public class ClimbingState : IPlayerState
{

    private const float LedgeUpCheck = 1.0f;
    private const float LedgeForward = 0.45f;
    private const float LedgeDownCheck = 2.0f;

    private Vector3 _lastWallNormal = Vector3.forward;
    private IStateTransitioner _stateTransitioner;

    public ClimbingState()
    {
    }

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public ClimbingState(IStateTransitioner stateTransitioner)
    {
        _stateTransitioner = stateTransitioner;
    } 

    public void Enter(PlayerModelRefactored model)
    {
        model.GetAnimationService().SetClimbing(true);
        model.Velocity = Vector3.zero;
    }

    public void Exit(PlayerModelRefactored model)
    {
        model.GetAnimationService().SetClimbing(false);
        model.Stats?.SetClimbing(false);
    }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        if (model.Stats != null && model.Stats.Stamina <= 0f)
        {
            TransitionToFalling(model);
            return;
        }

        // 1) Try to stay attached to wall
        if (model.TryClimb(out RaycastHit hit))
        {
            _lastWallNormal = hit.normal;

            // Face the wall
            model.Transform.rotation = Quaternion.Slerp(
                model.Transform.rotation,
                Quaternion.LookRotation(-hit.normal, Vector3.up),
                Time.fixedDeltaTime * model.RotationSmoothness
            );

            // Move on the wall: x=strafe, y=up/down
            Vector3 climbLocal = new Vector3(input.x, input.y, 0f);
            Vector3 climbMotion = model.Transform.TransformDirection(climbLocal) * model.ClimbSpeed;
            model.Move(climbMotion);

            model.GetAnimationService().UpdateMovement(new Vector3(input.x, 0f, input.y), model.ClimbSpeed);
            model.Velocity = Vector3.zero; // no gravity while attached

            // Drain stamina
            model.Stats?.SetClimbing(true);

            // 2) Try to mantle out while still touching the wall
            if (model.TryMantleTopFrom(hit.point, hit.normal, LedgeUpCheck, LedgeForward, LedgeDownCheck, out Vector3 topPoint))
            {
                FinishClimbToTop(model, topPoint);
                return;
            }
        }
        else
        {
            // 3) Lost the wall – BEFORE falling, try one last mantle using the last seen normal
            if (_lastWallNormal != Vector3.zero &&
                model.TryMantleTopFrom(model.Transform.position + Vector3.up * 0.5f, _lastWallNormal, LedgeUpCheck, LedgeForward, LedgeDownCheck, out Vector3 topPoint2))
            {
                FinishClimbToTop(model, topPoint2);
                return;
            }

            // No ledge found -> fall
            TransitionToFalling(model);
        }
    }

    private void TransitionToFalling(PlayerModelRefactored model)
    {
        if (_stateTransitioner != null)
        {
            _stateTransitioner.TransitionTo(new FallingState(_stateTransitioner));
        }
        else
        {
            Debug.LogWarning("ClimbingState: No state transitioner available for falling transition!");
        }
    }

    private void TransitionToWalking(PlayerModelRefactored model)
    {
        if (_stateTransitioner != null)
        {
            _stateTransitioner.TransitionTo(new WalkingState(_stateTransitioner));
        }
        else
        {
            Debug.LogWarning("ClimbingState: No state transitioner available for walking transition!");
        }
    }

    private void FinishClimbToTop(PlayerModelRefactored model, Vector3 topPoint)
    {
        // If you have a climb-up animation, trigger here and snap on event.
        model.SnapToTop(topPoint);

        // Face forward along the surface normal's tangent (optional)
        Vector3 forward = Vector3.ProjectOnPlane(model.Transform.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
            model.Transform.forward = forward.normalized;

        // Use state transitioner
        TransitionToWalking(model);
    }

    public void OnJump(PlayerModelRefactored model, Vector2 input)
    {
        // Hop off the wall
        Vector3 vel = model.Velocity;
        vel.y = model.JumpForce;
        model.Velocity = vel;
        TransitionToFalling(model);
    }

    public void OnClimb(PlayerModelRefactored model)
    {
        // Manually release from wall
        model.Velocity = Vector3.zero;
        
        // Check if player is already on ground - if so, go to walking instead of falling
        IPhysicsService physicsService = model.GetPhysicsService();
        if (physicsService != null && physicsService.IsGrounded())
        {
            TransitionToWalking(model);
        }
        else
        {
            TransitionToFalling(model);
        }
    }
}
