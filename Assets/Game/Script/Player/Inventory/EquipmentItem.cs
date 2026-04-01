using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Equipment item that can be equipped to a specific slot.
/// Extends InventoryItem for seamless integration with existing inventory system.
/// Implements IEquippable to provide equipment-specific functionality.
/// Follows Open/Closed Principle - can be extended for specialized equipment.
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Equipment Item")]
public class EquipmentItem : InventoryItem, IEquippable
{
    [Header("Equipment Properties")]
    [SerializeField] private EquipmentSlotType equipmentSlot;
    
    [Header("Stat Modifiers")]
    [SerializeField] private StatModifier[] statModifiers = new StatModifier[0];

    [Header("Runtime Roll Metadata")]
    [SerializeField, HideInInspector] private bool isRuntimeGenerated;
    [SerializeField, HideInInspector] private string runtimeInstanceId;
    [SerializeField, HideInInspector] private string runtimeTemplateItemId;

    public EquipmentSlotType EquipmentSlot => equipmentSlot;
    
    public IReadOnlyList<IStatModifier> StatModifiers => statModifiers;

    public bool IsRuntimeGenerated => isRuntimeGenerated;
    public string RuntimeInstanceId => runtimeInstanceId;
    public string RuntimeTemplateItemId => runtimeTemplateItemId;

    public void ConfigureRuntimeRoll(string templateItemId, string instanceId, EquipmentSlotType slot, StatModifier[] modifiers)
    {
        equipmentSlot = slot;
        statModifiers = modifiers ?? new StatModifier[0];

        isRuntimeGenerated = true;
        runtimeTemplateItemId = templateItemId;
        runtimeInstanceId = instanceId;
    }

    /// <summary>
    /// Called when this equipment is equipped.
    /// Virtual to allow specialized equipment to override.
    /// </summary>
    public virtual void OnEquip()
    {
        //Debug.Log($"Equipped: {itemName} to {equipmentSlot} slot");
        
        // Log stat modifiers for debugging
        if (statModifiers.Length > 0)
        {
            //Debug.Log($"Applied {statModifiers.Length} stat modifier(s):");
            /*foreach (var modifier in statModifiers)
            {
                Debug.Log($"  - {modifier}");
            }*/
        }
    }

    /// <summary>
    /// Called when this equipment is unequipped.
    /// Virtual to allow specialized equipment to override.
    /// </summary>
    public virtual void OnUnequip()
    {
        //Debug.Log($"Unequipped: {itemName} from {equipmentSlot} slot");
    }

    /// <summary>
    /// Gets a summary of all stat modifiers for UI display.
    /// </summary>
    public string GetModifiersSummary()
    {
        if (statModifiers.Length == 0)
        {
            return "No stat bonuses";
        }

        return string.Join("\n", statModifiers.Select(m => m.ToString()));
    }

    /// <summary>
    /// Validates the equipment configuration in the Unity Editor.
    /// </summary>
    private void OnValidate()
    {
        // Ensure equipment items have the correct item type
        if (itemType != ItemType.Equipment)
        {
            itemType = ItemType.Equipment;
        }

        // Equipment items should not stack
        if (maxStackSize != 1)
        {
            maxStackSize = 1;
        }
    }
}
