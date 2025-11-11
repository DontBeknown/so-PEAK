using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    /// Service interface for player physics queries.
    /// Centralizes all physics checks for better testability and consistency.
    /// </summary>
    public interface IPhysicsService
    {
        /// <summary>
        /// Checks if the player is currently grounded
        /// </summary>
        /// <returns>True if player is on ground, false otherwise</returns>
        bool IsGrounded();

        /// <summary>
        /// Attempts to detect a climbable surface in front of the player
        /// </summary>
        /// <param name="hit">RaycastHit information if a surface is found</param>
        /// <returns>True if a climbable surface was detected</returns>
        bool TryDetectClimbable(out RaycastHit hit);

        /// <summary>
        /// Checks if the player can mantle onto a ledge
        /// </summary>
        /// <param name="contactPoint">Point where player touches the wall</param>
        /// <param name="wallNormal">Normal vector of the wall surface</param>
        /// <param name="topPoint">Output: The point on top of the ledge</param>
        /// <returns>True if mantling is possible</returns>
        bool CanMantle(Vector3 contactPoint, Vector3 wallNormal, out Vector3 topPoint);

        /// <summary>
        /// Performs a capsule overlap check to detect obstacles
        /// </summary>
        /// <param name="bottom">Bottom center of the capsule</param>
        /// <param name="top">Top center of the capsule</param>
        /// <param name="radius">Radius of the capsule</param>
        /// <returns>True if overlapping with an obstacle</returns>
        bool CheckCapsuleOverlap(Vector3 bottom, Vector3 top, float radius);
    }
}
