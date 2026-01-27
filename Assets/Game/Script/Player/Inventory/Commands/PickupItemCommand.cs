using UnityEngine;

namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Command for picking up an item from the world.
    /// Supports undo by dropping the item back.
    /// Note: Undo creates a simple GameObject, not a full ResourceCollector.
    /// </summary>
    public class PickupItemCommand : IInventoryCommand
    {
        private readonly InventoryManager _inventoryManager;
        private readonly ResourceCollector _resourceCollector;
        private InventoryItem _pickedItem;
        private int _pickedQuantity;

        public bool CanUndo => false; // Undo not supported - would need to recreate ResourceCollector
        public string Description => $"Pickup {_resourceCollector?.name ?? "Unknown Item"}";

        public PickupItemCommand(InventoryManager inventoryManager, ResourceCollector resourceCollector)
        {
            _inventoryManager = inventoryManager;
            _resourceCollector = resourceCollector;
        }

        public bool Execute()
        {
            if (_inventoryManager == null || _resourceCollector == null)
            {
                Debug.LogWarning("PickupItemCommand: Invalid state - cannot execute");
                return false;
            }

            // Use the ResourceCollector's built-in collect method
            bool success = _resourceCollector.CollectResource(_inventoryManager);
            
            if (success)
            {
                //Debug.Log($"Picked up: {_resourceCollector.name}");
                return true;
            }

            Debug.LogWarning($"Failed to pickup {_resourceCollector.name}");
            return false;
        }

        public bool Undo()
        {
            // Undo not supported for pickup - would need to recreate entire ResourceCollector prefab
            Debug.LogWarning("PickupItemCommand: Undo not supported for item pickup");
            return false;
        }
    }
}
