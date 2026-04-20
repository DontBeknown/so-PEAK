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
        [Header("Collectable Pools by Biome")]
        [SerializeField] private CollectableItem[] forestPool;
        [SerializeField] private CollectableItem[] desertPool;
        [SerializeField] private CollectableItem[] snowPool;
        [SerializeField] private CollectableBiome selectedBiome = CollectableBiome.Forest;
        
        [Header("Behavior")]
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

        /// <summary>
        /// Gets the collectable pool for the currently selected biome.
        /// </summary>
        private CollectableItem[] GetCurrentPool()
        {
            return selectedBiome switch
            {
                CollectableBiome.Forest => forestPool,
                CollectableBiome.Desert => desertPool,
                CollectableBiome.Tundra => snowPool,
                _ => forestPool
            };
        }

        public void SetBiome(CollectableBiome biome)
        {
            selectedBiome = biome;
        }
        private bool RefreshBiomeAndCleanupIfInvalid()
        {
            var saveLoadService = ServiceContainer.Instance.TryGet<ISaveLoadService>();
            if (saveLoadService != null)
            {
                int currentLevel = saveLoadService.GetCurrentLevel();
                selectedBiome = GetBiomeFromLevel(currentLevel);
            }

            if (!HasValidCollectablePool())
            {
                PersistSpawnDestroyedState();
                Destroy(gameObject);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Maps level number to CollectableBiome using WorldLevel enum for extensibility.
        /// If new levels are added to WorldLevel, simply add a new case here.
        /// </summary>
        private static CollectableBiome GetBiomeFromLevel(int level)
        {
            // WorldLevel enum: Forest = 1, Desert = 2, Tundra = 3
            return (WorldLevel)level switch
            {
                WorldLevel.Forest => CollectableBiome.Forest,
                WorldLevel.Desert => CollectableBiome.Desert,
                WorldLevel.Tundra => CollectableBiome.Tundra,
                _ => CollectableBiome.Forest  // Fallback to Forest
            };
        }

        public void OnHighlighted(bool highlighted)
        {
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!RefreshBiomeAndCleanupIfInvalid())
            {
                return;
            }

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
            var pool = GetCurrentPool();
            if (pool == null || pool.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < pool.Length; i++)
            {
                if (IsValidCollectable(pool[i]))
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

            var pool = GetCurrentPool();
            if (pool == null)
            {
                return lockedCollectables;
            }

            for (int i = 0; i < pool.Length; i++)
            {
                var collectable = pool[i];
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

            var pool = GetCurrentPool();
            for (int i = 0; i < pool.Length; i++)
            {
                var collectable = pool[i];
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
                alreadyUnlockedMessage = "Note";
            }
        }
    }
}