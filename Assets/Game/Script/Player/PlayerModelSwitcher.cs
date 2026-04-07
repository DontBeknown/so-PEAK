using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Game.Core.DI;

namespace Game.Player
{
    /// <summary>
    /// Switches between multiple visual player models based on equipped items.
    /// Keeps gameplay systems on the root object and only swaps visual bindings.
    /// </summary>
    public class PlayerModelSwitcher : MonoBehaviour
    {
        [Serializable]
        private class ModelBinding
        {
            public string label;
            public GameObject modelRoot;
            public Animator animator;
            public FootIKControllerRefactored footIKController;
            public HandIKControllerRefactored handIKController;
            public Rig handRig;
            public Transform leftHandTarget;
            public Transform rightHandTarget;
            public Transform rightHandEquipBone;
            public Transform leftHandEquipBone;
        }

        [Header("Core References")]
        [SerializeField] private EquipmentManager equipmentManager;
        [SerializeField] private PlayerControllerRefactored playerController;
        [SerializeField] private FootIKControllerRefactored footIKController;
        [SerializeField] private HandIKControllerRefactored handIKController;
        [SerializeField] private HeldItemBehaviorManager heldItemBehaviorManager;

        [Header("Model Setup (3 models)")]
        [SerializeField] private List<ModelBinding> modelBindings = new List<ModelBinding>();
        [SerializeField, Min(0)] private int initialModelIndex;
        private int _activeModelIndex = -1;

        public int ActiveModelIndex => _activeModelIndex;

        private void Awake()
        {
            playerController ??= GetComponent<PlayerControllerRefactored>();
            footIKController ??= GetComponent<FootIKControllerRefactored>();
            handIKController ??= GetComponent<HandIKControllerRefactored>();
            heldItemBehaviorManager ??= GetComponent<HeldItemBehaviorManager>();
        }

        private void Start()
        {
            equipmentManager ??= ServiceContainer.Instance.TryGet<EquipmentManager>();
            if (equipmentManager == null)
            {
                equipmentManager = GetComponent<EquipmentManager>();
            }

            if (modelBindings.Count > 0)
            {
                int clampedInitialIndex = Mathf.Clamp(initialModelIndex, 0, modelBindings.Count - 1);
                ApplyModel(clampedInitialIndex, "startup");
            }

            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
            }
            else
            {
                Debug.LogWarning("[PlayerModelSwitcher] EquipmentManager not found; model switching disabled.");
            }
        }

        private void OnDestroy()
        {
            if (equipmentManager != null)
            {
                equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
            }
        }

        private void OnEquipmentChanged(EquipmentSlotType slotType, IEquippable item)
        {
            if (item == null)
            {
                ReevaluateModelAfterEquipmentChange();
                return;
            }

            EquipmentItem equipmentItem = item as EquipmentItem;
            if (equipmentItem == null)
            {
                return;
            }

            if (!equipmentItem.TryGetPlayerModelIndex(out int modelIndex))
            {
                return;
            }

            ApplyModel(modelIndex, equipmentItem.itemName);
        }

        private void ReevaluateModelAfterEquipmentChange()
        {
            if (equipmentManager == null)
            {
                ApplyModel(GetDefaultModelIndex(), "unequip_default");
                return;
            }

            foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                EquipmentItem equippedItem = equipmentManager.GetEquippedItem(slotType) as EquipmentItem;
                if (equippedItem != null && equippedItem.TryGetPlayerModelIndex(out int mappedModelIndex))
                {
                    ApplyModel(mappedModelIndex, $"reevaluate_{equippedItem.itemName}");
                    return;
                }
            }

            ApplyModel(GetDefaultModelIndex(), "unequip_default");
        }

        private int GetDefaultModelIndex()
        {
            if (modelBindings.Count == 0)
            {
                return 0;
            }

            return Mathf.Clamp(initialModelIndex, 0, modelBindings.Count - 1);
        }

        private void ApplyModel(int modelIndex, string reason)
        {
            if (modelIndex < 0 || modelIndex >= modelBindings.Count)
            {
                Debug.LogWarning($"[PlayerModelSwitcher] Invalid model index {modelIndex} for reason '{reason}'.");
                return;
            }

            if (_activeModelIndex == modelIndex)
            {
                return;
            }

            for (int i = 0; i < modelBindings.Count; i++)
            {
                ModelBinding candidate = modelBindings[i];
                if (candidate != null && candidate.modelRoot != null)
                {
                    candidate.modelRoot.SetActive(i == modelIndex);
                }
            }

            _activeModelIndex = modelIndex;
            RebindSystems(modelBindings[modelIndex]);
        }

        private void RebindSystems(ModelBinding binding)
        {
            if (binding == null)
            {
                return;
            }

            FootIKControllerRefactored targetFootIKController = binding.footIKController != null
                ? binding.footIKController
                : footIKController;
            HandIKControllerRefactored targetHandIKController = binding.handIKController != null
                ? binding.handIKController
                : handIKController;

            if (playerController != null && binding.animator != null)
            {
                playerController.RebindAnimationAnimator(binding.animator);
            }

            if (targetFootIKController != null && binding.animator != null)
            {
                targetFootIKController.RebindAnimator(binding.animator, resetState: true);
            }

            if (targetHandIKController != null)
            {
                targetHandIKController.RebindRigTargets(
                    binding.handRig,
                    binding.leftHandTarget,
                    binding.rightHandTarget,
                    resetWeight: true);
            }

            if (heldItemBehaviorManager != null)
            {
                heldItemBehaviorManager.RefreshHandBoneCache(binding.rightHandEquipBone, binding.leftHandEquipBone);
                heldItemBehaviorManager.RebuildActiveBehaviorAttachment();
            }
        }
    }
}
