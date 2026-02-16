using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Game.Menu
{
    /// <summary>
    /// Loading panel UI that displays during world loading
    /// Shows animated loading spinner and progress text
    /// </summary>
    public class LoadingPanelUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private RectTransform spinnerTransform;
        [SerializeField] private Image progressBar;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float spinnerRotationSpeed = 360f; // Degrees per second
        [SerializeField] private Ease fadeEase = Ease.OutQuad;

        [Header("Loading Messages")]
        [SerializeField] private string[] loadingMessages = new string[]
        {
            "Loading world...",
            "Generating terrain...",
            "Preparing environment...",
            "Almost there..."
        };
        [SerializeField] private float messageChangeInterval = 1.5f;

        private Sequence fadeSequence;
        private Tweener spinnerTween;
        private Sequence messageTween;
        private int currentMessageIndex = 0;
        private bool isLoading = false;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            //loadingPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // Clean up all tweens
            if (fadeSequence != null && fadeSequence.IsActive())
            {
                fadeSequence.Kill();
            }
            if (spinnerTween != null && spinnerTween.IsActive())
            {
                spinnerTween.Kill();
            }
            if (messageTween != null && messageTween.IsActive())
            {
                messageTween.Kill();
            }
        }

        /// <summary>
        /// Show the loading panel with fade-in animation
        /// </summary>
        public void Show()
        {
            
            loadingPanel.SetActive(true);
            isLoading = true;
            currentMessageIndex = 0;

            // Kill any existing fade animation
            if (fadeSequence != null && fadeSequence.IsActive())
            {
                fadeSequence.Kill();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true; // Block raycasts while loading

            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }

            if (loadingText != null && loadingMessages.Length > 0)
            {
                loadingText.text = loadingMessages[0];
            }

            // Fade in
            /*fadeSequence = DOTween.Sequence();
            fadeSequence.Append(canvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));
            fadeSequence.SetLink(gameObject);*/

            // Start spinner rotation
            //StartSpinnerAnimation();

            // Start cycling through loading messages
            if (loadingMessages.Length > 1)
            {
                StartMessageCycle();
            }
        }

        /// <summary>
        /// Hide the loading panel with fade-out animation
        /// </summary>
        public void Hide(System.Action onComplete = null)
        {
            isLoading = false;

            // Stop animations
            StopSpinnerAnimation();
            StopMessageCycle();

            // Kill any existing fade animation
            if (fadeSequence != null && fadeSequence.IsActive())
            {
                fadeSequence.Kill();
            }

            // Fade out
            fadeSequence = DOTween.Sequence();
            fadeSequence.Append(canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase));
            fadeSequence.OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                loadingPanel.SetActive(false);
                onComplete?.Invoke();
            });
            fadeSequence.SetLink(gameObject);
        }

        /// <summary>
        /// Update progress bar (0-1 range)
        /// </summary>
        public void SetProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Set a custom loading message
        /// </summary>
        public void SetLoadingMessage(string message)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        private void StartSpinnerAnimation()
        {
            if (spinnerTransform == null) return;

            // Kill existing spinner animation
            if (spinnerTween != null && spinnerTween.IsActive())
            {
                spinnerTween.Kill();
            }

            // Rotate spinner continuously
            spinnerTween = spinnerTransform.DORotate(new Vector3(0, 0, -360f), 360f / spinnerRotationSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        private void StopSpinnerAnimation()
        {
            if (spinnerTween != null && spinnerTween.IsActive())
            {
                spinnerTween.Kill();
            }
        }

        private void StartMessageCycle()
        {
            if (loadingText == null || loadingMessages.Length <= 1) return;

            CycleMessage();
        }

        private void CycleMessage()
        {
            if (!isLoading || loadingText == null) return;

            // Kill existing message tween
            if (messageTween != null && messageTween.IsActive())
            {
                messageTween.Kill();
            }

            // Create sequence for message fade out, change, and fade in
            Sequence messageSequence = DOTween.Sequence();
            
            // Fade out current message
            messageSequence.Append(loadingText.DOFade(0f, 0.3f));
            
            // Change message
            messageSequence.AppendCallback(() =>
            {
                currentMessageIndex = (currentMessageIndex + 1) % loadingMessages.Length;
                if (loadingText != null)
                {
                    loadingText.text = loadingMessages[currentMessageIndex];
                }
            });
            
            // Fade in new message
            messageSequence.Append(loadingText.DOFade(1f, 0.3f));
            
            // Wait before next cycle
            messageSequence.AppendInterval(messageChangeInterval);
            
            // Schedule next message change
            messageSequence.OnComplete(() =>
            {
                if (isLoading)
                {
                    CycleMessage();
                }
            });
            
            messageSequence.SetLink(gameObject);
            messageTween = messageSequence;
        }

        private void StopMessageCycle()
        {
            if (messageTween != null && messageTween.IsActive())
            {
                messageTween.Kill();
            }
        }
    }
}
