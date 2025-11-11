using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player.Strategies
{
    /// <summary>
    /// Movement strategy for falling/airborne state.
    /// Handles gravity and limited air control.
    /// </summary>
    public class FallMovementStrategy : IMovementStrategy
    {
        private const float GRAVITY = -9.81f;
        private const float AIR_CONTROL_MULTIPLIER = 0.3f;

        private IStateTransitioner _stateTransitioner;

        public FallMovementStrategy(IStateTransitioner stateTransitioner)
        {
            _stateTransitioner = stateTransitioner;
        }

        public void OnEnter(IMovementContext context)
        {
            context.AnimationService.SetFalling(true);
            context.AnimationService.SetGrounded(false);
        }

        public void OnExit(IMovementContext context)
        {
            context.AnimationService.SetFalling(false);
        }

        public void Execute(IMovementContext context, Vector2 input)
        {
            // Limited air control
            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 moveDir = context.CameraProvider.GetWorldDirection(input);
                Vector3 airControl = moveDir * context.WalkSpeed * AIR_CONTROL_MULTIPLIER;
                
                // Apply air control to horizontal velocity
                Vector3 vel = context.Velocity;
                vel.x = airControl.x;
                vel.z = airControl.z;
                context.Velocity = vel;
            }

            // Apply full motion
            context.Move(context.Velocity);

            // Apply gravity
            context.ApplyGravity(GRAVITY);

            // Update animation
            Vector3 horizontalVel = new Vector3(context.Velocity.x, 0f, context.Velocity.z);
            context.AnimationService.UpdateMovement(horizontalVel, context.WalkSpeed);

            // Check if landed
            if (context.PhysicsService.IsGrounded() && context.Velocity.y <= 0f)
            {
                _stateTransitioner?.TransitionTo<WalkingState>();
            }
        }

        public void HandleJump(IMovementContext context, Vector2 input)
        {
            // Can't jump while in air (could add double-jump here if desired)
        }
    }
}
