using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Game.Menu
{
    /// <summary>
    /// Animates an underline element when hovering over a button
    /// Fades in/out and optionally scales the underline
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonUnderlineAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private CanvasGroup underlineCanvasGroup;
        [SerializeField] private RectTransform underlineTransform;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private Ease fadeEase = Ease.InOutQuad;
        [SerializeField] private bool animateScale = true;
        [SerializeField] private float scaleDuration = 0.3f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;

        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;

        private Button button;
        private Tweener fadeTween;
        private Tweener scaleTween;

        private void Awake()
        {
            button = GetComponent<Button>();

            // Auto-find underline if not assigned
            if (underlineCanvasGroup == null && underlineTransform != null)
            {
                underlineCanvasGroup = underlineTransform.GetComponent<CanvasGroup>();
            }

            // Initialize underline to hidden state
            if (underlineCanvasGroup != null)
            {
                underlineCanvasGroup.alpha = 0f;
            }

            if (animateScale && underlineTransform != null)
            {
                underlineTransform.localScale = new Vector3(0f, 1f, 1f);
            }
        }

        private void OnDestroy()
        {
            // Clean up tweens
            if (fadeTween != null && fadeTween.IsActive())
            {
                fadeTween.Kill();
            }
            if (scaleTween != null && scaleTween.IsActive())
            {
                scaleTween.Kill();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable)
                return;

            if (enableDebug)
            {
                Debug.Log($"[ButtonUnderlineAnimator] Pointer Enter: {gameObject.name}");
            }

            ShowUnderline();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (enableDebug)
            {
                Debug.Log($"[ButtonUnderlineAnimator] Pointer Exit: {gameObject.name}");
            }

            HideUnderline();
        }

        /// <summary>
        /// Show the underline with fade-in animation
        /// </summary>
        public void ShowUnderline()
        {
            if (underlineCanvasGroup == null)
                return;

            // Kill existing tweens
            if (fadeTween != null && fadeTween.IsActive())
            {
                fadeTween.Kill();
            }
            if (scaleTween != null && scaleTween.IsActive())
            {
                scaleTween.Kill();
            }

            // Fade in
            fadeTween = underlineCanvasGroup.DOFade(1f, fadeDuration)
                .SetEase(fadeEase)
                .SetLink(gameObject);

            // Scale in
            if (animateScale && underlineTransform != null)
            {
                scaleTween = underlineTransform.DOScaleX(1f, scaleDuration)
                    .SetEase(scaleEase)
                    .SetLink(gameObject);
            }
        }

        /// <summary>
        /// Hide the underline with fade-out animation
        /// </summary>
        public void HideUnderline()
        {
            if (underlineCanvasGroup == null)
                return;

            // Kill existing tweens
            if (fadeTween != null && fadeTween.IsActive())
            {
                fadeTween.Kill();
            }
            if (scaleTween != null && scaleTween.IsActive())
            {
                scaleTween.Kill();
            }

            // Fade out
            fadeTween = underlineCanvasGroup.DOFade(0f, fadeDuration)
                .SetEase(fadeEase)
                .SetLink(gameObject);

            // Scale out
            if (animateScale && underlineTransform != null)
            {
                scaleTween = underlineTransform.DOScaleX(0f, scaleDuration)
                    .SetEase(scaleEase)
                    .SetLink(gameObject);
            }
        }

        /// <summary>
        /// Manually set underline visibility without animation
        /// </summary>
        public void SetUnderlineVisible(bool visible)
        {
            if (underlineCanvasGroup != null)
            {
                underlineCanvasGroup.alpha = visible ? 1f : 0f;
            }

            if (animateScale && underlineTransform != null)
            {
                underlineTransform.localScale = new Vector3(visible ? 1f : 0f, 1f, 1f);
            }
        }
    }
}
