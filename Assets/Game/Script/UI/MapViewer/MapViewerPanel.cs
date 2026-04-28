using System.Collections.Generic;
using System.IO;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;
using Game.Player;

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

        [Header("Path Overlay")]
        [SerializeField] private Image pathOverlayImage;
        [SerializeField] private Color lineColor = Color.red;
        [SerializeField] private Color endpointColor = Color.red;
        [SerializeField] private Color walkPathColor = Color.green;
        [SerializeField] private Color aStarLineColor = Color.blue;
        [SerializeField] private int lineThickness = 3;
        [SerializeField] private int endpointRadius = 3;
        [SerializeField] private float pathFadeDuration = 0.4f;

        [Header("Player Position Marker")]
        [SerializeField] private Color playerMarkerColor = Color.cyan;
        [SerializeField] private int playerMarkerRadius = 15;
        [SerializeField] private Color playerMarkerOutlineColor = Color.black;
        [SerializeField] private int playerMarkerOutlineThickness = 3;

        [Header("A Star Path")]
        [SerializeField] bool includeAStarPath;
        private Tween activeTween;
        private IEventBus eventBus;
        private ISaveLoadService saveLoadService;

        private HeldMapData currentMapData;
        private byte[] baseMapBytes;
        private Texture2D currentTex;
        private Sprite currentSprite;
        private Texture2D pathOverlayTex;
        private Sprite pathOverlaySprite;
        private Tween pathFadeTween;
        private bool pathCurrentlyDrawn;
        private bool includeWalkPath;

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
            pathFadeTween?.Kill();

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClicked);

            ReleaseMapSprite();
            ReleasePathOverlay();
        }

        private void Update()
        {
            if (!IsActive || baseMapBytes == null || mapImage == null)
                return;

            if (includeWalkPath)
                return;

            bool shouldShow = MapPathRevealState.IsRevealed;
            if (shouldShow == pathCurrentlyDrawn)
                return;

            if (pathOverlayImage != null)
            {
                FadePathOverlayTo(shouldShow ? 1f : 0f);
                pathCurrentlyDrawn = shouldShow;
                return;
            }

            var rebuilt = BuildMapSprite(shouldShow);
            mapImage.sprite = rebuilt;
            mapImage.enabled = rebuilt != null;
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
            includeWalkPath = false;
            return ApplyMapData(mapData);
        }

        public bool SetMapDataWithWalkPath(HeldMapData mapData)
        {
            includeWalkPath = true;
            return ApplyMapData(mapData);
        }

        public bool SetMapDataWithAStarPath(HeldMapData mapData)
        {
            includeWalkPath = false;
            return ApplyMapData(mapData);
        }

        public void SetShowAStarPath(bool show)
        {
            if (includeAStarPath == show) return;
            includeAStarPath = show;
            RebuildOverlay();
        }

        public void ToggleAStarPath()
        {
            includeAStarPath = !includeAStarPath;
            RebuildOverlay();
        }

        private void RebuildOverlay()
        {
            if (baseMapBytes == null) return;

            bool revealed = MapPathRevealState.IsRevealed;
            bool showOverlay = includeWalkPath || includeAStarPath || revealed;

            if (pathOverlayImage != null && currentTex != null)
            {
                ReleasePathOverlay();
                var overlay = BuildPathOverlaySprite(currentTex.width, currentTex.height);
                pathOverlayImage.sprite = overlay;
                pathOverlayImage.enabled = overlay != null;
                SetPathOverlayAlphaImmediate(showOverlay ? 1f : 0f);
                pathCurrentlyDrawn = showOverlay;
                return;
            }

            var rebuilt = BuildMapSprite(withPath: showOverlay);
            if (mapImage != null)
            {
                mapImage.sprite = rebuilt;
                mapImage.enabled = rebuilt != null;
            }
        }

        private bool ApplyMapData(HeldMapData mapData)
        {
            currentMapData = mapData;

            if (titleText != null)
                titleText.text = mapData != null ? mapData.MapTitle : string.Empty;

            ReleaseMapSprite();

            Sprite mapSprite = null;
            if (mapData != null && !string.IsNullOrWhiteSpace(mapData.MapSpriteResourcePath))
            {
                string rootPath = Application.persistentDataPath;
                string loadPath = Path.Combine(rootPath, mapData.MapSpriteResourcePath);

                if (File.Exists(loadPath))
                {
                    baseMapBytes = File.ReadAllBytes(loadPath);
                    bool useOverlayImage = pathOverlayImage != null;
                    bool revealed = MapPathRevealState.IsRevealed;
                    bool showOverlay = includeWalkPath || includeAStarPath || revealed;
                    mapSprite = BuildMapSprite(withPath: !useOverlayImage && showOverlay);

                    if (useOverlayImage && currentTex != null)
                    {
                        var overlay = BuildPathOverlaySprite(currentTex.width, currentTex.height);
                        pathOverlayImage.sprite = overlay;
                        pathOverlayImage.enabled = overlay != null;
                        SetPathOverlayAlphaImmediate(showOverlay ? 1f : 0f);
                        pathCurrentlyDrawn = showOverlay;
                    }
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

        private Sprite BuildMapSprite(bool withPath)
        {
            if (baseMapBytes == null)
                return null;

            ReleaseMapSprite();

            currentTex = new Texture2D(2, 2);
            currentTex.LoadImage(baseMapBytes);

            if (withPath)
            {
                saveLoadService ??= ServiceContainer.Instance.TryGet<ISaveLoadService>();
                var path = saveLoadService?.GetCachedPathForCurrentLevel();
                bool hasCached = path != null && path.Count >= 1;
                bool hasWalk = includeWalkPath && HasWalkPath();
                bool hasAStar = includeAStarPath && HasAStarPath();
                if (hasCached || hasWalk || hasAStar)
                    DrawPathOnTexture(currentTex, path);
            }

            pathCurrentlyDrawn = withPath;
            currentSprite = Sprite.Create(currentTex, new Rect(0, 0, currentTex.width, currentTex.height), new Vector2(0.5f, 0.5f), 100f);
            return currentSprite;
        }

        private void ReleaseMapSprite()
        {
            if (currentSprite != null)
            {
                Destroy(currentSprite);
                currentSprite = null;
            }
            if (currentTex != null)
            {
                Destroy(currentTex);
                currentTex = null;
            }
            pathCurrentlyDrawn = false;
        }

        private Sprite BuildPathOverlaySprite(int width, int height)
        {
            ReleasePathOverlay();

            pathOverlayTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var clearPixels = new Color[width * height];
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = clear;
            pathOverlayTex.SetPixels(clearPixels);

            saveLoadService ??= ServiceContainer.Instance.TryGet<ISaveLoadService>();
            var path = saveLoadService?.GetCachedPathForCurrentLevel();
            bool hasCached = path != null && path.Count >= 1;
            bool hasWalk = includeWalkPath && HasWalkPath();
            bool hasAStar = HasAStarPath();
            if (hasCached || hasWalk || hasAStar)
                DrawPathOnTexture(pathOverlayTex, path);
            else
                pathOverlayTex.Apply();

            pathOverlaySprite = Sprite.Create(pathOverlayTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            return pathOverlaySprite;
        }

        private void ReleasePathOverlay()
        {
            pathFadeTween?.Kill();
            pathFadeTween = null;

            if (pathOverlaySprite != null)
            {
                Destroy(pathOverlaySprite);
                pathOverlaySprite = null;
            }
            if (pathOverlayTex != null)
            {
                Destroy(pathOverlayTex);
                pathOverlayTex = null;
            }
        }

        private void SetPathOverlayAlphaImmediate(float alpha)
        {
            if (pathOverlayImage == null) return;
            pathFadeTween?.Kill();
            pathFadeTween = null;
            var c = pathOverlayImage.color;
            c.a = alpha;
            pathOverlayImage.color = c;
        }

        private void FadePathOverlayTo(float targetAlpha)
        {
            if (pathOverlayImage == null) return;
            pathFadeTween?.Kill();
            pathFadeTween = pathOverlayImage
                .DOFade(targetAlpha, pathFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() => pathFadeTween = null);
        }

        private void DrawPathOnTexture(Texture2D tex, List<Vector3> cachedPath)
        {
            if (cachedPath != null && cachedPath.Count >= 1)
            {
                DrawPathLines(tex, cachedPath, lineColor, lineThickness);

                int ex0 = Mathf.RoundToInt(cachedPath[0].x);
                int ey0 = Mathf.RoundToInt(cachedPath[0].z);
                PlotDisc(tex, ex0, ey0, endpointRadius, endpointColor);

                int ex1 = Mathf.RoundToInt(cachedPath[cachedPath.Count - 1].x);
                int ey1 = Mathf.RoundToInt(cachedPath[cachedPath.Count - 1].z);
                PlotDisc(tex, ex1, ey1, endpointRadius, endpointColor);
            }

            if (includeAStarPath)
            {
                var aStarPath = ResolveAStarPath();
                if (aStarPath != null && aStarPath.Count >= 2)
                    DrawPathLines(tex, aStarPath, aStarLineColor, lineThickness);
            }

            if (includeWalkPath)
            {
                var walkPath = ResolveWalkPath();
                if (walkPath != null && walkPath.Count >= 1)
                    DrawPathLines(tex, walkPath, walkPathColor, lineThickness);
            }

            // Draw player position marker
            DrawPlayerPositionMarker(tex);

            tex.Apply();
        }

        private static void DrawPathLines(Texture2D tex, List<Vector3> path, Color color, int thickness)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                int x0 = Mathf.RoundToInt(path[i].x);
                int y0 = Mathf.RoundToInt(path[i].z);
                int x1 = Mathf.RoundToInt(path[i + 1].x);
                int y1 = Mathf.RoundToInt(path[i + 1].z);
                PlotLine(tex, x0, y0, x1, y1, color, thickness);
            }
        }

        private static List<Vector3> ResolveWalkPath()
        {
            var tracker = ServiceContainer.Instance.TryGet<PlayerStatsTrackerService>()?.GetPathTracker();
            return tracker?.PathPositions;
        }

        private static bool HasWalkPath()
        {
            var walk = ResolveWalkPath();
            return walk != null && walk.Count >= 1;
        }

        private static List<Vector3> ResolveAStarPath()
        {
            var ctrl = HJBClickPathController.Instance;
            if (ctrl == null || ctrl.provider == null || ctrl.provider.worldDataManager == null) return null;
            var lvl = ctrl.provider.worldDataManager.currentLevel;
            if (ctrl.savedAStarPathsByLevel == null) return null;
            return ctrl.savedAStarPathsByLevel.TryGetValue(lvl, out var p) ? p : null;
        }

        private static bool HasAStarPath()
        {
            var p = ResolveAStarPath();
            return p != null && p.Count >= 2;
        }

        private void DrawPlayerPositionMarker(Texture2D tex)
        {
            var playerController = ServiceContainer.Instance.TryGet<PlayerControllerRefactored>();
            if (playerController != null)
            {
                Vector3 playerPos = playerController.transform.position;
                int px = Mathf.RoundToInt(playerPos.x);
                int py = Mathf.RoundToInt(playerPos.z);
                PlotDisc(tex, px, py, playerMarkerRadius, playerMarkerColor);
                PlotCircleOutline(tex, px, py, playerMarkerRadius, playerMarkerOutlineThickness, playerMarkerOutlineColor);
            }
        }

        private static void PlotLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                PlotBrush(tex, x0, y0, c, thickness);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private static void PlotBrush(Texture2D tex, int cx, int cy, Color c, int thickness)
        {
            int half = thickness / 2;
            for (int dy = -half; dy <= half; dy++)
            {
                for (int dx = -half; dx <= half; dx++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        tex.SetPixel(px, py, c);
                }
            }
        }

        private static void PlotDisc(Texture2D tex, int cx, int cy, int radius, Color c)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            tex.SetPixel(px, py, c);
                    }
                }
            }
        }

        private static void PlotCircleOutline(Texture2D tex, int cx, int cy, int radius, int thickness, Color c)
        {
            if (radius <= 0 || thickness <= 0)
                return;

            int innerRadius = Mathf.Max(0, radius - thickness);
            int outerSq = radius * radius;
            int innerSq = innerRadius * innerRadius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int distSq = dx * dx + dy * dy;
                    if (distSq <= outerSq && distSq >= innerSq)
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            tex.SetPixel(px, py, c);
                    }
                }
            }
        }
    }
}