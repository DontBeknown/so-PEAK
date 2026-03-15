using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Core.DI;
using Game.Core.Events;
using Game.Collectable;

namespace Game.UI.Collectable
{
    public class CollectablesHubUI : MonoBehaviour
    {
        [SerializeField] private GameObject hubRoot;
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private DocumentPageUI documentPageUI;
        [SerializeField] private CollectableItem[] allCollectables;

        private readonly List<GameObject> _spawnedEntries = new List<GameObject>();
        private ICollectableManager _collectableManager;
        private IEventBus _eventBus;
        private string _selectedCollectableId;

        private void Awake()
        {
            HideHubPanel();
        }

        private void Start()
        {
            _collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Subscribe<CollectableUnlockedEvent>(OnCollectableUnlocked);
            _eventBus?.Subscribe<CollectableHubFocusRequestedEvent>(OnCollectableHubFocusRequested);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<CollectableUnlockedEvent>(OnCollectableUnlocked);
            _eventBus?.Unsubscribe<CollectableHubFocusRequestedEvent>(OnCollectableHubFocusRequested);
        }

        public void ShowHubPanel()
        {
            if (hubRoot != null)
                hubRoot.SetActive(true);

            RefreshEntries();
        }

        public void HideHubPanel()
        {
            if (hubRoot != null)
                hubRoot.SetActive(false);

            documentPageUI?.Hide();
        }

        public void RefreshEntries()
        {
            ClearEntries();
            if (entryPrefab == null || listContainer == null || allCollectables == null)
                return;

            _collectableManager ??= ServiceContainer.Instance.TryGet<ICollectableManager>();

            foreach (var collectable in allCollectables.Where(c => c != null))
            {
                var entryObject = Instantiate(entryPrefab, listContainer);
                _spawnedEntries.Add(entryObject);

                var isUnlocked = _collectableManager != null && _collectableManager.IsUnlocked(collectable.id);
                var isSelected = !string.IsNullOrWhiteSpace(_selectedCollectableId) && _selectedCollectableId == collectable.id;
                BindEntry(entryObject, collectable, isUnlocked, isSelected);
            }
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
                    button.onClick.AddListener(() => documentPageUI?.Toggle(collectable));
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
            ShowHubPanel();

            var focusedCollectable = allCollectables?.FirstOrDefault(c => c != null && c.id == evt.CollectableId);
            if (focusedCollectable == null)
                return;

            if (focusedCollectable.type == CollectableType.TextDocument)
            {
                documentPageUI?.Show(focusedCollectable);
            }
        }
    }
}
