using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Adapter component that wraps the existing ResourceCollector class
    /// to make it compatible with the new IInteractable system.
    /// This provides backwards compatibility during migration.
    /// 
    /// Usage: Add this component alongside ResourceCollector on existing objects.
    /// </summary>
    [RequireComponent(typeof(ResourceCollector))]
    public class ResourceCollectorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Adapter Settings")]
        [SerializeField] private ResourceCollector resourceCollector;
        [SerializeField] private float interactionPriority = 1f;
        [SerializeField] private string interactionVerb = "Press E to";
        
        [Header("Optional Overrides")]
        [SerializeField] private bool useCustomPrompt = false;
        [SerializeField] private string customPrompt = "";

        #region IInteractable Implementation

        public string InteractionPrompt
        {
            get
            {
                if (useCustomPrompt && !string.IsNullOrEmpty(customPrompt))
                    return customPrompt;
                
                if (resourceCollector != null)
                {
                    return $"Pick up {resourceCollector.GetDisplayName()}";
                }
                
                return "Pick up Item";
            }
        }

        public string InteractionVerb => interactionVerb;

        public bool CanInteract => resourceCollector != null && resourceCollector.CanBeCollected;

        public float InteractionPriority => interactionPriority;

        public Transform GetTransform() => transform;

        public void OnHighlighted(bool highlighted)
        {
            // Delegate to ResourceCollector's existing highlight system
            if (resourceCollector != null)
            {
                resourceCollector.SetHighlighted(highlighted);
            }
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!CanInteract)
                return;

            // Get inventory manager from player
            InventoryManager inventoryManager = player.GetInventoryManager();
            if (inventoryManager == null)
            {
                Debug.LogError("[ResourceCollectorInteractable] Player has no InventoryManager!");
                return;
            }

            // Use ResourceCollector's existing collection logic
            bool collected = resourceCollector.CollectResource(inventoryManager);
            
            if (!collected)
            {
                Debug.LogWarning("[ResourceCollectorInteractable] Failed to collect resource (inventory full?)");
            }
        }

        #endregion

        private void Awake()
        {
            // Auto-assign ResourceCollector if not set
            if (resourceCollector == null)
            {
                resourceCollector = GetComponent<ResourceCollector>();
            }

            if (resourceCollector == null)
            {
                Debug.LogError("[ResourceCollectorInteractable] No ResourceCollector component found! This adapter requires ResourceCollector.");
            }
        }

        private void OnValidate()
        {
            // Auto-assign in editor
            if (resourceCollector == null)
            {
                resourceCollector = GetComponent<ResourceCollector>();
            }
        }

        #region Editor Helpers

        private void OnDrawGizmos()
        {
            // Show adapter status in editor
            if (resourceCollector != null && resourceCollector.CanBeCollected)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f);
            }
        }

        #endregion
    }
}
