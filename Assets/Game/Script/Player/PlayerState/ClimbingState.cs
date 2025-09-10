using UnityEngine;

public class ClimbingState : IPlayerState
{
    public void Enter(PlayerController player) { }
    public void Exit(PlayerController player) { }
    public void HandleInput(PlayerController player, Vector2 moveInput) { }

    public void FixedUpdate(PlayerController player)
    {
        Vector2 input = player.moveInput;
        Vector3 offset = player.transform.TransformDirection(Vector2.one * 0.5f);
        Vector3 checkDirection = Vector3.zero;
        int k = 0;

        for (int i = 0; i < 4; i++)
        {
            if (Physics.Raycast(player.transform.position + offset, player.transform.forward,
                out RaycastHit checkHit, player.climbDetectionRange, player.climbableLayer))
            {
                checkDirection += checkHit.normal;
                k++;
            }
            offset = Quaternion.AngleAxis(90f, player.transform.forward) * offset;
        }

        if (k > 0)
            checkDirection /= k;

        if (Physics.Raycast(player.transform.position, -checkDirection, out RaycastHit hit,
            player.climbDetectionRange, player.climbableLayer))
        {
            // stick to wall
            Quaternion targetRot = Quaternion.LookRotation(-hit.normal, Vector3.up);
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, Time.fixedDeltaTime * player.rotationSmoothness);

            player.rb.position = Vector3.Lerp(player.rb.position,
                hit.point + hit.normal * (player.capsuleRadius + player.wallOffset),
                Time.fixedDeltaTime * 10f);

            player.rb.linearVelocity = player.transform.TransformDirection(new Vector3(input.x, input.y, 0f)) * player.climbSpeed;
        }
        else
        {
            player.ChangeState(new FallingState());
        }
    }
}
