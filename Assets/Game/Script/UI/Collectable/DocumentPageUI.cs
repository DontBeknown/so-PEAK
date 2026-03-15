using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Collectable;

namespace Game.UI.Collectable
{
    public class DocumentPageUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Image iconImage;
        [SerializeField] private float slideDuration = 0.35f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float slideDistance = 700f;
        [SerializeField] private float slideOvershoot = 1.1f;

        private IEventBus _eventBus;
        private RectTransform _panelRect;
        private CanvasGroup _canvasGroup;
        private Vector2 _shownAnchoredPosition;
        private Tween _panelTween;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot != null)
            {
                _panelRect = panelRoot.GetComponent<RectTransform>();
                if (_panelRect != null)
                    _shownAnchoredPosition = _panelRect.anchoredPosition;

                _canvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            HideImmediate();
        }

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Subscribe<CollectableOpenRequestedEvent>(OnCollectableOpenRequested);
        }

        private void OnDestroy()
        {
            _panelTween?.Kill();
            _eventBus?.Unsubscribe<CollectableOpenRequestedEvent>(OnCollectableOpenRequested);
        }

        public void Show(CollectableItem collectable)
        {
            if (collectable == null)
                return;

            if (headerText != null)
                headerText.text = collectable.headerName;

            if (contentText != null)
                contentText.text = collectable.content;

            if (iconImage != null)
            {
                iconImage.enabled = collectable.icon != null;
                iconImage.sprite = collectable.icon;
            }

            PlayShowAnimation();
        }

        public void Hide()
        {
            if (panelRoot != null)
                PlayHideAnimation();
        }

        private void OnCollectableOpenRequested(CollectableOpenRequestedEvent evt)
        {
            if (evt?.Collectable == null)
                return;

            if (evt.Collectable.type != CollectableType.TextDocument)
                return;

            Show(evt.Collectable);
        }

        public void Toggle(CollectableItem collectable)
        {
            if (IsVisible)
                Hide();
            else
                Show(collectable);
        }

        private void PlayShowAnimation()
        {
            if (panelRoot == null)
                return;

            panelRoot.SetActive(true);

            if (_panelRect == null || _canvasGroup == null)
                return;

            _panelTween?.Kill();

            _panelRect.anchoredPosition = _shownAnchoredPosition + Vector2.right * slideDistance;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(_panelRect.DOAnchorPos(_shownAnchoredPosition, slideDuration)
                .SetEase(Ease.OutBack, slideOvershoot));
            sequence.Join(_canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _panelTween = null;
            });

            _panelTween = sequence;
        }

        private void PlayHideAnimation()
        {
            if (panelRoot == null)
                return;

            if (!panelRoot.activeSelf)
            {
                HideImmediate();
                return;
            }

            if (_panelRect == null || _canvasGroup == null)
            {
                HideImmediate();
                return;
            }

            _panelTween?.Kill();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var targetPosition = _shownAnchoredPosition + Vector2.right * slideDistance;
            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(_panelRect.DOAnchorPos(targetPosition, slideDuration)
                .SetEase(Ease.InQuad));
            sequence.Join(_canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad));
            sequence.OnComplete(HideImmediate);

            _panelTween = sequence;
        }

        private void HideImmediate()
        {
            _panelTween?.Kill();
            _panelTween = null;

            if (_panelRect != null)
                _panelRect.anchoredPosition = _shownAnchoredPosition;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}
