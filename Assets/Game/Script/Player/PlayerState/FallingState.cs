using UnityEngine;

public class FallingState : IPlayerState
{
    public void Enter(PlayerModel model) => model.Animator.SetFalling(true);

    public void Exit(PlayerModel model)
    {
        model.Animator.SetFalling(false);
    }

    public void HandleInput(PlayerModel model, Vector2 input) { }

    public void FixedUpdate(PlayerModel model, Vector2 input)
    {
        model.Move(model.Velocity);
        model.ApplyGravity(-9.81f);
    }

    public void OnJump(PlayerModel model, Vector2 input) { }

    public void OnClimb(PlayerModel model)
    {
        if (model.TryClimb(out RaycastHit _))
        {
            PlayerController controller = model.Transform.GetComponent<PlayerController>();
            controller.ChangeState(new ClimbingState());
        }
    }
}
