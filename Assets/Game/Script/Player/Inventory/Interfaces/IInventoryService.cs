using System;

namespace Game.Player.Inventory
{
    /// <summary>
    /// Handles inventory business logic
    /// </summary>
    public interface IInventoryService
    {
        // Business operations
        bool AddItem(InventoryItem item, int quantity = 1, bool suppressNotification = false);
        bool RemoveItem(InventoryItem item, int quantity = 1, bool suppressNotification = false);
        bool HasItem(InventoryItem item, int quantity = 1);
        
        // Advanced operations
        bool TransferItem(InventoryItem item, int quantity, IInventoryStorage targetInventory);
        void ClearInventory();
        
        // Validation
        bool CanFitItem(InventoryItem item, int quantity);
        
        // Stats/Info
        int GetTotalWeight();
    }
}
