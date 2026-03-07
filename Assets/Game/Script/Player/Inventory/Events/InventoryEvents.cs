using System;

namespace Game.Player.Inventory.Events
{
    // Event classes for EventBus pattern
    public class ItemAddedEvent
    {
        public InventoryItem Item { get; }
        public int Quantity { get; }

        public ItemAddedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    public class ItemRemovedEvent
    {
        public InventoryItem Item { get; }
        public int Quantity { get; }

        public ItemRemovedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    public class ItemConsumedEvent
    {
        public InventoryItem Item { get; }

        public ItemConsumedEvent(InventoryItem item)
        {
            Item = item;
        }
    }

    public class InventoryChangedEvent
    {
        // Empty event, just signals a change occurred
    }

    public class InventoryFullEvent
    {
        public InventoryItem Item { get; }
        public int Quantity { get; }

        public InventoryFullEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    // ── Grid-specific events ──

    public class ItemPlacedEvent
    {
        public InventoryItem Item { get; }
        public UnityEngine.Vector2Int Position { get; }
        public UnityEngine.Vector2Int Size { get; }

        public ItemPlacedEvent(InventoryItem item, UnityEngine.Vector2Int position, UnityEngine.Vector2Int size)
        {
            Item = item;
            Position = position;
            Size = size;
        }
    }

    public class ItemMovedEvent
    {
        public InventoryItem Item { get; }
        public UnityEngine.Vector2Int OldPosition { get; }
        public UnityEngine.Vector2Int NewPosition { get; }

        public ItemMovedEvent(InventoryItem item, UnityEngine.Vector2Int oldPosition, UnityEngine.Vector2Int newPosition)
        {
            Item = item;
            OldPosition = oldPosition;
            NewPosition = newPosition;
        }
    }

    public class ItemRemovedFromGridEvent
    {
        public InventoryItem Item { get; }
        public UnityEngine.Vector2Int Position { get; }

        public ItemRemovedFromGridEvent(InventoryItem item, UnityEngine.Vector2Int position)
        {
            Item = item;
            Position = position;
        }
    }

    // Legacy instance-based events (keep for backward compatibility during migration)
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
