using UnityEngine;
using Game.Core.DI;
using Game.UI;

namespace Game.Interaction
{
    /// <summary>
    /// Interactable that opens the Assessment Report UI when the player presses E.
    /// Can be attached to terminal objects, signs, or any 3D object in the world.
    /// </summary>
    public class AssessmentTerminalInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [SerializeField] private string customPrompt = "Check Assessment Report";
        [SerializeField] private string interactionVerb = "Press F to";
        [SerializeField] private float interactionPriority = 0.8f; // Slightly lower than items
        
        [Header("Player Control")]
        [SerializeField] private bool lockPlayerInputDuringUI = true;
        [SerializeField] private bool pauseGameDuringUI = false;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject highlightEffect;
        [SerializeField] private AudioClip interactSound;
        [SerializeField] private Color highlightColor = Color.cyan;
        
        [Header("Cooldown (Optional)")]
        [SerializeField] private bool useCooldown = false;
        [SerializeField] private float cooldownTime = 2f;
        
        private bool isHighlighted = false;
        private float lastInteractionTime = -999f;
        private Game.Player.PlayerControllerRefactored currentPlayer;
        private UIServiceProvider uiServiceProvider;

        #region IInteractable Implementation

        private void Awake()
        {
            // Get UIServiceProvider from ServiceContainer
            uiServiceProvider = ServiceContainer.Instance.TryGet<UIServiceProvider>();
        }

        public string InteractionPrompt => customPrompt;

        public string InteractionVerb => interactionVerb;

        public bool CanInteract
        {
            get
            {
                if (uiServiceProvider == null)
                    return false;
                
                // Check cooldown
                if (useCooldown && Time.time < lastInteractionTime + cooldownTime)
                    return false;
                
                return true;
            }
        }

        public float InteractionPriority => interactionPriority;

        public Transform GetTransform() => transform;

        public void OnHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            
            // Toggle highlight effect
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(highlighted);
            }
            
            // Optional: Change material color or emission
            // You can add more sophisticated highlighting here
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!CanInteract)
                return;

            currentPlayer = player;
            lastInteractionTime = Time.time;
            
            // Play interaction sound
            if (interactSound != null)
            {
                AudioSource.PlayClipAtPoint(interactSound, transform.position);
            }
            
            // Lock player input if configured
            if (lockPlayerInputDuringUI && player != null)
            {
                player.SetInputBlocked(true);
            }
            
            // Pause game if configured
            if (pauseGameDuringUI)
            {
                Time.timeScale = 0f;
            }
            
            // Open assessment UI
            OpenAssessmentUI();
        }

        #endregion

        private void OpenAssessmentUI()
        {
            if (uiServiceProvider == null)
            {
                Debug.LogError("[AssessmentTerminalInteractable] UIServiceProvider not found!");
                UnlockPlayer();
                return;
            }
            
            // Open PlayerStatsTracker panel via UIServiceProvider (SOLID: Facade pattern)
            uiServiceProvider.OpenPanel("PlayerStatsTracker");
            Debug.Log("[AssessmentTerminalInteractable] Stats tracker opened via UIServiceProvider");
        }

        /// <summary>
        /// Call this when the assessment UI is closed (e.g., from a close button)
        /// </summary>
        public void OnAssessmentUIClosed()
        {
            UnlockPlayer();
        }

        private void UnlockPlayer()
        {
            // Unlock player input
            if (lockPlayerInputDuringUI && currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(false);
            }
            
            // Unpause game
            if (pauseGameDuringUI)
            {
                Time.timeScale = 1f;
            }
        }

        #region Editor Helpers

        private void OnDrawGizmos()
        {
            // Draw terminal icon in editor
            Gizmos.color = highlightColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
            // Draw interaction range indicator
            Gizmos.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 2.5f); // Default detection radius
        }

        private void OnDrawGizmosSelected()
        {
            // Draw label in editor
            UnityEditor.Handles.Label(transform.position + Vector3.up, "Assessment Terminal");
        }

        #endregion
    }
}
