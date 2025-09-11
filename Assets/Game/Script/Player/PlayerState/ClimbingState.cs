using UnityEngine;
using UnityEngine.InputSystem.XR;

public class ClimbingState : IPlayerState
{
    public void Enter(PlayerModel model) => model.Animator.SetClimbing(true);
    public void Exit(PlayerModel model) => model.Animator.SetClimbing(false);

    public void HandleInput(PlayerModel model, Vector2 input) { }

    public void FixedUpdate(PlayerModel model, Vector2 input)
    {
        if (model.TryClimb(out RaycastHit hit))
        {
            model.Transform.rotation = Quaternion.Slerp(
                model.Transform.rotation,
                Quaternion.LookRotation(-hit.normal, Vector3.up),
                Time.fixedDeltaTime * model.RotationSmoothness);

            Vector3 climbMotion = model.Transform.TransformDirection(new Vector3(input.x, input.y, 0f)) * model.ClimbSpeed;
            model.Move(climbMotion);

            model.Animator.UpdateMovement(new Vector3(input.x, 0f, input.y), model.ClimbSpeed);
            model.Velocity.y = 0f;
        }
    }

    public void OnJump(PlayerModel model)
    {
        model.Velocity.y = model.JumpForce;
    }

    public void OnClimb(PlayerModel model)
    {
        model.Velocity.y = 0f;
        PlayerController controller = model.Transform.GetComponent<PlayerController>();
        controller.ChangeState(new FallingState());
    }
}
