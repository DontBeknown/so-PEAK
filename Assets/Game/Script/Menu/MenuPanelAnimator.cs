using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Game.Menu
{
    /// <summary>
    /// Helper class for menu UI transitions and animations
    /// Add this component to panels that need fade or scale animations
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class MenuPanelAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float fadeSpeed = 5f;
        [SerializeField] private float scaleSpeed = 5f;
        [SerializeField] private bool fadeOnEnable = true;
        [SerializeField] private bool scaleOnEnable = false;

        [Header("Animation Values")]
        [SerializeField] private Vector3 startScale = Vector3.one * 0.9f;
        [SerializeField] private Vector3 targetScale = Vector3.one;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Coroutine currentAnimation;

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

        public void PlayShowAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(ShowAnimation());
        }

        public void PlayHideAnimation(System.Action onComplete = null)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(HideAnimation(onComplete));
        }

        private IEnumerator ShowAnimation()
        {
            // Initialize
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

            // Animate
            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.deltaTime * fadeSpeed;

                if (fadeOnEnable)
                {
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                }

                if (scaleOnEnable)
                {
                    rectTransform.localScale = Vector3.Lerp(startScale, targetScale, progress);
                }

                yield return null;
            }

            // Finalize
            if (fadeOnEnable)
            {
                canvasGroup.alpha = 1f;
            }

            if (scaleOnEnable)
            {
                rectTransform.localScale = targetScale;
            }

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            currentAnimation = null;
        }

        private IEnumerator HideAnimation(System.Action onComplete)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.deltaTime * fadeSpeed;

                if (fadeOnEnable)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
                }

                if (scaleOnEnable)
                {
                    rectTransform.localScale = Vector3.Lerp(targetScale, startScale, progress);
                }

                yield return null;
            }

            // Finalize
            if (fadeOnEnable)
            {
                canvasGroup.alpha = 0f;
            }

            if (scaleOnEnable)
            {
                rectTransform.localScale = startScale;
            }

            currentAnimation = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Instantly show the panel without animation
        /// </summary>
        public void Show()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
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
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
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
