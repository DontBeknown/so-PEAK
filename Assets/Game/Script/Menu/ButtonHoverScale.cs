using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Game.Sound;

namespace Game.Menu
{
    [RequireComponent(typeof(Button))]
    public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {

        [Header("References")]

        [SerializeField] private SoundService soundService;

        [Header("Scale Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float hoverDuration = 0.2f;
        [SerializeField] private float pressDuration = 0.1f;
        [SerializeField] private Ease hoverEase = Ease.OutBack;
        [SerializeField] private Ease pressEase = Ease.OutQuad;

        [Header("Sound Settings")]
        [SerializeField] private string hoverSoundId = "ui_hover";
        [SerializeField] private float hoverVolumeScale = 0.3f;
        [SerializeField] private string clickSoundId = "ui_click";
        [SerializeField] private float clickVolumeScale = 0.3f;

        private Button button;
        private Vector3 originalScale;
        private Tweener scaleTween;

        private void Awake()
        {
            if(soundService == null)
            {
                soundService = FindFirstObjectByType<SoundService>();
            }
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
            if (!string.IsNullOrEmpty(hoverSoundId))
                soundService?.PlayUISound(hoverSoundId, volumeScale: hoverVolumeScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ScaleTo(originalScale, hoverDuration, hoverEase);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;
            ScaleTo(originalScale * pressedScale, pressDuration, pressEase);
            if (!string.IsNullOrEmpty(clickSoundId))
                soundService?.PlayUISound(clickSoundId, volumeScale: clickVolumeScale);
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
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
