using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Tutorial
{
    /// <summary>
    /// Slideshow panel that appears when ShowGameplayTipEvent is published.
    /// Wire up all serialized fields to a UI prefab in the scene.
    /// </summary>
    public class GameplayTipUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private TMP_Text pageIndicatorText;

        [Header("Buttons")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button dismissButton;

        [Header("Animation")]
        [SerializeField] private float showDuration = 0.28f;
        [SerializeField] private float hideDuration = 0.2f;

        private IEventBus _eventBus;
        private CanvasGroup _canvasGroup;
        private GameplayTipSlide[] _slides;
        private int _currentSlide;

        private void Awake()
        {
            _canvasGroup = panelRoot != null ? panelRoot.GetComponent<CanvasGroup>() : null;

            if (prevButton != null)    prevButton.onClick.AddListener(OnPrev);
            if (nextButton != null)    nextButton.onClick.AddListener(OnNext);
            if (dismissButton != null) dismissButton.onClick.AddListener(HidePanel);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Subscribe<ShowGameplayTipEvent>(OnShowTip);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<ShowGameplayTipEvent>(OnShowTip);
        }

        private void OnShowTip(ShowGameplayTipEvent evt)
        {
            if (evt.TipData == null || evt.TipData.slides == null || evt.TipData.slides.Length == 0) return;

            _slides = evt.TipData.slides;
            _currentSlide = 0;
            ShowPanel();
            DisplaySlide(_currentSlide);
        }

        private void DisplaySlide(int index)
        {
            if (_slides == null || index < 0 || index >= _slides.Length) return;

            var slide = _slides[index];

            if (titleText != null) titleText.text = slide.title;
            if (bodyText != null)  bodyText.text  = slide.bodyText;

            if (illustrationImage != null)
            {
                illustrationImage.gameObject.SetActive(slide.illustration != null);
                if (slide.illustration != null)
                    illustrationImage.sprite = slide.illustration;
            }

            if (pageIndicatorText != null)
                pageIndicatorText.text = $"{index + 1} / {_slides.Length}";

            if (prevButton != null)    prevButton.interactable    = index > 0;
            if (nextButton != null)    nextButton.interactable    = index < _slides.Length - 1;
            if (dismissButton != null) dismissButton.gameObject.SetActive(index == _slides.Length - 1);
        }

        private void OnPrev()
        {
            if (_currentSlide > 0)
                DisplaySlide(--_currentSlide);
        }

        private void OnNext()
        {
            if (_slides != null && _currentSlide < _slides.Length - 1)
                DisplaySlide(++_currentSlide);
            else
                HidePanel();
        }

        private void ShowPanel()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, showDuration).SetUpdate(true);
            }
        }

        private void HidePanel()
        {
            if (panelRoot == null) return;

            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, hideDuration)
                    .SetUpdate(true)
                    .OnComplete(() => panelRoot.SetActive(false));
            }
            else
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
