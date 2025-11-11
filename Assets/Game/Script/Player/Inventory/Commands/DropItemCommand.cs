using UnityEngine;

namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Command for dropping an item from inventory to the world.
    /// Note: Creating world objects from inventory items requires item prefab references.
    /// This is a simplified implementation that just removes from inventory.
    /// </summary>
    public class DropItemCommand : IInventoryCommand
    {
        private readonly InventoryManager _inventoryManager;
        private readonly InventoryItem _item;
        private readonly int _quantity;
        private bool _wasDropped;

        public bool CanUndo => false; // Undo would require picking up the spawned object
        public string Description => $"Drop {_item?.itemName ?? "Unknown Item"} x{_quantity}";

        public DropItemCommand(InventoryManager inventoryManager, InventoryItem item, 
            Vector3 dropPosition, Vector3 dropDirection, int quantity = 1)
        {
            _inventoryManager = inventoryManager;
            _item = item;
            _quantity = quantity;
            // Note: dropPosition and dropDirection stored but not used in simplified version
        }

        public bool Execute()
        {
            if (_inventoryManager == null || _item == null)
            {
                Debug.LogWarning("DropItemCommand: Invalid state - cannot execute");
                return false;
            }

            // Remove from inventory
            bool removed = _inventoryManager.RemoveItem(_item, _quantity);
            
            if (removed)
            {
                _wasDropped = true;
                Debug.Log($"Dropped {_item.itemName} x{_quantity}");
                
                // TODO: Spawn world object if item has prefab reference
                // This would require adding a prefab field to InventoryItem
                
                return true;
            }

            return false;
        }

        public bool Undo()
        {
            Debug.LogWarning("DropItemCommand: Undo not supported for item drop");
            return false;
        }
    }
}
