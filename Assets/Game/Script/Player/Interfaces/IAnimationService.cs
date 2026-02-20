using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    /// Service interface for player animation control.
    /// Abstracts animator implementation details from game logic.
    /// </summary>
    public interface IAnimationService
    {
        /// <summary>
        /// Updates movement animation parameters
        /// </summary>
        /// <param name="velocity">Current velocity vector</param>
        /// <param name="maxSpeed">Maximum speed for normalization</param>
        void UpdateMovement(Vector3 velocity, float maxSpeed);

        /// <summary>
        /// Sets the climbing animation state
        /// </summary>
        /// <param name="isClimbing">True if player is climbing</param>
        void SetClimbing(bool isClimbing);

        /// <summary>
        /// Sets the walking animation state
        /// </summary>
        /// <param name="isWalking">True if player is walking</param>
        void SetWalking(bool isWalking);

        /// <summary>
        /// Sets the running/sprinting animation state
        /// </summary>
        /// <param name="isRunning">True if player is running</param>
        void SetRunning(bool isRunning);

        /// <summary>
        /// Sets the speed multiplier for walk/run animation blending.
        /// 0 = walk speed, 1 = full run speed.
        /// </summary>
        /// <param name="multiplier">Blend value between 0 and 1</param>
        void SetSpeedMultiplier(float multiplier);

        /// <summary>
        /// Sets the falling animation state
        /// </summary>
        /// <param name="isFalling">True if player is falling</param>
        void SetFalling(bool isFalling);

        /// <summary>
        /// Sets the grounded animation state
        /// </summary>
        /// <param name="isGrounded">True if player is grounded</param>
        void SetGrounded(bool isGrounded);

        /// <summary>
        /// Enables or disables foot IK
        /// </summary>
        /// <param name="enabled">True to enable foot IK</param>
        void SetFootIKEnabled(bool enabled);

        /// <summary>
        /// Triggers a jump animation
        /// </summary>
        void TriggerJump();
    }
}
