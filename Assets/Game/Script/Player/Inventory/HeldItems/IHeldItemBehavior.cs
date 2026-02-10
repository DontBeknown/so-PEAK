using UnityEngine;

namespace Game.Player.Inventory.HeldItems
{
    /// <summary>
    /// Interface for runtime behavior of held items (torch, canteen).
    /// Follows Interface Segregation Principle.
    /// </summary>
    public interface IHeldItemBehavior
    {
        /// <summary>
        /// Called when the held item is equipped by the player.
        /// </summary>
        void OnEquipped();

        /// <summary>
        /// Called when the held item is unequipped by the player.
        /// </summary>
        void OnUnequipped();

        /// <summary>
        /// Called every frame while the item is equipped.
        /// </summary>
        void UpdateBehavior();

        /// <summary>
        /// Gets the current state description (e.g., "5/5 charges", "87% durability").
        /// </summary>
        string GetStateDescription();

        /// <summary>
        /// Checks if the item is still usable (not depleted/destroyed).
        /// </summary>
        bool IsUsable();
    }
}
