using UnityEngine;
using System.Collections.Generic;
using System;
using Game.Interaction.UI;
using Game.UI;

namespace Game.Interaction
{
    /// <summary>
    /// Detects and manages nearby interactable objects.
    /// Refactored from ItemDetector to support all IInteractable types.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 2.5f;
        [SerializeField] private LayerMask interactableLayerMask = -1; // Which layers to detect interactables on
        [SerializeField] private Transform detectionCenter; // Optional custom center point
        [SerializeField] private float updateInterval = 0.1f; // Optimize performance

        [Header("Visual Feedback")]
        [SerializeField] private bool enableGizmos = true;
        [SerializeField] private Color gizmoColorWithTarget = Color.green;
        [SerializeField] private Color gizmoColorNoTarget = Color.yellow;
        
        [Header("UI Markers")]
        [SerializeField] private bool enableUIMarkers = true;
        [SerializeField] private bool showMarkersOnlyWhenStill = true;
        [SerializeField] private float stillDelayBeforeShowingMarkers = 2.5f; // Seconds to wait before showing markers
        [SerializeField] private float movementThreshold = 0.1f; // Minimum movement to be considered "moving"
        [SerializeField] private Sprite markerSprite; // Optional custom marker sprite
        [SerializeField] private Canvas markerCanvas; // Canvas to spawn markers on (auto-finds if null)
        [SerializeField] private Transform markerContainer; // Container for markers (auto-creates if null)
        [SerializeField] private Color markerInRangeColor = new Color(1f, 1f, 0f, 0.7f); // Yellow
        [SerializeField] private Color markerSelectedColor = new Color(0f, 1f, 0f, 1f); // Green
        [SerializeField] private Color markerDepletedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grey

        // Instance Events (each detector has its own events)
        public event Action<IInteractable> OnNearestInteractableChanged;
        public event Action<bool> OnInteractableInRange; // True when interactable is in range

        private List<IInteractable> interactablesInRange = new List<IInteractable>();
        private IInteractable nearestInteractable = null;
        private IInteractable previousNearestInteractable = null;
        private float updateTimer = 0f;
        private Dictionary<IInteractable, InteractableUIMarker> markerMap = new Dictionary<IInteractable, InteractableUIMarker>();
        private CharacterController characterController;
        private UIServiceProvider uiServiceProvider;
        private float stillTimer = 0f; // Tracks how long player has been standing still
        private bool isEnabled = true; // Can be disabled during certain actions (like gathering)

        // Properties
        public IInteractable NearestInteractable => nearestInteractable;
        public bool HasInteractableInRange => nearestInteractable != null;
        public bool IsEnabled => isEnabled;

        private void Awake()
        {
            if (detectionCenter == null)
                detectionCenter = transform;
            
            // Get player reference
            characterController = GetComponent<CharacterController>();
            uiServiceProvider = UIServiceProvider.Instance;
            
            // Auto-find canvas for markers
            if (enableUIMarkers && markerCanvas == null)
            {
                markerCanvas = FindFirstObjectByType<Canvas>();
                if (markerCanvas == null)
                {
                    Debug.LogWarning("[InteractionDetector] UI Markers enabled but no Canvas found. Markers will be disabled.");
                    enableUIMarkers = false;
                }
            }
            
            // Create marker container if not assigned
            if (enableUIMarkers && markerCanvas != null && markerContainer == null)
            {
                GameObject containerObj = new GameObject("InteractionMarkers");
                markerContainer = containerObj.transform;
                markerContainer.SetParent(markerCanvas.transform, false);
            }
        }

        private void Update()
        {
            // Skip update if detector is disabled
            if (!isEnabled)
                return;
            
            // Optimize by updating at intervals
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateNearestInteractable();
            }
            
            // Update marker visibility based on player movement
            if (enableUIMarkers && showMarkersOnlyWhenStill)
            {
                UpdateMarkerVisibilityBasedOnMovement();
            }
        }

        private void UpdateNearestInteractable()
        {
            // Clear the list and find all interactables in range
            interactablesInRange.Clear();

            Collider[] colliders = Physics.OverlapSphere(detectionCenter.position, detectionRadius, interactableLayerMask);

            foreach (var collider in colliders)
            {
                IInteractable interactable = collider.GetComponent<IInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    interactablesInRange.Add(interactable);
                    
                    // Create marker if UI markers are enabled and marker doesn't exist
                    if (enableUIMarkers && !markerMap.ContainsKey(interactable))
                    {
                        CreateMarkerForInteractable(interactable);
                    }
                }
            }
            
            // Remove markers for interactables no longer in range
            if (enableUIMarkers)
            {
                CleanupOutOfRangeMarkers();
            }

            // Find the nearest interactable with priority system
            IInteractable newNearestInteractable = null;
            float nearestScore = float.MaxValue;

            foreach (var interactable in interactablesInRange)
            {
                float distance = Vector3.Distance(detectionCenter.position, interactable.GetTransform().position);
                
                // Calculate score (lower is better): distance - priority bonus
                float score = distance - (interactable.InteractionPriority * 0.5f);
                
                if (score < nearestScore)
                {
                    nearestScore = score;
                    newNearestInteractable = interactable;
                }
            }

            // Update nearest interactable if it changed
            if (newNearestInteractable != nearestInteractable)
            {
                // Remove highlight from previous interactable
                if (nearestInteractable != null)
                {
                    nearestInteractable.OnHighlighted(false);
                    
                    // Update marker to "in range" state
                    if (enableUIMarkers && markerMap.ContainsKey(nearestInteractable))
                    {
                        markerMap[nearestInteractable].ShowInRange();
                    }
                }

                previousNearestInteractable = nearestInteractable;
                nearestInteractable = newNearestInteractable;

                // Highlight new nearest interactable
                if (nearestInteractable != null)
                {
                    nearestInteractable.OnHighlighted(true);
                    
                    // Update marker to "selected" state
                    if (enableUIMarkers && markerMap.ContainsKey(nearestInteractable))
                    {
                        markerMap[nearestInteractable].ShowSelected();
                    }
                }

                // Trigger events
                OnNearestInteractableChanged?.Invoke(nearestInteractable);
                OnInteractableInRange?.Invoke(nearestInteractable != null);
            }
        }

        /// <summary>
        /// Attempts to interact with the nearest interactable object
        /// </summary>
        /// <returns>True if interaction was successful</returns>
        public bool TryInteractWithNearest()
        {
            if (nearestInteractable == null)
            {
                return false;
            }

            if (!nearestInteractable.CanInteract)
            {
                return false;
            }

            // Get player reference
            var player = GetComponent<Game.Player.PlayerControllerRefactored>();
            if (player == null)
            {
                Debug.LogError("[InteractionDetector] No PlayerControllerRefactored found on this GameObject!");
                return false;
            }

            // Execute interaction
            nearestInteractable.Interact(player);

            // Force update to find next nearest interactable
            UpdateNearestInteractable();

            return true;
        }

        /// <summary>
        /// Gets all interactables currently in range
        /// </summary>
        public List<IInteractable> GetInteractablesInRange()
        {
            return new List<IInteractable>(interactablesInRange);
        }

        /// <summary>
        /// Force an immediate update of nearest interactable
        /// </summary>
        public void ForceUpdate()
        {
            UpdateNearestInteractable();
        }
        
        /// <summary>
        /// Disable detection temporarily (e.g., during gathering interactions)
        /// </summary>
        public void DisableDetection()
        {
            if (!isEnabled) return;
            
            isEnabled = false;
            
            // Clear current highlight
            if (nearestInteractable != null)
            {
                nearestInteractable.OnHighlighted(false);
            }
            
            // Hide all markers
            if (enableUIMarkers)
            {
                foreach (var kvp in markerMap)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.HideInstant();
                    }
                }
            }
            
            // Notify that we lost the interactable
            if (nearestInteractable != null)
            {
                nearestInteractable = null;
                OnNearestInteractableChanged?.Invoke(null);
                OnInteractableInRange?.Invoke(false);
            }
        }
        
        /// <summary>
        /// Re-enable detection after it was disabled
        /// </summary>
        public void EnableDetection()
        {
            if (isEnabled) return;
            
            isEnabled = true;
            
            // Force immediate update to find new nearest interactable
            ForceUpdate();
        }
        
        private void CreateMarkerForInteractable(IInteractable interactable)
        {
            if (markerCanvas == null || markerContainer == null) return;
            
            InteractableUIMarker marker = InteractableUIMarker.CreateMarker(
                interactable.GetTransform(),
                markerSprite,
                markerCanvas,
                markerInRangeColor,
                markerSelectedColor,
                markerDepletedColor
            );
            
            if (marker != null)
            {                
                marker.transform.SetParent(markerContainer, false);                
                markerMap[interactable] = marker;
                marker.ShowInRange(); // Start in "in range" state
            }
        }
        
        private void CleanupOutOfRangeMarkers()
        {
            // Find markers that are no longer in range
            List<IInteractable> toRemove = new List<IInteractable>();
            
            foreach (var kvp in markerMap)
            {
                if (!interactablesInRange.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            // Remove and destroy markers
            foreach (var interactable in toRemove)
            {
                if (markerMap.ContainsKey(interactable))
                {
                    InteractableUIMarker marker = markerMap[interactable];
                    if (marker != null)
                    {
                        marker.Hide();
                        Destroy(marker.gameObject, 0.5f); // Delay to allow fade out
                    }
                    markerMap.Remove(interactable);
                }
            }
        }
        
        private void UpdateMarkerVisibilityBasedOnMovement()
        {
            bool isPlayerMoving = IsPlayerMoving();
            bool isMenuOpen = uiServiceProvider != null && uiServiceProvider.IsAnyPanelOpen();

            //Debug.Log($"[InteractionDetector] Player moving: {isPlayerMoving}, Menu open: {isMenuOpen}, Still timer: {stillTimer}");            
            // Update still timer
            if (isPlayerMoving)
            {
                stillTimer = 0f; // Reset timer when moving
            }
            else
            {
                stillTimer += Time.deltaTime; // Increment timer when still
            }
            
            // Only show markers if player has been still long enough AND no menu is open
            bool shouldShowMarkers = !isPlayerMoving && !isMenuOpen && stillTimer >= stillDelayBeforeShowingMarkers;
            
            // Hide/show all markers based on movement and delay
            foreach (var kvp in markerMap)
            {
                InteractableUIMarker marker = kvp.Value;
                if (marker != null)
                {
                    if (shouldShowMarkers)
                    {
                        // Show marker with appropriate state
                        if (kvp.Key == nearestInteractable)
                        {
                            marker.ShowSelected();
                        }
                        else
                        {
                            marker.ShowInRange();
                        }
                    }
                    else
                    {
                        // Hide marker instantly if moving or delay not reached yet (no fade)
                        marker.HideInstant();
                    }
                }
            }
        }
        
        private bool IsPlayerMoving()
        {
            if (characterController == null) return false;
            
            if (characterController != null)
            {
                // CharacterController doesn't have velocity property, check if it moved in last frame
                Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
                return horizontalVelocity.magnitude > movementThreshold;
            }
            
            return false;
        }

        private void OnDrawGizmos()
        {
            if (!enableGizmos || detectionCenter == null) return;

            // Draw detection radius
            Gizmos.color = HasInteractableInRange ? gizmoColorWithTarget : gizmoColorNoTarget;
            Gizmos.DrawWireSphere(detectionCenter.position, detectionRadius);

            // Draw line to nearest interactable
            if (nearestInteractable != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(detectionCenter.position, nearestInteractable.GetTransform().position);
            }
        }

        private void OnDestroy()
        {
            // Clean up highlights on destroy
            if (nearestInteractable != null)
            {
                nearestInteractable.OnHighlighted(false);
            }
            
            // Cleanup all markers
            foreach (var kvp in markerMap)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            markerMap.Clear();
            
            // Cleanup marker container
            if (markerContainer != null)
            {
                Destroy(markerContainer.gameObject);
            }
        }
    }
}
