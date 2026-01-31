using UnityEngine;
using System.Collections.Generic;
using System;

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

        // Instance Events (each detector has its own events)
        public event Action<IInteractable> OnNearestInteractableChanged;
        public event Action<bool> OnInteractableInRange; // True when interactable is in range

        private List<IInteractable> interactablesInRange = new List<IInteractable>();
        private IInteractable nearestInteractable = null;
        private IInteractable previousNearestInteractable = null;
        private float updateTimer = 0f;

        // Properties
        public IInteractable NearestInteractable => nearestInteractable;
        public bool HasInteractableInRange => nearestInteractable != null;

        private void Awake()
        {
            if (detectionCenter == null)
                detectionCenter = transform;
        }

        private void Update()
        {
            // Optimize by updating at intervals
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateNearestInteractable();
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
                }
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
                }

                previousNearestInteractable = nearestInteractable;
                nearestInteractable = newNearestInteractable;

                // Highlight new nearest interactable
                if (nearestInteractable != null)
                {
                    nearestInteractable.OnHighlighted(true);
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
        }
    }
}
