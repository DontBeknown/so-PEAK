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
        private bool _isInitialized;

        public PelvisAdjuster(FootIKConfig config)
        {
            _config = config;
            _lastPelvisPositionY = 0f;
            _isInitialized = false;
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

            if (!_isInitialized)
            {
                _lastPelvisPositionY = animator.bodyPosition.y;
                _isInitialized = true;
            }

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
        /// Resets the pelvis adjuster state.
        /// Pass the current body Y so the lerp doesn't start from 0 and sink the character.
        /// </summary>
        public void Reset(float currentBodyY = 0f)
        {
            _lastPelvisPositionY = currentBodyY;
            _isInitialized = true;
        }
    }
}
