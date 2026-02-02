using System.Collections.Generic;
using UnityEngine;
using Game.Inventory.Effects;

namespace Game.Inventory
{
    /// <summary>
    /// Service layer for inventory business logic
    /// Single Responsibility: Business rules only
    /// Depends on abstractions (IInventoryStorage)
    /// </summary>
    public class InventoryService
    {
        private readonly IInventoryStorage _storage;
        private readonly InventoryEvents _events;
        private readonly PlayerStats _playerStats;
        
        public InventoryService(IInventoryStorage storage, InventoryEvents events, PlayerStats playerStats)
        {
            _storage = storage;
            _events = events;
            _playerStats = playerStats;
        }
        
        /// <summary>
        /// Adds an item to the inventory
        /// </summary>
        public bool AddItem(InventoryItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;
            
            // Try to stack with existing items first
            if (item.maxStackSize > 1)
            {
                var slotsWithItem = _storage.FindSlotsWithItem(item);
                foreach (var slotIndex in slotsWithItem)
                {
                    var slot = _storage.GetSlot(slotIndex);
                    if (slot.CanAddItem(item, quantity))
                    {
                        _storage.AddToSlot(slotIndex, item, quantity);
                        _events.RaiseItemAdded(item, quantity);
                        return true;
                    }
                }
            }
            
            // Find empty slot
            int emptySlot = _storage.FindEmptySlot();
            if (emptySlot >= 0)
            {
                _storage.AddToSlot(emptySlot, item, quantity);
                _events.RaiseItemAdded(item, quantity);
                return true;
            }
            
            return false; // Inventory full
        }
        
        /// <summary>
        /// Removes an item from the inventory
        /// </summary>
        public bool RemoveItem(InventoryItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;
            
            int remainingToRemove = quantity;
            var slotsWithItem = _storage.FindSlotsWithItem(item);
            
            foreach (var slotIndex in slotsWithItem)
            {
                if (remainingToRemove <= 0)
                    break;
                
                var slot = _storage.GetSlot(slotIndex);
                int canRemove = Mathf.Min(slot.quantity, remainingToRemove);
                
                _storage.RemoveFromSlot(slotIndex, canRemove);
                remainingToRemove -= canRemove;
                _events.RaiseItemRemoved(item, canRemove);
            }
            
            return remainingToRemove == 0;
        }
        
        /// <summary>
        /// Consumes an item and applies its effects
        /// </summary>
        public bool ConsumeItem(InventoryItem item)
        {
            if (item == null || !item.isConsumable)
                return false;
            
            if (!HasItem(item, 1))
                return false;
            
            // Create effects from item data
            var effects = ConsumableEffectFactory.CreateEffects(new System.Collections.Generic.List<ConsumableEffect>(item.consumableEffects ?? new ConsumableEffect[0]));
            
            // Apply all effects to injected PlayerStats
            foreach (var effect in effects)
            {
                if (effect.CanApply(_playerStats))
                {
                    effect.Apply(_playerStats);
                }
            }
            
            // Remove the consumed item
            RemoveItem(item, 1);
            _events.RaiseItemConsumed(item, 1);
            
            return true;
        }
        
        /// <summary>
        /// Checks if inventory has a specific item
        /// </summary>
        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            var slotsWithItem = _storage.FindSlotsWithItem(item);
            int totalQuantity = 0;
            
            foreach (var slotIndex in slotsWithItem)
            {
                var slot = _storage.GetSlot(slotIndex);
                totalQuantity += slot.quantity;
            }
            
            return totalQuantity >= quantity;
        }
        
        /// <summary>
        /// Gets the total quantity of an item in inventory
        /// </summary>
        public int GetItemQuantity(InventoryItem item)
        {
            var slotsWithItem = _storage.FindSlotsWithItem(item);
            int totalQuantity = 0;
            
            foreach (var slotIndex in slotsWithItem)
            {
                var slot = _storage.GetSlot(slotIndex);
                totalQuantity += slot.quantity;
            }
            
            return totalQuantity;
        }
        
        /// <summary>
        /// Gets all inventory slots (read-only)
        /// </summary>
        public IReadOnlyList<InventorySlot> GetSlots()
        {
            return _storage.GetSlots();
        }
    }
}
