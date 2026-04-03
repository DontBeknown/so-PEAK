using UnityEngine;
using System.Collections;
using DG.Tweening;
using Game.Core.DI;
using System.Threading.Tasks;
using Game.Core.Events;
using Game.Sound.Events;
using UnityEngine.Serialization;

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
    /// Timed gathering interactable requiring player to HOLD E button.
    /// Locks player movement during gathering, shows progress bar, and can be cancelled.
    /// Examples: Berry bushes, mining nodes, herb gathering, fishing spots.
    /// </summary>
    public class GatheringInteractable : MonoBehaviour, IInteractable
    {
        [Header("Resource Settings")]
        [SerializeField] private ResourceDrop[] resourceDrops;
        [SerializeField] private string customPrompt = ""; // e.g., "Berries", "Iron Ore"
        
        [Header("Gathering Settings")]
        [SerializeField] private float gatherDuration = 3f;
        [SerializeField] private bool isMultiUse = true; // Can be gathered multiple times?
        [SerializeField] private float respawnTime = 60f; // Only used if multiUse
        [SerializeField] private bool destroyOnUse = true; // Destroy object after use (only applies if not multiUse)
        
        [Header("Interaction Settings")]
        [SerializeField] private float interactionPriority = 1.2f; // Slightly higher than instant items
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject highlightEffect;
        [SerializeField] private GameObject depletedVisual; // Optional different model when empty
        
        [Header("Audio")]
        [SerializeField] private AudioClip gatherStartSound;
        [SerializeField] private AudioClip gatherLoopSound;
        [SerializeField] private AudioClip gatherCompleteSound;
        [SerializeField] private AudioClip gatherCancelSound;
        [SerializeField] private string itemPickupSFXId = "item_pickup";
        [SerializeField] private float itemPickupSFXVolume = 0.45f;
        [SerializeField] private string startGatherSoundId = "gather_start";
        [SerializeField] private float startGatherSoundVolume = 0.5f;
        
        // State
        private bool isHighlighted = false;
        private bool isCurrentlyGathering = false;
        private bool isDepleted = false;
        private float respawnTimer = 0f;
        private float currentGatherProgress = 0f;
        
        // References
        private Game.Player.PlayerControllerRefactored currentPlayer;
        private Coroutine gatheringCoroutine;
        private Game.Interaction.UI.InteractionPromptUI promptUI;
        private AudioSource loopingAudioSource;
        private IEventBus eventBus;

        public ResourceDrop[] ResourceDrops => resourceDrops;

        #region IInteractable Implementation

        public string InteractionPrompt
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

        public string InteractionVerb => "Hold to";

        public bool CanInteract => !isCurrentlyGathering && !isDepleted && resourceDrops != null && resourceDrops.Length > 0;

        public float InteractionPriority => interactionPriority;

        public Transform GetTransform() => transform;

        public void OnHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(highlighted && !isDepleted);
            }
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!CanInteract)
                return;

            currentPlayer = player;
            StartGathering();
        }

        #endregion

        private void Update()
        {
            // Handle respawn timer
            if (isDepleted && isMultiUse)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
            
            // Check if player is still holding E during gathering
            if (isCurrentlyGathering)
            {
                CheckGatheringInput();
            }
        }

        private void StartGathering()
        {
            if (isCurrentlyGathering)
                return;

            isCurrentlyGathering = true;
            currentGatherProgress = 0f;
            
            // Disable interaction detector to prevent other prompts
            if (currentPlayer != null)
            {
                var detector = currentPlayer.GetComponent<Game.Interaction.InteractionDetector>();
                if (detector != null)
                {
                    detector.DisableDetection();
                }
            }
            
            // Lock player movement
            if (currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(true);
            }
            
            // Play start sound
            if (gatherStartSound != null)
            {
                AudioSource.PlayClipAtPoint(gatherStartSound, transform.position);
            }

            if(eventBus == null)
            {
                eventBus = ServiceContainer.Instance?.Get<IEventBus>();
            }
            eventBus?.Publish(new PlayPositionalSFXEvent(startGatherSoundId, transform.position,startGatherSoundVolume));
            
            // Start loop sound
            if (gatherLoopSound != null)
            {
                loopingAudioSource = gameObject.AddComponent<AudioSource>();
                loopingAudioSource.clip = gatherLoopSound;
                loopingAudioSource.loop = true;
                loopingAudioSource.spatialBlend = 1f;
                loopingAudioSource.Play();
            }
            
            // Trigger player animation (if animator available)
            // TODO: Implement animation system
            
            // Get prompt UI from service container or cache
            if (promptUI == null)
            {
                promptUI = ServiceContainer.Instance.TryGet<Game.Interaction.UI.InteractionPromptUI>();
            }
            
            if (promptUI != null)
            {
                promptUI.ShowProgressBar();
            }
            
            // Start gathering coroutine
            gatheringCoroutine = StartCoroutine(GatheringProcess());
            
            //Debug.Log($"[GatheringInteractable] Started gathering {InteractionPrompt}");
        }

        private IEnumerator GatheringProcess()
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < gatherDuration)
            {
                elapsedTime += Time.deltaTime;
                currentGatherProgress = Mathf.Clamp01(elapsedTime / gatherDuration);
                
                // Update progress bar
                if (promptUI != null)
                {
                    promptUI.UpdateProgress(currentGatherProgress);
                }
                
                yield return null;
            }
            
            // Gathering complete!
            CompleteGathering();
        }

        private void CheckGatheringInput()
        {
            // Check if pickup button is still physically held (bypasses input blocking)
            if (currentPlayer != null)
            {
                if (!currentPlayer.IsPickupButtonPhysicallyHeld)
                {
                    // Player released button - cancel gathering
                    CancelGathering("Released button");
                }
            }
            else
            {
                // No player reference - shouldn't happen, but cancel just in case
                CancelGathering("Lost player reference");
            }
        }

        private void CompleteGathering()
        {
            if (!isCurrentlyGathering)
                return;

            //Debug.Log($"[GatheringInteractable] Gathering complete!");
            
            // Add items to inventory
            if (currentPlayer != null && resourceDrops != null && resourceDrops.Length > 0)
            {
                var inventoryService = Game.Core.DI.ServiceContainer.Instance.Get<Game.Player.Inventory.IInventoryService>();
                if (inventoryService != null)
                {
                    foreach (var drop in resourceDrops)
                    {
                        int dropAmount = drop?.RollAmount() ?? 0;
                        if (drop.item != null && dropAmount > 0)
                        {
                            //Debug.Log($"[GatheringInteractable] Adding {dropAmount}x {drop.item.itemName} to inventory");
                            inventoryService.AddItem(drop.item, dropAmount);
                        }
                    }
                    ShowCompletionNotification();
                }
            }
            
            // Play completion sound
            if (gatherCompleteSound != null)
            {
                AudioSource.PlayClipAtPoint(gatherCompleteSound, transform.position);
            }
            if(eventBus == null)
            {
                eventBus = ServiceContainer.Instance?.Get<IEventBus>();
            }
            eventBus?.Publish(new PlayPositionalSFXEvent(itemPickupSFXId, transform.position, itemPickupSFXVolume));

            eventBus?.Publish(new HoldInteractCompletedEvent(gameObject));
            
            // Deplete resource
            if (!isMultiUse)
            {
                isDepleted = true;
                if (destroyOnUse)
                {
                    PersistSpawnDestroyedState();
                    DestroyResource();
                }
                else
                {
                    PersistSpawnDestroyedState();
                    UpdateDepletedVisual();
                }
            }
            else
            {
                isDepleted = true;
                respawnTimer = respawnTime;
                UpdateDepletedVisual();
            }
            
            CleanupGathering();
        }

        private void CancelGathering(string reason)
        {
            if (!isCurrentlyGathering)
                return;

            //Debug.Log($"[GatheringInteractable] Gathering cancelled: {reason}");
            
            // Play cancel sound
            if (gatherCancelSound != null)
            {
                AudioSource.PlayClipAtPoint(gatherCancelSound, transform.position);
            }
            
            // Show notification
            //Debug.Log("Gathering cancelled!");
            
            CleanupGathering();
        }

        private void CleanupGathering()
        {
            isCurrentlyGathering = false;
            currentGatherProgress = 0f;
            
            // Stop gathering coroutine
            if (gatheringCoroutine != null)
            {
                StopCoroutine(gatheringCoroutine);
                gatheringCoroutine = null;
            }
            
            // Stop looping audio
            if (loopingAudioSource != null)
            {
                loopingAudioSource.Stop();
                Destroy(loopingAudioSource);
                loopingAudioSource = null;
            }
            
            // Hide progress bar
            if (promptUI != null)
            {
                promptUI.HideProgressBar();
                promptUI = null;
            }
            
            // Re-enable interaction detector
            if (currentPlayer != null)
            {
                var detector = currentPlayer.GetComponent<Game.Interaction.InteractionDetector>();
                if (detector != null)
                {
                    detector.EnableDetection();
                }
            }
            
            // Unlock player
            if (currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(false);
                currentPlayer = null;
            }
        }

        /// <summary>
        /// Called when player takes damage during gathering
        /// </summary>
        public void OnPlayerDamaged()
        {
            if (isCurrentlyGathering)
            {
                CancelGathering("Took damage");
            }
        }

        private void Respawn()
        {
            isDepleted = false;
            respawnTimer = 0f;
            UpdateDepletedVisual();
            Debug.Log($"[GatheringInteractable] {InteractionPrompt} respawned");
        }

        private void DestroyResource()
        {
            // Play scale-down animation if available, otherwise fall back to plain destroy
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

        private void OnDestroy()
        {
            // Cleanup if destroyed during gathering
            if (isCurrentlyGathering)
            {
                CleanupGathering();
            }
        }

        #region Editor Helpers

        /*private void OnDrawGizmos()
        {
            Color gizmoColor = isDepleted ? Color.gray : (isCurrentlyGathering ? Color.green : Color.yellow);
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            string label = resourceDrops != null && resourceDrops.Length > 0 && resourceDrops[0].item != null
                ? $"{resourceDrops[0].item.itemName} x{resourceDrops[0].amount}\n{gatherDuration}s gather"
                : "Gathering Node";
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        }*/

        #endregion
    }
}
