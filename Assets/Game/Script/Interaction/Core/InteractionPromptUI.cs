using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.UI;

namespace Game.Interaction.UI
{
    /// <summary>
    /// UI component that displays interaction prompts to the player.
    /// Shows messages like "[E] Pick up Wooden Log" at bottom-center of screen.
    /// Uses DOTween for optimized, smooth animations.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Detection Reference")]
        [SerializeField] private InteractionDetector interactionDetector;
        [SerializeField] private bool autoFindDetector = true;
        
        private IEventBus eventBus;

        [Header("UI References")]
        [SerializeField] private GameObject promptContainer;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject progressBarContainer;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private TextMeshProUGUI progressPercentText;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        [SerializeField] private float pulseDuration = 1f;
        [SerializeField] private float pulseScaleAmount = 0.05f;
        [SerializeField] private Ease pulseEase = Ease.InOutSine;

        [Header("Formatting")]
        [SerializeField] private string keyFormat = "[{0}]"; // Format for key display
        //[SerializeField] private string promptFormat = "{0} {1}"; // "{verb} {action}"

        private IInteractable currentInteractable;
        private bool isVisible = false;
        private bool isShowingProgress = false;
        private Tweener fadeTween;
        private Tweener pulseTween;

        private void Awake()
        {
            // Auto-find InteractionDetector if not assigned
            if (interactionDetector == null && autoFindDetector)
            {
                interactionDetector = ServiceContainer.Instance.TryGet<InteractionDetector>();
                if (interactionDetector == null)
                {
                    Debug.LogWarning("[InteractionPromptUI] No InteractionDetector found in ServiceContainer. Prompt UI will not function.");
                }
            }
            
            // Get EventBus from ServiceContainer (SOLID: Dependency Inversion)
            eventBus = ServiceContainer.Instance.TryGet<IEventBus>();

            // Auto-assign if not set
            if (promptContainer == null)
                promptContainer = gameObject;
            
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Start hidden
            canvasGroup.alpha = 0f;
            promptContainer.SetActive(false);
            
            // Hide progress bar initially
            if (progressBarContainer != null)
            {
                progressBarContainer.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Subscribe to interaction events from the detector instance
            if (interactionDetector != null)
            {
                interactionDetector.OnNearestInteractableChanged += HandleInteractableChanged;
                interactionDetector.OnInteractableInRange += HandleInteractableInRange;
            }
            
            // Subscribe to panel events via EventBus (SOLID: Observer pattern via EventBus)
            if (eventBus != null)
            {
                eventBus.Subscribe<PanelOpenedEvent>(HandlePanelOpened);
                eventBus.Subscribe<PanelClosedEvent>(HandlePanelClosed);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (interactionDetector != null)
            {
                interactionDetector.OnNearestInteractableChanged -= HandleInteractableChanged;
                interactionDetector.OnInteractableInRange -= HandleInteractableInRange;
            }
            
            // Unsubscribe from EventBus panel events
            if (eventBus != null)
            {
                eventBus.Unsubscribe<PanelOpenedEvent>(HandlePanelOpened);
                eventBus.Unsubscribe<PanelClosedEvent>(HandlePanelClosed);
            }
            
            // Kill all active tweens to prevent errors
            KillAllTweens();
        }
        
        private void OnDestroy()
        {
            // Ensure tweens are cleaned up
            KillAllTweens();
        }
        
        private void KillAllTweens()
        {
            fadeTween?.Kill();
            pulseTween?.Kill();
        }

        private void HandleInteractableChanged(IInteractable interactable)
        {
            currentInteractable = interactable;
            //Debug.Log($"[InteractionPromptUI] Interactable changed: {interactable?.InteractionPrompt ?? "null"}");
            UpdatePromptText();
        }

        private void HandleInteractableInRange(bool inRange)
        {
            //Debug.Log($"[InteractionPromptUI] Interactable in range: {inRange}, current: {currentInteractable?.InteractionPrompt ?? "null"}");
            if (inRange && currentInteractable != null)
            {
                ShowPrompt();
            }
            else
            {
                HidePrompt();
            }
        }
        
        /// <summary>
        /// Handle panel opened event - hide prompt immediately
        /// SOLID: Observer pattern via EventBus for reactive UI behavior
        /// </summary>
        private void HandlePanelOpened(PanelOpenedEvent evt)
        {
            // Immediately hide prompt when any panel opens
            HidePrompt();
            //Debug.Log("[InteractionPromptUI] Hiding prompt due to panel opened: " + evt.PanelName);
        }
        
        /// <summary>
        /// Handle panel closed event - show prompt if interactable still in range
        /// SOLID: Observer pattern via EventBus
        /// </summary>
        private void HandlePanelClosed(PanelClosedEvent evt)
        {
            //Debug.Log($"[InteractionPromptUI] Panel closed: {evt.PanelName}");
            
            // Check if we should show prompt again when panel closes
            if (currentInteractable != null && interactionDetector != null)
            {
                var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
                bool anyPanelOpen = uiService != null && uiService.IsAnyPanelOpen();
                var nearestInteractable = interactionDetector.NearestInteractable;
                
                //Debug.Log($"[InteractionPromptUI] Current: {currentInteractable?.InteractionPrompt ?? "null"}, Nearest: {nearestInteractable?.InteractionPrompt ?? "null"}, AnyPanelOpen: {anyPanelOpen}");
                
                // Only show if no other panels are open and interactable still in range
                if (uiService != null && !anyPanelOpen)
                {
                    if (nearestInteractable == currentInteractable)
                    {
                        //Debug.Log("[InteractionPromptUI] Attempting to show prompt after panel closed");
                        ShowPrompt();
                    }
                    else
                    {
                        //Debug.Log("[InteractionPromptUI] Nearest interactable changed, not showing prompt");
                    }
                }
                else
                {
                    //Debug.Log("[InteractionPromptUI] Not showing: UIService null or another panel is open");
                }
            }
            else
            {
                //Debug.Log($"[InteractionPromptUI] Not showing: currentInteractable={currentInteractable != null}, detector={interactionDetector != null}");
            }
        }

        private void UpdatePromptText()
        {
            if (currentInteractable == null || promptText == null)
                return;

            // Format: "[E] Press E to Pick up Wooden Log"
            string verb = currentInteractable.InteractionVerb;
            string action = currentInteractable.InteractionPrompt;
            
            // Add key indicator
            string keyIndicator = string.Format(keyFormat, "F");
            
            // Combine into full prompt
            string fullPrompt = $"{keyIndicator} {verb} {action}";
            
            promptText.text = fullPrompt;
        }

        private void ShowPrompt()
        {
            //Debug.Log($"[InteractionPromptUI] ShowPrompt called, isVisible={isVisible}");
            
            if (isVisible)
            {
                //Debug.Log("[InteractionPromptUI] Already visible, skipping");
                return;
            }
            
            // Don't show prompt if any UI panels are open (SOLID: SRP - self-manages visibility)
            var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
            bool anyPanelOpen = uiService != null && uiService.IsAnyPanelOpen();
            
            //Debug.Log($"[InteractionPromptUI] UIService found: {uiService != null}, AnyPanelOpen: {anyPanelOpen}");
            
            if (anyPanelOpen)
            {
                //Debug.Log("[InteractionPromptUI] Panel is open, not showing prompt");
                return;
            }

            //Debug.Log($"[InteractionPromptUI] Showing prompt: {currentInteractable?.InteractionPrompt}");
            isVisible = true;
            promptContainer.SetActive(true);
            UpdatePromptText();

            // Kill any existing fade tween
            fadeTween?.Kill();
            
            // Fade in with DOTween
            fadeTween = canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(StartPulseAnimation);
        }

        private void HidePrompt()
        {
            //Debug.Log($"[InteractionPromptUI] HidePrompt called, isVisible={isVisible}");
            
            if (!isVisible) return;

            isVisible = false;

            // Kill any existing tweens
            fadeTween?.Kill();
            pulseTween?.Kill();
            
            // Fade out with DOTween
            fadeTween = canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() => 
                {
                    promptContainer.SetActive(false);
                    promptContainer.transform.localScale = Vector3.one; // Reset scale
                });
        }
        
        private void StartPulseAnimation()
        {
            if (!isVisible || promptContainer == null) return;
            
            // Kill existing pulse
            pulseTween?.Kill();
            
            // Create looping scale pulse animation
            pulseTween = promptContainer.transform
                .DOScale(1f + pulseScaleAmount, pulseDuration)
                .SetEase(pulseEase)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// Manually show a custom prompt (useful for special cases)
        /// </summary>
        public void ShowCustomPrompt(string text)
        {
            if (promptText != null)
            {
                promptText.text = text;
                ShowPrompt();
            }
        }

        /// <summary>
        /// Manually hide the prompt instantly without fade animation
        /// </summary>
        public void ForceHide()
        {
            if (!isVisible) return;

            isVisible = false;

            // Kill all tweens immediately
            fadeTween?.Kill();
            pulseTween?.Kill();

            // Instantly hide without animation
            canvasGroup.alpha = 0f;
            promptContainer.transform.localScale = Vector3.one;
            promptContainer.SetActive(false);
            
            // Hide progress bar
            HideProgressBar();
        }
        
        /// <summary>
        /// Show progress bar for gathering interactions
        /// </summary>
        public void ShowProgressBar()
        {
            if (progressBarContainer != null)
            {
                isShowingProgress = true;
                progressBarContainer.SetActive(true);
                
                // Reset progress
                if (progressBarFill != null)
                    progressBarFill.fillAmount = 0f;
                
                if (progressPercentText != null)
                    progressPercentText.text = "0%";
                
                // Stop pulse animation when showing progress
                pulseTween?.Kill();
                promptContainer.transform.localScale = Vector3.one;
            }
        }
        
        /// <summary>
        /// Update progress bar fill amount (0-1)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            if (!isShowingProgress || progressBarFill == null)
                return;
            
            progress = Mathf.Clamp01(progress);
            progressBarFill.fillAmount = progress;
            
            if (progressPercentText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                progressPercentText.text = $"{percentage}%";
            }
        }
        
        /// <summary>
        /// Hide progress bar
        /// </summary>
        public void HideProgressBar()
        {
            if (progressBarContainer != null)
            {
                isShowingProgress = false;
                progressBarContainer.SetActive(false);
                
                // Resume pulse animation if still visible
                if (isVisible)
                {
                    StartPulseAnimation();
                }
            }
        }
    }
}
