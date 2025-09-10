using UnityEngine;

public class FallingState : IPlayerState
{
    public void Enter(PlayerController player) { }
    public void Exit(PlayerController player) { }
    public void HandleInput(PlayerController player, Vector2 moveInput) { }

    public void FixedUpdate(PlayerController player)
    {
        // falling handled by gravity → nothing extra
    }
}

