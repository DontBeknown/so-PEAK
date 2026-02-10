using UnityEngine;
using System.Collections;
using Game.Core.DI;

namespace Game.Interaction
{
    /// <summary>
    /// Water source interactable for refilling canteens.
    /// Player must have canteen equipped in HeldItem slot.
    /// Hold-to-interact mechanic with progress bar (similar to GatheringInteractable).
    /// Follows Single Responsibility Principle - only refills canteens.
    /// </summary>
    public class WaterSourceInteractable : MonoBehaviour, IInteractable
    {
        [Header("Water Source Settings")]
        [SerializeField] private string customPrompt = ""; // e.g., "Well", "River", "Rain Barrel"
        [SerializeField] private float refillDuration = 3f;
        
        [Header("Interaction Settings")]
        [SerializeField] private float interactionPriority = 1.2f;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject highlightEffect;
        
        [Header("Audio")]
        [SerializeField] private AudioClip refillStartSound;
        [SerializeField] private AudioClip refillCompleteSound;
        [SerializeField] private AudioClip refillCancelSound;
        
        // State
        private bool isHighlighted = false;
        private bool isCurrentlyRefilling = false;
        private float currentRefillProgress = 0f;
        
        // References
        private Game.Player.PlayerControllerRefactored currentPlayer;
        private Coroutine refillingCoroutine;
        private Game.Interaction.UI.InteractionPromptUI promptUI;

        #region IInteractable Implementation

        public string InteractionPrompt
        {
            get
            {
                // Check if player has canteen equipped
                var canteen = GetEquippedCanteen();
                
                if (canteen == null)
                {
                    // Check if player has canteen in inventory
                    var inventoryService = ServiceContainer.Instance.TryGet<Game.Player.Inventory.IInventoryService>();
                    if (inventoryService != null && inventoryService.HasItem(GetAnyCanteen()))
                    {
                        return "Equip Canteen to Refill";
                    }
                    return "No Canteen";
                }
                
                if (canteen.IsFull())
                {
                    return "Canteen Full";
                }
                
                if (!string.IsNullOrEmpty(customPrompt))
                    return $"Refill Canteen ({customPrompt})";
                
                return "Refill Canteen";
            }
        }

        public string InteractionVerb => "Hold to";

        public bool CanInteract
        {
            get
            {
                if (isCurrentlyRefilling)
                    return false;
                
                var canteen = GetEquippedCanteen();
                return canteen != null && !canteen.IsFull();
            }
        }

        public float InteractionPriority => interactionPriority;

        public Transform GetTransform() => transform;

        public void OnHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(highlighted);
            }
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!CanInteract)
                return;

            currentPlayer = player;
            StartRefilling();
        }

        #endregion

        private void Update()
        {
            // Check if player is still holding E during refilling
            if (isCurrentlyRefilling)
            {
                CheckRefillingInput();
            }
        }

        private void StartRefilling()
        {
            if (isCurrentlyRefilling)
                return;

            isCurrentlyRefilling = true;
            currentRefillProgress = 0f;
            
            // Lock player movement
            if (currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(true);
            }
            
            // Play start sound
            if (refillStartSound != null)
            {
                AudioSource.PlayClipAtPoint(refillStartSound, transform.position);
            }
            
            // Get prompt UI
            if (promptUI == null)
            {
                promptUI = ServiceContainer.Instance.TryGet<Game.Interaction.UI.InteractionPromptUI>();
            }
            
            if (promptUI != null)
            {
                promptUI.ShowProgressBar();
            }
            
            // Start refilling coroutine
            refillingCoroutine = StartCoroutine(RefillingProcess());
            
            //Debug.Log($"[WaterSourceInteractable] Started refilling canteen");
        }

        private IEnumerator RefillingProcess()
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < refillDuration)
            {
                elapsedTime += Time.deltaTime;
                currentRefillProgress = Mathf.Clamp01(elapsedTime / refillDuration);
                
                // Update progress bar
                if (promptUI != null)
                {
                    promptUI.UpdateProgress(currentRefillProgress);
                }
                
                yield return null;
            }
            
            // Refilling complete!
            CompleteRefilling();
        }

        private void CheckRefillingInput()
        {
            // Check if pickup button is still physically held
            if (currentPlayer != null)
            {
                if (!currentPlayer.IsPickupButtonPhysicallyHeld)
                {
                    // Player released button - cancel refilling
                    CancelRefilling("Released button");
                }
            }
            else
            {
                // No player reference - cancel
                CancelRefilling("Lost player reference");
            }
        }

        private void CompleteRefilling()
        {
            if (!isCurrentlyRefilling)
                return;

            //Debug.Log($"[WaterSourceInteractable] Refilling complete!");
            
            // Refill the canteen
            var canteen = GetEquippedCanteen();
            if (canteen != null)
            {
                canteen.Refill();
                ShowCompletionNotification(canteen);
            }
            
            // Play completion sound
            if (refillCompleteSound != null)
            {
                AudioSource.PlayClipAtPoint(refillCompleteSound, transform.position);
            }
            
            CleanupRefilling();
        }

        private void CancelRefilling(string reason)
        {
            if (!isCurrentlyRefilling)
                return;

            //Debug.Log($"[WaterSourceInteractable] Refilling cancelled: {reason}");
            
            // Play cancel sound
            if (refillCancelSound != null)
            {
                AudioSource.PlayClipAtPoint(refillCancelSound, transform.position);
            }
            
            CleanupRefilling();
        }

        private void CleanupRefilling()
        {
            isCurrentlyRefilling = false;
            currentRefillProgress = 0f;
            
            // Stop refilling coroutine
            if (refillingCoroutine != null)
            {
                StopCoroutine(refillingCoroutine);
                refillingCoroutine = null;
            }
            
            // Hide progress bar
            if (promptUI != null)
            {
                promptUI.HideProgressBar();
                promptUI = null;
            }
            
            // Unlock player
            if (currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(false);
                currentPlayer = null;
            }
        }

        private CanteenItem GetEquippedCanteen()
        {
            var equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();
            if (equipmentManager == null)
                return null;
            
            var equippedItem = equipmentManager.GetEquippedItem(EquipmentSlotType.HeldItem);
            return equippedItem as CanteenItem;
        }

        private InventoryItem GetAnyCanteen()
        {
            // This is a placeholder - we'd need to search inventory for any canteen
            // For now, just return null
            return null;
        }

        private void ShowCompletionNotification(CanteenItem canteen)
        {
            string message = $"Canteen Refilled [{canteen.GetStateDescription()}]";
            //Debug.Log(message);
            // TODO: Connect to notification system
        }

        private void OnDestroy()
        {
            // Cleanup if destroyed during refilling
            if (isCurrentlyRefilling)
            {
                CleanupRefilling();
            }
        }

        #region Editor Helpers

        private void OnDrawGizmos()
        {
            Gizmos.color = isCurrentlyRefilling ? Color.cyan : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            string label = !string.IsNullOrEmpty(customPrompt)
                ? $"Water Source: {customPrompt}\n{refillDuration}s refill"
                : $"Water Source\n{refillDuration}s refill";
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        }

        #endregion
    }
}
