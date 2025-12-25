using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Calculates and applies stat modifiers from equipped items.
/// Implements IStatModifierCalculator for dependency inversion.
/// Separated from EquipmentManager (Single Responsibility Principle).
/// </summary>
public class StatModifierApplicator : IStatModifierCalculator
{
    private readonly EquipmentManager equipmentManager;
    private readonly StatModifierCollection modifierCollection;

    public StatModifierApplicator(EquipmentManager equipmentManager)
    {
        this.equipmentManager = equipmentManager;
        this.modifierCollection = new StatModifierCollection();

        // Subscribe to equipment changes to update modifiers
        equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
        
        // Initialize with current equipment
        RefreshModifiers();
    }

    /// <summary>
    /// Called when equipment changes (equip or unequip).
    /// </summary>
    private void OnEquipmentChanged(EquipmentSlotType slotType, IEquippable item)
    {
        RefreshModifiers();
    }

    /// <summary>
    /// Refreshes all modifiers from currently equipped items.
    /// </summary>
    private void RefreshModifiers()
    {
        modifierCollection.Clear();

        // Collect all modifiers from equipped items
        foreach (var equippedItem in equipmentManager.GetAllEquippedItems())
        {
            foreach (var modifier in equippedItem.StatModifiers)
            {
                modifierCollection.AddModifier(modifier);
            }
        }
    }

    /// <summary>
    /// Gets the modified value for a specific stat modifier type.
    /// </summary>
    public float GetModifiedValue(StatModifierType type, float baseValue)
    {
        return modifierCollection.GetModifiedValue(type, baseValue);
    }

    /// <summary>
    /// Checks if there are any modifiers of the specified type.
    /// </summary>
    public bool HasModifier(StatModifierType type)
    {
        return modifierCollection.HasModifier(type);
    }

    /// <summary>
    /// Gets all active modifiers (for debugging/UI).
    /// </summary>
    public IReadOnlyList<IStatModifier> GetAllActiveModifiers()
    {
        return modifierCollection.GetAllModifiers();
    }

    /// <summary>
    /// Gets the total count of active modifiers.
    /// </summary>
    public int ActiveModifierCount => modifierCollection.Count;

    /// <summary>
    /// Cleanup when no longer needed.
    /// </summary>
    public void Dispose()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }
}
