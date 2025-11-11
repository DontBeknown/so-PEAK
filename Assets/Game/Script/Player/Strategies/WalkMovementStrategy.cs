using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player.Strategies
{
    /// <summary>
    /// Movement strategy for walking/running on the ground.
    /// Implements Strategy Pattern for ground-based movement.
    /// </summary>
    public class WalkMovementStrategy : IMovementStrategy
    {
        private const float GRAVITY = -9.81f;

        public void OnEnter(IMovementContext context)
        {
            context.AnimationService.SetWalking(true);
            context.AnimationService.SetGrounded(true);
        }

        public void OnExit(IMovementContext context)
        {
            context.AnimationService.SetWalking(false);
        }

        public void Execute(IMovementContext context, Vector2 input)
        {
            // Get movement direction from camera
            Vector3 moveDir = context.CameraProvider.GetWorldDirection(input);

            // Apply horizontal movement
            Vector3 horizontalVelocity = moveDir * context.WalkSpeed;

            // Combine with vertical velocity
            Vector3 totalMotion = new Vector3(horizontalVelocity.x, context.Velocity.y, horizontalVelocity.z);
            context.Move(totalMotion);

            // Rotate to face movement direction
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                context.Transform.rotation = Quaternion.Slerp(
                    context.Transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * context.RotationSmoothness
                );
            }

            // Update animation
            context.AnimationService.UpdateMovement(horizontalVelocity, context.WalkSpeed);

            // Apply gravity
            context.ApplyGravity(GRAVITY);
        }

        public void HandleJump(IMovementContext context, Vector2 input)
        {
            // Check stamina
            if (context.Stats != null)
            {
                context.Stats.OnJump();
                if (context.Stats.Stamina < 0.01f)
                    return;
            }

            // Only jump if grounded
            if (!context.PhysicsService.IsGrounded())
                return;

            // Apply jump force
            if (input.sqrMagnitude <= 0.01f)
            {
                // Vertical jump
                Vector3 vel = context.Velocity;
                vel.y = context.JumpForce;
                context.Velocity = vel;
            }
            else
            {
                // Jump with momentum
                Vector3 moveDir = context.CameraProvider.GetWorldDirection(input);
                Vector3 horizontalVel = moveDir * context.WalkSpeed;
                context.Velocity = new Vector3(horizontalVel.x, context.JumpForce, horizontalVel.z);
            }

            context.AnimationService.TriggerJump();
        }
    }
}
