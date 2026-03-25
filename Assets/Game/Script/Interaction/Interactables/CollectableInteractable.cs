using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Collectable;
using Game.Dialog;
using Game.UI;

namespace Game.Interaction
{
    public class CollectableInteractable : MonoBehaviour, IInteractable
    {
        [Header("Collectable")]
        [SerializeField] private CollectableItem collectableItem;
        [SerializeField] private DialogData dialogDataOverride;
        [SerializeField] private bool destroyOnInteract = true;

        [Header("Interaction")]
        [SerializeField] private float interactionPriority = 1f;
        [SerializeField] private string interactionVerb = "Press to";

        public string InteractionPrompt => collectableItem != null ? $"Collect {collectableItem.headerName}" : "Collect";
        public string InteractionVerb => interactionVerb;
        public float InteractionPriority => interactionPriority;

        public bool CanInteract
        {
            get
            {
                if (collectableItem == null || string.IsNullOrWhiteSpace(collectableItem.id))
                    return false;

                var manager = ServiceContainer.Instance.TryGet<ICollectableManager>();
                return manager != null && !manager.IsUnlocked(collectableItem.id);
            }
        }

        public Transform GetTransform() => transform;

        public void OnHighlighted(bool highlighted)
        {
            // Optional highlight logic can be added in scene-specific variants.
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!CanInteract)
                return;

            var collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
            if (collectableManager == null)
                return;

            collectableManager.Unlock(collectableItem);

            if(collectableItem.type == CollectableType.TextDocument)
            {
                // Open through panel controller so input/cursor state is updated.
                var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
                uiService?.OpenPanel("Inventory");

                // Switch silently to avoid playing a second tab sound.
                var tabbedInventory = ServiceContainer.Instance.TryGet<TabbedInventoryUI>();
                tabbedInventory?.SwitchTab(TabbedInventoryUI.TabType.Collectables, playSound: false);

                var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
                eventBus?.Publish(new CollectableHubFocusRequestedEvent(collectableItem.id));
            }
            else if (collectableItem.type == CollectableType.ScriptDialog)
            {
                var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
                eventBus?.Publish(new CollectableOpenRequestedEvent(collectableItem, true));
            }

            if (destroyOnInteract)
            {
                PersistSpawnDestroyedState();
                Destroy(gameObject);
            }
        }

        private void PersistSpawnDestroyedState()
        {
            var spawnedState = GetComponent<SpawnedObjectState>();
            spawnedState?.MarkDestroyed();
        }
    }
}
