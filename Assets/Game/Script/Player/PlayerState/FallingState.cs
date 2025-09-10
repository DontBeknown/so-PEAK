using UnityEngine;

public class FallingState : IPlayerState
{
    public void Enter(PlayerController player)
    {
        player.playerAnimator.SetFalling(true);
        player.playerAnimator.SetWalking(false);
        player.playerAnimator.SetClimbing(false);
    }

    public void Exit(PlayerController player)
    {
        player.playerAnimator.SetFalling(false);
    }

    public void HandleInput(PlayerController player, Vector2 moveInput) { }

    public void FixedUpdate(PlayerController player)
    {
        /*bool grounded = player.IsGrounded();
        player.playerAnimator.SetGrounded(grounded);

        if (grounded && player.rb.linearVelocity.y <= 0.1f)
        {
            player.ChangeState(new WalkingState());
        }*/
    }
}

