using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    /// Service interface for handling player input.
    /// Abstracts input system implementation details from game logic.
    /// </summary>
    public interface IInputService
    {
        /// <summary>
        /// Gets the current movement input (WASD/Left Stick)
        /// </summary>
        Vector2 MovementInput { get; }

        /// <summary>
        /// Gets whether the jump button was pressed this frame
        /// </summary>
        bool JumpPressed { get; }

        /// <summary>
        /// Gets whether the climb button was pressed this frame
        /// </summary>
        bool ClimbPressed { get; }

        /// <summary>
        /// Gets whether the sprint button is held
        /// </summary>
        bool SprintHeld { get; }

        /// <summary>
        /// Gets whether the pickup button was pressed this frame
        /// </summary>
        bool PickupPressed { get; }

        /// <summary>
        /// Gets whether the inventory button was pressed this frame
        /// </summary>
        bool InventoryPressed { get; }

        /// <summary>
        /// Enables input processing
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables input processing
        /// </summary>
        void Disable();
    }
}
