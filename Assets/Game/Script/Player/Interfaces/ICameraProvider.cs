using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    /// Provides access to the camera system.
    /// Abstracts Camera.main to allow for testing and multiple camera scenarios.
    /// </summary>
    public interface ICameraProvider
    {
        /// <summary>
        /// Gets the main gameplay camera transform
        /// </summary>
        Transform CameraTransform { get; }

        /// <summary>
        /// Converts screen-relative input to world-relative direction
        /// </summary>
        /// <param name="input">Normalized input vector (e.g., from joystick)</param>
        /// <returns>World-space direction vector</returns>
        Vector3 GetWorldDirection(Vector2 input);

        /// <summary>
        /// Gets the camera's forward direction projected onto the horizontal plane
        /// </summary>
        Vector3 ForwardDirection { get; }

        /// <summary>
        /// Gets the camera's right direction projected onto the horizontal plane
        /// </summary>
        Vector3 RightDirection { get; }
    }
}
