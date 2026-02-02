using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.DI;
using Game.Core.Events;

/// <summary>
/// Manages all equipment slots for the player.
/// Follows Single Responsibility Principle - only manages equipment slots.
/// Depends on IEquippable abstraction (Dependency Inversion Principle).
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    private Dictionary<EquipmentSlotType, EquipmentSlot> equipmentSlots;
    private IEventBus eventBus;

    /// <summary>
    /// Event fired when any equipment changes (equip or unequip).
    /// </summary>
    public event Action<EquipmentSlotType, IEquippable> OnEquipmentChanged;

    private void Awake()
    {
        // Get EventBus from ServiceContainer
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        
        InitializeSlots();
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
    /// Returns the previously equipped item if any.
    /// </summary>
    public IEquippable Equip(IEquippable item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var slot = GetSlot(item.EquipmentSlot);
        return slot.Equip(item);
    }

    /// <summary>
    /// Unequips an item from the specified slot.
    /// </summary>
    public IEquippable Unequip(EquipmentSlotType slotType)
    {
        var slot = GetSlot(slotType);
        return slot.Unequip();
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
    /// </summary>
    public void UnequipAll()
    {
        foreach (var slot in equipmentSlots.Values)
        {
            slot.Unequip();
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
