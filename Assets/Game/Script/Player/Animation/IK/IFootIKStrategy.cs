using UnityEngine;

namespace Game.Player.Animation
{
    /// <summary>
    /// Interface for foot IK handling strategies.
    /// Implements Strategy Pattern for different IK modes (ground, climbing, etc.)
    /// </summary>
    public interface IFootIKStrategy
    {
        /// <summary>
        /// Processes IK for a specific foot
        /// </summary>
        /// <param name="foot">Which foot to process</param>
        /// <param name="animator">The animator component</param>
        /// <param name="transform">Character transform</param>
        /// <returns>Vertical offset for pelvis adjustment</returns>
        float ProcessFootIK(AvatarIKGoal foot, Animator animator, Transform transform);

        /// <summary>
        /// Called when this strategy becomes active
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called when this strategy stops being active
        /// </summary>
        void OnExit();
    }
}
