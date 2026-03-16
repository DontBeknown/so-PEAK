using UnityEngine;
using Game.Core.DI;

namespace Game.Interaction
{
    /// <summary>
    /// REFACTORED VERSION using HoldInteractableBase.
    /// Much simpler - only contains gathering-specific logic!
    /// All hold mechanics, detector management, and player locking handled by base class.
    /// </summary>
    public class GatheringInteractable_Refactored : HoldInteractableBase
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
        
        // State
        private bool isDepleted = false;
        private float respawnTimer = 0f;

        #region IInteractable Implementation

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

        #endregion

        protected override void Update()
        {
            base.Update(); // Important: call base for hold input checking
            
            // Handle respawn timer
            if (isDepleted && isMultiUse)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
        }

        #region Hold Interaction Overrides (Gathering-specific logic only!)

        protected override void OnHoldComplete()
        {
            // Add items to inventory
            if (currentPlayer != null && resourceDrops != null && resourceDrops.Length > 0)
            {
                var inventoryService = ServiceContainer.Instance.Get<Game.Player.Inventory.IInventoryService>();
                if (inventoryService != null)
                {
                    foreach (var drop in resourceDrops)
                    {
                        int dropAmount = drop?.RollAmount() ?? 0;
                        if (drop.item != null && dropAmount > 0)
                        {
                            inventoryService.AddItem(drop.item, dropAmount);
                        }
                    }
                    ShowCompletionNotification();
                }
            }
            
            // Deplete resource
            if (!isMultiUse)
            {
                isDepleted = true;
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

        #endregion

        #region Gathering-specific Methods

        private void Respawn()
        {
            isDepleted = false;
            respawnTimer = 0f;
            UpdateDepletedVisual();
            Debug.Log($"[GatheringInteractable] {InteractionPrompt} respawned");
        }

        private void DestroyResource()
        {
            Destroy(gameObject, 0.5f);
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

            string message = "";
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
            
            //Debug.Log(message);
            // TODO: Connect to notification system
        }

        #endregion
    }
}
