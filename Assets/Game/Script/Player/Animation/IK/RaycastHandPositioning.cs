using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Default hand positioning strategy for climbing.
    /// Uses raycasts to find wall surface and position hands.
    /// </summary>
    public class RaycastHandPositioning : IHandPositioningStrategy
    {
        private readonly HandIKConfig _config;

        public RaycastHandPositioning(HandIKConfig config)
        {
            _config = config;
        }

        public void PositionHand(Transform handTarget, Transform transform, float horizontalOffset)
        {
            if (handTarget == null || transform == null) return;

            // Calculate raycast origin (from character position with offset)
            Vector3 origin = transform.position + 
                           Vector3.up * _config.handHeightOffset + 
                           transform.right * horizontalOffset;
            Vector3 direction = transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 
                _config.handReachDistance, _config.climbableLayer, QueryTriggerInteraction.Ignore))
            {
                // Position hand on wall surface with offset
                Vector3 targetPosition = hit.point - hit.normal * _config.handOffsetFromWall;
                handTarget.position = targetPosition;

                // Rotate hand to face wall
                handTarget.rotation = Quaternion.LookRotation(-hit.normal);

                #if UNITY_EDITOR
                Debug.DrawLine(origin, hit.point, Color.cyan);
                Debug.DrawRay(hit.point, hit.normal * 0.1f, Color.yellow);
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.DrawLine(origin, origin + direction * _config.handReachDistance, Color.red);
                #endif
            }
        }
    }
}
