using System.Collections.Generic;

/// <summary>
/// Interface for items that can be equipped.
/// Follows Interface Segregation Principle - only equipment-related methods.
/// </summary>
public interface IEquippable
{
    /// <summary>
    /// The equipment slot this item occupies.
    /// </summary>
    EquipmentSlotType EquipmentSlot { get; }
    
    /// <summary>
    /// The stat modifiers this equipment provides.
    /// </summary>
    IReadOnlyList<IStatModifier> StatModifiers { get; }
    
    /// <summary>
    /// Called when this item is equipped.
    /// </summary>
    void OnEquip();
    
    /// <summary>
    /// Called when this item is unequipped.
    /// </summary>
    void OnUnequip();
}
