using System.Collections.Generic;

namespace Game.Player.Inventory
{
    /// <summary>
    /// Handles inventory data storage only
    /// </summary>
    public interface IInventoryStorage
    {
        // Query operations
        IReadOnlyList<InventorySlot> GetAllSlots();
        InventorySlot GetSlot(int index);
        int GetSlotCount();
        
        // Item operations
        bool AddItem(InventoryItem item, int quantity);
        bool RemoveItem(InventoryItem item, int quantity);
        bool HasItem(InventoryItem item, int quantity = 1);
        int GetItemQuantity(InventoryItem item);
        
        // Capacity management
        bool CanAddItem(InventoryItem item, int quantity);
        bool ExpandInventory(int additionalSlots);
        int GetEmptySlotCount();
        
        // Utility
        int FindStackableSlot(InventoryItem item, int quantity);
    }
}
