using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Game.Menu
{
    [RequireComponent(typeof(Button))]
    public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float hoverDuration = 0.2f;
        [SerializeField] private float pressDuration = 0.1f;
        [SerializeField] private Ease hoverEase = Ease.OutBack;
        [SerializeField] private Ease pressEase = Ease.OutQuad;

        private Button button;
        private Vector3 originalScale;
        private Tweener scaleTween;

        private void Awake()
        {
            button = GetComponent<Button>();
            originalScale = transform.localScale;
        }

        private void OnDestroy()
        {
            scaleTween?.Kill();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;
            ScaleTo(originalScale * hoverScale, hoverDuration, hoverEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ScaleTo(originalScale, hoverDuration, hoverEase);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;
            ScaleTo(originalScale * pressedScale, pressDuration, pressEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!button.interactable) return;
            ScaleTo(originalScale * hoverScale, pressDuration, pressEase);
        }

        private void ScaleTo(Vector3 target, float duration, Ease ease)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(target, duration)
                .SetEase(ease)
                .SetLink(gameObject);
        }
    }
}
