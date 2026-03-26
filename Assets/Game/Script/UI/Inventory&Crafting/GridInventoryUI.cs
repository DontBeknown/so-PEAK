using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Core.DI;
using Game.Player.Inventory;
using Game.Player.Inventory.Storage;
using Game.Player.Inventory.Events;
using Game.Sound.Events;

/// <summary>
/// Main UI panel for the 2D grid inventory.
/// Builds the cell grid (using GridLayoutGroup), spawns item visuals,
/// handles highlight during drag, and wires context menu / tooltip.
/// </summary>
public class GridInventoryUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Grid")]
    [SerializeField] private RectTransform gridContainer;   // pivot (0,1) top-left
    [SerializeField] private Transform cellsParent;          // has GridLayoutGroup
    [SerializeField] private Transform itemsParent;          // items positioned manually
    [SerializeField] private GameObject cellPrefab;          // GridCellUI prefab
    [SerializeField] private GameObject gridItemPrefab;      // GridItemUI prefab
    [SerializeField] private float cellSize = 64f;

    [Header("Sibling Panels")]
    [SerializeField] private EquipmentUI equipmentUI;
    [SerializeField] private TooltipUI tooltipUI;
    [SerializeField] private ContextMenuUI contextMenuUI;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI thirstText;
    [SerializeField] private TextMeshProUGUI staminaText;

    // Runtime references
    private InventoryManagerRefactored _inventoryManager;
    private GridInventoryStorage _gridStorage;
    private DragDropManager _dragDrop;
    private Game.Core.Events.IEventBus _eventBus;
    private PlayerStats _playerStats;
    private EquipmentManager _equipmentManager;

    private GridCellUI[,] _cells;
    private List<GridItemUI> _itemUIs = new List<GridItemUI>();
    private bool _isOpen;

    public bool IsOpen => _isOpen;
    public bool IsDragging => _dragDrop != null && _dragDrop.IsDragging;
    public GridInventoryStorage GridStorage => _gridStorage;
    public DragDropManager DragDrop => _dragDrop;

    // ──────────────────────────────────────────────
    #region Unity Lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        // Resolve services
        _inventoryManager = ServiceContainer.Instance.Get<InventoryManagerRefactored>();
        _gridStorage = ServiceContainer.Instance.Get<GridInventoryStorage>();
        _eventBus = ServiceContainer.Instance.TryGet<Game.Core.Events.IEventBus>();
        _playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        _equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();

        if (tooltipUI == null)
            tooltipUI = ServiceContainer.Instance.TryGet<TooltipUI>();
        if (contextMenuUI == null)
            contextMenuUI = ServiceContainer.Instance.TryGet<ContextMenuUI>();

        // DragDropManager lives on the same GameObject
        _dragDrop = GetComponent<DragDropManager>();
        if (_dragDrop == null)
            _dragDrop = gameObject.AddComponent<DragDropManager>();

        var parentCanvas = GetComponentInParent<Canvas>();
        _dragDrop.SetReferences(gridContainer, cellSize, parentCanvas);

        BuildGrid();
        SubscribeToEvents();
        RefreshGrid();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        if (_isOpen)
        {
            UpdateStatsDisplay();
        }
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Grid Construction
    // ──────────────────────────────────────────────

    private void BuildGrid()
    {
        if (cellsParent == null || cellPrefab == null || _gridStorage == null) return;

        int w = _gridStorage.Width;
        int h = _gridStorage.Height;

        // Configure GridLayoutGroup on cellsParent
        var layoutGroup = cellsParent.GetComponent<GridLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = cellsParent.gameObject.AddComponent<GridLayoutGroup>();

        layoutGroup.cellSize = new Vector2(cellSize, cellSize);
        // spacing is not overridden — use the value set in the Inspector
        layoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layoutGroup.constraintCount = w;

        // Size the grid container to fit all cells
        gridContainer.sizeDelta = new Vector2(w * cellSize, h * cellSize);

        // Clear existing cells
        foreach (Transform child in cellsParent)
            Destroy(child.gameObject);

        _cells = new GridCellUI[w, h];

        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                GameObject cellObj = Instantiate(cellPrefab, cellsParent);
                var cellUI = cellObj.GetComponent<GridCellUI>();
                if (cellUI != null)
                {
                    cellUI.Initialize(this, new Vector2Int(col, row));
                }
                _cells[col, row] = cellUI;
            }
        }
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Refresh & Item Visuals
    // ──────────────────────────────────────────────

    public void RefreshGrid()
    {
        if (_gridStorage == null) return;

        // Destroy existing item visuals
        foreach (var itemUI in _itemUIs)
        {
            if (itemUI != null)
                Destroy(itemUI.gameObject);
        }
        _itemUIs.Clear();

        // Recreate from current placements
        var placements = _gridStorage.GetAllPlacements();
        foreach (var placement in placements)
        {
            SpawnItemUI(placement);
        }
    }

    private void SpawnItemUI(GridPlacement placement)
    {
        if (gridItemPrefab == null || itemsParent == null) return;

        GameObject itemObj = Instantiate(gridItemPrefab, itemsParent);
        var itemUI = itemObj.GetComponent<GridItemUI>();
        if (itemUI != null)
        {
            itemUI.Initialize(this, _dragDrop, placement, cellSize);
            _itemUIs.Add(itemUI);
        }
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Drag Highlight
    // ──────────────────────────────────────────────

    public void ShowHighlight(Vector2Int topLeft, Vector2Int size, GridPlacement ignore)
    {
        ClearHighlight();

        if (_cells == null || _gridStorage == null) return;

        bool canPlace = _gridStorage.CanPlaceAt(topLeft, size, ignore);

        for (int x = topLeft.x; x < topLeft.x + size.x; x++)
        {
            for (int y = topLeft.y; y < topLeft.y + size.y; y++)
            {
                if (x >= 0 && x < _gridStorage.Width && y >= 0 && y < _gridStorage.Height)
                {
                    _cells[x, y]?.SetHighlight(canPlace);
                }
            }
        }
    }

    /// <summary>
    /// Called by GridCellUI.OnPointerEnter during a drag.
    /// </summary>
    public void ShowHighlightAtPointer(Vector2 screenPos)
    {
        if (!IsDragging || _dragDrop.DragItem == null) return;

        var targetCell = _dragDrop.UpdateDrag(screenPos);
        var placement = _dragDrop.DragItem.Placement;
        ShowHighlight(targetCell, placement.Size, placement);
    }

    public void ClearHighlight()
    {
        if (_cells == null) return;

        for (int x = 0; x < _gridStorage.Width; x++)
            for (int y = 0; y < _gridStorage.Height; y++)
                _cells[x, y]?.SetNormal();
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Move / Use / Drop
    // ──────────────────────────────────────────────

    public bool RequestMoveItem(GridPlacement placement, Vector2Int newPos)
    {
        if (_inventoryManager == null || placement == null) return false;
        return _inventoryManager.MoveItem(placement, newPos);
    }

    public void UseItem(GridItemUI itemUI)
    {
        if (itemUI == null || itemUI.Placement == null) return;

        var item = itemUI.Placement.Item;

        // Equipment → equip
        if (item is EquipmentItem equipItem && _equipmentManager != null)
        {
            // Remove the exact clicked placement first (same behavior style as drop),
            // then equip without generic inventory-type removal.
            if (_inventoryManager != null)
            {
                _inventoryManager.RemoveFromGrid(itemUI.Placement, suppressNotification: true);
            }
            _equipmentManager.Equip(equipItem, syncInventory: false);
            RefreshGrid();
            return;
        }

        // Consumable → apply effects then remove THIS specific placement
        if (item.isConsumable && _inventoryManager != null)
        {
            // Apply effects manually (same as ConsumeItem but we control which placement gets removed)
            if (item.consumableEffects != null && _playerStats != null)
            {
                var effectSystem = _inventoryManager.EffectSystem;
                foreach (var effect in item.consumableEffects)
                {
                    effectSystem.ApplyEffect(effect, _playerStats);
                }
            }

            // Remove the exact placement the user clicked on
            _inventoryManager.RemoveFromGrid(itemUI.Placement);
            UpdateStatsDisplay();
        }
    }

    public void DropItem(GridItemUI itemUI)
    {
        if (itemUI == null || itemUI.Placement == null || _inventoryManager == null) return;
        var item = itemUI.Placement.Item;
        _inventoryManager.RemoveFromGrid(itemUI.Placement);
        WorldItemSpawner.SpawnDroppedItem(item, 1);
        _eventBus.Publish(new PlayUISoundEvent("UI_ItemEndDrag", volumeScale: 0.3f));
    }

    public bool RotateItem(GridItemUI itemUI)
    {
        if (itemUI == null || itemUI.Placement == null || _inventoryManager == null) return false;
        _eventBus.Publish(new PlayUISoundEvent("UI_ItemEndDrag", volumeScale: 0.3f));
        return _inventoryManager.RotateItem(itemUI.Placement);
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Context Menu & Tooltip
    // ──────────────────────────────────────────────

    public void ShowContextMenu(GridItemUI itemUI, Vector2 screenPos)
    {
        if (contextMenuUI == null || itemUI == null) return;
        contextMenuUI.ShowGridItemMenu(this, itemUI);
    }

    public void ShowTooltip(GridItemUI itemUI)
    {
        if (tooltipUI == null || itemUI == null || itemUI.Placement == null) return;
        tooltipUI.ShowTooltip(itemUI.Placement.Item, 1);
    }

    public void HideTooltip()
    {
        if (tooltipUI != null)
            tooltipUI.HideTooltip();
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Panel Show/Hide (for TabbedInventoryUI)
    // ──────────────────────────────────────────────

    public void ShowInventoryPanel()
    {
        _isOpen = true;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);

        if (equipmentUI != null)
            equipmentUI.ShowEquipmentPanel();

        RefreshGrid();
        UpdateStatsDisplay();
    }

    public void HideInventoryPanel()
    {
        _isOpen = false;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (tooltipUI != null)
            tooltipUI.HideTooltip();

        /*if (equipmentUI != null)
            equipmentUI.HideEquipmentPanel();*/

        if (contextMenuUI != null)
            contextMenuUI.HideMenu();
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Events
    // ──────────────────────────────────────────────

    private void SubscribeToEvents()
    {
        if (_eventBus == null) return;
        _eventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        _eventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
        _eventBus.Subscribe<ItemRemovedEvent>(OnItemRemoved);
    }

    private void UnsubscribeFromEvents()
    {
        if (_eventBus == null) return;
        _eventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        _eventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        _eventBus.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnInventoryChanged(InventoryChangedEvent evt) => RefreshGrid();

    private void OnItemAdded(ItemAddedEvent evt)
    {
        UpdateStatsDisplay();
        RefreshGrid();
    }

    private void OnItemRemoved(ItemRemovedEvent evt)
    {
        UpdateStatsDisplay();
        RefreshGrid();
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Stats Display
    // ──────────────────────────────────────────────

    private void UpdateStatsDisplay()
    {
        if (_playerStats == null) return;

        if (healthText != null)
            healthText.text = $"{_playerStats.Health:F0}/{_playerStats.MaxHealth:F0}";
        if (hungerText != null)
            hungerText.text = $"{_playerStats.Hunger:F0}/{_playerStats.MaxHunger:F0}";
        if (thirstText != null)
            thirstText.text = $"{_playerStats.Thirst:F0}/{_playerStats.MaxThirst:F0}";
        if (staminaText != null)
            staminaText.text = $"{_playerStats.Stamina:F0}/{_playerStats.MaxStamina:F0}";
    }

    #endregion
}
