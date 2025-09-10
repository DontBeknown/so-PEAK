using UnityEngine;

public class ClimbingState : IPlayerState
{
    public void Enter(PlayerController player) 
    {
        player.playerAnimator.SetClimbing(true);
        //player.rb.useGravity = false;
    }
    public void Exit(PlayerController player)
    { 
        player.playerAnimator.SetClimbing(false);
        //player.rb.useGravity = true;
    }

    public void HandleInput(PlayerController player, Vector2 moveInput) { }

    public void FixedUpdate(PlayerController player)
    {
        Vector2 input = player.moveInput;
        Vector3 wallNormal = Vector3.zero;
        int hitCount = 0;

        Vector3[] probeDirs =
        {
        player.transform.forward,
        Quaternion.AngleAxis(45f, Vector3.up) * player.transform.forward,
        Quaternion.AngleAxis(-45f, Vector3.up) * player.transform.forward,
        Quaternion.AngleAxis(90f, Vector3.up) * player.transform.forward,
        Quaternion.AngleAxis(-90f, Vector3.up) * player.transform.forward
    };

        foreach (var dir in probeDirs)
        {
            if (Physics.Raycast(player.transform.position,
                                dir,
                                out RaycastHit hit,
                                player.climbDetectionRange,
                                player.climbableLayer))
            {
                wallNormal += hit.normal;
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            wallNormal.Normalize();

            if (Physics.Raycast(player.transform.position, -wallNormal,
                out RaycastHit stickHit,
                player.climbDetectionRange + 0.5f,
                player.climbableLayer))
            {
                player.transform.rotation = Quaternion.Slerp(
                    player.transform.rotation,
                    Quaternion.LookRotation(-stickHit.normal, Vector3.up),
                    Time.fixedDeltaTime * player.rotationSmoothness
                );

                player.rb.position = Vector3.Lerp(
                    player.rb.position,
                    stickHit.point + stickHit.normal * (player.capsuleRadius + player.wallOffset),
                    Time.fixedDeltaTime * 10f
                );

                if (input.sqrMagnitude > 0.01f)
                    player.rb.linearVelocity = player.transform.TransformDirection(new Vector3(input.x, input.y, 0f)) * player.climbSpeed;
                else
                    player.rb.linearVelocity = Vector3.zero;

                player.rb.useGravity = false;

                player.playerAnimator.UpdateMovement(
                    player.transform.TransformDirection(new Vector3(input.x, 0f, input.y)),
                    player.climbSpeed
                );


                return;
            }
        }



        //player.ChangeState(new FallingState());
    }

}
