using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// One instance per grid cell. Provides drop-target and hover highlight functionality.
/// </summary>
public class GridCellUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image highlightImage;

    private GridInventoryUI _gridUI;
    private Vector2Int _cellPosition;

    private static readonly Color ValidColor = new Color(0f, 1f, 0f, 0.3f);
    private static readonly Color InvalidColor = new Color(1f, 0f, 0f, 0.3f);

    public Vector2Int CellPosition => _cellPosition;

    public void Initialize(GridInventoryUI gridUI, Vector2Int position)
    {
        _gridUI = gridUI;
        _cellPosition = position;
        SetNormal();
    }

    public void SetNormal()
    {
        if (highlightImage != null)
        {
            var c = highlightImage.color;
            c.a = 0f;
            highlightImage.color = c;
        }
    }

    public void SetHighlight(bool valid)
    {
        if (highlightImage != null)
        {
            highlightImage.color = valid ? ValidColor : InvalidColor;
        }
    }

    // ── EventSystem handlers ──

    public void OnDrop(PointerEventData eventData)
    {
        // Drop is handled by DragDropManager.EndDrag → GridInventoryUI.RequestMoveItem
        // This handler exists so OnDrop can fire on the cell (Unity requires it)
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_gridUI != null && _gridUI.IsDragging)
        {
            _gridUI.ShowHighlightAtPointer(eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Highlight is managed by GridInventoryUI during drag; no per-cell clear needed
    }
}
