using UnityEngine;

public interface IPlayerState
{
    void Enter(PlayerModelRefactored model);
    void Exit(PlayerModelRefactored model);
    void HandleInput(PlayerModelRefactored model, Vector2 input);
    void FixedUpdate(PlayerModelRefactored model, Vector2 input);
    void OnJump(PlayerModelRefactored model, Vector2 input) { }
    void OnClimb(PlayerModelRefactored model) { }
}
