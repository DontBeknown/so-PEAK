using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private Transform progressBarSpawnPoint; // Where to show progress UI
        [SerializeField] private GameObject progressBarPrefab; // Optional custom progress bar prefab
        
        [Header("Audio")]
        [SerializeField] private AudioClip gatherStartSound;
        [SerializeField] private AudioClip gatherLoopSound;
        [SerializeField] private AudioClip gatherCompleteSound;
        [SerializeField] private AudioClip gatherCancelSound;
        
        [Header("Animation")]
        [SerializeField] private string playerGatherAnimationTrigger = "Gather";
        
        // State
        private bool isHighlighted = false;
        private bool isCurrentlyGathering = false;
        private bool isDepleted = false;
        private float respawnTimer = 0f;
        private float currentGatherProgress = 0f;
        
        // References
        private Game.Player.PlayerControllerRefactored currentPlayer;
        private Coroutine gatheringCoroutine;
        private Game.Interaction.UI.GatheringProgressUI currentProgressBar;
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

        public string InteractionVerb => "Hold E to";

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
            
            // Show progress bar
            ShowProgressBar();
            
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
                UpdateProgressBar(currentGatherProgress);
                
                yield return null;
            }
            
            // Gathering complete!
            CompleteGathering();
        }

        private void CheckGatheringInput()
        {
            // Check if E key is still held
            var keyboard = Keyboard.current;
            if (keyboard != null && !keyboard.eKey.isPressed)
            {
                // Player released E - cancel gathering
                CancelGathering("Released E button");
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
            HideProgressBar();
            
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

        #region Progress Bar

        private void ShowProgressBar()
        {
            Vector3 spawnPos = progressBarSpawnPoint != null 
                ? progressBarSpawnPoint.position 
                : transform.position + Vector3.up * 2f;
            
            // Create progress bar
            if (progressBarPrefab != null)
            {
                GameObject barObj = Instantiate(progressBarPrefab, spawnPos, Quaternion.identity);
                currentProgressBar = barObj.GetComponent<Game.Interaction.UI.GatheringProgressUI>();
            }
            else
            {
                // Create basic progress bar programmatically
                currentProgressBar = Game.Interaction.UI.GatheringProgressUI.CreateProgressBar(transform, null);
            }
            
            if (currentProgressBar != null)
            {
                // Setup with item info
                Sprite itemIcon = resourceItem != null ? resourceItem.icon : null;
                string itemName = resourceItem != null ? resourceItem.itemName : "Resource";
                
                currentProgressBar.Show(transform, itemName, itemIcon);
            }
        }

        private void UpdateProgressBar(float progress)
        {
            if (currentProgressBar != null)
            {
                currentProgressBar.UpdateProgress(progress);
            }
        }

        private void HideProgressBar()
        {
            if (currentProgressBar != null)
            {
                currentProgressBar.Hide();
                Destroy(currentProgressBar.gameObject, 0.5f); // Destroy after fade out
                currentProgressBar = null;
            }
        }

        #endregion

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
            
            // Draw progress bar spawn point
            if (progressBarSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(progressBarSpawnPoint.position, 0.2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPos = progressBarSpawnPoint != null 
                ? progressBarSpawnPoint.position 
                : transform.position + Vector3.up * 2f;
            
            string label = resourceItem != null 
                ? $"{resourceItem.itemName} x{resourcesPerGather}\n{gatherDuration}s gather"
                : "Gathering Node";
            
            UnityEditor.Handles.Label(labelPos, label);
        }

        #endregion
    }
}
