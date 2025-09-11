using UnityEngine;

public class WalkingState : IPlayerState
{
    public void Enter(PlayerModel model) => model.Animator.SetWalking(true);
    public void Exit(PlayerModel model) => model.Animator.SetWalking(false);

    public void HandleInput(PlayerModel model, Vector2 input) { }

    public void FixedUpdate(PlayerModel model, Vector2 input)
    {
        Vector2 circle = (input.sqrMagnitude >= 1f) ? input.normalized : input;

        Transform cam = Camera.main.transform;
        Vector3 moveDir = Quaternion.FromToRotation(cam.up, Vector3.up) *
                          cam.TransformDirection(new Vector3(circle.x, 0f, circle.y));

        Vector3 horizontal = moveDir * model.WalkSpeed;
        model.Move(horizontal + Vector3.up * model.Velocity.y);

        if (moveDir.sqrMagnitude > 0.01f)
            model.Transform.forward = Vector3.Slerp(
                model.Transform.forward, moveDir,
                Time.fixedDeltaTime * model.RotationSmoothness);

        model.Animator.UpdateMovement(horizontal, model.WalkSpeed);
        model.ApplyGravity(-9.81f);
    }

    public void OnJump(PlayerModel model) => model.Jump();

    public void OnClimb(PlayerModel model)
    {
        if (model.TryClimb(out RaycastHit hit))
        {
            PlayerController controller = model.Transform.GetComponent<PlayerController>();
            controller.ChangeState(new ClimbingState());
        }
    }
}
