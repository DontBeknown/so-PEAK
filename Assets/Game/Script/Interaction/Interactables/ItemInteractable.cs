using UnityEngine;
using Game.Core.DI;
using Game.Player.Inventory;

namespace Game.Interaction
{
    /// <summary>
    /// Simple instant-pickup interactable for collecting items.
    /// Player presses E to instantly add item to inventory.
    /// REFACTORED: Now uses IInventoryService from ServiceContainer
    /// </summary>
    public class ItemInteractable : MonoBehaviour, IInteractable
    {
        [Header("Item Settings")]
        [SerializeField] private InventoryItem item;
        [SerializeField] private int quantity = 1;
        [SerializeField] private string customPrompt = ""; // Optional custom prompt

        /// <summary>The InventoryItem this interactable gives.</summary>
        public InventoryItem Item => item;
        /// <summary>How many of the item this interactable gives.</summary>
        public int Quantity => quantity;
        
        [Header("Interaction Settings")]
        [SerializeField] private float interactionPriority = 1f;
        [SerializeField] private string interactionVerb = "Press to";
        
        [Header("Feedback")]
        [SerializeField] private GameObject highlightEffect; // Optional visual effect
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private GameObject pickupParticles;
        [SerializeField] private bool destroyOnPickup = true;

        [Header("Multiple Use")]
        [SerializeField] private bool allowMultipleUse = false;
        [SerializeField] private int maxUses = 0; // 0 = unlimited; > 0 = limited number of uses
        
        private bool isHighlighted = false;
        private bool hasBeenCollected = false;
        private int remainingUses;

        private void Start()
        {
            remainingUses = maxUses;
        }

        #region IInteractable Implementation

        public string InteractionPrompt 
        {
            get
            {
                if (!string.IsNullOrEmpty(customPrompt))
                    return customPrompt;
                
                if (item != null)
                    return $"Pick up {item.itemName}";
                
                return "Pick up Item";
            }
        }

        public string InteractionVerb => interactionVerb;

        public bool CanInteract => !hasBeenCollected && item != null;

        public float InteractionPriority => interactionPriority;

        public Transform GetTransform() => transform;

        public void OnHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            
            // Toggle highlight effect if available
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(highlighted);
            }
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!CanInteract || hasBeenCollected)
                return;

            // Get inventory service from ServiceContainer
            var inventoryService = ServiceContainer.Instance.Get<IInventoryService>();
            if (inventoryService == null)
            {
                Debug.LogError("[ItemInteractable] IInventoryService not registered in ServiceContainer!");
                return;
            }

            // Try to add item to inventory
            bool added = inventoryService.AddItem(item, quantity);
            
            if (added)
            {
                // Play pickup feedback
                PlayPickupFeedback();
                
                // Show notification
                ShowPickupNotification();

                bool usesExhausted = false;

                if (!allowMultipleUse)
                {
                    usesExhausted = true;
                }
                else if (maxUses > 0)
                {
                    remainingUses--;
                    if (remainingUses <= 0)
                        usesExhausted = true;
                }
                // else: unlimited uses — never exhausted

                if (usesExhausted)
                {
                    hasBeenCollected = true;

                    if (destroyOnPickup)
                        Destroy(gameObject);
                    else
                        gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[ItemInteractable] Failed to add {item.itemName} to inventory (full?)");
            }
        }

        #endregion

        private void PlayPickupFeedback()
        {
            // Play sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            // Spawn particles
            if (pickupParticles != null)
            {
                Instantiate(pickupParticles, transform.position, Quaternion.identity);
            }
        }

        private void ShowPickupNotification()
        {
            if (item != null)
            {
                string message = quantity > 1 
                    ? $"Collected {quantity}x {item.itemName}" 
                    : $"Collected {item.itemName}";
                
                // TODO: Connect to notification system when available
                //Debug.Log(message);
            }
        }

        private void OnValidate()
        {
            // Ensure quantity is at least 1
            if (quantity < 1)
                quantity = 1;

            // Ensure maxUses is non-negative
            if (maxUses < 0)
                maxUses = 0;
        }

        #region Editor Gizmos

        /*private void OnDrawGizmos()
        {
            // Draw a small sphere to show interaction point
            Gizmos.color = hasBeenCollected ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw item name label in editor
            if (item != null)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                    $"{item.itemName} x{quantity}");
            }
        }*/

        #endregion
    }
}
