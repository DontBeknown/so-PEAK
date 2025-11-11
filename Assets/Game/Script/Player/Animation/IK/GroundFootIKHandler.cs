using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Handles foot IK for ground-based movement.
    /// Adjusts feet to terrain height and slope.
    /// </summary>
    public class GroundFootIKHandler : IFootIKStrategy
    {
        private readonly FootIKConfig _config;

        public GroundFootIKHandler(FootIKConfig config)
        {
            _config = config;
        }

        public void OnEnter()
        {
            // Could initialize any state here
        }

        public void OnExit()
        {
            // Cleanup if needed
        }

        public float ProcessFootIK(AvatarIKGoal foot, Animator animator, Transform transform)
        {
            // Get the current foot position from the animation
            Vector3 footPosition = animator.GetIKPosition(foot);

            // Raycast from above the foot position downward
            float rayStartY = Mathf.Max(transform.position.y, footPosition.y) + _config.raycastUpOffset;
            Vector3 rayOrigin = new Vector3(footPosition.x, rayStartY, footPosition.z);

            float totalRayDistance = _config.raycastDistance + _config.raycastUpOffset + 
                                    Mathf.Abs(rayStartY - footPosition.y);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, totalRayDistance, 
                _config.groundLayer, QueryTriggerInteraction.Ignore))
            {
                // Calculate how much we need to adjust the foot
                float footAdjustment = hit.point.y - footPosition.y;

                // Apply IK if the adjustment is within reasonable limits
                if (Mathf.Abs(footAdjustment) < _config.raycastDistance)
                {
                    // Only apply IK if adjustment is significant enough (reduces jittering)
                    if (Mathf.Abs(footAdjustment) > _config.minFootDistance)
                    {
                        // Set the foot position to the ground with a small offset
                        Vector3 targetPosition = hit.point + Vector3.up * _config.footOffset;
                        animator.SetIKPosition(foot, targetPosition);

                        // Align foot rotation to ground normal
                        Vector3 footForward = transform.forward;
                        Vector3 slopeForward = Vector3.ProjectOnPlane(footForward, hit.normal).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(slopeForward, hit.normal);
                        animator.SetIKRotation(foot, targetRotation);

                        #if UNITY_EDITOR
                        Debug.DrawLine(rayOrigin, hit.point, Color.green);
                        Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.blue);
                        Debug.DrawLine(footPosition, targetPosition, Color.yellow);
                        Debug.DrawRay(hit.point, slopeForward * 0.15f, Color.cyan);
                        #endif

                        return footAdjustment;
                    }
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
                    #endif
                }
            }
            else
            {
                #if UNITY_EDITOR
                Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * totalRayDistance, Color.red);
                #endif
            }

            return 0f;
        }
    }
}
