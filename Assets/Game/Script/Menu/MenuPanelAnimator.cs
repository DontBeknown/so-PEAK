using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.Menu
{
    /// <summary>
    /// Helper class for menu UI transitions and animations
    /// Add this component to panels that need fade or scale animations
    /// Uses DOTween for smooth animations
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MenuPanelAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        [SerializeField] private Ease scaleEase = Ease.OutBack;
        [SerializeField] private bool fadeOnEnable = true;
        [SerializeField] private bool scaleOnEnable = false;

        [Header("Animation Values")]
        [SerializeField] private Vector3 startScale = Vector3.one * 0.9f;
        [SerializeField] private Vector3 targetScale = Vector3.one;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Sequence currentAnimation;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (fadeOnEnable || scaleOnEnable)
            {
                PlayShowAnimation();
            }
        }

        private void OnDisable()
        {
            // Kill any active animations when disabled
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
                currentAnimation = null;
            }
        }

        public void PlayShowAnimation()
        {
            // Kill any existing animation
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
            }

            // Initialize starting values
            if (fadeOnEnable)
            {
                canvasGroup.alpha = 0f;
            }

            if (scaleOnEnable)
            {
                rectTransform.localScale = startScale;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Create animation sequence
            currentAnimation = DOTween.Sequence();

            if (fadeOnEnable)
            {
                currentAnimation.Join(canvasGroup.DOFade(1f, animationDuration).SetEase(fadeEase));
            }

            if (scaleOnEnable)
            {
                currentAnimation.Join(rectTransform.DOScale(targetScale, animationDuration).SetEase(scaleEase));
            }

            currentAnimation.OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                currentAnimation = null;
            });

            currentAnimation.SetLink(gameObject);
        }

        public void PlayHideAnimation(System.Action onComplete = null)
        {
            // Kill any existing animation
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Create animation sequence
            currentAnimation = DOTween.Sequence();

            if (fadeOnEnable)
            {
                currentAnimation.Join(canvasGroup.DOFade(0f, animationDuration).SetEase(fadeEase));
            }

            if (scaleOnEnable)
            {
                currentAnimation.Join(rectTransform.DOScale(startScale, animationDuration).SetEase(scaleEase));
            }

            currentAnimation.OnComplete(() =>
            {
                currentAnimation = null;
                onComplete?.Invoke();
            });

            currentAnimation.SetLink(gameObject);
        }

        /// <summary>
        /// Instantly show the panel without animation
        /// </summary>
        public void Show()
        {
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
                currentAnimation = null;
            }

            canvasGroup.alpha = 1f;
            rectTransform.localScale = targetScale;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Instantly hide the panel without animation
        /// </summary>
        public void Hide()
        {
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
                currentAnimation = null;
            }

            canvasGroup.alpha = 0f;
            rectTransform.localScale = startScale;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
