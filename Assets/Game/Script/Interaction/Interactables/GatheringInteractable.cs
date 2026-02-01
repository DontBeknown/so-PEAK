using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace Game.Interaction
{
    /// <summary>
    /// Timed gathering interactable requiring player to HOLD E button.
    /// Locks player movement during gathering, shows progress bar, and can be cancelled.
    /// Examples: Berry bushes, mining nodes, herb gathering, fishing spots.
    /// </summary>
    public class GatheringInteractable : MonoBehaviour, IInteractable
    {
        [Header("Resource Settings")]
        [SerializeField] private InventoryItem resourceItem;
        [SerializeField] private int resourcesPerGather = 3;
        [SerializeField] private string customPrompt = ""; // e.g., "Berries", "Iron Ore"
        
        [Header("Gathering Settings")]
        [SerializeField] private float gatherDuration = 3f;
        [SerializeField] private bool isMultiUse = true; // Can be gathered multiple times?
        [SerializeField] private float respawnTime = 60f; // Only used if multiUse
        
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

        #region IInteractable Implementation

        public string InteractionPrompt
        {
            get
            {
                if (!string.IsNullOrEmpty(customPrompt))
                    return $"Gather {customPrompt}";
                
                if (resourceItem != null)
                    return $"Gather {resourceItem.itemName}";
                
                return "Gather Resource";
            }
        }

        public string InteractionVerb => "Hold to";

        public bool CanInteract => !isCurrentlyGathering && !isDepleted && resourceItem != null;

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
            
            // Find and setup progress bar in prompt UI
            promptUI = FindFirstObjectByType<Game.Interaction.UI.InteractionPromptUI>();
            if (promptUI != null)
            {
                promptUI.ShowProgressBar();
            }
            
            // Start gathering coroutine
            gatheringCoroutine = StartCoroutine(GatheringProcess());
            
            Debug.Log($"[GatheringInteractable] Started gathering {InteractionPrompt}");
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

            Debug.Log($"[GatheringInteractable] Gathering complete!");
            
            // Add items to inventory
            if (currentPlayer != null && resourceItem != null)
            {
                InventoryManager inventory = currentPlayer.GetInventoryManager();
                if (inventory != null)
                {
                    bool added = inventory.AddItem(resourceItem, resourcesPerGather);
                    if (added)
                    {
                        ShowCompletionNotification();
                    }
                }
            }
            
            // Play completion sound
            if (gatherCompleteSound != null)
            {
                AudioSource.PlayClipAtPoint(gatherCompleteSound, transform.position);
            }
            
            // Deplete resource
            if (!isMultiUse)
            {
                isDepleted = true;
                DestroyResource();
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

            Debug.Log($"[GatheringInteractable] Gathering cancelled: {reason}");
            
            // Play cancel sound
            if (gatherCancelSound != null)
            {
                AudioSource.PlayClipAtPoint(gatherCancelSound, transform.position);
            }
            
            // Show notification
            Debug.Log("Gathering cancelled!");
            
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
            // For single-use resources, destroy the object
            Destroy(gameObject, 0.5f); // Small delay for sound to play
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
            string message = resourcesPerGather > 1
                ? $"Collected {resourcesPerGather}x {resourceItem.itemName}"
                : $"Collected {resourceItem.itemName}";
            
            Debug.Log(message);
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

        private void OnDrawGizmos()
        {
            Color gizmoColor = isDepleted ? Color.gray : (isCurrentlyGathering ? Color.green : Color.yellow);
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            string label = resourceItem != null 
                ? $"{resourceItem.itemName} x{resourcesPerGather}\n{gatherDuration}s gather"
                : "Gathering Node";
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        }

        #endregion
    }
}
