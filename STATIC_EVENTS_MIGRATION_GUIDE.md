# Phase 3 Completion Guide - Static Events to EventBus Migration

## Overview
This guide shows how to migrate from static events to the EventBus pattern for better testability and decoupling.

---

## Why Migrate from Static Events?

### Problems with Static Events:
```csharp
// ❌ Static events create hidden dependencies
public static event Action<InventoryItem> OnItemAdded;

// Subscribers can be anywhere
void Start() {
    InventoryManager.OnItemAdded += HandleItemAdded;  // Hidden dependency!
}
```

**Issues:**
- ❌ Hard to test (static state)
- ❌ Memory leaks if not unsubscribed
- ❌ Hidden dependencies
- ❌ No way to mock events
- ❌ Tight coupling between systems

### Benefits of EventBus:
```csharp
// ✅ EventBus with explicit dependency
private IEventBus eventBus;

void Start() {
    ServiceContainer.Instance.TryGet(out eventBus);
    eventBus.Subscribe<ItemAddedEvent>(HandleItemAdded);
}
```

**Benefits:**
- ✅ Testable (injectable dependency)
- ✅ Auto-cleanup via WeakReferences
- ✅ Explicit dependencies
- ✅ Easy to mock
- ✅ Loose coupling

---

## Migration Steps

### Step 1: Create Event Classes (✅ COMPLETE)

All event classes have been created in `GameEvents.cs`:

```csharp
// Equipment Events
public class ItemEquippedEvent { public IEquippable Item { get; } }
public class ItemUnequippedEvent { public IEquippable Item { get; } }

// Crafting Events
public class CraftingStartedEvent { public CraftingRecipe Recipe { get; } }
public class CraftingCompletedEvent { public CraftingRecipe Recipe { get; } }
public class CraftingFailedEvent { public CraftingRecipe Recipe { get; } }

// Interaction Events
public class NearestItemChangedEvent { public ResourceCollector NewNearest { get; } }
public class ItemInRangeChangedEvent { public bool IsInRange { get; } }
```

### Step 2: Update Publishers (Event Raisers)

#### Example: EquipmentManager

**Before (Static Event):**
```csharp
public class EquipmentManager : MonoBehaviour
{
    public static event Action<IEquippable> OnItemEquipped;
    public static event Action<IEquippable> OnItemUnequipped;
    
    public void EquipItem(IEquippable item, EquipmentSlotType slot)
    {
        // ... equipment logic ...
        OnItemEquipped?.Invoke(item);  // Static event
    }
    
    public void UnequipSlot(EquipmentSlotType slot)
    {
        // ... unequip logic ...
        OnItemUnequipped?.Invoke(item);  // Static event
    }
}
```

**After (EventBus):**
```csharp
using Game.Core.DI;
using Game.Core.Events;

public class EquipmentManager : MonoBehaviour
{
    // Keep static events for backward compatibility (mark deprecated)
    [Obsolete("Use EventBus with ItemEquippedEvent instead")]
    public static event Action<IEquippable> OnItemEquipped;
    [Obsolete("Use EventBus with ItemUnequippedEvent instead")]
    public static event Action<IEquippable> OnItemUnequipped;
    
    private IEventBus eventBus;
    
    private void Awake()
    {
        // Get EventBus from ServiceContainer
        ServiceContainer.Instance.TryGet(out eventBus);
    }
    
    public void EquipItem(IEquippable item, EquipmentSlotType slot)
    {
        // ... equipment logic ...
        
        // Raise both events during migration period
        OnItemEquipped?.Invoke(item);  // Legacy - will remove later
        eventBus?.Publish(new ItemEquippedEvent(item));  // New way
    }
    
    public void UnequipSlot(EquipmentSlotType slot)
    {
        // ... unequip logic ...
        
        // Raise both events during migration period
        OnItemUnequipped?.Invoke(item);  // Legacy - will remove later
        eventBus?.Publish(new ItemUnequippedEvent(item));  // New way
    }
}
```

### Step 3: Update Subscribers (Event Listeners)

#### Example: UI Component Listening to Equipment Changes

**Before (Static Event):**
```csharp
public class EquipmentStatsUI : MonoBehaviour
{
    private void OnEnable()
    {
        EquipmentManager.OnItemEquipped += HandleItemEquipped;
        EquipmentManager.OnItemUnequipped += HandleItemUnequipped;
    }
    
    private void OnDisable()
    {
        EquipmentManager.OnItemEquipped -= HandleItemEquipped;
        EquipmentManager.OnItemUnequipped -= HandleItemUnequipped;
    }
    
    private void HandleItemEquipped(IEquippable item)
    {
        UpdateStats();
    }
    
    private void HandleItemUnequipped(IEquippable item)
    {
        UpdateStats();
    }
}
```

**After (EventBus):**
```csharp
using Game.Core.DI;
using Game.Core.Events;

public class EquipmentStatsUI : MonoBehaviour
{
    private IEventBus eventBus;
    
    private void Awake()
    {
        // Get EventBus from ServiceContainer
        ServiceContainer.Instance.TryGet(out eventBus);
    }
    
    private void OnEnable()
    {
        // Subscribe to EventBus events
        eventBus?.Subscribe<ItemEquippedEvent>(HandleItemEquipped);
        eventBus?.Subscribe<ItemUnequippedEvent>(HandleItemUnequipped);
    }
    
    private void OnDisable()
    {
        // Unsubscribe from EventBus events
        eventBus?.Unsubscribe<ItemEquippedEvent>(HandleItemEquipped);
        eventBus?.Unsubscribe<ItemUnequippedEvent>(HandleItemUnequipped);
    }
    
    private void HandleItemEquipped(ItemEquippedEvent evt)
    {
        var item = evt.Item;  // Get data from event object
        UpdateStats();
    }
    
    private void HandleItemUnequipped(ItemUnequippedEvent evt)
    {
        var item = evt.Item;  // Get data from event object
        UpdateStats();
    }
}
```

---

## Migration Priority List

### High Priority (Core Systems)

#### 1. EquipmentManager
- **Events:** OnItemEquipped, OnItemUnequipped
- **Subscribers:** EquipmentUI, EquipmentStatsUI, PlayerStats, EquipmentSlotUI
- **Impact:** Medium - Equipment system used frequently

#### 2. InventoryManager
- **Events:** OnItemAdded, OnItemRemoved, OnItemConsumed, OnInventoryChanged
- **Subscribers:** InventoryUI, CraftingUI, QuestSystem, AchievementSystem
- **Impact:** HIGH - Inventory is core to the game
- **Note:** Already has instance-based events in RefactoredInventoryManager (use as reference)

#### 3. CraftingManager
- **Events:** OnCraftingStarted, OnCraftingCompleted, OnCraftingFailed
- **Subscribers:** CraftingUI, QuestSystem, TutorialSystem
- **Impact:** Medium - Crafting is important but less frequent

### Medium Priority

#### 4. ItemDetector (Legacy System)
- **Events:** OnNearestItemChanged, OnItemInRange
- **Subscribers:** InteractionPromptUI, PlayerController
- **Impact:** Low - Being replaced by InteractionDetector
- **Note:** Consider deprecating instead of migrating

---

## Testing Strategy

### Before Migration (Static Events)
```csharp
// ❌ Hard to test - requires actual MonoBehaviour
[Test]
public void WhenItemEquipped_ShouldNotifySubscribers()
{
    // Can't easily test without scene setup
    var manager = GameObject.FindObjectOfType<EquipmentManager>();
    bool eventFired = false;
    EquipmentManager.OnItemEquipped += (item) => eventFired = true;
    
    manager.EquipItem(testItem, EquipmentSlotType.Head);
    
    Assert.IsTrue(eventFired);
}
```

### After Migration (EventBus)
```csharp
// ✅ Easy to test - pure C# without Unity
[Test]
public void WhenItemEquipped_ShouldPublishEvent()
{
    // Arrange
    var mockEventBus = new Mock<IEventBus>();
    var manager = new EquipmentManager();
    manager.SetEventBus(mockEventBus.Object);  // Inject mock
    
    // Act
    manager.EquipItem(testItem, EquipmentSlotType.Head);
    
    // Assert
    mockEventBus.Verify(bus => 
        bus.Publish(It.IsAny<ItemEquippedEvent>()), 
        Times.Once);
}
```

---

## Example: Complete Migration of EquipmentManager

### Files to Modify:
1. ✅ `GameEvents.cs` - Event classes (already created)
2. 🔄 `EquipmentManager.cs` - Add EventBus publishing
3. 🔄 `EquipmentUI.cs` - Subscribe via EventBus
4. 🔄 `EquipmentSlotUI.cs` - Subscribe via EventBus
5. 🔄 `EquipmentStatsUI.cs` - Subscribe via EventBus (if exists)

### Step-by-Step:

#### 1. Update EquipmentManager.cs

Add at the top:
```csharp
using Game.Core.DI;
using Game.Core.Events;
```

Add private field:
```csharp
private IEventBus eventBus;
```

Add to Awake():
```csharp
private void Awake()
{
    ServiceContainer.Instance.TryGet(out eventBus);
    // ... existing code ...
}
```

Update EquipItem():
```csharp
public void EquipItem(IEquippable item, EquipmentSlotType slot)
{
    // ... existing equipment logic ...
    
    // Publish events
    OnItemEquipped?.Invoke(item);  // Legacy (keep for now)
    eventBus?.Publish(new ItemEquippedEvent(item));  // New
}
```

#### 2. Update EquipmentUI.cs

Add subscription in OnEnable():
```csharp
private void OnEnable()
{
    if (eventBus != null)
    {
        eventBus.Subscribe<ItemEquippedEvent>(OnItemEquipped);
        eventBus.Subscribe<ItemUnequippedEvent>(OnItemUnequipped);
    }
}

private void OnDisable()
{
    if (eventBus != null)
    {
        eventBus.Unsubscribe<ItemEquippedEvent>(OnItemEquipped);
        eventBus.Unsubscribe<ItemUnequippedEvent>(OnItemUnequipped);
    }
}

private void OnItemEquipped(ItemEquippedEvent evt)
{
    UpdateEquipmentDisplay();
}

private void OnItemUnequipped(ItemUnequippedEvent evt)
{
    UpdateEquipmentDisplay();
}
```

---

## Cleanup Phase (After All Migrations)

Once all subscribers have been migrated to EventBus:

1. Remove [Obsolete] attribute
2. Remove static event declarations
3. Remove legacy event invocations
4. Run all tests to verify

```csharp
// Final clean version
public class EquipmentManager : MonoBehaviour
{
    private IEventBus eventBus;
    
    public void EquipItem(IEquippable item, EquipmentSlotType slot)
    {
        // ... equipment logic ...
        eventBus?.Publish(new ItemEquippedEvent(item));  // Only EventBus
    }
}
```

---

## Checklist

### EquipmentManager Migration
- [ ] Add EventBus field to EquipmentManager
- [ ] Mark static events as [Obsolete]
- [ ] Publish ItemEquippedEvent alongside static event
- [ ] Publish ItemUnequippedEvent alongside static event
- [ ] Update EquipmentUI to subscribe via EventBus
- [ ] Update EquipmentSlotUI to subscribe via EventBus
- [ ] Test equipment system thoroughly
- [ ] Remove static events after testing

### CraftingManager Migration
- [ ] Add EventBus field to CraftingManager
- [ ] Mark static events as [Obsolete]
- [ ] Publish crafting events via EventBus
- [ ] Update CraftingUI to subscribe via EventBus
- [ ] Test crafting system thoroughly
- [ ] Remove static events after testing

### InventoryManager Migration
- [ ] Note: RefactoredInventoryManager already uses instance events
- [ ] Decide: Migrate legacy InventoryManager OR deprecate it
- [ ] If migrating: Follow same pattern as Equipment/Crafting
- [ ] Update all UI subscribers

---

## Summary

**Current Status:**
- ✅ Event classes created
- ✅ EventBus implemented and registered
- ✅ ServiceContainer provides EventBus access
- 🔄 Publishers need updating (3 managers)
- 🔄 Subscribers need updating (~10 UI components)

**Next Steps:**
1. Start with EquipmentManager (smallest, simplest)
2. Then CraftingManager
3. Finally InventoryManager (or deprecate for RefactoredInventoryManager)
4. Test each migration thoroughly
5. Remove static events once stable

**Estimated Time:**
- EquipmentManager: 30 minutes
- CraftingManager: 30 minutes
- InventoryManager: 1 hour (more complex)
- Testing: 1 hour
- **Total: ~3 hours**
