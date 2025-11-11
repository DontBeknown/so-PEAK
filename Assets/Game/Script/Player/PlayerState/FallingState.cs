using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Falling state - handles airborne movement and landing.
/// Refactored to use dependency injection for state transitions.
/// </summary>
public class FallingState : IPlayerState
{
    private IStateTransitioner _stateTransitioner;

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

    public void Enter(PlayerModelRefactored model)
    {
        model.GetAnimationService().SetFalling(true);
    }

    public void Exit(PlayerModelRefactored model)
    {
        model.GetAnimationService().SetFalling(false);
    }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        model.Move(model.Velocity);
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
