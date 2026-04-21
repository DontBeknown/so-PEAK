using System.IO;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

namespace Game.UI
{
    /// <summary>
    /// Dedicated map viewer panel for held map items.
    /// Loads the active map image from a Resources path defined by MapData.
    /// </summary>
    public class MapViewerPanel : MonoBehaviour, IUIPanel
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Content")]
        [SerializeField] private Image mapImage;
        [SerializeField] private TMP_Text titleText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private string openSoundId = "UI_InventoryOpen";
        [SerializeField] private float openSoundVolumeScale = 0.5f;
        [SerializeField] private string closeSoundId = "UI_InventoryClose";
        [SerializeField] private float closeSoundVolumeScale = 0.5f;

        private Tween activeTween;
        private IEventBus eventBus;

        private HeldMapData currentMapData;

        public string PanelName => "MapViewer";
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        public bool IsActive => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            eventBus = ServiceContainer.Instance.TryGet<IEventBus>();

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClicked);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            activeTween?.Kill();

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        private void HandleCloseClicked()
        {
            if (UIServiceProvider.Instance != null)
            {
                UIServiceProvider.Instance.ClosePanel(PanelName);
                return;
            }

            Hide();
        }

        public bool SetMapData(HeldMapData mapData)
        {
            currentMapData = mapData;

            if (titleText != null)
                titleText.text = mapData != null ? mapData.MapTitle : string.Empty;

            Sprite mapSprite = null;
            if (mapData != null && !string.IsNullOrWhiteSpace(mapData.MapSpriteResourcePath))
            {
                // 1. Get the safe base folder for whoever is playing the game right now
                string rootPath = Application.persistentDataPath;

                // 2. Glue it to your fixed config string ("SavedMaps/TopographicMap.png")
                string loadPath = Path.Combine(rootPath, mapData.MapSpriteResourcePath);

                // 3. Load the image from the hard drive!
                if (File.Exists(loadPath))
                {
                    byte[] fileData = File.ReadAllBytes(loadPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(fileData);

                    mapSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
                else
                {
                    Debug.LogError($"Could not find map file at: {loadPath}");
                }
            }

            if (mapImage != null)
            {
                mapImage.enabled = mapSprite != null;
                mapImage.sprite = mapSprite;
            }

            if (mapData == null)
            {
                Debug.LogWarning("[MapViewerPanel] No MapData assigned.");
                return false;
            }

            if (mapSprite == null)
            {
                Debug.LogWarning($"[MapViewerPanel] Could not load map sprite from AppData path '{mapData.MapSpriteResourcePath}'.");
                return false;
            }

            return true;
        }

        public void Show()
        {
            if (panelRoot == null)
                return;

            PublishUISound(openSoundId, openSoundVolumeScale);
            activeTween?.Kill();
            panelRoot.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                activeTween = canvasGroup
                    .DOFade(1f, fadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(() => activeTween = null);
            }
        }

        public void Hide()
        {
            if (panelRoot == null)
                return;

            PublishUISound(closeSoundId, closeSoundVolumeScale);
            activeTween?.Kill();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                activeTween = canvasGroup
                    .DOFade(0f, fadeDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        panelRoot.SetActive(false);
                        activeTween = null;
                    });

                return;
            }

            panelRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (IsActive)
                Hide();
            else
                Show();
        }

        private void PublishUISound(string clipId, float volumeScale)
        {
            if (string.IsNullOrWhiteSpace(clipId))
                return;

            eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
            eventBus?.Publish(new PlayUISoundEvent(clipId, volumeScale));
        }
    }
}