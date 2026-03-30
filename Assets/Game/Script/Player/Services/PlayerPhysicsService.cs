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
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform), "Transform cannot be null");
            if (controller == null)
                throw new System.ArgumentNullException(nameof(controller), "CharacterController cannot be null");
            if (config == null)
                throw new System.ArgumentNullException(nameof(config), "PlayerConfig cannot be null");

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

            return Physics.SphereCast(
                origin,
                sphereRadius,
                Vector3.down,
                out _,
                maxDistance,
                _config.groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }

        public bool TryDetectClimbable(out RaycastHit hit)
        {
            Vector3 origin = _transform.TransformPoint(_controller.center);
            float castRadius = Mathf.Max(0.05f, _controller.radius * _config.climbDetectionRadiusMultiplier);

            bool hasHit = Physics.SphereCast(
                origin,
                castRadius,
                _transform.forward,
                out hit,
                _config.climbDetectionRange,
                _config.climbableLayer,
                QueryTriggerInteraction.Ignore
            );

            // Draw sphere cast visualization
            Vector3 castEnd = origin + _transform.forward * _config.climbDetectionRange;
            Debug.DrawLine(origin, castEnd, Color.cyan, 0);
            DebugDrawWireSphere(origin, castRadius, Color.cyan, 0);
            if (hasHit)
                DebugDrawWireSphere(hit.point, castRadius, Color.green, 0);

            if (!hasHit)
            {
                Debug.Log($"[TryDetectClimbable] No climbable surface hit. Cast from {origin} forward {_config.climbDetectionRange}m with radius {castRadius}");
                return false;
            }

            // Wall angle: 0° = ceiling (normal points up), 90° = vertical wall (normal points horizontal), 180° = floor
            // Config range 70-110° means we accept surfaces that are nearly vertical (± 20° from perfectly vertical)
            float wallAngle = Vector3.Angle(hit.normal, Vector3.up);
            Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.red, 0);
            
            if (wallAngle < _config.minClimbableWallAngle || wallAngle > _config.maxClimbableWallAngle)
            {
                Debug.Log($"[TryDetectClimbable] Wall angle REJECTED: {wallAngle:F1}° (valid range: {_config.minClimbableWallAngle}-{_config.maxClimbableWallAngle}°)");
                return false;
            }

            float approachAngle = Vector3.Angle(_transform.forward, -hit.normal);
            Debug.DrawRay(hit.point, -hit.normal * 0.5f, Color.magenta, 0);
            
            if (approachAngle > _config.maxClimbApproachAngle)
            {
                Debug.Log($"[TryDetectClimbable] Approach angle REJECTED: {approachAngle:F1}° (max allowed: {_config.maxClimbApproachAngle}°)");
                return false;
            }

            Debug.Log($"[TryDetectClimbable] SUCCESS - Wall angle: {wallAngle:F1}°, Approach angle: {approachAngle:F1}°");
            return true;
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

        /// <summary>
        /// Draw a wire sphere for debugging purposes
        /// </summary>
        private void DebugDrawWireSphere(Vector3 center, float radius, Color color, float duration)
        {
            int segments = 16;
            float angleDelta = 360f / segments;

            // Draw circles on X-Z plane (horizontal)
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleDelta * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleDelta * Mathf.Deg2Rad;

                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                Debug.DrawLine(p1, p2, color, duration);

                // Draw circles on Y-Z plane (vertical)
                p1 = center + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
                p2 = center + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
                Debug.DrawLine(p1, p2, color, duration);

                // Draw circles on X-Y plane (vertical)
                p1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
                p2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
                Debug.DrawLine(p1, p2, color, duration);
            }
        }
    }
}
