using System.Collections.Generic;

namespace Game.Inventory
{
    /// <summary>
    /// Interface for inventory data storage
    /// Follows Dependency Inversion Principle
    /// Allows swapping storage implementations (memory, file, database)
    /// </summary>
    public interface IInventoryStorage
    {
        /// <summary>
        /// Gets all inventory slots
        /// </summary>
        IReadOnlyList<InventorySlot> GetSlots();
        
        /// <summary>
        /// Gets a specific slot by index
        /// </summary>
        InventorySlot GetSlot(int index);
        
        /// <summary>
        /// Finds the first empty slot
        /// </summary>
        int FindEmptySlot();
        
        /// <summary>
        /// Finds slots containing a specific item
        /// </summary>
        List<int> FindSlotsWithItem(InventoryItem item);
        
        /// <summary>
        /// Adds items to a specific slot
        /// </summary>
        bool AddToSlot(int slotIndex, InventoryItem item, int quantity);
        
        /// <summary>
        /// Removes items from a specific slot
        /// </summary>
        bool RemoveFromSlot(int slotIndex, int quantity);
        
        /// <summary>
        /// Clears a specific slot
        /// </summary>
        void ClearSlot(int slotIndex);
        
        /// <summary>
        /// Gets total number of slots
        /// </summary>
        int SlotCount { get; }
        
        /// <summary>
        /// Checks if storage is full
        /// </summary>
        bool IsFull();
    }
}
