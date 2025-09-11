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
        model.Move(Vector3.up * model.Velocity.y);
        model.ApplyGravity(-9.81f);
    }
    public void OnClimb(PlayerModel model)
    {
        if (model.TryClimb(out RaycastHit hit))
        {
            PlayerController controller = model.Transform.GetComponent<PlayerController>();
            controller.ChangeState(new ClimbingState());
        }
    }
}
