using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Player.Inventory.Storage;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

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

    [SerializeField] private GameObject highlightGameObject;

    [Header("Sound")]
    [SerializeField] private string clickSoundId = "UI_ItemClick";
    [SerializeField] private float clickVolumeScale = 1f;
    [SerializeField] private string hoverSoundId = "UI_ItemHover";
    [SerializeField] private float hoverVolumeScale = 1f;
    [SerializeField] private string beginDragSoundId = "UI_ItemBeginDrag";
    [SerializeField] private float beginDragVolumeScale = 1f;
    [SerializeField] private string endDragSoundId = "UI_ItemEndDrag";
    [SerializeField] private float endDragVolumeScale = 1f;

    private GridInventoryUI _gridUI;
    private DragDropManager _dragDrop;
    private GridPlacement _placement;
    private float _cellSize;

    private IEventBus _eventBus;
    private bool _suppressNextEnter;

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

        _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
    }

    private void OnDestroy()
    {
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
        _eventBus.Publish(new PlayUISoundEvent(beginDragSoundId, volumeScale: beginDragVolumeScale));
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

        _suppressNextEnter = true;
        _eventBus.Publish(new PlayUISoundEvent(endDragSoundId, volumeScale: endDragVolumeScale));
    }

    // ── Click Handlers ──

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _gridUI.ShowContextMenu(this, eventData.position);
            _eventBus.Publish(new PlayUISoundEvent(clickSoundId, volumeScale: clickVolumeScale));
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

            if (!_suppressNextEnter)
                _eventBus?.Publish(new PlayUISoundEvent(hoverSoundId, volumeScale: hoverVolumeScale));

            _suppressNextEnter = false;
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
