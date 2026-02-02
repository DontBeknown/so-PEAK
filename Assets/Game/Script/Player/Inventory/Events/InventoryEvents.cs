using System;

namespace Game.Inventory
{
    /// <summary>
    /// Instance-based events for inventory changes
    /// Replaces static events to avoid global coupling
    /// Follows Dependency Inversion Principle
    /// </summary>
    public class InventoryEvents
    {
        public event Action<InventoryItem, int> OnItemAdded;
        public event Action<InventoryItem, int> OnItemRemoved;
        public event Action<InventoryItem, int> OnItemConsumed;
        public event Action OnInventoryChanged;
        
        public void RaiseItemAdded(InventoryItem item, int quantity)
        {
            OnItemAdded?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
        }
        
        public void RaiseItemRemoved(InventoryItem item, int quantity)
        {
            OnItemRemoved?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
        }
        
        public void RaiseItemConsumed(InventoryItem item, int quantity)
        {
            OnItemConsumed?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
        }
        
        public void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
