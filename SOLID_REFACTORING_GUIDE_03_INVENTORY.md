# SOLID Principles Refactoring Guide - Part 3: Inventory System Refactoring

## Overview

The current `InventoryManager` violates the Single Responsibility Principle by mixing data storage, business logic, and effect application. This guide shows how to separate these concerns.

---

## Current Problems

### InventoryManager Responsibilities (Too Many!)

```csharp
public class InventoryManager : MonoBehaviour
{
    // 1. Data Storage
    private List<InventorySlot> inventorySlots;
    
    // 2. Business Logic
    public bool AddItem(InventoryItem item, int quantity) { }
    public bool RemoveItem(InventoryItem item, int quantity) { }
    
    // 3. Effect Application
    public bool ConsumeItem(InventoryItem item)
    {
        ApplyConsumableEffect(effect); // ← Should be separate
    }
    
    // 4. Event Broadcasting
    public static event Action<InventoryItem, int> OnItemAdded;
    
    // 5. Stats Integration
    [SerializeField] private PlayerStats playerStats;
    private void ApplyConsumableEffect(ConsumableEffect effect)
    {
        playerStats.Eat(effectValue); // ← Tight coupling
    }
}
```

**Issues:**
1. Can't test business logic without data layer
2. Can't swap storage implementation (e.g., database, save file)
3. Effect system tightly coupled to specific stats implementation
4. Static events create global dependencies
5. Violates OCP for new stat types (switch statement)

---

## Refactoring Strategy

### Separate Concerns

Break InventoryManager into focused components:

1. **IInventoryStorage** - Data access layer (DAL)
2. **InventoryService** - Business logic layer
3. **IConsumableEffect** - Effect application (Strategy Pattern)
4. **InventoryEvents** - Event management
5. **InventoryFacade** - Simple API for external systems

---

## Refactored Architecture

```
┌─────────────────────────────────────┐
│      InventoryFacade (API)          │  ← Simplified external API
│  - AddItem()                         │
│  - RemoveItem()                      │
│  - ConsumeItem()                     │
└─────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────┐
│     InventoryService (Logic)        │  ← Business rules
│  - Validation                        │
│  - Stack logic                       │
│  - Consumption logic                 │
└─────────────────────────────────────┘
            │
            ├──────────────────┬────────────────────┐
            ▼                  ▼                    ▼
┌──────────────────┐  ┌─────────────────┐  ┌───────────────────┐
│ IInventoryStorage│  │IConsumableEffect│  │  InventoryEvents  │
│  - GetSlots()    │  │  - Apply()      │  │  - Broadcast      │
│  - AddToSlot()   │  │  Strategy       │  │  Instance-based   │
└──────────────────┘  └─────────────────┘  └───────────────────┘
```

---

## Implementation Plan

### Phase 1: Create Storage Interface

#### 1.1 IInventoryStorage Interface

Create: `Assets/Game/Script/Player/Inventory/Interfaces/IInventoryStorage.cs`

```csharp
using System.Collections.Generic;

namespace Game.Inventory
{
    /// <summary>
    /// Interface for inventory data storage
    /// Follows Dependency Inversion Principle
    /// Allows swapping storage implementations (memory, file, database)
    /// </summary>
    public interface IInventoryStorage
    {
        /// <summary>
        /// Gets all inventory slots
        /// </summary>
        IReadOnlyList<InventorySlot> GetSlots();
        
        /// <summary>
        /// Gets a specific slot by index
        /// </summary>
        InventorySlot GetSlot(int index);
        
        /// <summary>
        /// Finds the first empty slot
        /// </summary>
        int FindEmptySlot();
        
        /// <summary>
        /// Finds slots containing a specific item
        /// </summary>
        List<int> FindSlotsWithItem(InventoryItem item);
        
        /// <summary>
        /// Adds items to a specific slot
        /// </summary>
        bool AddToSlot(int slotIndex, InventoryItem item, int quantity);
        
        /// <summary>
        /// Removes items from a specific slot
        /// </summary>
        bool RemoveFromSlot(int slotIndex, int quantity);
        
        /// <summary>
        /// Clears a specific slot
        /// </summary>
        void ClearSlot(int slotIndex);
        
        /// <summary>
        /// Gets total number of slots
        /// </summary>
        int SlotCount { get; }
        
        /// <summary>
        /// Checks if storage is full
        /// </summary>
        bool IsFull();
    }
}
```

#### 1.2 InMemoryInventoryStorage Implementation

Create: `Assets/Game/Script/Player/Inventory/Storage/InMemoryInventoryStorage.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// In-memory implementation of inventory storage
    /// Single Responsibility: Data storage only
    /// </summary>
    public class InMemoryInventoryStorage : IInventoryStorage
    {
        private readonly List<InventorySlot> _slots;
        private readonly int _maxSlots;
        
        public int SlotCount => _slots.Count;
        
        public InMemoryInventoryStorage(int initialSlots, int maxSlots)
        {
            _maxSlots = maxSlots;
            _slots = new List<InventorySlot>(initialSlots);
            
            for (int i = 0; i < initialSlots; i++)
            {
                _slots.Add(new InventorySlot());
            }
        }
        
        public IReadOnlyList<InventorySlot> GetSlots()
        {
            return _slots.AsReadOnly();
        }
        
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return null;
            
            return _slots[index];
        }
        
        public int FindEmptySlot()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }
        
        public List<int> FindSlotsWithItem(InventoryItem item)
        {
            var result = new List<int>();
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item == item)
                {
                    result.Add(i);
                }
            }
            return result;
        }
        
        public bool AddToSlot(int slotIndex, InventoryItem item, int quantity)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;
            
            _slots[slotIndex].AddItem(item, quantity);
            return true;
        }
        
        public bool RemoveFromSlot(int slotIndex, int quantity)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;
            
            _slots[slotIndex].RemoveQuantity(quantity);
            return true;
        }
        
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                _slots[slotIndex].Clear();
            }
        }
        
        public bool IsFull()
        {
            return _slots.All(slot => !slot.IsEmpty);
        }
        
        /// <summary>
        /// Expands inventory capacity
        /// </summary>
        public bool TryAddSlots(int count)
        {
            if (_slots.Count + count > _maxSlots)
                return false;
            
            for (int i = 0; i < count; i++)
            {
                _slots.Add(new InventorySlot());
            }
            
            return true;
        }
    }
}
```

---

### Phase 2: Create Effect System

#### 2.1 IConsumableEffect Interface

Create: `Assets/Game/Script/Player/Inventory/Effects/IConsumableEffect.cs`

```csharp
namespace Game.Inventory.Effects
{
    /// <summary>
    /// Interface for consumable item effects
    /// Follows Strategy Pattern - effects are interchangeable
    /// Follows Open/Closed Principle - new effects without modification
    /// </summary>
    public interface IConsumableEffect
    {
        /// <summary>
        /// Applies the effect to the target
        /// </summary>
        void Apply(object target);
        
        /// <summary>
        /// Gets a description of what this effect does
        /// </summary>
        string GetDescription();
        
        /// <summary>
        /// Checks if this effect can be applied to the target
        /// </summary>
        bool CanApply(object target);
    }
}
```

#### 2.2 Base Effect Class

Create: `Assets/Game/Script/Player/Inventory/Effects/ConsumableEffectBase.cs`

```csharp
using UnityEngine;

namespace Game.Inventory.Effects
{
    /// <summary>
    /// Base class for consumable effects
    /// Provides common functionality
    /// </summary>
    public abstract class ConsumableEffectBase : IConsumableEffect
    {
        protected float _value;
        
        protected ConsumableEffectBase(float value)
        {
            _value = value;
        }
        
        public abstract void Apply(object target);
        public abstract string GetDescription();
        
        public virtual bool CanApply(object target)
        {
            return target != null;
        }
    }
}
```

#### 2.3 Concrete Effect Implementations

Create: `Assets/Game/Script/Player/Inventory/Effects/HealthEffect.cs`

```csharp
using UnityEngine;

namespace Game.Inventory.Effects
{
    /// <summary>
    /// Restores health when consumed
    /// Follows Single Responsibility - only affects health
    /// </summary>
    public class HealthEffect : ConsumableEffectBase
    {
        public HealthEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                // Assuming PlayerStats has a Heal method
                // If not, we'll need to add it
                stats.ModifyHealth(_value);
                Debug.Log($"Restored {_value} health");
            }
        }
        
        public override string GetDescription()
        {
            return $"+{_value} Health";
        }
        
        public override bool CanApply(object target)
        {
            return target is PlayerStats;
        }
    }
}
```

Create similar classes for:
- `HungerEffect.cs`
- `ThirstEffect.cs`
- `StaminaEffect.cs`
- `TemperatureEffect.cs`

```csharp
namespace Game.Inventory.Effects
{
    public class HungerEffect : ConsumableEffectBase
    {
        public HungerEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.Eat(_value);
            }
        }
        
        public override string GetDescription() => $"+{_value} Hunger";
        public override bool CanApply(object target) => target is PlayerStats;
    }
    
    public class ThirstEffect : ConsumableEffectBase
    {
        public ThirstEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.Drink(_value);
            }
        }
        
        public override string GetDescription() => $"+{_value} Thirst";
        public override bool CanApply(object target) => target is PlayerStats;
    }
    
    public class StaminaEffect : ConsumableEffectBase
    {
        public StaminaEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.RestoreStamina(_value);
            }
        }
        
        public override string GetDescription() => $"+{_value} Stamina";
        public override bool CanApply(object target) => target is PlayerStats;
    }
}
```

#### 2.4 Effect Factory

Create: `Assets/Game/Script/Player/Inventory/Effects/ConsumableEffectFactory.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory.Effects
{
    /// <summary>
    /// Factory for creating consumable effects from data
    /// Follows Factory Pattern
    /// Converts legacy ConsumableEffect to new IConsumableEffect
    /// </summary>
    public static class ConsumableEffectFactory
    {
        /// <summary>
        /// Creates effect instances from consumable effect data
        /// </summary>
        public static List<IConsumableEffect> CreateEffects(List<ConsumableEffect> effects)
        {
            var result = new List<IConsumableEffect>();
            
            if (effects == null)
                return result;
            
            foreach (var effect in effects)
            {
                var consumableEffect = CreateEffect(effect);
                if (consumableEffect != null)
                {
                    result.Add(consumableEffect);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a single effect from data
        /// </summary>
        public static IConsumableEffect CreateEffect(ConsumableEffect effect)
        {
            if (effect == null)
                return null;
            
            switch (effect.statType)
            {
                case StatType.Health:
                    return new HealthEffect(effect.value);
                    
                case StatType.Hunger:
                    return new HungerEffect(effect.value);
                    
                case StatType.Thirst:
                    return new ThirstEffect(effect.value);
                    
                case StatType.Stamina:
                    return new StaminaEffect(effect.value);
                    
                case StatType.Temperature:
                    return new TemperatureEffect(effect.value);
                    
                default:
                    Debug.LogWarning($"Unknown stat type: {effect.statType}");
                    return null;
            }
        }
    }
}
```

---

### Phase 3: Create Service Layer

#### 3.1 InventoryEvents

Create: `Assets/Game/Script/Player/Inventory/Events/InventoryEvents.cs`

```csharp
using System;

namespace Game.Inventory
{
    /// <summary>
    /// Instance-based events for inventory changes
    /// Replaces static events to avoid global coupling
    /// Follows Dependency Inversion Principle
    /// </summary>
    public class InventoryEvents
    {
        public event Action<InventoryItem, int> OnItemAdded;
        public event Action<InventoryItem, int> OnItemRemoved;
        public event Action<InventoryItem, int> OnItemConsumed;
        public event Action OnInventoryChanged;
        
        public void RaiseItemAdded(InventoryItem item, int quantity)
        {
            OnItemAdded?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
        }
        
        public void RaiseItemRemoved(InventoryItem item, int quantity)
        {
            OnItemRemoved?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
        }
        
        public void RaiseItemConsumed(InventoryItem item, int quantity)
        {
            OnItemConsumed?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
        }
        
        public void RaiseInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
```

#### 3.2 InventoryService

Create: `Assets/Game/Script/Player/Inventory/Services/InventoryService.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Inventory.Effects;

namespace Game.Inventory
{
    /// <summary>
    /// Service layer for inventory business logic
    /// Single Responsibility: Business rules only
    /// Depends on abstractions (IInventoryStorage)
    /// </summary>
    public class InventoryService
    {
        private readonly IInventoryStorage _storage;
        private readonly InventoryEvents _events;
        
        public InventoryService(IInventoryStorage storage, InventoryEvents events)
        {
            _storage = storage;
            _events = events;
        }
        
        /// <summary>
        /// Adds an item to the inventory
        /// </summary>
        public bool AddItem(InventoryItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;
            
            // Try to stack with existing items first
            if (item.isStackable)
            {
                var slotsWithItem = _storage.FindSlotsWithItem(item);
                foreach (var slotIndex in slotsWithItem)
                {
                    var slot = _storage.GetSlot(slotIndex);
                    if (slot.CanAddItem(item, quantity))
                    {
                        _storage.AddToSlot(slotIndex, item, quantity);
                        _events.RaiseItemAdded(item, quantity);
                        return true;
                    }
                }
            }
            
            // Find empty slot
            int emptySlot = _storage.FindEmptySlot();
            if (emptySlot >= 0)
            {
                _storage.AddToSlot(emptySlot, item, quantity);
                _events.RaiseItemAdded(item, quantity);
                return true;
            }
            
            return false; // Inventory full
        }
        
        /// <summary>
        /// Removes an item from the inventory
        /// </summary>
        public bool RemoveItem(InventoryItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;
            
            int remainingToRemove = quantity;
            var slotsWithItem = _storage.FindSlotsWithItem(item);
            
            foreach (var slotIndex in slotsWithItem)
            {
                if (remainingToRemove <= 0)
                    break;
                
                var slot = _storage.GetSlot(slotIndex);
                int canRemove = Mathf.Min(slot.quantity, remainingToRemove);
                
                _storage.RemoveFromSlot(slotIndex, canRemove);
                remainingToRemove -= canRemove;
                _events.RaiseItemRemoved(item, canRemove);
            }
            
            return remainingToRemove == 0;
        }
        
        /// <summary>
        /// Consumes an item and applies its effects
        /// </summary>
        public bool ConsumeItem(InventoryItem item, object target)
        {
            if (item == null || !item.isConsumable)
                return false;
            
            if (!HasItem(item, 1))
                return false;
            
            // Create effects from item data
            var effects = ConsumableEffectFactory.CreateEffects(item.consumableEffects);
            
            // Apply all effects
            foreach (var effect in effects)
            {
                if (effect.CanApply(target))
                {
                    effect.Apply(target);
                }
            }
            
            // Remove the consumed item
            RemoveItem(item, 1);
            _events.RaiseItemConsumed(item, 1);
            
            return true;
        }
        
        /// <summary>
        /// Checks if inventory has a specific item
        /// </summary>
        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            var slotsWithItem = _storage.FindSlotsWithItem(item);
            int totalQuantity = 0;
            
            foreach (var slotIndex in slotsWithItem)
            {
                var slot = _storage.GetSlot(slotIndex);
                totalQuantity += slot.quantity;
            }
            
            return totalQuantity >= quantity;
        }
        
        /// <summary>
        /// Gets the total quantity of an item in inventory
        /// </summary>
        public int GetItemQuantity(InventoryItem item)
        {
            var slotsWithItem = _storage.FindSlotsWithItem(item);
            int totalQuantity = 0;
            
            foreach (var slotIndex in slotsWithItem)
            {
                var slot = _storage.GetSlot(slotIndex);
                totalQuantity += slot.quantity;
            }
            
            return totalQuantity;
        }
        
        /// <summary>
        /// Gets all inventory slots (read-only)
        /// </summary>
        public IReadOnlyList<InventorySlot> GetSlots()
        {
            return _storage.GetSlots();
        }
    }
}
```

---

### Phase 4: Create Facade

#### 4.1 RefactoredInventoryManager

Create: `Assets/Game/Script/Player/Inventory/RefactoredInventoryManager.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Game.Inventory
{
    /// <summary>
    /// Refactored inventory manager using SOLID principles
    /// Acts as a facade to the inventory service layer
    /// MonoBehaviour wrapper for dependency injection
    /// </summary>
    public class RefactoredInventoryManager : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int initialSlots = 10;
        [SerializeField] private int maxSlots = 30;
        
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        
        // Services
        private IInventoryStorage _storage;
        private InventoryService _service;
        private InventoryEvents _events;
        
        // Public properties
        public InventoryEvents Events => _events;
        
        private void Awake()
        {
            // Get PlayerStats if not assigned
            if (playerStats == null)
                playerStats = GetComponent<PlayerStats>();
            
            // Initialize services
            InitializeServices();
        }
        
        private void InitializeServices()
        {
            // Create storage
            _storage = new InMemoryInventoryStorage(initialSlots, maxSlots);
            
            // Create events
            _events = new InventoryEvents();
            
            // Create service
            _service = new InventoryService(_storage, _events);
        }
        
        // Public API - delegates to service
        
        public bool AddItem(InventoryItem item, int quantity = 1)
        {
            return _service.AddItem(item, quantity);
        }
        
        public bool RemoveItem(InventoryItem item, int quantity = 1)
        {
            return _service.RemoveItem(item, quantity);
        }
        
        public bool ConsumeItem(InventoryItem item)
        {
            return _service.ConsumeItem(item, playerStats);
        }
        
        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            return _service.HasItem(item, quantity);
        }
        
        public int GetItemQuantity(InventoryItem item)
        {
            return _service.GetItemQuantity(item);
        }
        
        public IReadOnlyList<InventorySlot> GetInventorySlots()
        {
            return _service.GetSlots();
        }
        
        // Legacy support - converts to List for backward compatibility
        public List<InventorySlot> GetInventorySlotsLegacy()
        {
            return new List<InventorySlot>(_service.GetSlots());
        }
    }
}
```

---

## Migration Guide

### Step 1: Create New Structure

1. Create folders:
   - `Assets/Game/Script/Player/Inventory/Interfaces/`
   - `Assets/Game/Script/Player/Inventory/Storage/`
   - `Assets/Game/Script/Player/Inventory/Effects/`
   - `Assets/Game/Script/Player/Inventory/Services/`
   - `Assets/Game/Script/Player/Inventory/Events/`

2. Add all interface and implementation files

### Step 2: Test New Implementation

1. Add `RefactoredInventoryManager` component alongside old `InventoryManager`
2. Test that it works correctly
3. Verify events are firing

### Step 3: Update References

Replace references to old manager:
```csharp
// Old
InventoryManager.OnItemAdded += Handler;

// New
refactoredManager.Events.OnItemAdded += Handler;
```

### Step 4: Remove Old Manager

Once all systems use new manager:
1. Disable old `InventoryManager`
2. Test thoroughly
3. Delete old implementation

---

## Benefits of Refactored Design

### ✅ Single Responsibility Principle

- `IInventoryStorage` - Only data storage
- `InventoryService` - Only business logic
- `HealthEffect` - Only health restoration
- `InventoryEvents` - Only event management

### ✅ Open/Closed Principle

Add new effects without modifying existing code:
```csharp
public class PoisonEffect : ConsumableEffectBase
{
    // New effect type, no changes to existing code
}
```

### ✅ Dependency Inversion Principle

Services depend on abstractions:
```csharp
public InventoryService(
    IInventoryStorage storage,  // ← Interface, not concrete class
    InventoryEvents events)
```

### ✅ Testability

Easy to unit test:
```csharp
// Mock storage for testing
var mockStorage = new MockInventoryStorage();
var events = new InventoryEvents();
var service = new InventoryService(mockStorage, events);
// Test without Unity!
```

### ✅ Swappable Storage

Can easily swap storage implementations:
```csharp
// Save to file
var fileStorage = new FileInventoryStorage("save.dat");
var service = new InventoryService(fileStorage, events);

// Or database
var dbStorage = new DatabaseInventoryStorage(connection);
var service = new InventoryService(dbStorage, events);
```

---

## Next Steps

Continue to:
- **Part 4:** Dependency Injection Implementation
- **Part 5:** Testing Strategy & Examples
- **Part 6:** Performance Optimizations

---

**Document Version:** 1.0  
**Last Updated:** February 2, 2026
