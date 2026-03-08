# UI System Overview

**Location:** `Assets/Game/Script/UI/`  
**Last Updated:** February 16, 2026

---

## What is This System?

The **UI System** provides a centralized, event-driven user interface framework with:
- **Service Locator Pattern** - Unified access to all UI panels
- **Panel Management** - Open/close panels with proper lifecycle
- **Cursor Control** - Automatic cursor show/hide based on UI state
- **Event-Driven Updates** - UI subscribes to game events, no polling
- **Adapter Pattern** - Clean separation between UI and game logic
- **Blur Effects** - Visual feedback for menus/overlays

This system follows **MVVM-like architecture** with adapters acting as ViewModels.

---

## Key Components

### 1. UIServiceProvider

**File:** `UI/UIServiceProvider.cs`

**Purpose:** Central service locator for all UI panels.

**Pattern:** Service Locator + Facade

**Key Methods:**
```csharp
public T GetPanel<T>() where T : MonoBehaviour, IUIPanel
public void OpenPanel<T>() where T : MonoBehaviour, IUIPanel
public void ClosePanel<T>() where T : MonoBehaviour, IUIPanel
public bool IsPanelOpen<T>() where T : MonoBehaviour, IUIPanel
public ICursorManager GetCursorManager()
```

**Responsibilities:**
- Panel registration and retrieval
- Opening/closing panels
- Managing cursor visibility
- Coordinating panel lifecycle

### 2. UIPanelController

**File:** `UI/UIPanelController.cs`

**Purpose:** Manages panel open/close state and transitions.

**Features:**
```csharp
private Dictionary<Type, IUIPanel> registeredPanels;

public void RegisterPanel(IUIPanel panel)
public void OpenPanel<T>()
{
    var panel = GetPanel<T>();
    panel.Show();
    OnPanelOpened?.Invoke(panel);
}
public void ClosePanel<T>()
{
    var panel = GetPanel<T>();
    panel.Hide();
    OnPanelClosed?.Invoke(panel);
}
```

### 3. IUIPanel Interface

**File:** `UI/IUIPanel.cs`

**Purpose:** Contract for all UI panels.

```csharp
public interface IUIPanel
{
    void Show();
    void Hide();
    void Refresh();
    bool IsVisible { get; }
}
```

### 4. CursorManager

**File:** `UI/CursorManager.cs`

**Purpose:** Controls cursor visibility and lock state.

**Pattern:** State Machine

**Methods:**
```csharp
public void ShowCursor()
{
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
}

public void HideCursor()
{
    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;
}

public void SetCursorState(CursorState state)
{
    switch (state)
    {
        case CursorState.UI:
            ShowCursor();
            break;
        case CursorState.Gameplay:
            HideCursor();
            break;
    }
}
```

**States:**
- **UI Mode:** Cursor visible, free movement
- **Gameplay Mode:** Cursor hidden, locked to center

### 5. UI Adapters

**Purpose:** Bridge between UI panels and game systems.

**Pattern:** Adapter

**Implementation:**
```csharp
public class InventoryUIAdapter : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;
    private InventoryManagerRefactored inventoryManager;
    private IEventBus eventBus;
    
    private void Awake()
    {
        var container = ServiceContainer.Instance;
        inventoryManager = container.TryGet<InventoryManagerRefactored>();
        eventBus = container.TryGet<IEventBus>();
        
        // Subscribe to events
        eventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
        eventBus.Subscribe<ItemRemovedEvent>(OnItemRemoved);
    }
    
    private void OnItemAdded(ItemAddedEvent e)
    {
        inventoryUI.UpdateSlot(e.slotIndex);
    }
    
    private void OnItemRemoved(ItemRemovedEvent e)
    {
        inventoryUI.UpdateSlot(e.slotIndex);
    }
}
```

**Adapters:**
- **InventoryUIAdapter** - Inventory ↔ UI
- **EquipmentUIAdapter** - Equipment ↔ UI
- **CraftingUIAdapter** - Crafting ↔ UI

### 6. Major UI Panels

#### GridInventoryUI
**File:** `UI/Inventory&Crafting/GridInventoryUI.cs`

**Features:**
- Unturned-style Grid-based slot display.
- Drag-and-drop support with multiple grid sizes (e.g. 2x2 items).
- Automatically converts screen coordinates to grid coordinates.
- Item tooltips.
- Context menu integration.

**Key Components:**
- **GridCellUI**: Cell background and drop highlight.
- **GridItemUI**: Draggable item visual representing an item taking 1x1 to NxN cells.
- **DragDropManager**: Screen-to-grid coordinate conversion and drag state.

**Key Methods:**
```csharp
public void BuildGrid()
public void RefreshGrid()
public void RequestMoveItem(GridPlacement placement, Vector2Int newPos)
public void ShowHighlight(Vector2Int topLeft, Vector2Int size, GridPlacement ignore)
```

#### DeathScreenUI
**File:** `UI/DeathScreen/DeathScreenUI.cs`

**Features:**
- Displays on player death.
- Shows the relevant death cause (e.g., starvation, fall damage).
- Handles player respawn or exiting.

#### EquipmentUI
**File:** `UI/Equipment/EquipmentUI.cs`

**Features:**
- 5 equipment slot display (Head, Body, Foot, Hand, HeldItem)
- Equipment preview
- Stat display
- Equip/unequip actions

**Key Methods:**
```csharp
public void UpdateEquipmentSlot(EquipmentSlotType slotType)
public void UpdateAllEquipmentSlots()
public void HighlightCompatibleSlots(EquipmentSlotType slotType)
```

#### CraftingUI
**File:** `UI/Crafting/CraftingUI.cs`

**Features:**
- Recipe list display
- Ingredient requirements
- Craft button with validation
- Result preview

**Key Methods:**
```csharp
public void DisplayRecipes(List<CraftingRecipe> recipes)
public void SelectRecipe(CraftingRecipe recipe)
public void UpdateCraftButton()
```

#### TabbedInventoryUI
**File:** `UI/TabbedInventory/TabbedInventoryUI.cs`

**Features:**
- Tab switching (Inventory, Equipment, Crafting)
- Single panel with multiple views
- Keyboard shortcuts (Tab, I, E, C)

**Key Methods:**
```csharp
public void SwitchToTab(TabType tabType)
public void Show()
{
    gameObject.SetActive(true);
    cursorManager.ShowCursor();
    Time.timeScale = 0f; // Pause game
}
public void Hide()
{
    gameObject.SetActive(false);
    cursorManager.HideCursor();
    Time.timeScale = 1f; // Resume game
}
```

#### ContextMenuUI
**File:** `UI/ContextMenu/ContextMenuUI.cs`

**Features:**
- Right-click context menu
- Dynamic action buttons (Equip, Consume, Drop, Drink)
- Position near mouse cursor

**Key Methods:**
```csharp
public void ShowGridItemMenu(GridInventoryUI gridUI, GridItemUI itemUI)
{
    // Evaluates item context to populate right-click actions (Equip, Drop, Consume)
}

private void PopulateActions(InventoryItem item)
{
    ClearActions();
    
    if (item is IEquippable equippable)
        AddAction("Equip", () => Equip(equippable));
    
    if (item.isConsumable)
        AddAction("Consume", () => Consume(item));
    
    if (item is CanteenItem canteen && canteen.CanDrink())
        AddAction($"Drink [{canteen.CurrentCharges}/{canteen.MaxCharges}]", () => Drink(canteen));
    
    AddAction("Drop", () => Drop(item));
}
```

#### TooltipUI
**File:** `UI/Tooltip/TooltipUI.cs`

**Features:**
- Item name, description, stats
- Dynamic positioning (avoids screen edges)
- Held item state display (charges, durability)

**Key Methods:**
```csharp
public void Show(InventoryItem item, Vector2 position)
{
    nameText.text = item.itemName;
    descriptionText.text = item.description;
    
    // Show stats
    if (item is IEquippable equippable)
        ShowEquipmentStats(equippable);
    
    // Show state
    if (item is HeldEquipmentItem heldItem)
        stateText.text = heldItem.GetStateDescription();
    
    PositionTooltip(position);
    gameObject.SetActive(true);
}
```

#### SimpleStatsHUD
**File:** `UI/HUD/SimpleStatsHUD.cs`

**Features:**
- Health, hunger, thirst, stamina bars
- Temperature display
- Auto-updates via events

**Event Subscriptions:**
```csharp
private void Start()
{
    var stats = ServiceContainer.Instance.TryGet<PlayerStats>();
    if (stats != null)
    {
        stats.OnHealthChanged += UpdateHealthBar;
        stats.OnHungerChanged += UpdateHungerBar;
        stats.OnStaminaChanged += UpdateStaminaBar;
        stats.OnThirstChanged += UpdateThirstBar;
    }
}
```

#### InteractionPromptUI
**File:** `UI/Interaction/InteractionPromptUI.cs`

**Features:**
- Interaction prompt text ("Press E to Pick Up")
- Hold-to-interact progress bar
- Dynamic positioning above interactable

**Key Methods:**
```csharp
public void ShowPrompt(string text)
public void HidePrompt()
public void ShowProgressBar()
public void UpdateProgress(float percent)
{
    progressBar.fillAmount = percent;
}
public void HideProgressBar()
```

### 7. BlurOverlaySystem

**File:** `UI/BlurOverlay/BlurOverlaySystem.cs`

**Purpose:** Visual feedback for paused/menu states.

**Features:**
- Gaussian blur shader
- Fade in/out animations
- Multiple blur layers support

**Usage:**
```csharp
public void ShowBlur(float intensity = 5f, float duration = 0.3f)
{
    StartCoroutine(FadeBlur(0f, intensity, duration));
}

public void HideBlur(float duration = 0.3f)
{
    StartCoroutine(FadeBlur(currentIntensity, 0f, duration));
}
```

---

## How It Works in Game

### Panel Opening Flow

```
1. Player presses I key
   │
   ▼
2. PlayerInputHandler detects input
   │
   ▼
3. PlayerController calls UIServiceProvider.OpenPanel<TabbedInventoryUI>()
   │
   ▼
4. UIPanelController.OpenPanel<TabbedInventoryUI>()
   │
   ├─► TabbedInventoryUI.Show()
   │   ├─► gameObject.SetActive(true)
   │   ├─► CursorManager.ShowCursor()
   │   ├─► Time.timeScale = 0f (pause)
   │   └─► BlurOverlaySystem.ShowBlur()
   │
   └─► Fire OnPanelOpened event
```

### Inventory UI Update Flow

```
1. Player picks up item
   │
   ▼
2. InventoryManager.AddItem(item, quantity)
   │
   ▼
3. EventBus.Publish(ItemAddedEvent)
   │
   ▼
4. GridInventoryUI receives event and triggers RefreshGrid()
   │
   ├─► Destroys all old GridItemUI visuals
   ├─► Recreates instances from _gridStorage.GetAllPlacements()
   │
   └─► (Optional) NotificationUI shows pickup notification
```

### Context Menu Flow

```
1. Player right-clicks item in GridInventoryUI
   │
   ▼
2. GridItemUI.OnPointerClick(PointerEventData e)
   │
   ├─► if (e.button == PointerEventData.InputButton.Right)
   │
   ▼
3. ContextMenuUI.ShowGridItemMenu(GridInventoryUI gridUI, GridItemUI itemUI)
   │
   ├─► ClearPreviousActions()
   │
   ├─► PopulateActions routing to gridUI.UseItem() / gridUI.DropItem()
   │   ├─► Check: Is IEquippable? → Add "Equip" button
   │   ├─► Check: Is consumable? → Add "Consume" button
   │   ├─► Check: Is CanteenItem? → Add "Drink [X/5]" button
   │   └─► Always add "Drop" button
   │
   ├─► SetPosition(mousePosition)
   │   └─► Clamp to screen bounds
   │
   └─► Show()
```

### Tooltip Display Flow

```
1. Player hovers over item slot
   │
   ▼
2. InventorySlotUI.OnPointerEnter(PointerEventData e)
   │
   ▼
3. TooltipUI.Show(item, mousePosition)
   │
   ├─► Set nameText = item.itemName
   ├─► Set descriptionText = item.description
   │
   ├─► Show equipment stats (if IEquippable)
   │   └─► "Armor: +10" etc.
   │
   ├─► Show held item state (if HeldEquipmentItem)
   │   └─► "Durability: 150/300" or "Charges: 3/5"
   │
   ├─► PositionTooltip(mousePosition)
   │   ├─► Check if tooltip would go off screen
   │   └─► Adjust position to stay within bounds
   │
   └─► gameObject.SetActive(true)
```

---

## How to Use

### Opening a Panel from Code

```csharp
// From PlayerController or any system
var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
if (uiService != null)
{
    uiService.OpenPanel<TabbedInventoryUI>();
}
```

### Closing a Panel

```csharp
uiService.ClosePanel<TabbedInventoryUI>();
```

### Subscribing to UI Events

```csharp
public class MyUIPanel : MonoBehaviour, IUIPanel
{
    private IEventBus eventBus;
    
    private void Awake()
    {
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        
        // Subscribe to game events
        eventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
    }
    
    private void OnDestroy()
    {
        // Always unsubscribe!
        eventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
    }
    
    private void OnItemAdded(ItemAddedEvent e)
    {
        // Update UI
        RefreshDisplay();
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    public void Refresh()
    {
        RefreshDisplay();
    }
    
    public bool IsVisible => gameObject.activeSelf;
}
```

### Creating a Custom UI Panel

1. **Create panel prefab:**
   - Add Canvas, Panel, UI elements
   - Setup layout groups, anchors

2. **Create panel script:**
```csharp
using UnityEngine;
using Game.UI;

public class CustomPanel : MonoBehaviour, IUIPanel
{
    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f; // Pause if needed
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f; // Resume
    }
    
    public void Refresh()
    {
        // Update display
    }
    
    public bool IsVisible => gameObject.activeSelf;
}
```

3. **Register in UIServiceProvider:**
```csharp
// In UIServiceProvider.Awake()
var customPanel = FindFirstObjectByType<CustomPanel>();
if (customPanel != null)
{
    panelController.RegisterPanel(customPanel);
}
```

### Showing Context Menu

```csharp
// On right-click
if (Input.GetMouseButtonDown(1))
{
    var contextMenu = uiService.GetPanel<ContextMenuUI>();
    contextMenu.ShowInventoryMenu(item, Input.mousePosition);
}
```

---

## How to Expand

### Adding New Panel

**Example: QuestLogUI**

1. **Create UI prefab:**
   - Canvas → Panel → QuestLogPanel
   - Add ScrollView for quest list
   - Add buttons for actions

2. **Create script:**
```csharp
public class QuestLogUI : MonoBehaviour, IUIPanel
{
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questEntryPrefab;
    
    private QuestManager questManager;
    private IEventBus eventBus;
    
    private void Awake()
    {
        var container = ServiceContainer.Instance;
        questManager = container.TryGet<QuestManager>();
        eventBus = container.TryGet<IEventBus>();
        
        eventBus.Subscribe<QuestStartedEvent>(OnQuestStarted);
        eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
    }
    
    public void Show()
    {
        Refresh();
        gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    public void Refresh()
    {
        ClearQuestList();
        DisplayActiveQuests();
    }
    
    private void DisplayActiveQuests()
    {
        var quests = questManager.GetActiveQuests();
        foreach (var quest in quests)
        {
            var entry = Instantiate(questEntryPrefab, questListContainer);
            entry.GetComponent<QuestEntryUI>().Setup(quest);
        }
    }
    
    public bool IsVisible => gameObject.activeSelf;
}
```

3. **Register in UIServiceProvider:**
   - Happens automatically via FindFirstObjectByType

### Adding Custom Tooltip Info

```csharp
// Extend TooltipUI
public class TooltipUI : MonoBehaviour
{
    // ... existing code ...
    
    public void Show(InventoryItem item, Vector2 position)
    {
        // ... existing setup ...
        
        // NEW: Custom info for specific item types
        if (item is WeaponItem weapon)
        {
            ShowWeaponStats(weapon);
        }
        else if (item is FoodItem food)
        {
            ShowNutritionInfo(food);
        }
    }
    
    private void ShowWeaponStats(WeaponItem weapon)
    {
        var statsText = $"Damage: {weapon.damage}\n";
        statsText += $"Attack Speed: {weapon.attackSpeed}\n";
        statsText += $"Range: {weapon.range}m";
        
        customStatsText.text = statsText;
    }
}
```

### Adding New Context Menu Action

```csharp
// In ContextMenuUI.PopulateActions()
private void PopulateActions(InventoryItem item)
{
    // ... existing actions ...
    
    // NEW: Repair action for damaged equipment
    if (item is IEquippable equippable && equippable.IsDamaged())
    {
        AddAction("Repair", () => RepairItem(equippable));
    }
}

private void RepairItem(IEquippable item)
{
    var repairSystem = ServiceContainer.Instance.TryGet<RepairSystem>();
    if (repairSystem != null && repairSystem.CanRepair(item))
    {
        repairSystem.Repair(item);
        Hide();
    }
}
```

---

## Architecture Patterns

### Service Locator Pattern

**UIServiceProvider** acts as service locator:
- Central access point for all UI panels
- Decouples panel access from panel implementation
- Simplifies panel management

**Benefits:**
- No need to pass panel references around
- Easy to swap panel implementations
- Clear API for panel operations

### Adapter Pattern

**UI Adapters** bridge game systems and UI:
- Adapters subscribe to EventBus
- Adapters call UI update methods
- UI never directly accesses game systems

**Benefits:**
- UI is decoupled from game logic
- Easy to test UI independently
- Clear separation of concerns

### Observer Pattern (Events)

**EventBus-driven updates:**
- Game systems publish events
- UI adapters subscribe to events
- UI updates automatically

**Benefits:**
- No polling or Update() loops
- Low coupling between systems
- Easy to add new subscribers

---

## Performance Considerations

### UI Update Frequency
- **Event-driven:** Only updates when data changes
- **Best practice:** Batch updates (UpdateAllSlots vs UpdateSlot per item)

### Canvas Optimization
- **Static UI:** Mark as static if never moves
- **Separate canvases:** Inventory, HUD, Menus on different canvases
- **Disable raycasting:** On non-interactive elements

### Tooltip Performance
- **Object pooling:** Reuse tooltip instances
- **Clamping:** Calculate once, not every frame

---

## Troubleshooting

### Panel Not Opening

**Check:**
1. Panel registered in UIServiceProvider?
2. Panel GameObject active in scene?
3. Panel implements IUIPanel?

**Debug:**
```csharp
var panel = uiService.GetPanel<MyPanel>();
Debug.Log($"Panel found: {panel != null}");
```

### UI Not Updating

**Check:**
1. Adapter subscribed to correct events?
2. EventBus publishing events?
3. Adapter still alive (not destroyed)?

**Debug:**
```csharp
// In adapter
private void OnItemAdded(ItemAddedEvent e)
{
    Debug.Log($"Adapter received ItemAdded: {e.item.itemName}");
    inventoryUI.UpdateSlot(e.slotIndex);
}
```

### Cursor Not Showing/Hiding

**Check:**
1. CursorManager.ShowCursor() called?
2. Another script overriding cursor state?
3. Panel's Show() method calling cursor manager?

**Debug:**
```csharp
Debug.Log($"Cursor visible: {Cursor.visible}");
Debug.Log($"Cursor lock state: {Cursor.lockState}");
```

---

## Best Practices

### ✅ DO
- Use EventBus for all UI updates
- Implement IUIPanel for all panels
- Unsubscribe from events in OnDestroy
- Use adapters to separate UI from game logic
- Cache panel references (don't find every time)
- Batch UI updates when possible
- Use object pooling for frequently created UI elements

### ❌ DON'T
- Don't access game managers directly from UI
- Don't use Update() to poll for changes
- Don't forget to unsubscribe from events
- Don't mix game logic in UI scripts
- Don't create UI elements every frame
- Don't use Find or GetComponent in Update()
- Don't set Time.timeScale in every panel (centralize it)

---

## File Structure

```
UI/
├── UIServiceProvider.cs              # Service locator
├── UIPanelController.cs              # Panel management
├── IUIPanel.cs                       # Panel interface
├── CursorManager.cs                  # Cursor control
│
├── Adapters/
│   ├── InventoryUIAdapter.cs         # Inventory bridge
│   ├── EquipmentUIAdapter.cs         # Equipment bridge
│   └── CraftingUIAdapter.cs          # Crafting bridge
│
├── Inventory/
│   ├── InventoryUI.cs                # Main inventory panel
│   ├── InventorySlotUI.cs            # Individual slot
│   └── TabbedInventoryUI.cs          # Tabbed interface
│
├── Equipment/
│   ├── EquipmentUI.cs                # Equipment display
│   └── EquipmentSlotUI.cs            # Equipment slot
│
├── Crafting/
│   ├── CraftingUI.cs                 # Crafting panel
│   └── CraftingRecipeUI.cs           # Recipe display
│
├── ContextMenu/
│   ├── ContextMenuUI.cs              # Right-click menu
│   └── ContextMenuButton.cs          # Menu action button
│
├── Tooltip/
│   └── TooltipUI.cs                  # Item tooltip
│
├── HUD/
│   ├── SimpleStatsHUD.cs             # Health/stamina bars
│   └── NotificationUI.cs             # Popup notifications
│
├── Interaction/
│   └── InteractionPromptUI.cs        # "Press E" prompt
│
└── BlurOverlay/
    ├── BlurOverlaySystem.cs          # Blur controller
    └── BlurShader.shader             # Gaussian blur
```

---

## Integration Points

### With Core System
- Registered in GameServiceBootstrapper
- Uses ServiceContainer for DI
- Subscribes to EventBus events

### With Player System
- Player input opens/closes panels
- PlayerStats drives HUD updates

### With Inventory System
- Displays inventory slots
- Shows equipment
- Triggers item actions

### With Interaction System
- Shows interaction prompts
- Displays progress bars

---

## Related Documentation

- [CORE_SYSTEM_OVERVIEW.md](../Core/CORE_SYSTEM_OVERVIEW.md) - EventBus and DI
- [PLAYER_SYSTEM_OVERVIEW.md](../Player/PLAYER_SYSTEM_OVERVIEW.md) - Player integration
- [INVENTORY_SYSTEM_OVERVIEW.md](../Player/Inventory/INVENTORY_SYSTEM_OVERVIEW.md) - Inventory backend
- [BLUR_OVERLAY_SYSTEM.md](BlurOverlay/BLUR_OVERLAY_SYSTEM.md) - Blur shader details

---

**Last Updated:** February 16, 2026  
**System Status:** ✅ Production Ready  
**Architecture Quality:** ⭐⭐⭐⭐⭐ (Event-driven, decoupled, highly maintainable)
