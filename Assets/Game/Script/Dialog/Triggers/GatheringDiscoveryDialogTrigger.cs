using UnityEngine;
using Game.Core.DI;
using Game.Interaction;
using System;
using System.Collections.Generic;

namespace Game.Dialog.Triggers
{
    [Serializable]
    public class GatheringDiscoveryDialogEntry
    {
        public InventoryItem targetItem;
        public DialogData dialogData;
        public bool triggerOnce = true;
    }

    /// <summary>
    /// Triggers dialogs the first time a configured gathering item is detected.
    /// Matching is done by exact InventoryItem reference from GatheringInteractable drops.
    /// </summary>
    public class GatheringDiscoveryDialogTrigger : MonoBehaviour
    {
        [Header("Discovery Dialog Entries")]
        [SerializeField] private List<GatheringDiscoveryDialogEntry> discoveryEntries = new List<GatheringDiscoveryDialogEntry>();

        [Header("References")]
        [SerializeField] private InteractionDetector interactionDetector;
        [SerializeField] private DialogManager dialogManager;

        private IDialogManager _dialogManagerInterface;
        private readonly HashSet<string> _triggeredDialogIds = new HashSet<string>();
        private readonly HashSet<int> _triggeredEntryIndices = new HashSet<int>();

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshTriggeredStateFromDialogHistory();

            if (interactionDetector != null)
            {
                interactionDetector.OnNearestInteractableChanged += OnNearestInteractableChanged;
            }
        }

        private void OnDisable()
        {
            if (interactionDetector != null)
            {
                interactionDetector.OnNearestInteractableChanged -= OnNearestInteractableChanged;
            }
        }

        private void ResolveReferences()
        {
            if (interactionDetector == null)
            {
                interactionDetector = GetComponent<InteractionDetector>();
                if (interactionDetector == null)
                {
                    interactionDetector = FindFirstObjectByType<InteractionDetector>();
                }
            }

            if (dialogManager == null)
            {
                dialogManager = FindFirstObjectByType<DialogManager>();
            }

            _dialogManagerInterface = ServiceContainer.Instance.TryGet<IDialogManager>();
            if (_dialogManagerInterface == null)
            {
                _dialogManagerInterface = dialogManager;
            }
        }

        private void RefreshTriggeredStateFromDialogHistory()
        {
            _triggeredDialogIds.Clear();
            _triggeredEntryIndices.Clear();

            if (_dialogManagerInterface == null)
            {
                return;
            }

            if (discoveryEntries == null)
            {
                return;
            }

            for (int i = 0; i < discoveryEntries.Count; i++)
            {
                var entry = discoveryEntries[i];
                if (entry == null || entry.dialogData == null || string.IsNullOrWhiteSpace(entry.dialogData.dialogId))
                {
                    continue;
                }

                if (_dialogManagerInterface.HasTriggered(entry.dialogData.dialogId))
                {
                    _triggeredDialogIds.Add(entry.dialogData.dialogId);
                    if (entry.triggerOnce)
                    {
                        _triggeredEntryIndices.Add(i);
                    }
                }
            }
        }

        private void OnNearestInteractableChanged(IInteractable nearestInteractable)
        {
            if (nearestInteractable == null || _dialogManagerInterface == null)
            {
                return;
            }

            var gatheringInteractable = nearestInteractable as GatheringInteractable;
            if (gatheringInteractable == null)
            {
                return;
            }

            var drops = gatheringInteractable.ResourceDrops;
            if (drops == null || drops.Length == 0)
            {
                return;
            }

            if (discoveryEntries == null || discoveryEntries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < discoveryEntries.Count; i++)
            {
                var entry = discoveryEntries[i];
                if (entry == null || entry.targetItem == null || entry.dialogData == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.dialogData.dialogId))
                {
                    continue;
                }

                if (entry.triggerOnce && _triggeredEntryIndices.Contains(i))
                {
                    continue;
                }

                if (_triggeredDialogIds.Contains(entry.dialogData.dialogId))
                {
                    continue;
                }

                if (!ContainsTargetItem(drops, entry.targetItem))
                {
                    continue;
                }

                if (TryStartDialog(entry.dialogData))
                {
                    _triggeredDialogIds.Add(entry.dialogData.dialogId);
                    if (entry.triggerOnce)
                    {
                        _triggeredEntryIndices.Add(i);
                    }
                }

                // Start at most one dialog per detection change.
                return;
            }
        }

        private bool TryStartDialog(DialogData dialogData)
        {
            if (dialogData == null || string.IsNullOrWhiteSpace(dialogData.dialogId))
            {
                return false;
            }

            if (_dialogManagerInterface.HasTriggered(dialogData.dialogId))
            {
                _triggeredDialogIds.Add(dialogData.dialogId);
                return true;
            }

            if (_dialogManagerInterface.IsActive)
            {
                return false;
            }

            _dialogManagerInterface.StartDialog(dialogData, false);
            return true;
        }

        private static bool ContainsTargetItem(ResourceDrop[] drops, InventoryItem targetItem)
        {
            for (int i = 0; i < drops.Length; i++)
            {
                var drop = drops[i];
                if (drop != null && drop.item == targetItem)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
