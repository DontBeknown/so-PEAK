# SOLID Principles Refactoring Guide - Part 4: Dependency Injection & Static Events

## Overview

This guide addresses two major issues:
1. **FindFirstObjectByType** creating hidden dependencies
2. **Static events** creating global coupling

Both violate the Dependency Inversion Principle and make testing difficult.

---

## Problem 1: FindFirstObjectByType Dependencies

### Current Issues

```csharp
// Hidden dependency - hard to test, slow
inventoryManager ??= FindFirstObjectByType<InventoryManager>();
craftingManager ??= FindFirstObjectByType<CraftingManager>();
tabbedInventoryUI ??= FindFirstObjectByType<TabbedInventoryUI>();
```

**Problems:**
- ❌ Hidden dependencies (not visible in constructor/method signature)
- ❌ Can't test without full Unity scene
- ❌ Slow performance (searches entire scene)
- ❌ Tight coupling to Unity's scene management
- ❌ No way to mock dependencies

---

## Solution 1: Dependency Injection Container

### Strategy

Instead of finding dependencies at runtime, inject them explicitly:

```
┌────────────────────────────────────────┐
│     GameServiceProvider                │  ← Central DI Container
│  - Registers all services              │
│  - Provides dependencies                │
│  - Manages lifecycle                    │
└────────────────────────────────────────┘
            │
            ├─────────────────────┬──────────────────────┐
            ▼                     ▼                      ▼
    [PlayerController]    [InventoryManager]    [UIServiceProvider]
         ↑ Inject             ↑ Inject              ↑ Inject
```

---

### Implementation

#### 1.1 Service Provider Interface

Create: `Assets/Game/Script/Core/DependencyInjection/IServiceProvider.cs`

```csharp
namespace Game.Core.DI
{
    /// <summary>
    /// Interface for service provider
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Registers a service instance
        /// </summary>
        void Register<TService>(TService instance) where TService : class;
        
        /// <summary>
        /// Gets a registered service
        /// </summary>
        TService Get<TService>() where TService : class;
        
        /// <summary>
        /// Tries to get a service, returns null if not found
        /// </summary>
        TService TryGet<TService>() where TService : class;
        
        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        bool Has<TService>() where TService : class;
        
        /// <summary>
        /// Unregisters a service
        /// </summary>
        void Unregister<TService>() where TService : class;
    }
}
```

#### 1.2 Simple Service Container

Create: `Assets/Game/Script/Core/DependencyInjection/ServiceContainer.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.DI
{
    /// <summary>
    /// Simple dependency injection container
    /// Singleton pattern for Unity game-wide services
    /// </summary>
    public class ServiceContainer : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        private static ServiceContainer _instance;
        public static ServiceContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceContainer();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Registers a service
        /// </summary>
        public void Register<TService>(TService instance) where TService : class
        {
            if (instance == null)
            {
                Debug.LogWarning($"[ServiceContainer] Attempted to register null instance of {typeof(TService).Name}");
                return;
            }
            
            var type = typeof(TService);
            
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceContainer] Service {type.Name} is already registered. Overwriting.");
            }
            
            _services[type] = instance;
            Debug.Log($"[ServiceContainer] Registered {type.Name}");
        }
        
        /// <summary>
        /// Gets a registered service
        /// Throws exception if not found
        /// </summary>
        public TService Get<TService>() where TService : class
        {
            var type = typeof(TService);
            
            if (_services.TryGetValue(type, out object service))
            {
                return service as TService;
            }
            
            throw new InvalidOperationException($"Service {type.Name} is not registered");
        }
        
        /// <summary>
        /// Tries to get a service, returns null if not found
        /// </summary>
        public TService TryGet<TService>() where TService : class
        {
            var type = typeof(TService);
            
            if (_services.TryGetValue(type, out object service))
            {
                return service as TService;
            }
            
            return null;
        }
        
        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool Has<TService>() where TService : class
        {
            return _services.ContainsKey(typeof(TService));
        }
        
        /// <summary>
        /// Unregisters a service
        /// </summary>
        public void Unregister<TService>() where TService : class
        {
            var type = typeof(TService);
            
            if (_services.Remove(type))
            {
                Debug.Log($"[ServiceContainer] Unregistered {type.Name}");
            }
        }
        
        /// <summary>
        /// Clears all registered services
        /// Useful for testing or scene transitions
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            Debug.Log("[ServiceContainer] Cleared all services");
        }
    }
}
```

#### 1.3 Game Service Bootstrapper

Create: `Assets/Game/Script/Core/GameServiceBootstrapper.cs`

```csharp
using UnityEngine;
using Game.Core.DI;
using Game.Player;

namespace Game.Core
{
    /// <summary>
    /// Bootstraps and registers all game services
    /// Should be attached to a persistent GameObject
    /// Executes early in the game lifecycle
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other scripts
    public class GameServiceBootstrapper : MonoBehaviour
    {
        [Header("Auto-Find Services")]
        [SerializeField] private bool autoFindServices = true;
        
        [Header("Manual References (Optional)")]
        [SerializeField] private PlayerControllerRefactored playerController;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private CraftingManager craftingManager;
        [SerializeField] private TabbedInventoryUI inventoryUI;
        [SerializeField] private CinemachinePlayerCamera playerCamera;
        
        private void Awake()
        {
            RegisterServices();
        }
        
        private void RegisterServices()
        {
            var container = ServiceContainer.Instance;
            
            // Auto-find services if enabled
            if (autoFindServices)
            {
                FindAndRegisterServices();
            }
            else
            {
                // Register manually assigned references
                RegisterManualServices();
            }
        }
        
        private void FindAndRegisterServices()
        {
            var container = ServiceContainer.Instance;
            
            // Find and register player controller
            var player = FindFirstObjectByType<PlayerControllerRefactored>();
            if (player != null)
            {
                container.Register(player);
            }
            
            // Find and register inventory
            var inventory = FindFirstObjectByType<InventoryManager>();
            if (inventory != null)
            {
                container.Register(inventory);
            }
            
            // Find and register crafting
            var crafting = FindFirstObjectByType<CraftingManager>();
            if (crafting != null)
            {
                container.Register(crafting);
            }
            
            // Find and register UI
            var inventoryUi = FindFirstObjectByType<TabbedInventoryUI>();
            if (inventoryUi != null)
            {
                container.Register(inventoryUi);
            }
            
            // Find and register camera
            var camera = FindFirstObjectByType<CinemachinePlayerCamera>();
            if (camera != null)
            {
                container.Register(camera);
            }
            
            Debug.Log("[GameServiceBootstrapper] Services registered");
        }
        
        private void RegisterManualServices()
        {
            var container = ServiceContainer.Instance;
            
            if (playerController != null)
                container.Register(playerController);
            
            if (inventoryManager != null)
                container.Register(inventoryManager);
            
            if (craftingManager != null)
                container.Register(craftingManager);
            
            if (inventoryUI != null)
                container.Register(inventoryUI);
            
            if (playerCamera != null)
                container.Register(playerCamera);
            
            Debug.Log("[GameServiceBootstrapper] Manual services registered");
        }
        
        private void OnDestroy()
        {
            // Optional: Clear services when destroyed
            // ServiceContainer.Instance.Clear();
        }
    }
}
```

#### 1.4 Update Components to Use DI

**Before:**
```csharp
private void Awake()
{
    inventoryManager = FindFirstObjectByType<InventoryManager>();
    craftingManager = FindFirstObjectByType<CraftingManager>();
}
```

**After:**
```csharp
private void Awake()
{
    var container = ServiceContainer.Instance;
    inventoryManager = container.TryGet<InventoryManager>();
    craftingManager = container.TryGet<CraftingManager>();
}
```

Even better - constructor injection:
```csharp
public class MyClass
{
    private readonly InventoryManager _inventory;
    
    public MyClass(InventoryManager inventory)
    {
        _inventory = inventory;
    }
}
```

---

## Problem 2: Static Events

### Current Issues

```csharp
// Static events = global coupling
public static event Action<InventoryItem, int> OnItemAdded;
public static event Action<InventoryItem, int> OnItemRemoved;

// Problem: Called from anywhere
InventoryManager.OnItemAdded += HandleItemAdded;
```

**Problems:**
- ❌ Global state (hard to test)
- ❌ Memory leaks if not unsubscribed
- ❌ Can't have multiple inventories
- ❌ Hidden dependencies
- ❌ Hard to debug (who subscribed?)

---

## Solution 2: Event Bus Pattern

### Strategy

Replace static events with instance-based event system:

```
┌─────────────────────────────────┐
│         EventBus                │  ← Central event dispatcher
│  - Subscribe<T>(handler)        │
│  - Unsubscribe<T>(handler)      │
│  - Publish<T>(eventData)        │
└─────────────────────────────────┘
            │
            ├──────────────────┬─────────────────┐
            ▼                  ▼                 ▼
    [ItemAddedEvent]  [ItemRemovedEvent]  [ItemConsumedEvent]
```

---

### Implementation

#### 2.1 Event Bus Interface

Create: `Assets/Game/Script/Core/Events/IEventBus.cs`

```csharp
using System;

namespace Game.Core.Events
{
    /// <summary>
    /// Interface for event bus
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Subscribes to an event type
        /// </summary>
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        
        /// <summary>
        /// Unsubscribes from an event type
        /// </summary>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        
        /// <summary>
        /// Publishes an event
        /// </summary>
        void Publish<TEvent>(TEvent eventData) where TEvent : class;
    }
}
```

#### 2.2 Event Bus Implementation

Create: `Assets/Game/Script/Core/Events/EventBus.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// Simple event bus implementation
    /// Allows decoupled communication between systems
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers 
            = new Dictionary<Type, List<Delegate>>();
        
        /// <summary>
        /// Subscribes to an event type
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                return;
            
            var eventType = typeof(TEvent);
            
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            
            _subscribers[eventType].Add(handler);
        }
        
        /// <summary>
        /// Unsubscribes from an event type
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                return;
            
            var eventType = typeof(TEvent);
            
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
        
        /// <summary>
        /// Publishes an event to all subscribers
        /// </summary>
        public void Publish<TEvent>(TEvent eventData) where TEvent : class
        {
            if (eventData == null)
                return;
            
            var eventType = typeof(TEvent);
            
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                // Create a copy to avoid modification during iteration
                var handlersCopy = new List<Delegate>(handlers);
                
                foreach (var handler in handlersCopy)
                {
                    try
                    {
                        (handler as Action<TEvent>)?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventBus] Error invoking handler for {eventType.Name}: {ex}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the number of subscribers for an event type
        /// </summary>
        public int GetSubscriberCount<TEvent>() where TEvent : class
        {
            var eventType = typeof(TEvent);
            
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                return handlers.Count;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Clears all subscriptions
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
        }
    }
}
```

#### 2.3 Define Event Types

Create: `Assets/Game/Script/Core/Events/InventoryEvents.cs`

```csharp
namespace Game.Core.Events
{
    /// <summary>
    /// Event raised when an item is added to inventory
    /// </summary>
    public class ItemAddedEvent
    {
        public InventoryItem Item { get; set; }
        public int Quantity { get; set; }
        
        public ItemAddedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
    
    /// <summary>
    /// Event raised when an item is removed from inventory
    /// </summary>
    public class ItemRemovedEvent
    {
        public InventoryItem Item { get; set; }
        public int Quantity { get; set; }
        
        public ItemRemovedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
    
    /// <summary>
    /// Event raised when an item is consumed
    /// </summary>
    public class ItemConsumedEvent
    {
        public InventoryItem Item { get; set; }
        public int Quantity { get; set; }
        
        public ItemConsumedEvent(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
    
    /// <summary>
    /// Event raised when inventory changes
    /// </summary>
    public class InventoryChangedEvent
    {
        // Empty event - just signals a change
    }
}
```

#### 2.4 Update Service to Use Event Bus

**Before (static events):**
```csharp
public class InventoryManager : MonoBehaviour
{
    public static event Action<InventoryItem, int> OnItemAdded;
    
    private void AddItem(InventoryItem item, int quantity)
    {
        // ...
        OnItemAdded?.Invoke(item, quantity);
    }
}
```

**After (event bus):**
```csharp
public class InventoryManager : MonoBehaviour
{
    private IEventBus _eventBus;
    
    private void Awake()
    {
        _eventBus = ServiceContainer.Instance.Get<IEventBus>();
    }
    
    private void AddItem(InventoryItem item, int quantity)
    {
        // ...
        _eventBus.Publish(new ItemAddedEvent(item, quantity));
    }
}
```

**Subscribing:**
```csharp
public class InventoryUI : MonoBehaviour
{
    private IEventBus _eventBus;
    
    private void Awake()
    {
        _eventBus = ServiceContainer.Instance.Get<IEventBus>();
        _eventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
    }
    
    private void OnDestroy()
    {
        _eventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
    }
    
    private void OnItemAdded(ItemAddedEvent evt)
    {
        Debug.Log($"Added {evt.Quantity}x {evt.Item.itemName}");
    }
}
```

---

## Complete Migration Example

### Before: Tightly Coupled

```csharp
public class PlayerInventoryFacade
{
    private InventoryManager _inventoryManager;
    private CraftingManager _craftingManager;
    
    public PlayerInventoryFacade()
    {
        // Bad: Hidden dependencies
        _inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
        _craftingManager = Object.FindFirstObjectByType<CraftingManager>();
        
        // Bad: Static events
        InventoryManager.OnItemAdded += HandleItemAdded;
    }
    
    private void HandleItemAdded(InventoryItem item, int quantity)
    {
        // Handle event
    }
}
```

### After: Dependency Injection

```csharp
public class PlayerInventoryFacade
{
    private readonly InventoryManager _inventoryManager;
    private readonly CraftingManager _craftingManager;
    private readonly IEventBus _eventBus;
    
    // Explicit dependencies via constructor
    public PlayerInventoryFacade(
        InventoryManager inventoryManager,
        CraftingManager craftingManager,
        IEventBus eventBus)
    {
        _inventoryManager = inventoryManager;
        _craftingManager = craftingManager;
        _eventBus = eventBus;
        
        // Subscribe to events via event bus
        _eventBus.Subscribe<ItemAddedEvent>(HandleItemAdded);
    }
    
    private void HandleItemAdded(ItemAddedEvent evt)
    {
        // Handle event
    }
    
    public void Dispose()
    {
        // Clean unsubscribe
        _eventBus.Unsubscribe<ItemAddedEvent>(HandleItemAdded);
    }
}
```

---

## Setup Instructions

### Step 1: Create Bootstrapper

1. Create empty GameObject in scene: "GameServices"
2. Add `GameServiceBootstrapper` component
3. Set execution order to -100
4. Enable auto-find or assign references manually

### Step 2: Register Event Bus

Add to bootstrapper:
```csharp
private void RegisterServices()
{
    var container = ServiceContainer.Instance;
    
    // Register event bus
    var eventBus = new EventBus();
    container.Register<IEventBus>(eventBus);
    
    // ... register other services
}
```

### Step 3: Update Components

Replace `FindFirstObjectByType` with:
```csharp
private void Awake()
{
    var container = ServiceContainer.Instance;
    _inventory = container.TryGet<InventoryManager>();
    _eventBus = container.Get<IEventBus>();
}
```

### Step 4: Replace Static Events

1. Define event classes
2. Publish events via event bus
3. Subscribe via event bus
4. Unsubscribe in OnDestroy

---

## Benefits

### ✅ Testability

```csharp
// Easy to test with mocks
var mockInventory = new MockInventoryManager();
var mockEventBus = new MockEventBus();
var facade = new PlayerInventoryFacade(mockInventory, mockCrafting, mockEventBus);
// Test without Unity!
```

### ✅ Explicit Dependencies

```csharp
// Clear what dependencies are needed
public MyClass(
    InventoryManager inventory,  // ← Visible dependency
    IEventBus eventBus)          // ← Visible dependency
```

### ✅ Decoupling

```csharp
// No direct reference needed
_eventBus.Publish(new ItemAddedEvent(item, quantity));
// Any system can listen without coupling
```

### ✅ Multiple Instances

```csharp
// Can have multiple inventories
var playerInventory = new InventoryManager();
var chestInventory = new InventoryManager();
// Each with their own events
```

---

## Next Steps

Continue to:
- **Part 5:** Testing Strategies
- **Part 6:** Performance Considerations
- **Part 7:** Complete Implementation Examples

---

**Document Version:** 1.0  
**Last Updated:** February 2, 2026
