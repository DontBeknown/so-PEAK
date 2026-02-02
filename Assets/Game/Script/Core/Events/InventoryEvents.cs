namespace Game.Core.Events
{
    /// <summary>
    /// Event raised when an item is added to inventory
    /// </summary>
    public class ItemAddedEvent
    {
        public InventoryItem Item { get; set; }
        public int Quantity { get; set; }
        
        public ItemAddedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
    
    /// <summary>
    /// Event raised when an item is removed from inventory
    /// </summary>
    public class ItemRemovedEvent
    {
        public InventoryItem Item { get; set; }
        public int Quantity { get; set; }
        
        public ItemRemovedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
    
    /// <summary>
    /// Event raised when an item is consumed
    /// </summary>
    public class ItemConsumedEvent
    {
        public InventoryItem Item { get; set; }
        public int Quantity { get; set; }
        
        public ItemConsumedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
    
    /// <summary>
    /// Event raised when inventory changes
    /// </summary>
    public class InventoryChangedEvent
    {
        // Empty event - just signals a change
    }
}
