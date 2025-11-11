using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player.Services
{
    /// <summary>
    /// Implementation of IPhysicsService for player physics queries.
    /// Centralizes all physics checks for consistency and testability.
    /// </summary>
    public class PlayerPhysicsService : IPhysicsService
    {
        private readonly Transform _transform;
        private readonly CharacterController _controller;
        private readonly PlayerConfig _config;

        // Mantle constants
        private const float LEDGE_UP_CHECK = 1.0f;
        private const float LEDGE_FORWARD_OFFSET = 0.45f;
        private const float LEDGE_DOWN_CHECK = 2.0f;

        public PlayerPhysicsService(Transform transform, CharacterController controller, PlayerConfig config)
        {
            _transform = transform;
            _controller = controller;
            _config = config;
        }

        public bool IsGrounded()
        {
            // Use CharacterController's built-in check first
            if (_controller.isGrounded)
                return true;

            // Perform a more reliable spherecast check
            float sphereRadius = _controller.radius * 0.9f;
            Vector3 origin = _transform.position + Vector3.up * (_controller.height * 0.5f);
            float maxDistance = (_controller.height * 0.5f) + _config.groundCheckDistance;

            bool hit = Physics.SphereCast(
                origin,
                sphereRadius,
                Vector3.down,
                out RaycastHit hitInfo,
                maxDistance,
                _config.groundLayer,
                QueryTriggerInteraction.Ignore
            );

            return hit;
        }

        public bool TryDetectClimbable(out RaycastHit hit)
        {
            Vector3 origin = _transform.position + Vector3.up * 0.5f;
            return Physics.SphereCast(
                origin,
                0.3f,
                _transform.forward,
                out hit,
                _config.climbDetectionRange,
                _config.climbableLayer,
                QueryTriggerInteraction.Ignore
            );
        }

        public bool CanMantle(Vector3 contactPoint, Vector3 wallNormal, out Vector3 topPoint)
        {
            topPoint = Vector3.zero;

            // Calculate points to check
            Vector3 startAbove = contactPoint + Vector3.up * LEDGE_UP_CHECK - wallNormal * 0.03f;
            Vector3 overLip = startAbove - wallNormal * LEDGE_FORWARD_OFFSET;

            // Check for headroom at the mantle position
            float radius = _controller.radius * 0.9f;
            float height = Mathf.Max(_controller.height * 0.95f, radius * 2f);
            
            if (CheckCapsuleOverlap(
                overLip + Vector3.up * radius,
                overLip + Vector3.up * (height - radius),
                radius))
            {
                return false; // Space is blocked
            }

            // Find ground surface on top
            if (Physics.Raycast(overLip, Vector3.down, out RaycastHit downHit, LEDGE_DOWN_CHECK, ~0, QueryTriggerInteraction.Ignore))
            {
                // Check if surface is walkable
                if (Vector3.Angle(downHit.normal, Vector3.up) <= _controller.slopeLimit + 0.1f)
                {
                    topPoint = downHit.point;
                    return true;
                }
            }

            return false;
        }

        public bool CheckCapsuleOverlap(Vector3 bottom, Vector3 top, float radius)
        {
            return Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
