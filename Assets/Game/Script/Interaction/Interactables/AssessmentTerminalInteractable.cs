using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using Game.Core.DI;
using Game.Core.Events;
using Game.Environment.DayNight;
using Game.UI;

namespace Game.Interaction
{
    /// <summary>
    /// Interactable that opens the Assessment Report UI when the player presses E.
    /// Can be attached to terminal objects, signs, or any 3D object in the world.
    /// </summary>
    public class AssessmentTerminalInteractable : HoldInteractableBase
    {
        [Header("Interaction Settings")]
        [SerializeField] private string customPrompt = "Check Assessment Report";
        [SerializeField] private string interactionVerb = "Hold F to";
        [FormerlySerializedAs("interactionPriority")]
        [SerializeField] private float terminalInteractionPriority = 0.8f; // Slightly lower than items
        
        [Header("Player Control")]
        [SerializeField] private bool lockPlayerInputDuringUI = true;
        [SerializeField] private bool pauseGameDuringUI = false;
        
        [Header("Visual Feedback")]
        [FormerlySerializedAs("highlightEffect")]
        [SerializeField] private GameObject terminalHighlightEffect;
        [SerializeField] private AudioClip interactSound;
        [SerializeField] private Color highlightColor = Color.cyan;
        
        [Header("Cooldown (Optional)")]
        [SerializeField] private bool useCooldown = false;
        [SerializeField] private float cooldownTime = 2f;
        
        [Header("Terminal Behaviour")]
        [Tooltip("If true, this terminal can only be interacted with once.")]
        [SerializeField] private bool oneTimeUse = false;
        [Tooltip("If true, closing the assessment UI will skip time to the next morning.")]
        [SerializeField] private bool skipDayOnUse = true;
        [Tooltip("If true, closing the assessment UI will fully reset the player's fatigue stat.")]
        [SerializeField] private bool resetFatigueOnUse = true;
        [Tooltip("If true, closing the assessment UI will heal the player by the configured amount.")]
        [SerializeField] private bool healOnUse = false;
        [Min(0f)]
        [SerializeField] private float healAmountOnUse = 25f;
        [Tooltip("Optional custom spawn transform to save for the player. If null, current player position is used.")]
        [SerializeField] private Transform customSaveSpawnPoint;
        [SerializeField] private bool progressNextLevelOnUse = false; 
        
        private float lastInteractionTime = -999f;
        private bool _hasBeenUsed = false;
        private UIServiceProvider uiServiceProvider;
        private IDayNightCycleService _dayNightService;
        private IEventBus _eventBus;
        private bool _isWaitingForPanelClose = false;
        private bool _playerLockManagedByTerminal = false;

        #region IInteractable Implementation

        private void Awake()
        {
           
        }
        private void Start()
        {
            // Defer service initialization - they may not be ready yet when spawned dynamically
            // Services will be fetched on-demand in other methods
        }
        
        /// <summary>
        /// Ensures all required services are initialized. Called before use.
        /// </summary>
        private void EnsureServicesInitialized()
        {
            if (_dayNightService == null)
            {
                _dayNightService = ServiceContainer.Instance.TryGet<IDayNightCycleService>();
                if (_dayNightService == null)
                {
                    // Debug.LogWarning("[AssessmentTerminalInteractable] IDayNightCycleService not available yet");
                }
            }
            
            if (_eventBus == null)
            {
                _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
                if (_eventBus == null)
                {
                    // Debug.LogError("[AssessmentTerminalInteractable] IEventBus not found in ServiceContainer!");
                }
            }
        }

        public override string InteractionPrompt => customPrompt;

        public override string InteractionVerb => interactionVerb;

        public override bool CanInteract
        {
            get
            {
                if (uiServiceProvider == null){
                    uiServiceProvider = ServiceContainer.Instance.TryGet<UIServiceProvider>();
                    if (uiServiceProvider == null)
                    {
                        // Debug.LogError("[AssessmentTerminalInteractable] UIServiceProvider not found in ServiceContainer!");
                        return false;
                    }
                }


                if (isCurrentlyHolding || _isWaitingForPanelClose)
                    return false;
                
                // One-time-use terminals are permanently disabled after first use
                if (oneTimeUse && _hasBeenUsed)
                    return false;
                
                // Check cooldown
                if (useCooldown && Time.time < lastInteractionTime + cooldownTime)
                    return false;
                
                return true;
            }
        }

        public override float InteractionPriority => terminalInteractionPriority;

        public override void OnHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            
            // Toggle highlight effect
            if (terminalHighlightEffect != null)
            {
                terminalHighlightEffect.SetActive(highlighted);
            }
            
            // Optional: Change material color or emission
            // You can add more sophisticated highlighting here
        }

        #endregion

        #region Hold Interaction Overrides

        protected override void OnHoldComplete()
        {
            lastInteractionTime = Time.time;

            // Play interaction sound when hold finishes.
            if (interactSound != null)
            {
                AudioSource.PlayClipAtPoint(interactSound, transform.position);
            }

            if (pauseGameDuringUI)
            {
                Time.timeScale = 0f;
            }

            OpenAssessmentUI();

            // Ensure services are available before subscribing
            EnsureServicesInitialized();

            // Subscribe to panel close to trigger skip to next morning and cleanup.
            if (_eventBus != null && !_isWaitingForPanelClose)
            {
                _isWaitingForPanelClose = true;
                // Debug.Log("[AssessmentTerminalInteractable] Subscribing to PanelClosedEvent");
                _eventBus.Subscribe<PanelClosedEvent>(OnPanelClosed);
            }
            else if (_eventBus == null)
            {
                // Debug.LogError("[AssessmentTerminalInteractable] EventBus is still null! Will attempt to bind on next frame.");
                // Retry next frame - services may have initialized
                StartCoroutine(RetryEventSubscription());
            }

            // Hold base cleanup unlocks the player; re-apply lock for terminal UI usage.
            if (lockPlayerInputDuringUI && currentPlayer != null)
            {
                _playerLockManagedByTerminal = true;
                StartCoroutine(ReapplyPlayerLockNextFrame(currentPlayer));
            }
        }
        
        /// <summary>
        /// Retry subscribing to PanelClosedEvent if initial attempt failed
        /// </summary>
        private IEnumerator RetryEventSubscription()
        {
            yield return new WaitForSeconds(0.5f);
            
            EnsureServicesInitialized();
            
            if (_eventBus != null && !_isWaitingForPanelClose)
            {
                _isWaitingForPanelClose = true;
                // Debug.Log("[AssessmentTerminalInteractable] Successfully subscribed to PanelClosedEvent on retry");
                _eventBus.Subscribe<PanelClosedEvent>(OnPanelClosed);
            }
            else if (_eventBus == null)
            {
                // Debug.LogError("[AssessmentTerminalInteractable] EventBus still unavailable after retry!");
            }
        }

        #endregion

        private void OpenAssessmentUI()
        {
            if (uiServiceProvider == null)
            {
                // Debug.LogError("[AssessmentTerminalInteractable] UIServiceProvider not found!");
                UnlockPlayer();
                return;
            }
            
            // Open PlayerStatsTracker panel via UIServiceProvider (SOLID: Facade pattern)
            uiServiceProvider.OpenPanel("PlayerStatsTracker");
            
            // Publish event to communicate context to UI (progression mode enabled/disabled)
            if (_eventBus == null)
            {
               _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
               if (_eventBus == null){
                   // Debug.LogError("[AssessmentTerminalInteractable] EventBus not found in ServiceContainer!");
                   return;
                }
            }

            _eventBus.Publish(new AssessmentUIOpenedEvent("PlayerStatsTracker", progressNextLevelOnUse));
            // Debug.Log("[AssessmentTerminalInteractable] Stats tracker opened via UIServiceProvider");
        }

        /// <summary>
        /// Call this when the assessment UI is closed (e.g., from a close button)
        /// </summary>
        public void OnAssessmentUIClosed()
        {
            UnlockPlayer();
        }

        private IEnumerator ReapplyPlayerLockNextFrame(Game.Player.PlayerControllerRefactored player)
        {
            yield return null;

            if (_isWaitingForPanelClose && _playerLockManagedByTerminal && player != null)
            {
                player.SetInputBlocked(true);
            }
        }
        
        private void OnPanelClosed(PanelClosedEvent evt)
        {
            if (evt.PanelName != "PlayerStatsTracker") return;
            
            // Debug.Log($"[AssessmentTerminalInteractable] OnPanelClosed called for {evt.PanelName}");
            
            // Unsubscribe immediately so this only fires once per interaction
            _eventBus?.Unsubscribe<PanelClosedEvent>(OnPanelClosed);
            _isWaitingForPanelClose = false;

            // Debug.Log("[AssessmentTerminalInteractable] Panel closed event received.");
            
            // Ensure day/night service is available
            if (_dayNightService == null)
            {
                _dayNightService = ServiceContainer.Instance.TryGet<IDayNightCycleService>();
            }
            
            // Skip time to next morning only if configured
            if (skipDayOnUse)
            {
                // Debug.Log("[AssessmentTerminalInteractable] Skipping to next morning...");
                if(_dayNightService == null)
                {
                    // Debug.LogError("[AssessmentTerminalInteractable] DayNightCycleService not found in ServiceContainer!");
                    UnlockPlayer();
                    return;
                }
                _dayNightService?.SkipToNextMorning();
            }

            PlayerStats playerStats = null;
            if (currentPlayer != null)
            {
                playerStats = currentPlayer.GetComponent<PlayerStats>();
            }
            
            // Reset fatigue (rest) only if configured
            if (resetFatigueOnUse)
            {
                playerStats?.FullRest();
            }

            // Heal player only if configured
            if (healOnUse && healAmountOnUse > 0f)
            {
                playerStats?.Heal(healAmountOnUse);
            }

            // Save the game (captures updated day, time, stats, etc.)
            SaveLoadService.Instance?.PerformAutoSave(customSaveSpawnPoint);

            // Mark as used for one-time-use terminals
            if (oneTimeUse)
            {
                _hasBeenUsed = true;
                // Hide highlight so it no longer appears interactive
                if (terminalHighlightEffect != null)
                    terminalHighlightEffect.SetActive(false);
            }
            
            UnlockPlayer();
        }

        private void UnlockPlayer()
        {
            // Unlock player input
            if (_playerLockManagedByTerminal && currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(false);
            }
            _playerLockManagedByTerminal = false;
            
            // Unpause game
            if (pauseGameDuringUI)
            {
                Time.timeScale = 1f;
            }
        }

        protected override void OnDestroy()
        {
            // Defensive cleanup if object is destroyed while waiting for panel close.
            if (_eventBus != null && _isWaitingForPanelClose)
            {
                _eventBus.Unsubscribe<PanelClosedEvent>(OnPanelClosed);
            }

            _isWaitingForPanelClose = false;
            UnlockPlayer();
            base.OnDestroy();
        }

        #region Editor Helpers

        /*private void OnDrawGizmos()
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
        }*/

        #endregion
    }
}
