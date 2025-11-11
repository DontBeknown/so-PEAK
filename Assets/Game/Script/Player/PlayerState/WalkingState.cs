using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Walking state - handles ground-based movement.
/// Refactored to use dependency injection and remove Camera.main references.
/// </summary>
public class WalkingState : IPlayerState
{
    private IStateTransitioner _stateTransitioner;

    public WalkingState()
    {
    }

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public WalkingState(IStateTransitioner stateTransitioner)
    {
        _stateTransitioner = stateTransitioner;
    }

    public void Enter(PlayerModelRefactored model)
    {
        var animService = model.GetAnimationService();
        animService.SetWalking(true);
        animService.SetGrounded(true);
    }

    public void Exit(PlayerModelRefactored model)
    {
        model.GetAnimationService().SetWalking(false);
    }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        var context = model.GetMovementContext();
        var cameraProvider = model.GetCameraProvider();
        var animService = model.GetAnimationService();

        // Get movement direction from camera
        Vector3 moveDir = cameraProvider.GetWorldDirection(input);

        // Apply horizontal movement
        Vector3 horizontal = moveDir * model.WalkSpeed;
        Vector3 motion = new Vector3(horizontal.x, model.Velocity.y, horizontal.z);
        model.Move(motion);

        // Rotate to face movement direction
        if (moveDir.sqrMagnitude > 0.01f)
        {
            model.Transform.forward = Vector3.Slerp(
                model.Transform.forward, moveDir,
                Time.fixedDeltaTime * model.RotationSmoothness);
        }

        // Update animation
        animService.UpdateMovement(horizontal, model.WalkSpeed);
        
        // Apply gravity
        model.ApplyGravity(-9.81f);
    }

    public void OnJump(PlayerModelRefactored model, Vector2 input)
    {
        if (model.Stats != null)
        {
            model.Stats.OnJump(); 
            if (model.Stats.Stamina < 0.01f) return; 
        }

        if (input.sqrMagnitude <= 0.01f)
        {
            model.Jump();
            return;
        }

        Vector3 moveDir = model.GetCameraProvider().GetWorldDirection(input);
        model.JumpWithMomentum(moveDir.normalized);
    }

    public void OnClimb(PlayerModelRefactored model)
    {
        if (model.TryClimb(out RaycastHit _))
        {
            if (_stateTransitioner != null)
            {
                _stateTransitioner.TransitionTo(new ClimbingState(_stateTransitioner));
            }
            else
            {
                Debug.LogWarning("WalkingState: No state transitioner available for climb transition!");
            }
        }
    }
}
