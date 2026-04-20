using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Collectable;

namespace Game.UI.Collectable
{
    public class CollectablesHubUI : MonoBehaviour
    {
        private static readonly CollectableBiome[] BiomeCycle =
        {
            CollectableBiome.Forest,
            CollectableBiome.Desert,
            CollectableBiome.Tundra
        };

        [SerializeField] private GameObject hubRoot;
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private DocumentPageUI documentPageUI;
        [SerializeField] private CollectableItem[] allCollectables;
        [SerializeField] private Button previousBiomeButton;
        [SerializeField] private Button nextBiomeButton;
        [SerializeField] private TMP_Text biomeLabel;

        [Header("Transition Targets")]
        [SerializeField] private CanvasGroup dimBackgroundCanvasGroup;
        [SerializeField] private RectTransform sharedBackgroundRect;
        [SerializeField] private GameObject menuRoot;

        [Header("Transition Settings")]
        [SerializeField] private float openDuration = 0.35f;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float sharedBackgroundSlideDistance = 120f;

        private readonly List<GameObject> _spawnedEntries = new List<GameObject>();
        private ICollectableManager _collectableManager;
        private IEventBus _eventBus;
        private string _selectedCollectableId;
        private CollectableBiome _activeBiome = CollectableBiome.Forest;
        private CanvasGroup _menuCanvasGroup;
        private RectTransform _menuRect;
        private Vector2 _menuShownPosition;
        private Vector2 _sharedBackgroundShownPosition;
        private bool _hasMenuRect;
        private bool _hasSharedBackgroundRect;
        private bool _menuHiddenForDocument;
        private bool _suppressRestoreOnDocumentHide;
        private Tween _openTween;
        private Tween _menuDocumentTween;
        private Tween _listFadeTween;
        private CanvasGroup _listCanvasGroup;

        private void Awake()
        {
            HookBiomeButtons();
            CacheListAnimationReferences();
            CacheAnimationReferences();
            UpdateBiomeLabel();
            HideHubPanel();
        }

        private void Start()
        {
            _collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Subscribe<CollectableUnlockedEvent>(OnCollectableUnlocked);
            _eventBus?.Subscribe<CollectableHubFocusRequestedEvent>(OnCollectableHubFocusRequested);
            if (documentPageUI != null)
                documentPageUI.VisibilityChanged += OnDocumentVisibilityChanged;
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<CollectableUnlockedEvent>(OnCollectableUnlocked);
            _eventBus?.Unsubscribe<CollectableHubFocusRequestedEvent>(OnCollectableHubFocusRequested);
            if (documentPageUI != null)
                documentPageUI.VisibilityChanged -= OnDocumentVisibilityChanged;

            _openTween?.Kill();
            _menuDocumentTween?.Kill();
            _listFadeTween?.Kill();
        }

        public void ShowHubPanel()
        {
            if (hubRoot != null)
                hubRoot.SetActive(true);

            SetBiomeControlsInteractable(true);
            PrepareHubAndSharedBackgroundForOpen();
            RefreshEntries();
            PlayHubOpenAnimation();
        }

        public void HideHubPanel()
        {
            _openTween?.Kill();
            _menuDocumentTween?.Kill();

            _suppressRestoreOnDocumentHide = true;
            documentPageUI?.HideImmediately();
            _suppressRestoreOnDocumentHide = false;
            _menuHiddenForDocument = false;

            if (hubRoot != null)
                hubRoot.SetActive(false);

            SetBiomeControlsInteractable(false);

            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = 1f;
                _menuCanvasGroup.interactable = true;
                _menuCanvasGroup.blocksRaycasts = true;
            }

            if (_hasMenuRect)
                _menuRect.anchoredPosition = _menuShownPosition;

            if (dimBackgroundCanvasGroup != null)
            {
                dimBackgroundCanvasGroup.alpha = 0f;
                dimBackgroundCanvasGroup.interactable = false;
                dimBackgroundCanvasGroup.blocksRaycasts = false;
            }

            if (_hasSharedBackgroundRect)
                sharedBackgroundRect.anchoredPosition = _sharedBackgroundShownPosition;
        }

        public void RefreshEntries()
        {
            if (entryPrefab == null || listContainer == null || allCollectables == null)
                return;

            _collectableManager ??= ServiceContainer.Instance.TryGet<ICollectableManager>();

            var canAnimate = hubRoot != null && hubRoot.activeSelf && _listCanvasGroup != null;
            if (canAnimate)
            {
                PlayListFadeRefreshAnimation();
                return;
            }

            RebuildEntries();
            SetListVisibilityImmediate(true);
        }

        public CollectableItem[] GetConfiguredCollectables()
        {
            return allCollectables;
        }

        private void RebuildEntries()
        {
            ClearEntries();

            foreach (var collectable in GetCollectablesForActiveBiome())
            {
                var entryObject = Instantiate(entryPrefab, listContainer);
                _spawnedEntries.Add(entryObject);

                var isUnlocked = _collectableManager != null && _collectableManager.IsUnlocked(collectable.id);
                var isSelected = !string.IsNullOrWhiteSpace(_selectedCollectableId) && _selectedCollectableId == collectable.id;
                BindEntry(entryObject, collectable, isUnlocked, isSelected);
            }
        }

        private void PlayListFadeRefreshAnimation()
        {
            if (_listCanvasGroup == null)
            {
                RebuildEntries();
                return;
            }

            _listFadeTween?.Kill();

            _listCanvasGroup.interactable = false;
            _listCanvasGroup.blocksRaycasts = false;

            var fadeSegmentDuration = Mathf.Max(0.05f, fadeDuration * 0.5f);
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            sequence.Append(_listCanvasGroup.DOFade(0f, fadeSegmentDuration).SetEase(Ease.InOutQuad));
            sequence.AppendCallback(RebuildEntries);
            sequence.Append(_listCanvasGroup.DOFade(1f, fadeSegmentDuration).SetEase(Ease.InOutQuad));
            sequence.OnComplete(() =>
            {
                _listCanvasGroup.interactable = true;
                _listCanvasGroup.blocksRaycasts = true;
                _listFadeTween = null;
            });

            _listFadeTween = sequence;
        }

        private void BindEntry(GameObject entryObject, CollectableItem collectable, bool unlocked, bool isSelected)
        {
            var labels = entryObject.GetComponentsInChildren<TMP_Text>(true);
            if (labels.Length > 0)
                labels[0].text = unlocked ? collectable.headerName : "???";

            if (labels.Length > 1)
                labels[1].text = unlocked
                    ? (isSelected ? $"{collectable.type} (Selected)" : collectable.type.ToString())
                    : "Locked";

            var buttons = entryObject.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name.ToLower().Contains("open"))
                {
                    button.gameObject.SetActive(unlocked && collectable.type == CollectableType.TextDocument);
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OpenCollectableFromMenu(collectable));
                }
                else if (button.name.ToLower().Contains("replay"))
                {
                    button.gameObject.SetActive(unlocked && collectable.type == CollectableType.ScriptDialog);
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        documentPageUI?.Hide();
                        _eventBus?.Publish(new CollectableOpenRequestedEvent(collectable, true));
                    });
                }
            }
        }

        private void ClearEntries()
        {
            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry);
            }

            _spawnedEntries.Clear();
        }

        private void OnCollectableUnlocked(CollectableUnlockedEvent _)
        {
            RefreshEntries();
        }

        private void OnCollectableHubFocusRequested(CollectableHubFocusRequestedEvent evt)
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.CollectableId))
                return;

            _selectedCollectableId = evt.CollectableId;

            var focusedCollectable = allCollectables?.FirstOrDefault(c => c != null && c.id == evt.CollectableId);
            if (focusedCollectable == null)
            {
                ShowHubPanel();
                return;
            }

            SetActiveBiome(focusedCollectable.biome, refresh: false);
            ShowHubPanel();

            if (focusedCollectable.type == CollectableType.TextDocument)
            {
                SetMenuVisibilityImmediate(false);
                OpenCollectableFromMenu(focusedCollectable);
            }
        }

        private void HookBiomeButtons()
        {
            if (previousBiomeButton != null)
            {
                previousBiomeButton.onClick.RemoveAllListeners();
                previousBiomeButton.onClick.AddListener(() => CycleBiome(-1));
            }

            if (nextBiomeButton != null)
            {
                nextBiomeButton.onClick.RemoveAllListeners();
                nextBiomeButton.onClick.AddListener(() => CycleBiome(1));
            }
        }

        private void CycleBiome(int direction)
        {
            var index = System.Array.IndexOf(BiomeCycle, _activeBiome);
            if (index < 0)
                index = 0;

            index = (index + direction) % BiomeCycle.Length;
            if (index < 0)
                index += BiomeCycle.Length;

            SetActiveBiome(BiomeCycle[index]);
        }

        private void SetActiveBiome(CollectableBiome biome, bool refresh = true)
        {
            _activeBiome = biome;
            UpdateBiomeLabel();

            if (refresh && isActiveAndEnabled && (hubRoot == null || hubRoot.activeSelf))
                RefreshEntries();
        }

        private void UpdateBiomeLabel()
        {
            if (biomeLabel != null)
                biomeLabel.text = _activeBiome.ToString();
        }

        private IEnumerable<CollectableItem> GetCollectablesForActiveBiome()
        {
            if (allCollectables == null)
                yield break;

            foreach (var collectable in allCollectables.Where(c => c != null && c.biome == _activeBiome))
            {
                yield return collectable;
            }
        }

        private void SetBiomeControlsInteractable(bool interactable)
        {
            if (previousBiomeButton != null)
                previousBiomeButton.interactable = interactable;

            if (nextBiomeButton != null)
                nextBiomeButton.interactable = interactable;
        }

        private void OpenCollectableFromMenu(CollectableItem collectable, bool delayed = false)
        {
            if (collectable == null || collectable.type != CollectableType.TextDocument || documentPageUI == null)
                return;

            if (delayed)
            {
                DOVirtual.DelayedCall(openDuration, () =>
                {
                    if (!isActiveAndEnabled || (hubRoot != null && !hubRoot.activeSelf))
                        return;

                    PlayMenuToDocumentTransition(collectable);
                }, true)
                    .SetLink(gameObject);
                return;
            }

            PlayMenuToDocumentTransition(collectable);
        }

        private void PlayMenuToDocumentTransition(CollectableItem collectable)
        {
            if (!documentPageUI.SetContent(collectable))
                return;

            CacheAnimationReferences();
            _menuDocumentTween?.Kill();
            _menuHiddenForDocument = true;

            if (hubRoot != null)
                hubRoot.SetActive(true);

            var documentRect = documentPageUI.PanelRect;
            var documentCanvas = documentPageUI.PanelCanvasGroup;

            documentPageUI.ShowImmediate(collectable);

            if (documentCanvas != null)
            {
                documentCanvas.alpha = 0f;
                documentCanvas.interactable = false;
                documentCanvas.blocksRaycasts = false;
            }

            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.interactable = false;
                _menuCanvasGroup.blocksRaycasts = false;
            }

            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            if (_menuCanvasGroup != null)
                sequence.Join(_menuCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad));

            if (documentCanvas != null)
                sequence.Join(documentCanvas.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));

            sequence.OnComplete(() =>
            {
                if (documentCanvas != null)
                {
                    documentCanvas.interactable = true;
                    documentCanvas.blocksRaycasts = true;
                }

                _menuDocumentTween = null;
            });

            _menuDocumentTween = sequence;
        }

        private void OnDocumentVisibilityChanged(bool isVisible)
        {
            if (isVisible || _suppressRestoreOnDocumentHide || !_menuHiddenForDocument)
                return;

            PlayRestoreMenuFromDocumentTransition();
        }

        private void PlayRestoreMenuFromDocumentTransition()
        {
            CacheAnimationReferences();
            _menuDocumentTween?.Kill();

            if (hubRoot != null)
                hubRoot.SetActive(true);

            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = 0f;
                _menuCanvasGroup.interactable = false;
                _menuCanvasGroup.blocksRaycasts = false;
            }

            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            if (_menuCanvasGroup != null)
                sequence.Join(_menuCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));

            sequence.OnComplete(() =>
            {
                if (_menuCanvasGroup != null)
                {
                    _menuCanvasGroup.interactable = true;
                    _menuCanvasGroup.blocksRaycasts = true;
                }

                _menuHiddenForDocument = false;
                _menuDocumentTween = null;
            });

            _menuDocumentTween = sequence;
        }

        private void CacheAnimationReferences()
        {
            GameObject effectiveMenuRoot = menuRoot;
            if (effectiveMenuRoot == null && listContainer != null)
            {
                var parent = listContainer.parent;
                effectiveMenuRoot = parent != null ? parent.gameObject : listContainer.gameObject;
            }

            if (effectiveMenuRoot == null)
                effectiveMenuRoot = hubRoot;

            if (_menuCanvasGroup == null && effectiveMenuRoot != null)
                _menuCanvasGroup = effectiveMenuRoot.GetComponent<CanvasGroup>() ?? effectiveMenuRoot.AddComponent<CanvasGroup>();

            if (_menuRect == null && effectiveMenuRoot != null)
            {
                _menuRect = effectiveMenuRoot.GetComponent<RectTransform>();
                _hasMenuRect = _menuRect != null;
                if (_hasMenuRect)
                    _menuShownPosition = _menuRect.anchoredPosition;
            }

            if (!_hasSharedBackgroundRect && sharedBackgroundRect != null)
            {
                _hasSharedBackgroundRect = true;
                _sharedBackgroundShownPosition = sharedBackgroundRect.anchoredPosition;
            }
        }

        private void CacheListAnimationReferences()
        {
            if (listContainer == null)
                return;

            var listRoot = listContainer as RectTransform;
            var target = listRoot != null ? listRoot.gameObject : listContainer.gameObject;
            _listCanvasGroup = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
            SetListVisibilityImmediate(true);
        }

        private void PrepareHubAndSharedBackgroundForOpen()
        {
            CacheAnimationReferences();

            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = 1f;
                _menuCanvasGroup.interactable = true;
                _menuCanvasGroup.blocksRaycasts = true;
            }

            if (_hasMenuRect)
                _menuRect.anchoredPosition = _menuShownPosition;

            if (dimBackgroundCanvasGroup != null)
            {
                dimBackgroundCanvasGroup.alpha = 0f;
                dimBackgroundCanvasGroup.interactable = false;
                dimBackgroundCanvasGroup.blocksRaycasts = false;
            }

            if (_hasSharedBackgroundRect)
                sharedBackgroundRect.anchoredPosition = _sharedBackgroundShownPosition + Vector2.down * sharedBackgroundSlideDistance;

            CacheListAnimationReferences();
            SetListVisibilityImmediate(false);
        }

        private void PlayHubOpenAnimation()
        {
            _openTween?.Kill();

            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            if (dimBackgroundCanvasGroup != null)
                sequence.Join(dimBackgroundCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));

            if (_hasSharedBackgroundRect)
                sequence.Join(sharedBackgroundRect.DOAnchorPos(_sharedBackgroundShownPosition, openDuration).SetEase(Ease.OutCubic));

            sequence.OnComplete(() =>
            {
                if (dimBackgroundCanvasGroup != null)
                {
                    dimBackgroundCanvasGroup.interactable = true;
                    dimBackgroundCanvasGroup.blocksRaycasts = true;
                }

                _openTween = null;
            });

            _openTween = sequence;
        }

        private void SetMenuVisibilityImmediate(bool visible)
        {
            if (_menuCanvasGroup == null)
                return;

            _menuCanvasGroup.alpha = visible ? 1f : 0f;
            _menuCanvasGroup.interactable = visible;
            _menuCanvasGroup.blocksRaycasts = visible;
        }

        private void SetListVisibilityImmediate(bool visible)
        {
            if (_listCanvasGroup == null)
                return;

            _listCanvasGroup.alpha = visible ? 1f : 0f;
            _listCanvasGroup.interactable = visible;
            _listCanvasGroup.blocksRaycasts = visible;
        }
    }
}
