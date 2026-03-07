using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Player.Inventory.Storage;
using Game.Core.DI;
using Game.Core.Events;
using DG.Tweening;

/// <summary>
/// Visual representation of one placed item on the grid.
/// Handles drag initiation, pointer clicks (context menu), and hover (tooltip).
/// </summary>
public class GridItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Equip Visual")]
    [SerializeField] private Color equippedColor = new Color(0.3f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    [Header("Equip Bounce")]
    [SerializeField] private float equipBounceScale = 1.2f;
    [SerializeField] private float equipBounceDuration = 0.25f;

    [SerializeField] private GameObject highlightGameObject;

    private GridInventoryUI _gridUI;
    private DragDropManager _dragDrop;
    private GridPlacement _placement;
    private float _cellSize;

    private IEventBus _eventBus;
    private EquipmentManager _equipmentManager;

    public GridPlacement Placement => _placement;

    public void Initialize(GridInventoryUI gridUI, DragDropManager dragDrop, GridPlacement placement, float cellSize)
    {
        _gridUI = gridUI;
        _dragDrop = dragDrop;
        _placement = placement;
        _cellSize = cellSize;

        // Size the rect to cover the item's grid area
        var rt = GetComponent<RectTransform>();
        rt.pivot = new Vector2(0f, 1f); // top-left
        rt.sizeDelta = new Vector2(placement.Size.x * cellSize, placement.Size.y * cellSize);

        // Position at the correct grid cell
        SnapToGridPosition();

        // Set icon
        if (iconImage != null && placement.Item != null && placement.Item.icon != null)
        {
            iconImage.sprite = placement.Item.icon;
            iconImage.preserveAspect = true;
        }

        // Subscribe to equipment events
        _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        _equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();

        if (_eventBus != null)
        {
            _eventBus.Subscribe<ItemEquippedEvent>(OnItemEquipped);
            _eventBus.Subscribe<ItemUnequippedEvent>(OnItemUnequipped);
        }

        // Reflect current equip state immediately
        RefreshEquippedVisual();
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<ItemEquippedEvent>(OnItemEquipped);
            _eventBus.Unsubscribe<ItemUnequippedEvent>(OnItemUnequipped);
        }
    }

    private void OnItemEquipped(ItemEquippedEvent evt)
    {
        if (_placement?.Item != null && ReferenceEquals(_placement.Item, evt.Item))
            SetEquippedVisual(true);
    }

    private void OnItemUnequipped(ItemUnequippedEvent evt)
    {
        if (_placement?.Item != null && ReferenceEquals(_placement.Item, evt.Item))
            SetEquippedVisual(false);
    }

    private void RefreshEquippedVisual()
    {
        if (_placement?.Item is EquipmentItem equipItem && _equipmentManager != null)
        {
            var equipped = _equipmentManager.GetEquippedItem(equipItem.EquipmentSlot);
            SetEquippedVisual(ReferenceEquals(equipped, equipItem));
        }
        else
        {
            SetEquippedVisual(false);
        }
    }

    private void SetEquippedVisual(bool equipped)
    {
        if (backgroundImage != null)
            backgroundImage.color = equipped ? equippedColor : normalColor;

        // Bounce the icon and background to give feedback
        var targets = new Transform[] {
            iconImage != null ? iconImage.transform : null,
            backgroundImage != null ? backgroundImage.transform : null
        };
        foreach (var t in targets)
        {
            if (t == null) continue;
            t.DOKill();
            t.localScale = Vector3.one;
            t.DOPunchScale(Vector3.one * (equipBounceScale - 1f), equipBounceDuration, vibrato: 2, elasticity: 0.5f)
             .SetLink(t.gameObject);
        }
    }

    public void SnapToGridPosition()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(
            _placement.Position.x * _cellSize,
            -_placement.Position.y * _cellSize
        );
    }

    // ── Drag Handlers ──

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        _dragDrop.BeginDrag(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        var targetCell = _dragDrop.UpdateDrag(eventData.position);
        _gridUI.ShowHighlight(targetCell, _placement.Size, _placement);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var targetCell = _dragDrop.EndDrag(eventData.position);

        // If the item was already removed from the grid by another drop handler
        // (e.g. dropped on NearbyPickupUI), destroy this orphan immediately
        if (_gridUI == null || _placement == null ||
            _gridUI.GridStorage == null ||
            !_gridUI.GridStorage.HasPlacement(_placement))
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        bool success = _gridUI.RequestMoveItem(_placement, targetCell);

        // Always snap to current data position (handles both success and failure)
        SnapToGridPosition();
        _gridUI.ClearHighlight();
    }

    // ── Click Handlers ──

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _gridUI.ShowContextMenu(this, eventData.position);
        }
    }

    // ── Hover Handlers ──

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_dragDrop == null || _gridUI == null) return;
        if (!_dragDrop.IsDragging)
        {
            _gridUI.ShowTooltip(this);
            if (highlightGameObject != null)
                highlightGameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_gridUI != null)
            _gridUI.HideTooltip();

        if (highlightGameObject != null)
            highlightGameObject.SetActive(false);
    }
}
