using UnityEngine;

namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Command for using/consuming an item.
    /// Uses the existing InventoryManager.ConsumeItem method.
    /// </summary>
    public class UseItemCommand : IInventoryCommand
    {
        private readonly InventoryManager _inventoryManager;
        private readonly InventoryItem _item;

        public bool CanUndo => false; // Cannot undo consumption (stat changes are permanent)
        public string Description => $"Use {_item?.itemName ?? "Unknown Item"}";

        public UseItemCommand(InventoryManager inventoryManager, InventoryItem item, PlayerStats playerStats)
        {
            _inventoryManager = inventoryManager;
            _item = item;
            // playerStats is passed but not stored - InventoryManager handles stat changes
        }

        public bool Execute()
        {
            if (_inventoryManager == null || _item == null)
            {
                Debug.LogWarning("UseItemCommand: Invalid state - cannot execute");
                return false;
            }

            // Use the existing ConsumeItem method which handles everything
            bool consumed = _inventoryManager.ConsumeItem(_item);
            
            if (consumed)
            {
                Debug.Log($"Used {_item.itemName}");
                return true;
            }

            Debug.LogWarning($"Failed to use {_item.itemName}");
            return false;
        }

        public bool Undo()
        {
            // Cannot undo consumption - stat changes are permanent
            Debug.LogWarning("UseItemCommand: Cannot undo item consumption");
            return false;
        }
    }
}
