using System;

/// <summary>
/// Manages a single equipment slot (Head, Body, Foot, or Hand).
/// Follows Single Responsibility Principle - only manages one slot.
/// Provides events for equipment changes.
/// </summary>
public class EquipmentSlot
{
    private readonly EquipmentSlotType slotType;
    private IEquippable equippedItem;

    /// <summary>
    /// Event fired when an item is equipped to this slot.
    /// </summary>
    public event Action<IEquippable> OnItemEquipped;
    
    /// <summary>
    /// Event fired when an item is unequipped from this slot.
    /// </summary>
    public event Action<IEquippable> OnItemUnequipped;

    public EquipmentSlotType SlotType => slotType;
    public IEquippable EquippedItem => equippedItem;
    public bool IsEmpty => equippedItem == null;

    public EquipmentSlot(EquipmentSlotType slotType)
    {
        this.slotType = slotType;
    }

    /// <summary>
    /// Equips an item to this slot.
    /// Returns the previously equipped item if any.
    /// </summary>
    /// <param name="item">The item to equip</param>
    /// <returns>The previously equipped item, or null if slot was empty</returns>
    public IEquippable Equip(IEquippable item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item), "Cannot equip null item");
        }

        if (item.EquipmentSlot != slotType)
        {
            throw new ArgumentException(
                $"Cannot equip {item.GetType().Name} to {slotType} slot. Item is for {item.EquipmentSlot} slot.",
                nameof(item)
            );
        }

        // Unequip current item if any
        IEquippable previousItem = equippedItem;
        if (previousItem != null)
        {
            previousItem.OnUnequip();
            OnItemUnequipped?.Invoke(previousItem);
        }

        // Equip new item
        equippedItem = item;
        item.OnEquip();
        OnItemEquipped?.Invoke(item);

        return previousItem;
    }

    /// <summary>
    /// Unequips the current item from this slot.
    /// </summary>
    /// <returns>The unequipped item, or null if slot was empty</returns>
    public IEquippable Unequip()
    {
        if (equippedItem == null)
        {
            return null;
        }

        IEquippable unequippedItem = equippedItem;
        equippedItem = null;

        unequippedItem.OnUnequip();
        OnItemUnequipped?.Invoke(unequippedItem);

        return unequippedItem;
    }

    /// <summary>
    /// Clears the slot without triggering unequip events.
    /// Use with caution - primarily for cleanup scenarios.
    /// </summary>
    public void Clear()
    {
        equippedItem = null;
    }
}
