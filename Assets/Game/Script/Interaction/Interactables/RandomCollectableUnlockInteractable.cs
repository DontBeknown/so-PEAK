using System.Collections.Generic;
using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Collectable;
using Game.UI;

namespace Game.Interaction
{
    public class RandomCollectableUnlockInteractable : MonoBehaviour, IInteractable
    {
        [Header("Collectable Pool")]
        [SerializeField] private CollectableItem[] collectablePool;
        [SerializeField] private bool destroyAfterSuccessfulUnlock = true;
        [SerializeField] private bool destroyOnExhaustion = true;
        [SerializeField] private string alreadyUnlockedMessage = "Already unlocked";

        [Header("Interaction")]
        [SerializeField] private float interactionPriority = 1f;
        [SerializeField] private string interactionVerb = "Press F to";
        [SerializeField] private string interactionPrompt = "collect note";

        public string InteractionPrompt => string.IsNullOrWhiteSpace(interactionPrompt) ? "Unlock random collectable" : interactionPrompt;
        public string InteractionVerb => interactionVerb;
        public float InteractionPriority => interactionPriority;

        public bool CanInteract => HasValidCollectablePool() && ServiceContainer.Instance.TryGet<ICollectableManager>() != null;

        public Transform GetTransform() => transform;

        public void OnHighlighted(bool highlighted)
        {
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            var collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
            if (collectableManager == null)
            {
                return;
            }

            var lockedCollectables = GetLockedCollectables(collectableManager);
            if (lockedCollectables.Count == 0)
            {
                ShowAlreadyUnlockedNotification();

                if (destroyOnExhaustion)
                {
                    PersistSpawnDestroyedState();
                    Destroy(gameObject);
                }

                return;
            }

            var selectedCollectable = lockedCollectables[UnityEngine.Random.Range(0, lockedCollectables.Count)];
            collectableManager.Unlock(selectedCollectable);
            OpenCollectable(selectedCollectable);

            if (destroyAfterSuccessfulUnlock)
            {
                PersistSpawnDestroyedState();
                Destroy(gameObject);
                return;
            }

            if (destroyOnExhaustion && AreAllUnlocked(collectableManager))
            {
                PersistSpawnDestroyedState();
                Destroy(gameObject);
            }
        }

        private bool HasValidCollectablePool()
        {
            if (collectablePool == null || collectablePool.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < collectablePool.Length; i++)
            {
                if (IsValidCollectable(collectablePool[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private List<CollectableItem> GetLockedCollectables(ICollectableManager collectableManager)
        {
            var lockedCollectables = new List<CollectableItem>();
            var seenIds = new HashSet<string>();

            if (collectablePool == null)
            {
                return lockedCollectables;
            }

            for (int i = 0; i < collectablePool.Length; i++)
            {
                var collectable = collectablePool[i];
                if (!IsValidCollectable(collectable))
                {
                    continue;
                }

                if (collectableManager.IsUnlocked(collectable.id))
                {
                    continue;
                }

                if (seenIds.Add(collectable.id))
                {
                    lockedCollectables.Add(collectable);
                }
            }

            return lockedCollectables;
        }

        private bool AreAllUnlocked(ICollectableManager collectableManager)
        {
            if (!HasValidCollectablePool())
            {
                return false;
            }

            for (int i = 0; i < collectablePool.Length; i++)
            {
                var collectable = collectablePool[i];
                if (!IsValidCollectable(collectable))
                {
                    continue;
                }

                if (!collectableManager.IsUnlocked(collectable.id))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsValidCollectable(CollectableItem collectable)
        {
            return collectable != null && !string.IsNullOrWhiteSpace(collectable.id);
        }

        private void ShowAlreadyUnlockedNotification()
        {
            var notificationUI = ServiceContainer.Instance.TryGet<ItemNotificationUI>();
            notificationUI?.ShowCustomNotification(alreadyUnlockedMessage, null, 1, NotificationType.AlreadyUnlocked);
        }

        private void OpenCollectable(CollectableItem collectable)
        {
            if (collectable == null)
            {
                return;
            }

            if (collectable.type == CollectableType.TextDocument)
            {
                var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
                uiService?.OpenPanel("Inventory");

                var tabbedInventory = ServiceContainer.Instance.TryGet<TabbedInventoryUI>();
                tabbedInventory?.SwitchTab(TabbedInventoryUI.TabType.Collectables, playSound: false);

                var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
                eventBus?.Publish(new CollectableHubFocusRequestedEvent(collectable.id));
            }
            else if (collectable.type == CollectableType.ScriptDialog)
            {
                var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
                eventBus?.Publish(new CollectableOpenRequestedEvent(collectable, true));
            }
        }

        private void PersistSpawnDestroyedState()
        {
            var spawnedState = GetComponent<SpawnedObjectState>();
            spawnedState?.MarkDestroyed();
        }

        private void OnValidate()
        {
            interactionPriority = Mathf.Max(0f, interactionPriority);

            if (string.IsNullOrWhiteSpace(interactionVerb))
            {
                interactionVerb = "Press to";
            }

            if (string.IsNullOrWhiteSpace(interactionPrompt))
            {
                interactionPrompt = "Unlock random collectable";
            }

            if (string.IsNullOrWhiteSpace(alreadyUnlockedMessage))
            {
                alreadyUnlockedMessage = "Already unlocked";
            }
        }
    }
}