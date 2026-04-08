using System;
using System.Collections.Generic;
using UnityEngine;

public static class RuntimeEquipmentFactory
{
    public static EquipmentItem CreateRolledInstance(EquipmentItem template, EquipmentSlotType slot, IReadOnlyList<StatModifier> modifiers)
    {
        if (template == null)
        {
            return null;
        }

        string runtimeId = Guid.NewGuid().ToString("N");
        var runtimeItem = UnityEngine.Object.Instantiate(template);
        runtimeItem.name = template.name + "_Runtime_" + runtimeId;

        var modifierArray = modifiers != null
            ? new List<StatModifier>(modifiers).ToArray()
            : new StatModifier[0];

        runtimeItem.ConfigureRuntimeRoll(template.name, runtimeId, slot, modifierArray);
        return runtimeItem;
    }

    public static EquipmentItem RestoreFromSave(GeneratedEquipmentSaveData generatedData)
    {
        if (generatedData == null || string.IsNullOrEmpty(generatedData.templateItemId))
        {
            return null;
        }

        EquipmentItem template = Resources.Load<EquipmentItem>($"Items/{generatedData.templateItemId}");
        if (template == null)
        {
            Debug.LogWarning($"[RuntimeEquipmentFactory] Missing template item: {generatedData.templateItemId}");
            return null;
        }

        if (!Enum.TryParse(generatedData.slotType, out EquipmentSlotType slot))
        {
            slot = template.EquipmentSlot;
        }

        List<StatModifier> modifiers = new List<StatModifier>();
        if (generatedData.modifiers != null)
        {
            for (int i = 0; i < generatedData.modifiers.Count; i++)
            {
                var modifierData = generatedData.modifiers[i];
                if (modifierData == null)
                {
                    continue;
                }

                if (!Enum.TryParse(modifierData.modifierType, out StatModifierType modifierType))
                {
                    continue;
                }

                modifiers.Add(new StatModifier(modifierType, modifierData.value, modifierData.isMultiplicative));
            }
        }

        string runtimeId = string.IsNullOrEmpty(generatedData.runtimeId)
            ? Guid.NewGuid().ToString("N")
            : generatedData.runtimeId;

        var runtimeItem = UnityEngine.Object.Instantiate(template);
        runtimeItem.name = template.name + "_Runtime_" + runtimeId;
        runtimeItem.ConfigureRuntimeRoll(template.name, runtimeId, slot, modifiers.ToArray());
        return runtimeItem;
    }

    public static GeneratedEquipmentSaveData CreateSavePayload(EquipmentItem equipment)
    {
        if (equipment == null || !equipment.IsRuntimeGenerated)
        {
            return null;
        }

        var payload = new GeneratedEquipmentSaveData
        {
            runtimeId = equipment.RuntimeInstanceId,
            templateItemId = string.IsNullOrEmpty(equipment.RuntimeTemplateItemId) ? equipment.name : equipment.RuntimeTemplateItemId,
            slotType = equipment.EquipmentSlot.ToString(),
            modifiers = new List<StatModifierSaveData>()
        };

        var modifiers = equipment.StatModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] is StatModifier statModifier)
            {
                payload.modifiers.Add(new StatModifierSaveData
                {
                    modifierType = statModifier.ModifierType.ToString(),
                    value = statModifier.Value,
                    isMultiplicative = statModifier.IsMultiplicative
                });
            }
        }

        return payload;
    }
}
