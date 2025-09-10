using UnityEngine;

public interface IPlayerState
{
    void Enter(PlayerController player);
    void Exit(PlayerController player);
    void HandleInput(PlayerController player, Vector2 moveInput);
    void FixedUpdate(PlayerController player);
}
