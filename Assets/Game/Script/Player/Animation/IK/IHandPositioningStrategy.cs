using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Strategy for positioning hands on walls during climbing.
    /// </summary>
    public interface IHandPositioningStrategy
    {
        /// <summary>
        /// Positions a hand target on the wall
        /// </summary>
        /// <param name="handTarget">The hand transform to position</param>
        /// <param name="transform">Character transform</param>
        /// <param name="horizontalOffset">Left/right offset from center</param>
        void PositionHand(Transform handTarget, Transform transform, float horizontalOffset);
    }
}
