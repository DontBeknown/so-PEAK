# Grid Inventory (Unturned-Style) — Implementation Plan

**Feature:** Drag-and-drop 2D grid inventory where each item occupies a rectangular area (1×1 to N×M cells). No item stacking. Items can be freely repositioned by dragging.

---

## Architecture Overview

```
InventoryManagerRefactored
  └─ GridInventoryStorage          ← 2D grid backend (new)
       └─ GridPlacement            ← per-item position/size record (new)
  └─ GridStorageAdapter            ← wraps GridInventoryStorage as IInventoryStorage (new)

GridInventoryUI  (MonoBehaviour)   ← main UI panel (new)
  ├─ GridCellUI  [w × h]          ← cell background + drop highlight (new)
  ├─ GridItemUI  [per item]        ← draggable item visual (new)
  └─ DragDropManager              ← screen↔grid coord conversion, drag state (new)
```

All new systems sit alongside existing code. `GridStorageAdapter` implements `IInventoryStorage` so every existing system (crafting, commands, equipment) continues to work unchanged.

---

## Phase 1 — Data Layer

### 1.1 `InventoryItem.cs` — add grid size field

**File:** `Assets/Game/Script/Player/Inventory/InventoryItem.cs`

```csharp
[Header("Grid Size (cells)")]
public Vector2Int gridSize = Vector2Int.one;

private void OnValidate()
{
    maxStackSize = 1;
    gridSize = Vector2Int.Max(gridSize, Vector2Int.one);
}
```

- `gridSize` is set per ScriptableObject asset in the Inspector.
- `OnValidate` forces `maxStackSize = 1` (grid inventory has no stacking) and clamps size to minimum 1×1.

---

### 1.2 `GridPlacement.cs` — placement record

**File:** `Assets/Game/Script/Player/Inventory/Storage/GridPlacement.cs`  
**Namespace:** `Game.Player.Inventory.Storage`

Tracks a single item placed on the grid.

| Member | Type | Description |
|---|---|---|
| `Item` | `InventoryItem` | The item occupying this area |
| `Position` | `Vector2Int` | Top-left cell (col, row) |
| `Size` | `Vector2Int` | Width × height in cells |
| `Bounds` | `RectInt` | Convenience rect for overlap checks |
| `OccupiesCell(Vector2Int)` | `bool` | Returns true if the given cell is inside this placement |
| `SetPosition(Vector2Int)` | `internal` | Used by `GridInventoryStorage.MoveItem` only |

---

### 1.3 `GridInventoryStorage.cs` — grid backend

**File:** `Assets/Game/Script/Player/Inventory/Storage/GridInventoryStorage.cs`  
**Namespace:** `Game.Player.Inventory.Storage`

```
_grid[col, row]   → GridPlacement reference (or null if empty)
_placements       → List<GridPlacement> (all current placements)
```

| Method | Description |
|---|---|
| `CanPlaceAt(topLeft, size, ignore)` | Returns true if all cells are empty (ignoring one placement for move ops) |
| `PlaceItem(item, topLeft)` | Places item; throws if occupied |
| `AutoPlace(item)` | Row-by-row scan, returns `GridPlacement` or null if full |
| `RemoveItem(placement)` | Clears all occupied cells |
| `MoveItem(placement, newPos)` | Atomic move with rollback on failure |
| `GetPlacementAt(cell)` | Returns whichever placement occupies that cell |
| `GetAllPlacements()` | Returns a snapshot list |
| `HasItem(item)` | Linear search |
| `FindPlacement(item)` | Returns placement for a specific item |
| `Width`, `Height` | Grid dimensions |

---

### 1.4 `GridStorageAdapter.cs` — backward-compat wrapper

**File:** `Assets/Game/Script/Player/Inventory/Storage/GridStorageAdapter.cs`  
**Namespace:** `Game.Player.Inventory.Storage`

Implements `IInventoryStorage` using `GridInventoryStorage` internally. Keeps all existing commands, crafting, and equipment code untouched.

| `IInventoryStorage` method | Grid behaviour |
|---|---|
| `AddItem(item, qty)` | Calls `AutoPlace` once per unit |
| `RemoveItem(item, qty)` | LIFO removal from placements list |
| `GetAllSlots()` | Synthesises `List<InventorySlot>` from placements |
| `HasItem`, `GetItemCount` | Delegates to grid |

---

### 1.5 `InventoryEvents.cs` — three new grid events

**File:** `Assets/Game/Script/Player/Inventory/Events/InventoryEvents.cs`

```csharp
public class ItemPlacedEvent          { InventoryItem Item; Vector2Int Position; Vector2Int Size; }
public class ItemMovedEvent           { InventoryItem Item; Vector2Int OldPosition; Vector2Int NewPosition; }
public class ItemRemovedFromGridEvent { InventoryItem Item; Vector2Int Position; }
```

---

## Phase 2 — Manager Integration

### 2.1 `InventoryManagerRefactored.cs` — expose grid API

**File:** `Assets/Game/Script/Player/Inventory/InventoryManagerRefactored.cs`

New inspector fields:
```csharp
[SerializeField] private int gridWidth  = 10;
[SerializeField] private int gridHeight = 6;
```

`InitializeSystems()` change — replace slot-based storage:
```csharp
_gridStorage = new GridInventoryStorage(gridWidth, gridHeight);
_storage     = new GridStorageAdapter(_gridStorage);
```

`RegisterServices()` — register concrete grid type:
```csharp
ServiceContainer.Instance.Register(_gridStorage);
```

New **Grid API region**:

| Method | Description |
|---|---|
| `PlaceItemAt(item, pos)` | Calls `_gridStorage.PlaceItem`, publishes `ItemPlacedEvent` |
| `MoveItem(placement, newPos)` | Calls `_gridStorage.MoveItem`, publishes `ItemMovedEvent` |
| `RemoveFromGrid(placement)` | Calls `_gridStorage.RemoveItem`, publishes `ItemRemovedFromGridEvent` |
| `GetAllPlacements()` | Delegates to storage |
| `GridStorage` (property) | Returns `_gridStorage` |
| `GridSize` (property) | Returns `new Vector2Int(gridWidth, gridHeight)` |

---

## Phase 3 — UI Components

### 3.1 `DragDropManager.cs`

**File:** `Assets/Game/Script/UI/Inventory&Crafting/DragDropManager.cs`  

Attached to same GameObject as `GridInventoryUI`. Handles all drag math.

| Method | Description |
|---|---|
| `SetReferences(gridContainer, cellSize)` | Called by `GridInventoryUI.Start()` |
| `ScreenToGrid(screenPos)` | `RectTransformUtility` → divide by cellSize, flip Y |
| `GridToLocal(cell)` | `new Vector2(cell.x * cellSize, -cell.y * cellSize)` |
| `BeginDrag(item, pointerPos)` | Records dragged item + grab offset |
| `UpdateDrag(pointerPos)` | Returns snapped grid cell for highlight |
| `EndDrag(pointerPos)` | Calls `GridInventoryUI.RequestMoveItem` |
| `CancelDrag()` | Clears state |
| `IsDragging` (property) | Bool |
| `DragItem` (property) | Currently dragged `GridItemUI` |

---

### 3.2 `GridCellUI.cs`

**File:** `Assets/Game/Script/UI/Inventory&Crafting/GridCellUI.cs`  

One instance per grid cell. Implements `IDropHandler`, `IPointerEnterHandler`, `IPointerExitHandler`.

| Method | Description |
|---|---|
| `Initialize(gridUI, position)` | Stores cell position |
| `SetNormal()` | Resets highlight image alpha to 0 |
| `SetHighlight(bool valid)` | Green tint (valid) or red tint (invalid) |

**Prefab requirements:**
- Root: `Image` (cell background, transparent border)
- Child: `Image` named `Highlight` (starts with alpha 0)
- Component: `GridCellUI`

---

### 3.3 `GridItemUI.cs`

**File:** `Assets/Game/Script/UI/Inventory&Crafting/GridItemUI.cs`  

One instance per placed item. Implements `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IPointerClickHandler`, `IPointerEnterHandler`, `IPointerExitHandler`.

| Member | Description |
|---|---|
| `Initialize(gridUI, dragDrop, placement, cellSize)` | Sizes rect to `size * cellSize`, positions at `position * cellSize` (top-left pivot), shows icon |
| `SnapToGridPosition(cellSize)` | Snaps `anchoredPosition` to `placement.Position * cellSize` |
| `Placement` (property) | Exposed `GridPlacement` reference |
| `OnBeginDrag` | `CanvasGroup.alpha = 0.6`, `blocksRaycasts = false`, calls `DragDropManager.BeginDrag` |
| `OnDrag` | Calls `DragDropManager.UpdateDrag` → `GridInventoryUI.ShowHighlight` |
| `OnEndDrag` | Restores alpha/raycast, calls `DragDropManager.EndDrag`; snaps back if move failed |
| Right-click | `GridInventoryUI.ShowContextMenu(this, screenPos)` |
| Hover enter/exit | `TooltipUI.ShowTooltip` / `HideTooltip` |

**Prefab requirements:**
- Root: `Image` (item background) + `CanvasGroup`
- Child: `Image` named `Icon` (`raycastTarget = false`)
- Component: `GridItemUI`

---

### 3.4 `GridInventoryUI.cs` — main panel

**File:** `Assets/Game/Script/UI/Inventory&Crafting/GridInventoryUI.cs`

Inspector references:

| Field | Description |
|---|---|
| `inventoryPanel` | Root GameObject toggled on/off |
| `gridContainer` | `RectTransform` with pivot `(0, 1)` top-left |
| `cellsParent` | Parent for `GridCellUI` instances |
| `itemsParent` | Parent for `GridItemUI` instances (renders on top of cells) |
| `cellPrefab` | GridCellUI prefab |
| `gridItemPrefab` | GridItemUI prefab |
| `cellSize` | Pixel size of one cell (default 64) |
| `equipmentUI` | Sibling EquipmentUI panel |
| `tooltipUI` | TooltipUI reference |
| `contextMenuUI` | ContextMenuUI reference |
| `pauseGameWhenOpen` | Freezes Time.timeScale and unlocks cursor |

Key methods:

| Method | Description |
|---|---|
| `BuildGrid()` | Instantiates `w × h` `GridCellUI` prefabs at correct positions |
| `RefreshGrid()` | Destroys all `GridItemUI`, recreates from `_gridStorage.GetAllPlacements()` |
| `ShowHighlight(topLeft, size, ignore)` | Calls `CanPlaceAt`; colours cells green/red |
| `ClearHighlight()` | Resets all cells to normal |
| `RequestMoveItem(placement, newPos)` | Calls `_inventoryManager.MoveItem`, snaps visual |
| `ShowInventoryPanel()` | For `TabbedInventoryUI` — shows panel without managing pause |
| `HideInventoryPanel()` | For `TabbedInventoryUI` — hides panel |
| `UseItem(itemUI)` | Equip or consume |
| `DropItem(itemUI)` | Remove from grid |
| `ShowContextMenu(itemUI, screenPos)` | Delegates to `ContextMenuUI.ShowGridItemMenu` |

Events subscribed (from `Game.Player.Inventory.Events`):
- `InventoryChangedEvent` → `RefreshGrid()`
- `ItemAddedEvent` → `UpdateStatsDisplay()` + `RefreshGrid()`
- `ItemRemovedEvent` → `UpdateStatsDisplay()` + `RefreshGrid()`

---

## Phase 4 — Wiring Existing Systems

### 4.1 `TabbedInventoryUI.cs`

Change `InventoryUI` reference → `GridInventoryUI`. Calls `ShowInventoryPanel()` / `HideInventoryPanel()` on tab switch.

### 4.2 `InventoryUIAdapter.cs`

Change wrapped type from `InventoryUI` → `GridInventoryUI`. `Show()` / `Hide()` delegate to `GridInventoryUI`.

### 4.3 `EquipmentSlotUI.cs` — drag-to-equip

`OnDrop` extracts `GridItemUI` from the dropped object, casts its item to `EquipmentItem`, and calls `TryEquipItem` if the slot type matches.

```csharp
public void OnDrop(PointerEventData eventData)
{
    var gridItem  = eventData.pointerDrag?.GetComponent<GridItemUI>();
    var equipItem = gridItem?.Placement?.Item as EquipmentItem;
    if (equipItem != null && equipItem.EquipmentSlot == slotType)
        TryEquipItem(equipItem);
}
```

### 4.4 `ContextMenuUI.cs` — grid item menu

New fields: `currentGridUI`, `currentGridItemUI`.

New method `ShowGridItemMenu(GridInventoryUI gridUI, GridItemUI itemUI)` builds **Equip / Consume / Drop** buttons routing to `gridUI.UseItem()` / `gridUI.DropItem()`.

---

## Phase 5 — Unity Scene Setup

After scripts compile (Unity regenerates `.csproj` automatically on open):

### Prefabs to create

#### GridCell prefab
```
GameObject "GridCell"
  ├─ Image (background, 64×64, transparent border colour)
  ├─ Image "Highlight" (child, fills parent, starts alpha 0)
  └─ GridCellUI (component)
```

#### GridItem prefab
```
GameObject "GridItem"
  ├─ Image (background, pivot top-left)
  ├─ CanvasGroup
  ├─ GridItemUI (component)
  └─ Image "Icon" (child, fills parent, raycastTarget = false)
```

### Scene hierarchy for GridInventoryUI

```
GridInventoryUI  [GridInventoryUI + DragDropManager]
  └─ InventoryPanel
       ├─ GridContainer   [RectTransform, pivot (0,1), anchored top-left]
       │    ├─ CellsParent
       │    └─ ItemsParent
       ├─ StatsBar (optional TMP texts: Health / Hunger / Thirst / Stamina)
       └─ CloseButton
```

### Inspector wiring checklist

- [ ] `GridInventoryUI` → assign `inventoryPanel`, `gridContainer`, `cellsParent`, `itemsParent`, `closeButton`
- [ ] `GridInventoryUI` → assign `cellPrefab`, `gridItemPrefab`
- [ ] `GridInventoryUI` → assign `equipmentUI`, `tooltipUI`, `contextMenuUI`
- [ ] `InventoryManagerRefactored` → set `gridWidth` / `gridHeight` (default 10 × 6)
- [ ] `TabbedInventoryUI` → re-assign `inventoryUI` field to `GridInventoryUI`
- [ ] Item ScriptableObject assets → set `gridSize` (1×1 default; e.g. rifle = 2×1, backpack = 2×2)

---

## Namespace Notes

| Symbol | Namespace |
|---|---|
| `IEventBus` | `Game.Core.Events` |
| `InventoryChangedEvent`, `ItemAddedEvent`, `ItemRemovedEvent` | `Game.Player.Inventory.Events` |
| `GridInventoryStorage`, `GridPlacement`, `GridStorageAdapter` | `Game.Player.Inventory.Storage` |

`GridInventoryUI` uses only `Game.Player.Inventory.Events` — **not** `Game.Core.Events` — to avoid the CS0104 ambiguous reference. `IEventBus` is referenced with its full path `Game.Core.Events.IEventBus`.

---

## File Checklist

| File | Status |
|---|---|
| `Player/Inventory/InventoryItem.cs` | Modified — `gridSize` + `OnValidate` |
| `Player/Inventory/Storage/GridPlacement.cs` | **New** |
| `Player/Inventory/Storage/GridInventoryStorage.cs` | **New** |
| `Player/Inventory/Storage/GridStorageAdapter.cs` | **New** |
| `Player/Inventory/Events/InventoryEvents.cs` | Modified — 3 new events |
| `Player/Inventory/InventoryManagerRefactored.cs` | Modified — grid storage + Grid API |
| `UI/Inventory&Crafting/DragDropManager.cs` | **New** |
| `UI/Inventory&Crafting/GridCellUI.cs` | **New** |
| `UI/Inventory&Crafting/GridItemUI.cs` | **New** |
| `UI/Inventory&Crafting/GridInventoryUI.cs` | **New** |
| `UI/Inventory&Crafting/TabbedInventoryUI.cs` | Modified — uses `GridInventoryUI` |
| `UI/Adapters/InventoryUIAdapter.cs` | Modified — wraps `GridInventoryUI` |
| `UI/Inventory&Crafting/EquipmentSlotUI.cs` | Modified — `OnDrop` implemented |
| `UI/HIUD/ContextMenuUI.cs` | Modified — `ShowGridItemMenu` added |
