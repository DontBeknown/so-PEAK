using UnityEngine;

namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Command for dropping an item from inventory to the world.
    /// Removes the item from inventory and spawns its world prefab in front of the player.
    /// REFACTORED: Now uses IInventoryService (SOLID principles)
    /// </summary>
    public class DropItemCommand : IInventoryCommand
    {
        private readonly IInventoryService _inventoryService;
        private readonly InventoryItem _item;
        private readonly int _quantity;
        private readonly Vector3 _dropPosition;
        private readonly Vector3 _dropDirection;

        public bool CanUndo => false; // Undo would require picking up the spawned object
        public string Description => $"Drop {_item?.itemName ?? "Unknown Item"} x{_quantity}";

        public DropItemCommand(IInventoryService inventoryService, InventoryItem item, 
            Vector3 dropPosition, Vector3 dropDirection, int quantity = 1)
        {
            _inventoryService = inventoryService;
            _item = item;
            _quantity = quantity;
            _dropPosition = dropPosition;
            _dropDirection = dropDirection;
        }

        public bool Execute()
        {
            if (_inventoryService == null || _item == null)
            {
                Debug.LogWarning("DropItemCommand: Invalid state - cannot execute");
                return false;
            }

            bool removed = _inventoryService.RemoveItem(_item, _quantity);
            if (removed)
            {
                WorldItemSpawner.SpawnDroppedItem(_item, _quantity, _dropPosition, _dropDirection);
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
