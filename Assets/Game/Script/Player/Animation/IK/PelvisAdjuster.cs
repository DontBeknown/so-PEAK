using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Handles pelvis height adjustment based on foot positions.
    /// Keeps character grounded when feet are at different heights.
    /// </summary>
    public class PelvisAdjuster
    {
        private readonly FootIKConfig _config;
        private float _lastPelvisPositionY;

        public PelvisAdjuster(FootIKConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Adjusts pelvis height based on foot offsets
        /// </summary>
        /// <param name="leftFootOffset">Left foot height adjustment</param>
        /// <param name="rightFootOffset">Right foot height adjustment</param>
        /// <param name="animator">The animator component</param>
        public void AdjustPelvisHeight(float leftFootOffset, float rightFootOffset, Animator animator)
        {
            if (!_config.enablePelvisAdjustment || animator == null)
                return;

            // Use the LOWEST foot to prevent overextension (keeps character grounded)
            float lowestFootOffset = Mathf.Min(leftFootOffset, rightFootOffset);

            // Clamp the adjustment to prevent extreme movements
            lowestFootOffset = Mathf.Clamp(lowestFootOffset, -_config.maxPelvisAdjustment, _config.maxPelvisAdjustment);

            // Only adjust if there's a meaningful difference
            if (Mathf.Abs(lowestFootOffset) > 0.01f)
            {
                Vector3 bodyPosition = animator.bodyPosition;
                float targetY = bodyPosition.y + lowestFootOffset + _config.pelvisOffset;

                // Smooth the pelvis movement
                bodyPosition.y = Mathf.Lerp(_lastPelvisPositionY, targetY, 
                    Time.deltaTime * _config.pelvisUpDownSpeed);
                _lastPelvisPositionY = bodyPosition.y;

                animator.bodyPosition = bodyPosition;
            }
        }

        /// <summary>
        /// Resets the pelvis adjuster state
        /// </summary>
        public void Reset()
        {
            _lastPelvisPositionY = 0f;
        }
    }
}
