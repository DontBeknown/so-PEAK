using UnityEngine;
using UnityEngine.Serialization;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

namespace Game.Interaction
{
    [System.Serializable]
    public class ResourceDrop
    {
        public InventoryItem item;
        [FormerlySerializedAs("amount")]
        public int guaranteedAmount = 2;
        [Range(0f, 1f)] public float bonusDropChance = 0.5f;
        public int bonusAmount = 1;

        public int RollAmount()
        {
            int total = Mathf.Max(0, guaranteedAmount);
            if (bonusAmount > 0 && bonusDropChance > 0f && Random.value < bonusDropChance)
            {
                total += bonusAmount;
            }

            return total;
        }
    }

    /// <summary>
    /// Hold-based gathering interactable implemented on top of HoldInteractableBase.
    /// </summary>
    public class GatheringInteractable : HoldInteractableBase
    {
        [Header("Resource Settings")]
        [SerializeField] private ResourceDrop[] resourceDrops;
        [SerializeField] private string customPrompt = "";

        [Header("Gathering Settings")]
        [SerializeField] private bool isMultiUse = true;
        [SerializeField] private float respawnTime = 60f;
        [SerializeField] private bool destroyOnUse = true;

        [Header("Depleted Visual")]
        [SerializeField] private GameObject depletedVisual;

        [Header("Audio")]
        [SerializeField] private string itemPickupSFXId = "item_pickup";
        [SerializeField] private float itemPickupSFXVolume = 0.45f;

        private bool isDepleted;
        private float respawnTimer;
        private IEventBus _eventBus;

        public ResourceDrop[] ResourceDrops => resourceDrops;

        public override string InteractionPrompt
        {
            get
            {
                if (!string.IsNullOrEmpty(customPrompt))
                    return $"Gather {customPrompt}";

                if (resourceDrops != null && resourceDrops.Length > 0 && resourceDrops[0].item != null)
                    return $"Gather {resourceDrops[0].item.itemName}";

                return "Gather Resource";
            }
        }

        public override bool CanInteract => !isCurrentlyHolding && !isDepleted && resourceDrops != null && resourceDrops.Length > 0;

        protected override void Update()
        {
            base.Update();

            if (isDepleted && isMultiUse)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
        }

        protected override void OnHoldStart()
        {
            _eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
        }

        protected override void OnHoldComplete()
        {
            if (currentPlayer != null && resourceDrops != null && resourceDrops.Length > 0)
            {
                var inventoryService = ServiceContainer.Instance.Get<Game.Player.Inventory.IInventoryService>();
                if (inventoryService != null)
                {
                    foreach (var drop in resourceDrops)
                    {
                        int dropAmount = drop?.RollAmount() ?? 0;
                        if (drop?.item != null && dropAmount > 0)
                        {
                            inventoryService.AddItem(drop.item, dropAmount);
                        }
                    }

                    ShowCompletionNotification();
                }
            }

            _eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Publish(new PlayPositionalSFXEvent(itemPickupSFXId, transform.position, itemPickupSFXVolume));

            if (!isMultiUse)
            {
                isDepleted = true;
                PersistSpawnDestroyedState();

                if (destroyOnUse)
                {
                    DestroyResource();
                }
                else
                {
                    UpdateDepletedVisual();
                }
            }
            else
            {
                isDepleted = true;
                respawnTimer = respawnTime;
                UpdateDepletedVisual();
            }
        }

        private void Respawn()
        {
            isDepleted = false;
            respawnTimer = 0f;
            UpdateDepletedVisual();
        }

        private void DestroyResource()
        {
            var scaleAnim = GetComponent<ScaleDownDestroyAnimation>();
            if (scaleAnim != null)
                scaleAnim.PlayAndDestroy();
            else
                Destroy(gameObject, 0.5f);
        }

        private void PersistSpawnDestroyedState()
        {
            var spawnedState = GetComponent<SpawnedObjectState>();
            spawnedState?.MarkDestroyed();
        }

        private void UpdateDepletedVisual()
        {
            if (depletedVisual != null)
            {
                depletedVisual.SetActive(isDepleted);
            }
        }

        private void ShowCompletionNotification()
        {
            if (resourceDrops == null || resourceDrops.Length == 0)
                return;

            string message;
            if (resourceDrops.Length == 1 && resourceDrops[0].item != null)
            {
                var drop = resourceDrops[0];
                int minAmount = Mathf.Max(0, drop.guaranteedAmount);
                int maxAmount = minAmount + ((drop.bonusAmount > 0 && drop.bonusDropChance > 0f) ? drop.bonusAmount : 0);
                message = maxAmount > minAmount
                    ? $"Collected {minAmount}-{maxAmount}x {drop.item.itemName}"
                    : minAmount > 1
                    ? $"Collected {minAmount}x {drop.item.itemName}"
                    : $"Collected {drop.item.itemName}";
            }
            else
            {
                message = "Collected resources";
            }

            _ = message;
        }
    }
}
