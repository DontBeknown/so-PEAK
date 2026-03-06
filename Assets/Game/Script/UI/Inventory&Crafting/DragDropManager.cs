using UnityEngine;
using DG.Tweening;

/// <summary>
/// Handles drag-and-drop math for the grid inventory.
/// Converts screen coordinates ↔ grid cell coordinates,
/// and manages ghost-canvas reparenting during drags.
/// </summary>
public class DragDropManager : MonoBehaviour
{
    [Header("Ghost Canvas (Overlay, high sort order)")]
    [SerializeField] private Canvas dragCanvas;

    [Header("Drag Animation")]
    [SerializeField] private float dragScale = 1.15f;
    [SerializeField] private float dragScaleDuration = 0.12f;
    [SerializeField] private float dropScaleDuration = 0.1f;

    private RectTransform _gridContainer;
    private float _cellSize;
    private Camera _uiCamera;
    private Canvas _parentCanvas;

    // Drag state
    private GridItemUI _dragItem;
    private Vector2 _grabOffset;       // grid-local offset for snap calculations
    private Vector3 _screenGrabOffset; // screen-space offset for visual follow
    private Transform _originalParent;
    private int _originalSiblingIndex;

    public bool IsDragging => _dragItem != null;
    public GridItemUI DragItem => _dragItem;

    /// <summary>
    /// Called by GridInventoryUI.Start() after the grid is built.
    /// </summary>
    public void SetReferences(RectTransform gridContainer, float cellSize, Canvas parentCanvas)
    {
        _gridContainer = gridContainer;
        _cellSize = cellSize;
        _parentCanvas = parentCanvas;

        // For Screen Space – Overlay canvases the camera is null
        _uiCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _parentCanvas.worldCamera;
    }

    /// <summary>
    /// Converts a screen position to a grid cell coordinate (col, row).
    /// Returns (-1,-1) if outside the grid.
    /// </summary>
    public Vector2Int ScreenToGrid(Vector2 screenPos)
    {
        if (_gridContainer == null) return new Vector2Int(-1, -1);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridContainer, screenPos, _uiCamera, out Vector2 localPoint);

        // gridContainer pivot is (0,1) → localPoint (0,0) is top-left
        int col = Mathf.FloorToInt(localPoint.x / _cellSize);
        int row = Mathf.FloorToInt(-localPoint.y / _cellSize); // Y goes down

        return new Vector2Int(col, row);
    }

    /// <summary>
    /// Converts a grid cell (col, row) to a local position inside gridContainer
    /// (top-left pivot, Y downward).
    /// </summary>
    public Vector2 GridToLocal(Vector2Int cell)
    {
        return new Vector2(cell.x * _cellSize, -cell.y * _cellSize);
    }

    public void BeginDrag(GridItemUI itemUI, Vector2 pointerScreenPos)
    {
        _dragItem = itemUI;

        // Remember original parent for snap-back
        _originalParent = itemUI.transform.parent;
        _originalSiblingIndex = itemUI.transform.GetSiblingIndex();

        // Grid-local offset for snap/cell calculations
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridContainer, pointerScreenPos, _uiCamera, out Vector2 localPointer);
        var itemLocal = GridToLocal(itemUI.Placement.Position);
        _grabOffset = localPointer - itemLocal;

        // Screen-space offset for visual follow (works across any canvas)
        _screenGrabOffset = (Vector3)pointerScreenPos - itemUI.transform.position;

        // Reparent to ghost canvas so it renders above everything
        if (dragCanvas != null)
        {
            itemUI.transform.SetParent(dragCanvas.transform, true);
        }

        // Scale up
        itemUI.transform.DOKill();
        itemUI.transform.DOScale(dragScale, dragScaleDuration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// Called every frame during drag. Returns the snapped grid cell the item
    /// would land on (top-left corner), taking the grab offset into account.
    /// </summary>
    public Vector2Int UpdateDrag(Vector2 pointerScreenPos)
    {
        if (_dragItem == null) return new Vector2Int(-1, -1);

        // Move the visual to follow the pointer (screen-space, works for any canvas)
        _dragItem.transform.position = (Vector3)pointerScreenPos - _screenGrabOffset;

        // Calculate which grid cell the top-left corner of the item maps to
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridContainer, pointerScreenPos, _uiCamera, out Vector2 gridLocal);
        Vector2 adjustedLocal = gridLocal - _grabOffset;

        int col = Mathf.RoundToInt(adjustedLocal.x / _cellSize);
        int row = Mathf.RoundToInt(-adjustedLocal.y / _cellSize);

        return new Vector2Int(col, row);
    }

    /// <summary>
    /// Ends the drag and reparents back to itemsParent.
    /// Returns the target grid cell for the move request.
    /// </summary>
    public Vector2Int EndDrag(Vector2 pointerScreenPos)
    {
        var targetCell = UpdateDrag(pointerScreenPos);

        // Reparent back regardless of success/failure
        if (_dragItem != null && _originalParent != null)
        {
            _dragItem.transform.SetParent(_originalParent, true);
            _dragItem.transform.SetSiblingIndex(_originalSiblingIndex);

            // Scale back to normal
            _dragItem.transform.DOKill();
            _dragItem.transform.DOScale(1f, dropScaleDuration).SetEase(Ease.OutQuad);
        }

        _dragItem = null;
        return targetCell;
    }

    public void CancelDrag()
    {
        if (_dragItem != null && _originalParent != null)
        {
            _dragItem.transform.SetParent(_originalParent, true);
            _dragItem.transform.SetSiblingIndex(_originalSiblingIndex);

            // Scale back to normal
            _dragItem.transform.DOKill();
            _dragItem.transform.DOScale(1f, dropScaleDuration).SetEase(Ease.OutQuad);
        }
        _dragItem = null;
    }
}
