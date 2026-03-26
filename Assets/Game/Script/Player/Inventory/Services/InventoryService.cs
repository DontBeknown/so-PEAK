using Game.Core.Events;
using Game.Player.Inventory.Events;
using UnityEngine;

namespace Game.Player.Inventory.Services
{
    /// <summary>
    /// Business logic for inventory operations
    /// Coordinates between storage and events
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryStorage _storage;
        private readonly IEventBus _eventBus;

        public InventoryService(IInventoryStorage storage, IEventBus eventBus)
        {
            _storage = storage;
            _eventBus = eventBus;
        }

        #region Business Operations

        public bool AddItem(InventoryItem item, int quantity = 1, bool suppressNotification = false)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("[InventoryService] Invalid item or quantity");
                return false;
            }

            if (!CanFitItem(item, quantity))
            {
                Debug.LogWarning($"[InventoryService] Cannot fit {quantity}x {item.itemName}");
                _eventBus.Publish(new Game.Player.Inventory.Events.InventoryFullEvent(item, quantity));
                return false;
            }

            bool success = _storage.AddItem(item, quantity);
            
            if (success)
            {
                //Debug.Log($"[InventoryService] Added {quantity}x {item.itemName}");
                if (!suppressNotification)
                {
                    PublishItemAdded(item, quantity);
                }
                PublishInventoryChanged();
            }
            else
            {
                _eventBus.Publish(new Game.Player.Inventory.Events.InventoryFullEvent(item, quantity));
            }

            return success;
        }

        public bool RemoveItem(InventoryItem item, int quantity = 1, bool suppressNotification = false)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("[InventoryService] Invalid item or quantity");
                return false;
            }

            if (!_storage.HasItem(item, quantity))
            {
                Debug.LogWarning($"[InventoryService] Not enough {item.itemName}");
                return false;
            }

            bool success = _storage.RemoveItem(item, quantity);
            
            if (success)
            {
                //Debug.Log($"[InventoryService] Removed {quantity}x {item.itemName}");
                if (!suppressNotification)
                {
                    PublishItemRemoved(item, quantity);
                }
                PublishInventoryChanged();
            }

            return success;
        }

        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            return _storage.HasItem(item, quantity);
        }

        public bool TransferItem(InventoryItem item, int quantity, IInventoryStorage targetStorage)
        {
            if (targetStorage == null)
            {
                Debug.LogWarning("[InventoryService] Target storage is null");
                return false;
            }

            // Check if we have the item
            if (!_storage.HasItem(item, quantity))
            {
                Debug.LogWarning($"[InventoryService] Not enough {item.itemName} to transfer");
                return false;
            }

            // Check if target can fit the item
            if (!targetStorage.CanAddItem(item, quantity))
            {
                Debug.LogWarning($"[InventoryService] Target cannot fit {quantity}x {item.itemName}");
                return false;
            }

            // Remove from source
            if (!_storage.RemoveItem(item, quantity))
                return false;

            // Add to target
            if (!targetStorage.AddItem(item, quantity))
            {
                // Rollback: add back to source
                _storage.AddItem(item, quantity);
                return false;
            }

            //Debug.Log($"[InventoryService] Transferred {quantity}x {item.itemName}");
            PublishInventoryChanged();
            return true;
        }

        public void ClearInventory()
        {
            // Not implemented in the guide, but useful for future
            Debug.LogWarning("[InventoryService] ClearInventory not implemented");
        }

        public bool CanFitItem(InventoryItem item, int quantity)
        {
            return _storage.CanAddItem(item, quantity);
        }

        public int GetTotalWeight()
        {
            int totalWeight = 0;
            var slots = _storage.GetAllSlots();
            
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item != null)
                {
                    // Note: InventoryItem doesn't have a weight property in the current implementation
                    // This is a placeholder for when weight is added
                    // totalWeight += slot.item.weight * slot.quantity;
                }
            }
            
            return totalWeight;
        }

        #endregion

        #region Event Publishing

        private void PublishItemAdded(InventoryItem item, int quantity)
        {
            _eventBus.Publish(new Game.Player.Inventory.Events.ItemAddedEvent(item, quantity));
        }

        private void PublishItemRemoved(InventoryItem item, int quantity)
        {
            _eventBus.Publish(new Game.Player.Inventory.Events.ItemRemovedEvent(item, quantity));
        }

        private void PublishInventoryChanged()
        {
            _eventBus.Publish(new Game.Player.Inventory.Events.InventoryChangedEvent());
        }

        #endregion
    }
}
