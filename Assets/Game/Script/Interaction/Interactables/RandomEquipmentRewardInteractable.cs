using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.DI;
using Game.Player.Inventory;

namespace Game.Interaction
{
    [Serializable]
    public class StatModifierRollEntry
    {
        public StatModifierType modifierType;
        public float minValue = 0.02f;
        public float maxValue = 0.1f;
        public bool isMultiplicative = true;
    }

    /// <summary>
    /// Hold interaction that rewards one random equipment item with rolled stats.
    /// Slot and modifier pools are configurable from the inspector.
    /// </summary>
    public class RandomEquipmentRewardInteractable : HoldInteractableBase
    {
        [Header("Prompt")]
        [SerializeField] private string customPrompt = "Assess Equipment";

        [Header("Reward Pool")]
        [SerializeField] private EquipmentItem[] equipmentTemplates;
        [SerializeField] private EquipmentSlotType[] allowedSlots =
        {
            EquipmentSlotType.Head,
            EquipmentSlotType.Body,
            EquipmentSlotType.Foot,
            EquipmentSlotType.Hand
        };
        [SerializeField] private StatModifierRollEntry[] modifierPool;

        [Header("Roll Settings")]
        [SerializeField, Min(1)] private int minModifierCount = 1;
        [SerializeField, Min(1)] private int maxModifierCount = 2;
        [SerializeField] private bool allowDuplicateModifierTypes = false;

        [Header("Usage")]
        [SerializeField] private bool infiniteUses = false;
        [SerializeField, Min(1)] private int maxUses = 1;
        [SerializeField] private bool destroyWhenExhausted = false;

        private int _remainingUses;

        public override string InteractionPrompt => customPrompt;

        public override bool CanInteract =>
            !isCurrentlyHolding &&
            HasValidConfig() &&
            (infiniteUses || _remainingUses > 0);

        private void Awake()
        {
            _remainingUses = Mathf.Max(1, maxUses);
        }

        protected override void OnHoldComplete()
        {
            var inventoryService = ServiceContainer.Instance.TryGet<IInventoryService>();
            if (inventoryService == null)
            {
                Debug.LogWarning("[RandomEquipmentRewardInteractable] IInventoryService not found.");
                return;
            }

            var reward = RollReward();
            if (reward == null)
            {
                Debug.LogWarning("[RandomEquipmentRewardInteractable] Failed to roll equipment reward.");
                return;
            }

            bool added = inventoryService.AddItem(reward, 1);
            if (!added)
            {
                Destroy(reward);
                return;
            }

            if (!infiniteUses)
            {
                _remainingUses--;
                if (_remainingUses <= 0)
                {
                    if (destroyWhenExhausted)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                }
            }
        }

        private EquipmentItem RollReward()
        {
            if (!HasValidConfig())
            {
                return null;
            }

            EquipmentItem template = equipmentTemplates[UnityEngine.Random.Range(0, equipmentTemplates.Length)];
            EquipmentSlotType slot = allowedSlots[UnityEngine.Random.Range(0, allowedSlots.Length)];

            int countUpper = Mathf.Max(minModifierCount, maxModifierCount);
            int count = UnityEngine.Random.Range(minModifierCount, countUpper + 1);
            int maxCount = allowDuplicateModifierTypes ? count : Mathf.Min(count, modifierPool.Length);

            List<StatModifier> rolledModifiers = new List<StatModifier>();
            List<int> usedIndices = new List<int>();

            for (int i = 0; i < maxCount; i++)
            {
                int selectedIndex = SelectModifierIndex(usedIndices);
                if (selectedIndex < 0)
                {
                    break;
                }

                var entry = modifierPool[selectedIndex];
                float minValue = Mathf.Min(entry.minValue, entry.maxValue);
                float maxValue = Mathf.Max(entry.minValue, entry.maxValue);
                float rolledValue = UnityEngine.Random.Range(minValue, maxValue);

                rolledModifiers.Add(new StatModifier(entry.modifierType, rolledValue, entry.isMultiplicative));
                if (!allowDuplicateModifierTypes)
                {
                    usedIndices.Add(selectedIndex);
                }
            }

            return RuntimeEquipmentFactory.CreateRolledInstance(template, slot, rolledModifiers);
        }

        private int SelectModifierIndex(List<int> usedIndices)
        {
            if (modifierPool == null || modifierPool.Length == 0)
            {
                return -1;
            }

            if (allowDuplicateModifierTypes)
            {
                return UnityEngine.Random.Range(0, modifierPool.Length);
            }

            List<int> available = new List<int>();
            for (int i = 0; i < modifierPool.Length; i++)
            {
                if (!usedIndices.Contains(i))
                {
                    available.Add(i);
                }
            }

            if (available.Count == 0)
            {
                return -1;
            }

            int picked = UnityEngine.Random.Range(0, available.Count);
            return available[picked];
        }

        private bool HasValidConfig()
        {
            return equipmentTemplates != null && equipmentTemplates.Length > 0 &&
                   allowedSlots != null && allowedSlots.Length > 0 &&
                   modifierPool != null && modifierPool.Length > 0;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(customPrompt))
            {
                customPrompt = "Assess Equipment";
            }

            minModifierCount = Mathf.Max(1, minModifierCount);
            maxModifierCount = Mathf.Max(minModifierCount, maxModifierCount);
            maxUses = Mathf.Max(1, maxUses);

            if (allowedSlots == null || allowedSlots.Length == 0)
            {
                allowedSlots = new[]
                {
                    EquipmentSlotType.Head,
                    EquipmentSlotType.Body,
                    EquipmentSlotType.Foot,
                    EquipmentSlotType.Hand
                };
            }
        }
    }
}
