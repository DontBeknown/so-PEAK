using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
        [SerializeField] private string promptFormat = "{0} {1}"; // "{verb} {action}"

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
                interactionDetector = FindFirstObjectByType<InteractionDetector>();
                
                if (interactionDetector == null)
                {
                    Debug.LogWarning("[InteractionPromptUI] No InteractionDetector found in scene. Prompt UI will not function.");
                }
            }

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
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (interactionDetector != null)
            {
                interactionDetector.OnNearestInteractableChanged -= HandleInteractableChanged;
                interactionDetector.OnInteractableInRange -= HandleInteractableInRange;
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
            UpdatePromptText();
        }

        private void HandleInteractableInRange(bool inRange)
        {
            if (inRange && currentInteractable != null)
            {
                ShowPrompt();
            }
            else
            {
                HidePrompt();
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
            if (isVisible) return;

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
