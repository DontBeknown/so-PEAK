using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Handles foot IK for climbing on walls.
    /// Positions feet on wall surface.
    /// </summary>
    public class ClimbingFootIKHandler : IFootIKStrategy
    {
        private readonly FootIKConfig _config;

        public ClimbingFootIKHandler(FootIKConfig config)
        {
            _config = config;
        }

        public void OnEnter()
        {
            // Initialize climbing IK state
        }

        public void OnExit()
        {
            // Cleanup
        }

        public float ProcessFootIK(AvatarIKGoal foot, Animator animator, Transform transform)
        {
            // Get the current foot position from the animation
            Vector3 footPosition = animator.GetIKPosition(foot);

            // Raycast forward from foot position to find wall
            Vector3 rayOrigin = footPosition;
            Vector3 rayDirection = transform.forward;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, 
                _config.climbingFootReachDistance, _config.climbableLayer, QueryTriggerInteraction.Ignore))
            {
                // Set the foot position on the wall with offset
                Vector3 targetPosition = hit.point - hit.normal * _config.climbingFootOffset;
                animator.SetIKPosition(foot, targetPosition);

                // Align foot rotation to wall normal (sole facing wall)
                Vector3 footUp = Vector3.up;
                Vector3 footForward = -hit.normal; // Foot points into wall
                Quaternion targetRotation = Quaternion.LookRotation(footForward, footUp);
                animator.SetIKRotation(foot, targetRotation);

                #if UNITY_EDITOR
                Debug.DrawLine(rayOrigin, hit.point, Color.magenta);
                Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.cyan);
                Debug.DrawLine(footPosition, targetPosition, Color.yellow);
                #endif

                return Vector3.Distance(footPosition, targetPosition);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.DrawLine(rayOrigin, rayOrigin + rayDirection * _config.climbingFootReachDistance, Color.red);
                #endif
            }

            return 0f;
        }
    }
}
