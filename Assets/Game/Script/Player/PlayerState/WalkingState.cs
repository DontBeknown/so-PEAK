using UnityEngine;

public class WalkingState : IPlayerState
{
    public void Enter(PlayerController player)
    {
        player.playerAnimator.SetWalking(true);
    }
    public void Exit(PlayerController player)
    {
        player.playerAnimator.SetWalking(false);
    }

    public void HandleInput(PlayerController player, Vector2 moveInput) { }

    public void FixedUpdate(PlayerController player)
    {
        Vector2 inputCircle = SquareToCircle(player.moveInput);

        Transform cam = Camera.main.transform;
        Vector3 moveDir = Quaternion.FromToRotation(cam.up, Vector3.up)
                        * cam.TransformDirection(new Vector3(inputCircle.x, 0f, inputCircle.y));

        Vector3 newVelo = moveDir * player.walkSpeed;
        newVelo.y = player.rb.linearVelocity.y;
        player.rb.linearVelocity = newVelo;

        if (moveDir.sqrMagnitude > 0.01f)
            player.transform.forward = Vector3.Slerp(
                player.transform.forward,
                moveDir,
                Time.fixedDeltaTime * player.rotationSmoothness);


        player.playerAnimator.UpdateMovement(player.rb.linearVelocity, player.walkSpeed);
    }

    Vector2 SquareToCircle(Vector2 input)
    {
        return (input.sqrMagnitude >= 1f) ? input.normalized : input;
    }
}
