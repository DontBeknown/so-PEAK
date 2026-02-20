using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Falling state - handles airborne movement and landing.
/// Carries forward horizontal momentum from the grounded state
/// and allows slight air control for steering.
/// </summary>
public class FallingState : IPlayerState
{
    private IStateTransitioner _stateTransitioner;
    private Vector3 _horizontalVelocity;

    /// <summary>
    /// How much the player can steer while airborne (0 = none, 1 = full ground control).
    /// </summary>
    private const float AirControlFactor = 0.3f;

    public FallingState()
    {
    }

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public FallingState(IStateTransitioner stateTransitioner)
    {
        _stateTransitioner = stateTransitioner;
    }

    /// <summary>
    /// Constructor with momentum carry-over from grounded state
    /// </summary>
    public FallingState(IStateTransitioner stateTransitioner, Vector3 lastHorizontalVelocity)
    {
        _stateTransitioner = stateTransitioner;
        _horizontalVelocity = new Vector3(lastHorizontalVelocity.x, 0f, lastHorizontalVelocity.z);
    }

    public void Enter(PlayerModelRefactored model)
    {
        // Clean up climbing state if transitioning from ClimbingState
        // (ClimbingState.Exit no longer handles this to avoid animation conflicts with MantlingState)
        model.GetAnimationService().SetClimbing(false);
        model.Stats?.SetClimbing(false);

        model.GetAnimationService().SetFalling(true);
    }

    public void Exit(PlayerModelRefactored model)
    {
        model.GetAnimationService().SetFalling(false);
    }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        // Allow slight air control from player input
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 airInput = model.GetCameraProvider().GetWorldDirection(input) * model.WalkSpeed * AirControlFactor;
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, airInput, Time.fixedDeltaTime * 2f);
        }

        // Combine horizontal momentum with vertical velocity (gravity)
        Vector3 motion = new Vector3(_horizontalVelocity.x, model.Velocity.y, _horizontalVelocity.z);
        model.Move(motion);
        model.ApplyGravity(-9.81f);
    }

    public void OnJump(PlayerModelRefactored model, Vector2 input) { }

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
                Debug.LogWarning("FallingState: No state transitioner available for climb transition!");
            }
        }
    }
}
