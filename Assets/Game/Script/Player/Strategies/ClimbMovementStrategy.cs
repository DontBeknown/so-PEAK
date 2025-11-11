using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player.Strategies
{
    /// <summary>
    /// Movement strategy for climbing walls.
    /// Handles wall attachment, climbing movement, and mantling.
    /// </summary>
    public class ClimbMovementStrategy : IMovementStrategy
    {
        private Vector3 _lastWallNormal = Vector3.forward;
        private IStateTransitioner _stateTransitioner;

        public ClimbMovementStrategy(IStateTransitioner stateTransitioner)
        {
            _stateTransitioner = stateTransitioner;
        }

        public void OnEnter(IMovementContext context)
        {
            context.AnimationService.SetClimbing(true);
            context.Velocity = Vector3.zero;
        }

        public void OnExit(IMovementContext context)
        {
            context.AnimationService.SetClimbing(false);
            context.Stats?.SetClimbing(false);
        }

        public void Execute(IMovementContext context, Vector2 input)
        {
            // Check stamina - fall if depleted
            if (context.Stats != null && context.Stats.Stamina <= 0f)
            {
                _stateTransitioner?.TransitionTo<FallingState>();
                return;
            }

            // Try to stay attached to wall
            if (context.PhysicsService.TryDetectClimbable(out RaycastHit hit))
            {
                _lastWallNormal = hit.normal;

                // Face the wall
                Quaternion targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
                context.Transform.rotation = Quaternion.Slerp(
                    context.Transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * context.RotationSmoothness
                );

                // Move on the wall: x=strafe, y=up/down
                Vector3 climbLocal = new Vector3(input.x, input.y, 0f);
                Vector3 climbMotion = context.Transform.TransformDirection(climbLocal) * context.ClimbSpeed;
                context.Move(climbMotion);

                // Update animation
                context.AnimationService.UpdateMovement(new Vector3(input.x, 0f, input.y), context.ClimbSpeed);

                // No gravity while attached
                context.Velocity = Vector3.zero;

                // Set climbing state for stamina drain
                context.Stats?.SetClimbing(true);

                // Try to mantle if possible
                if (context.PhysicsService.CanMantle(hit.point, hit.normal, out Vector3 topPoint))
                {
                    MantleToTop(context, topPoint);
                    return;
                }
            }
            else
            {
                // Lost the wall - try one last mantle attempt
                Vector3 checkPoint = context.Transform.position + Vector3.up * 0.5f;
                if (_lastWallNormal != Vector3.zero &&
                    context.PhysicsService.CanMantle(checkPoint, _lastWallNormal, out Vector3 topPoint))
                {
                    MantleToTop(context, topPoint);
                    return;
                }

                // No ledge found - fall
                _stateTransitioner?.TransitionTo<FallingState>();
            }
        }

        public void HandleJump(IMovementContext context, Vector2 input)
        {
            // Push off the wall
            Vector3 vel = context.Velocity;
            vel.y = context.JumpForce;
            context.Velocity = vel;

            _stateTransitioner?.TransitionTo<FallingState>();
        }

        private void MantleToTop(IMovementContext context, Vector3 topPoint)
        {
            // Snap to top position
            bool wasEnabled = context.Controller.enabled;
            context.Controller.enabled = false;

            float halfHeight = context.Controller.height * 0.5f;
            context.Transform.position = topPoint + Vector3.up * (halfHeight + context.Controller.skinWidth + 0.01f);
            context.Velocity = Vector3.zero;

            context.Controller.enabled = wasEnabled;

            // Face forward along surface
            Vector3 forward = Vector3.ProjectOnPlane(context.Transform.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.0001f)
            {
                context.Transform.forward = forward.normalized;
            }

            _stateTransitioner?.TransitionTo<WalkingState>();
        }
    }
}
