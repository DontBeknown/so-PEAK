using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player.Inventory.Storage;
using ItemAddedEvent        = Game.Player.Inventory.Events.ItemAddedEvent;
using ItemRemovedEvent      = Game.Player.Inventory.Events.ItemRemovedEvent;
using InventoryChangedEvent = Game.Player.Inventory.Events.InventoryChangedEvent;
using Game.UI;

/// <summary>
/// Horizontal sliding compass strip shown when the compass item is equipped.
///
/// ZERO SETUP REQUIRED — add this component to any child of your HUD Canvas
/// and it builds the full hierarchy (background, mask, tape, labels, ticks,
/// center marker) automatically in Awake().
///
/// Optional Inspector overrides let you adjust look without touching code.
/// </summary>
public class CompassPanel : MonoBehaviour, IUIPanel
{
    [Header("Dimensions")]
    [SerializeField] private float panelWidth      = 320f;
    [SerializeField] private float panelHeight     = 50f;
    [SerializeField] private float maskWidth       = 300f;
    [SerializeField] private float pixelsPerDegree = 1.5f;   // 1.5 px = 1°  →  540 px per full rotation

    [Header("Colors")]
    [SerializeField] private Color backgroundColor    = new Color(0f,   0f,   0f,   0.55f);
    [SerializeField] private Color cardinalColor      = new Color(1f,   0.85f, 0.2f, 1f);   // N E S W — gold
    [SerializeField] private Color intercardinalColor = new Color(0.9f, 0.9f, 0.9f, 1f);   // NE SE SW NW — white
    [SerializeField] private Color majorTickColor     = new Color(1f,   1f,   1f,   0.85f);
    [SerializeField] private Color minorTickColor     = new Color(1f,   1f,   1f,   0.4f);
    [SerializeField] private Color centerMarkerColor  = new Color(1f,   0.3f, 0.3f, 1f);   // red pointer

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.25f;

    // ── Runtime refs (built in Awake) ─────────────────────────────────────
    private GameObject    _panelRoot;
    private CanvasGroup   _canvasGroup;
    private RectTransform _compassTape;
    private RectTransform _maskContainer;

    private Tween _activeTween;
    private CinemachinePlayerCamera _playerCamera;
    private IEventBus               _eventBus;
    private GridInventoryStorage    _gridStorage;
    private float _maskHalfWidth;
    private float _tapeSingleWidth;   // 360 * pixelsPerDegree

    // Cardinal / intercardinal data
    private static readonly (string label, float degree, bool isCardinal)[] Directions =
    {
        ("N",  0f,   true),
        ("NE", 45f,  false),
        ("E",  90f,  true),
        ("SE", 135f, false),
        ("S",  180f, true),
        ("SW", 225f, false),
        ("W",  270f, true),
        ("NW", 315f, false),
    };

    // ── IUIPanel ──────────────────────────────────────────────────────────
    public string PanelName   => "Compass";
    public bool   BlocksInput => false;
    public bool   UnlocksCursor => false;
    public bool   IsActive    => _panelRoot != null && _panelRoot.activeSelf;

    // ── Unity Lifecycle ───────────────────────────────────────────────────

    private void Awake()
    {
        _playerCamera    = ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
        _tapeSingleWidth = 360f * pixelsPerDegree;
        _maskHalfWidth   = maskWidth * 0.5f;

        BuildHierarchy();

        _panelRoot.SetActive(false);
        _canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // Resolve after GameServiceBootstrapper has registered everything
        _eventBus    = ServiceContainer.Instance.TryGet<IEventBus>();
        _gridStorage = ServiceContainer.Instance.TryGet<GridInventoryStorage>();

        _eventBus?.Subscribe<ItemAddedEvent>(OnInventoryChanged);
        _eventBus?.Subscribe<ItemRemovedEvent>(OnInventoryChanged);
        _eventBus?.Subscribe<InventoryChangedEvent>(OnInventoryChanged);

        // Show immediately if the compass is already in the inventory at scene start
        RefreshCompassVisibility();
    }

    private void Update()
    {
        if (!IsActive || _compassTape == null || _playerCamera == null)
            return;

        float yaw     = Mathf.Repeat(_playerCamera.transform.eulerAngles.y, 360f);
        float xOffset = _maskHalfWidth - _tapeSingleWidth - yaw * pixelsPerDegree;
        _compassTape.anchoredPosition = new Vector2(xOffset, _compassTape.anchoredPosition.y);
    }

    // ── Inventory awareness ───────────────────────────────────────────────

    private void OnInventoryChanged(ItemAddedEvent _)        => RefreshCompassVisibility();
    private void OnInventoryChanged(ItemRemovedEvent _)      => RefreshCompassVisibility();
    private void OnInventoryChanged(InventoryChangedEvent _) => RefreshCompassVisibility();

    private void RefreshCompassVisibility()
    {
        bool hasCompass = _gridStorage != null &&
                          _gridStorage.GetAllPlacements().Any(p => p.Item is CompassItem);

        if (hasCompass && !IsActive) Show();
        else if (!hasCompass && IsActive) Hide();
    }

    // ── IUIPanel Show / Hide ──────────────────────────────────────────────

    public void Show()
    {
        if (_panelRoot == null) return;

        _activeTween?.Kill();
        _panelRoot.SetActive(true);

        _canvasGroup.alpha          = 0f;
        _canvasGroup.interactable   = true;
        _canvasGroup.blocksRaycasts = true;

        _activeTween = _canvasGroup
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => _activeTween = null);
    }

    public void Hide()
    {
        if (_panelRoot == null) return;

        _activeTween?.Kill();

        _canvasGroup.interactable   = false;
        _canvasGroup.blocksRaycasts = false;

        _activeTween = _canvasGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _panelRoot.SetActive(false);
                _activeTween = null;
            });
    }

    public void Toggle() { if (IsActive) Hide(); else Show(); }

    private void OnDestroy()
    {
        _eventBus?.Unsubscribe<ItemAddedEvent>(OnInventoryChanged);
        _eventBus?.Unsubscribe<ItemRemovedEvent>(OnInventoryChanged);
        _eventBus?.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        _activeTween?.Kill();
    }

    // ── Hierarchy Builder ─────────────────────────────────────────────────

    private void BuildHierarchy()
    {
        // ── Anchor GameObject (always active — holds the script) ──────────
        // _panelRoot is a CHILD so SetActive(false) never kills CompassPanel.
        var anchorRT = GetComponent<RectTransform>();
        anchorRT.sizeDelta = new Vector2(panelWidth, panelHeight);

        var panelRootGO = CreateChildGO("CompassPanelRoot", anchorRT);
        _panelRoot  = panelRootGO;
        _canvasGroup = panelRootGO.AddComponent<CanvasGroup>();

        var rootRT  = panelRootGO.GetComponent<RectTransform>();
        StretchFill(rootRT);   // fill the anchor object entirely

        // ── Background bar ────────────────────────────────────────────────
        var bg    = CreateChild<Image>("Background", rootRT);
        var bgRT  = bg.GetComponent<RectTransform>();
        StretchFill(bgRT);
        bg.color  = backgroundColor;

        // ── Mask container ────────────────────────────────────────────────
        var maskGO = CreateChildGO("Mask", rootRT);
        _maskContainer = maskGO.GetComponent<RectTransform>();
        _maskContainer.sizeDelta        = new Vector2(maskWidth, panelHeight);
        _maskContainer.anchoredPosition = Vector2.zero;
        maskGO.AddComponent<RectMask2D>();

        // ── Compass tape (3 copies wide for seamless wrapping) ────────────
        float tapeTotal = _tapeSingleWidth * 3f;
        var tapeGO      = CreateChildGO("CompassTape", _maskContainer);
        _compassTape    = tapeGO.GetComponent<RectTransform>();
        _compassTape.pivot            = new Vector2(0f, 0.5f);   // left-center — critical for scroll math
        _compassTape.anchorMin        = new Vector2(0f, 0.5f);
        _compassTape.anchorMax        = new Vector2(0f, 0.5f);
        _compassTape.sizeDelta        = new Vector2(tapeTotal, panelHeight);
        _compassTape.anchoredPosition = Vector2.zero;

        // Generate 3 identical copies of labels + ticks on the tape
        for (int copy = 0; copy < 3; copy++)
            GenerateTapeContent(copy * _tapeSingleWidth);

        // ── Center marker (fixed red pointer, outside mask) ───────────────
        BuildCenterMarker(rootRT);
    }

    private void GenerateTapeContent(float copyOffsetX)
    {
        // Anchor everything to the MIDDLE-LEFT of the tape (0, 0.5).
        // Y = 0 is now the vertical center — no more bottom-offset math.

        // ── Tick marks every 15°, skipping label positions (every 45°) ──
        for (int deg = 0; deg < 360; deg += 15)
        {
            if (deg % 45 == 0) continue;   // label sits here instead of a tick

            var tick   = CreateChild<Image>($"Tick_{deg + (int)(copyOffsetX / pixelsPerDegree)}", _compassTape);
            var tickRT = tick.GetComponent<RectTransform>();
            tickRT.pivot            = new Vector2(0.5f, 0.5f);
            tickRT.anchorMin        = new Vector2(0f,   0.5f);
            tickRT.anchorMax        = new Vector2(0f,   0.5f);
            tickRT.sizeDelta        = new Vector2(1f, 10f);
            tickRT.anchoredPosition = new Vector2(copyOffsetX + deg * pixelsPerDegree, 0f);
            tick.color              = minorTickColor;
        }

        // ── Direction labels ──────────────────────────────────────────────
        foreach (var (label, degree, isCardinal) in Directions)
        {
            var labelGO = CreateChildGO($"Label_{label}_{(int)(copyOffsetX)}", _compassTape);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.pivot           = new Vector2(0.5f, 0.5f);   // center pivot
            labelRT.anchorMin       = new Vector2(0f,   0.5f);   // middle-left of tape
            labelRT.anchorMax       = new Vector2(0f,   0.5f);
            labelRT.sizeDelta       = new Vector2(30f, 20f);
            labelRT.anchoredPosition = new Vector2(copyOffsetX + degree * pixelsPerDegree, 0f);

            var tmp               = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text              = label;
            tmp.fontSize          = isCardinal ? 14f : 11f;
            tmp.fontStyle         = isCardinal ? FontStyles.Bold : FontStyles.Normal;
            tmp.color             = isCardinal ? cardinalColor : intercardinalColor;
            tmp.alignment         = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private void BuildCenterMarker(RectTransform parent)
    {
        // A small downward-pointing triangle drawn as a rotated square Image
        var markerGO = CreateChildGO("CenterMarker", parent);
        var markerRT = markerGO.GetComponent<RectTransform>();
        markerRT.anchorMin       = new Vector2(0.5f, 0f);
        markerRT.anchorMax       = new Vector2(0.5f, 0f);
        markerRT.pivot           = new Vector2(0.5f, 0f);
        markerRT.sizeDelta       = new Vector2(10f, 10f);
        markerRT.anchoredPosition = new Vector2(0f, 2f);
        markerRT.localRotation   = Quaternion.Euler(0f, 0f, 45f);   // diamond shape

        var img  = markerGO.AddComponent<Image>();
        img.color = centerMarkerColor;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private T CreateChild<T>(string objName, RectTransform parent) where T : Component
    {
        var go = CreateChildGO(objName, parent);
        return go.AddComponent<T>();
    }

    private GameObject CreateChildGO(string objName, RectTransform parent)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt         = go.GetComponent<RectTransform>();
        rt.anchorMin   = new Vector2(0.5f, 0.5f);
        rt.anchorMax   = new Vector2(0.5f, 0.5f);
        rt.pivot       = new Vector2(0.5f, 0.5f);
        rt.localScale  = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin      = Vector2.zero;
        rt.anchorMax      = Vector2.one;
        rt.offsetMin      = Vector2.zero;
        rt.offsetMax      = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
