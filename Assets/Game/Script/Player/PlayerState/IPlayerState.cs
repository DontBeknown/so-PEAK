using UnityEngine;

public interface IPlayerState
{
    void Enter(PlayerModel model);
    void Exit(PlayerModel model);
    void HandleInput(PlayerModel model, Vector2 input);
    void FixedUpdate(PlayerModel model, Vector2 input);
    void OnJump(PlayerModel model) { }
    void OnClimb(PlayerModel model) { }
}
