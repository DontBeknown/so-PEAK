using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player.Inventory;

/// <summary>
/// Manages all equipment slots for the player.
/// Follows Single Responsibility Principle - only manages equipment slots.
/// Depends on IEquippable abstraction (Dependency Inversion Principle).
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    private Dictionary<EquipmentSlotType, EquipmentSlot> equipmentSlots;
    private IEventBus eventBus;
    private IInventoryService _inventoryService;

    /// <summary>
    /// Event fired when any equipment changes (equip or unequip).
    /// </summary>
    public event Action<EquipmentSlotType, IEquippable> OnEquipmentChanged;

    private void Awake()
    {
        InitializeSlots();
    }

    private void Start()
    {
        // Get EventBus from ServiceContainer
        // Done in Start() to ensure EventBus has been registered by GameServiceBootstrapper
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        
        // Get InventoryService from ServiceContainer for inventory sync
        _inventoryService = ServiceContainer.Instance.TryGet<IInventoryService>();
        if (_inventoryService == null)
        {
            Debug.LogWarning("[EquipmentManager] IInventoryService not found in ServiceContainer. Equipment will not be synced with inventory.");
        }
    }

    private void InitializeSlots()
    {
        equipmentSlots = new Dictionary<EquipmentSlotType, EquipmentSlot>();

        // Create a slot for each equipment type
        foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
        {
            var slot = new EquipmentSlot(slotType);
            
            // Subscribe to slot events
            slot.OnItemEquipped += item => {
                OnEquipmentChanged?.Invoke(slotType, item);
                eventBus?.Publish(new ItemEquippedEvent(item));
            };
            slot.OnItemUnequipped += item => {
                OnEquipmentChanged?.Invoke(slotType, null);
                eventBus?.Publish(new ItemUnequippedEvent(item));
            };
            
            equipmentSlots[slotType] = slot;
        }

        //Debug.Log($"EquipmentManager initialized with {equipmentSlots.Count} slots");
    }

    /// <summary>
    /// Equips an item to the appropriate slot.
    /// Optionally removes the item from inventory before equipping.
    /// If an item was previously equipped, attempts to return it to inventory or drops it if full.
    /// Returns the previously equipped item if any.
    /// </summary>
    public IEquippable Equip(IEquippable item, bool syncInventory = true)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        // Try to remove from inventory if it's an InventoryItem
        InventoryItem inventoryItem = item as InventoryItem;
        if (syncInventory && inventoryItem != null && _inventoryService != null)
        {
            bool removedFromInventory = _inventoryService.RemoveItem(inventoryItem, 1, suppressNotification: true);
            if (!removedFromInventory)
            {
                Debug.LogWarning($"[EquipmentManager] Could not remove {inventoryItem.itemName} from inventory before equipping. Item may come from another source.");
                // Continue anyway - item might come from other source
            }
        }

        // Get the slot and equip
        var slot = GetSlot(item.EquipmentSlot);
        IEquippable previousItem = slot.Equip(item);

        // Try to add previously equipped item back to inventory
        if (previousItem != null)
        {
            InventoryItem previousInventoryItem = previousItem as InventoryItem;
            if (previousInventoryItem != null && _inventoryService != null)
            {
                bool addedBack = _inventoryService.AddItem(previousInventoryItem, 1, suppressNotification: true);
                if (!addedBack)
                {
                    // Try to drop in world instead
                    WorldItemSpawner.SpawnDroppedItem(previousInventoryItem, 1);
                    Debug.LogWarning($"[EquipmentManager] {previousInventoryItem.itemName} could not be added back to inventory; dropped in world.");
                }
            }
        }

        return previousItem;
    }

    /// <summary>
    /// Unequips an item from the specified slot.
    /// Attempts to add the item back to inventory.
    /// If inventory is full, drops the item in the world.
    /// </summary>
    public IEquippable Unequip(EquipmentSlotType slotType)
    {
        var slot = GetSlot(slotType);
        IEquippable unequippedItem = slot.Unequip();

        // Try to add back to inventory
        if (unequippedItem != null)
        {
            InventoryItem inventoryItem = unequippedItem as InventoryItem;
            if (inventoryItem != null && _inventoryService != null)
            {
                bool addedToInventory = _inventoryService.AddItem(inventoryItem, 1, suppressNotification: true);
                if (!addedToInventory)
                {
                    // Drop in world if inventory is full
                    WorldItemSpawner.SpawnDroppedItem(inventoryItem, 1);
                    Debug.LogWarning($"[EquipmentManager] {inventoryItem.itemName} could not fit in inventory; dropped in world.");
                }
            }
        }

        return unequippedItem;
    }

    /// <summary>
    /// Gets the currently equipped item in a specific slot.
    /// </summary>
    public IEquippable GetEquippedItem(EquipmentSlotType slotType)
    {
        var slot = GetSlot(slotType);
        return slot.EquippedItem;
    }

    /// <summary>
    /// Checks if a slot is empty.
    /// </summary>
    public bool IsSlotEmpty(EquipmentSlotType slotType)
    {
        var slot = GetSlot(slotType);
        return slot.IsEmpty;
    }

    /// <summary>
    /// Gets all currently equipped items.
    /// </summary>
    public IEnumerable<IEquippable> GetAllEquippedItems()
    {
        return equipmentSlots.Values
            .Select(slot => slot.EquippedItem)
            .Where(item => item != null);
    }

    /// <summary>
    /// Unequips all items.
    /// Attempts to return each item to inventory or drops if inventory is full.
    /// </summary>
    public void UnequipAll()
    {
        foreach (var slot in equipmentSlots.Values)
        {
            // Use the public Unequip method which handles inventory sync
            Unequip(slot.SlotType);
        }
    }

    /// <summary>
    /// Gets the equipment slot for a specific slot type.
    /// </summary>
    private EquipmentSlot GetSlot(EquipmentSlotType slotType)
    {
        if (!equipmentSlots.TryGetValue(slotType, out var slot))
        {
            throw new InvalidOperationException($"Equipment slot {slotType} not found");
        }
        return slot;
    }

    /// <summary>
    /// Gets a summary of all equipped items for debugging.
    /// </summary>
    public string GetEquipmentSummary()
    {
        var equipped = GetAllEquippedItems().ToList();
        if (equipped.Count == 0)
        {
            return "No equipment equipped";
        }

        return $"Equipped items ({equipped.Count}):\n" + 
               string.Join("\n", equipped.Select(item => $"  - {item.GetType().Name}"));
    }
}
