using UnityEngine;
using Game.Core.DI;

namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Command for using/consuming an item.
    /// Uses InventoryManagerRefactored.ConsumeItem method (facade).
    /// REFACTORED: Now uses SOLID architecture
    /// </summary>
    public class UseItemCommand : IInventoryCommand
    {
        private readonly IInventoryService _inventoryService;
        private readonly InventoryItem _item;

        public bool CanUndo => false; // Cannot undo consumption (stat changes are permanent)
        public string Description => $"Use {_item?.itemName ?? "Unknown Item"}";

        public UseItemCommand(IInventoryService inventoryService, InventoryItem item, PlayerStats playerStats)
        {
            _inventoryService = inventoryService;
            _item = item;
            // playerStats is passed but not stored - service handles stat changes
        }

        public bool Execute()
        {
            if (_inventoryService == null || _item == null)
            {
                Debug.LogWarning("UseItemCommand: Invalid state - cannot execute");
                return false;
            }

            // Get InventoryManagerRefactored which has ConsumeItem method (facade)
            var inventoryManager = ServiceContainer.Instance.Get<InventoryManagerRefactored>();
            if (inventoryManager == null)
            {
                Debug.LogError("UseItemCommand: InventoryManagerRefactored not registered!");
                return false;
            }

            bool consumed = inventoryManager.ConsumeItem(_item);
            
            if (consumed)
            {
                //Debug.Log($"Used {_item.itemName}");
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
