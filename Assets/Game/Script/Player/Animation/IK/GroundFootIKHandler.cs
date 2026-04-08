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
        private float _lastLeftAdjustment;
        private float _lastRightAdjustment;

        public GroundFootIKHandler(FootIKConfig config)
        {
            _config = config;
            _lastLeftAdjustment = 0f;
            _lastRightAdjustment = 0f;
        }

        public void OnEnter()
        {
            // Could initialize any state here
        }

        public void OnExit()
        {
            // Cleanup if needed
        }

        private float GetSmoothedCachedAdjustment(AvatarIKGoal foot)
        {
            float cached = foot == AvatarIKGoal.LeftFoot ? _lastLeftAdjustment : _lastRightAdjustment;
            cached = Mathf.Lerp(cached, 0f, Time.deltaTime * _config.smoothSpeed);
            SetCachedAdjustment(foot, cached);
            return cached;
        }

        private void SetCachedAdjustment(AvatarIKGoal foot, float value)
        {
            if (foot == AvatarIKGoal.LeftFoot)
            {
                _lastLeftAdjustment = value;
                return;
            }

            _lastRightAdjustment = value;
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
                // Calculate how much we need to adjust the foot.
                float footAdjustment = hit.point.y - footPosition.y;
                float maxReasonableAdjustment = Mathf.Max(_config.maxPelvisAdjustment * 2f, _config.minFootDistance);

                // Ignore suspiciously large one-frame corrections and keep previous stable value.
                if (Mathf.Abs(footAdjustment) > maxReasonableAdjustment)
                {
                    #if UNITY_EDITOR
                    Debug.DrawLine(rayOrigin, hit.point, Color.magenta);
                    #endif
                    return GetSmoothedCachedAdjustment(foot);
                }

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

                    SetCachedAdjustment(foot, footAdjustment);
                    return footAdjustment;
                }

                SetCachedAdjustment(foot, 0f);
                return 0f;
            }

            #if UNITY_EDITOR
            Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * totalRayDistance, Color.red);
            #endif

            return GetSmoothedCachedAdjustment(foot);
        }
    }
}
