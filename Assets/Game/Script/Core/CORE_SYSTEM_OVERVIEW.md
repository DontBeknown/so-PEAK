# Core System Overview

**Location:** `Assets/Game/Script/Core/`  
**Last Updated:** February 16, 2026

---

## What is This System?

The **Core System** is the foundational infrastructure layer that provides:
- **Dependency Injection (DI)** - Service registration and resolution
- **Event Bus** - Type-safe event communication
- **Save/Load Service** - Persistent world and player data
- **Game Bootstrapping** - Service initialization on game start
- **Scene Management** - Gameplay scene initialization

This system is the backbone that all other systems depend on.

---

## Key Components

### 1. Dependency Injection (ServiceContainer)

**File:** `Core/DependencyInjection/ServiceContainer.cs`

**Purpose:** Central registry for all game services using singleton pattern.

**Features:**
- Type-based service registration
- Safe service resolution with `TryGet<T>()`
- Singleton instance pattern
- Simple Dictionary-based storage

**Example:**
```csharp
// Registration
ServiceContainer.Instance.Register<IEventBus>(new EventBus());
ServiceContainer.Instance.Register<PlayerStats>(playerStats);

// Resolution
var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
var stats = ServiceContainer.Instance.TryGet<PlayerStats>();
```

### 2. Event Bus (IEventBus)

**File:** `Core/Events/EventBus.cs`

**Purpose:** Decoupled event communication between systems.

**Features:**
- Type-safe event publishing/subscribing
- No static coupling
- Easy unsubscription
- Generic event data

**Example:**
```csharp
// Subscribe
eventBus.Subscribe<ItemEquippedEvent>(OnItemEquipped);

// Publish
eventBus.Publish(new ItemEquippedEvent { item = sword, slot = EquipmentSlotType.Weapon });

// Unsubscribe
eventBus.Unsubscribe<ItemEquippedEvent>(OnItemEquipped);
```

### 3. GameServiceBootstrapper

**File:** `Core/GameServiceBootstrapper.cs`

**Purpose:** Initializes all game services at startup.

**Features:**
- Execution order: -100 (runs first)
- Auto-finds or uses manual references
- Registers all managers, UI, and services
- One-time setup on game start

**Registered Services:**
- IEventBus
- ISaveLoadService
- PlayerController
- PlayerStats
- InventoryManager
- EquipmentManager
- CraftingManager
- All UI services
- Day/Night cycle
- Assessment services

### 4. Save/Load Service

**File:** `Core/SaveSystem/SaveLoadService.cs`

**Purpose:** Persistent world and player data management.

**Features:**
- World creation, saving, loading, deletion
- Automatic player state capture on save
- Auto-save system with configurable interval
- Backup and restore functionality
- Seed management system
- JSON serialization with optional compression

**Public API:**
```csharp
// Current save access
WorldSaveData save = SaveLoadService.Instance.CurrentWorldSave;

// Create/Load/Save/Delete
WorldSaveData world = SaveLoadService.Instance.CreateNewWorld(name, seed);
SaveLoadService.Instance.SaveWorld(saveData);
WorldSaveData loaded = SaveLoadService.Instance.LoadWorld(worldGuid);
SaveLoadService.Instance.DeleteWorld(worldGuid);

// Player data access
Vector3 position = SaveLoadService.Instance.GetSavedPlayerPosition();
Quaternion rotation = SaveLoadService.Instance.GetSavedPlayerRotation();
PlayerSaveData playerData = SaveLoadService.Instance.GetSavedPlayerData();

// New world detection
bool isNew = SaveLoadService.Instance.IsNewWorld();
bool useDefault = SaveLoadService.Instance.ShouldUseDefaultSpawn();

// Auto-save
SaveLoadService.Instance.EnableAutoSave(300f); // 5 minutes
SaveLoadService.Instance.PerformAutoSave();
```

### 5. GameplaySceneInitializer

**File:** `Core/GameplaySceneInitializer.cs`

**Purpose:** Initializes gameplay scene with loaded world data.

**Responsibilities:**
- Load world from SaveLoadService
- Spawn player at correct position (new vs existing world)
- Initialize terrain with world seed
- Restore player stats
- Enable auto-save

### 6. WorldPersistenceManager

**File:** `Core/WorldPersistenceManager.cs`

**Purpose:** ScriptableObject for transferring data between Menu and Gameplay scenes.

**Use Case:** Legacy system - SaveLoadService now handles this better.

---

## How It Works in Game

### Game Initialization Flow

```
1. Unity starts → GameServiceBootstrapper.Awake() [Order: -100]
2. Bootstrap creates ServiceContainer singleton
3. Bootstrap registers EventBus
4. Bootstrap finds and registers all services:
   - SaveLoadService
   - PlayerController
   - PlayerStats
   - InventoryManager
   - EquipmentManager
   - UI Services
   - etc.
5. SaveLoadService.Awake() initializes directories
6. Other systems Start() and resolve dependencies from ServiceContainer
7. ✅ All systems ready
```

### Auto-Save Flow

```
1. GameplaySceneInitializer starts auto-save timer
2. Every 5 minutes (configurable):
   - SaveLoadService.UpdatePlayerDataFromGame()
   - Resolve PlayerController from ServiceContainer
   - Resolve PlayerStats from ServiceContainer
   - Capture position, rotation, stats
   - Increment totalPlayTime
   - SaveWorld() writes JSON to disk
   - Create backup (optional)
   - Update metadata file
3. Timer resets
```

### Event Communication Flow

```
System A → Publish Event → EventBus → All Subscribers
                                        ↓
                               System B (handles event)
                               System C (handles event)
```

---

## How to Use

### Adding a New Service

1. **Create your service class:**
```csharp
public class MyNewService : MonoBehaviour
{
    public void DoSomething() { }
}
```

2. **Register in GameServiceBootstrapper:**
```csharp
// In FindAndRegisterServices()
var myService = FindFirstObjectByType<MyNewService>();
if (myService != null)
{
    container.Register(myService);
    if (enableDebugLogs)
        Debug.Log("[GameServiceBootstrapper] MyNewService registered");
}
```

3. **Resolve in other systems:**
```csharp
private void Start()
{
    var myService = ServiceContainer.Instance.TryGet<MyNewService>();
    if (myService != null)
    {
        myService.DoSomething();
    }
}
```

### Creating a New Event

1. **Define event class:**
```csharp
public class MyCustomEvent
{
    public int value;
    public string message;
}
```

2. **Publish event:**
```csharp
var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
eventBus?.Publish(new MyCustomEvent { value = 42, message = "Hello" });
```

3. **Subscribe to event:**
```csharp
private void OnEnable()
{
    var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
    eventBus?.Subscribe<MyCustomEvent>(OnMyCustomEvent);
}

private void OnDisable()
{
    var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
    eventBus?.Unsubscribe<MyCustomEvent>(OnMyCustomEvent);
}

private void OnMyCustomEvent(MyCustomEvent evt)
{
    Debug.Log($"Received: {evt.message} with value {evt.value}");
}
```

### Adding Data to Save System

1. **Add fields to PlayerSaveData:**
```csharp
// In WorldSaveData.cs
[Serializable]
public class PlayerSaveData
{
    // Existing fields...
    
    // NEW: Add your custom data
    public int myCustomValue;
    public string myCustomString;
}
```

2. **Capture data in UpdatePlayerDataFromGame():**
```csharp
// In SaveLoadService.cs - UpdatePlayerDataFromGame()
var myComponent = container.TryGet<MyComponent>();
if (myComponent != null)
{
    currentWorldSave.playerData.myCustomValue = myComponent.GetValue();
    currentWorldSave.playerData.myCustomString = myComponent.GetString();
}
```

3. **Restore data when loading:**
```csharp
// In your GameplaySceneInitializer or component
PlayerSaveData data = SaveLoadService.Instance.GetSavedPlayerData();
myComponent.SetValue(data.myCustomValue);
myComponent.SetString(data.myCustomString);
```

---

## How to Expand

### Adding Scene-Specific Services

If you need services that only exist in certain scenes:

```csharp
// Create a scene-specific bootstrapper
public class MenuSceneBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        var container = ServiceContainer.Instance;
        
        // Register menu-specific services
        var menuService = GetComponent<MenuSpecificService>();
        container.Register(menuService);
    }
    
    private void OnDestroy()
    {
        // Optional: Unregister when leaving scene
        var container = ServiceContainer.Instance;
        // Note: Current ServiceContainer doesn't have Unregister
        // Consider adding it if needed
    }
}
```

### Implementing Service Interfaces

For better testability and flexibility:

```csharp
// 1. Create interface
public interface IMyService
{
    void DoWork();
}

// 2. Implement interface
public class MyService : MonoBehaviour, IMyService
{
    public void DoWork() { }
}

// 3. Register as interface
container.Register<IMyService>(myService);

// 4. Resolve as interface (mockable for tests)
var service = container.TryGet<IMyService>();
```

### Adding Service Lifetime Management

Current implementation is singleton-only. To add scoped/transient:

```csharp
public enum ServiceLifetime
{
    Singleton,  // One instance for entire game
    Scoped,     // One instance per scene
    Transient   // New instance each time
}

// Enhanced ServiceContainer
public void Register<TService>(Func<TService> factory, ServiceLifetime lifetime)
{
    // Store factory and lifetime
    // Create instances based on lifetime strategy
}
```

---

## Architecture Patterns

### Dependency Injection (Service Locator Pattern)

**Benefits:**
- Decoupled dependencies
- Easy to test (can mock services)
- Central service management
- Reduced FindFirstObjectByType usage

**Trade-offs:**
- Service Locator is sometimes considered an anti-pattern
- Hidden dependencies (not explicit in constructor)
- Runtime resolution (not compile-time safe)

**Improvements:**
- Consider constructor injection for critical dependencies
- Add service validation on startup
- Implement proper lifetime scopes

### Event Bus (Observer Pattern)

**Benefits:**
- No coupling between publisher and subscriber
- One-to-many communication
- Easy to add new listeners
- Type-safe with generics

**Trade-offs:**
- Harder to trace event flow
- No compile-time guarantee of handlers
- Memory leak risk if unsubscribe forgotten

**Best Practices:**
- Always unsubscribe in OnDisable/OnDestroy
- Use meaningful event names
- Keep event data immutable
- Document event contracts

---

## Common Patterns

### Pattern 1: Service-Dependent MonoBehaviour

```csharp
public class MyComponent : MonoBehaviour
{
    private IEventBus eventBus;
    private PlayerStats stats;
    
    private void Start()
    {
        // Resolve dependencies
        var container = ServiceContainer.Instance;
        eventBus = container.TryGet<IEventBus>();
        stats = container.TryGet<PlayerStats>();
        
        // Validate
        if (eventBus == null || stats == null)
        {
            Debug.LogError("[MyComponent] Required services not found!");
            enabled = false;
            return;
        }
        
        // Subscribe to events
        eventBus.Subscribe<SomeEvent>(OnSomeEvent);
    }
    
    private void OnDestroy()
    {
        // Clean up
        eventBus?.Unsubscribe<SomeEvent>(OnSomeEvent);
    }
    
    private void OnSomeEvent(SomeEvent evt)
    {
        // Handle event
    }
}
```

### Pattern 2: Save/Load Integration

```csharp
public class MyPersistentComponent : MonoBehaviour
{
    private SaveLoadService saveService;
    
    private void Start()
    {
        saveService = SaveLoadService.Instance;
        
        // Check if loading existing world
        if (!saveService.IsNewWorld())
        {
            LoadMyData();
        }
        
        // Subscribe to save events
        saveService.OnWorldSaved += OnWorldSaved;
    }
    
    private void OnDestroy()
    {
        if (saveService != null)
        {
            saveService.OnWorldSaved -= OnWorldSaved;
        }
    }
    
    private void LoadMyData()
    {
        // Custom data loading logic
        var saveData = saveService.CurrentWorldSave;
        // Access your custom data from saveData
    }
    
    private void OnWorldSaved(WorldSaveData data)
    {
        // Save your component's data
        // This is called automatically on auto-save
    }
}
```

---

## Performance Considerations

### ServiceContainer
- **Dictionary lookup:** O(1) - very fast
- **No caching needed:** Already storing instances
- **Memory:** ~32 bytes per registered service

### EventBus
- **Subscription:** O(1) add to list
- **Publishing:** O(n) where n = subscriber count
- **Memory:** ~48 bytes per subscriber

### Save/Load
- **JSON serialization:** ~2-5ms for typical save
- **File I/O:** ~10-20ms for write
- **Background thread consideration:** Could move I/O to background
- **Compression:** Base64 adds ~30% size, consider GZIP

---

## Troubleshooting

### Service Not Found

**Problem:** `TryGet<T>()` returns null

**Solutions:**
1. Check GameServiceBootstrapper has the service registered
2. Verify FindFirstObjectByType succeeds (check scene for component)
3. Ensure service is registered before it's resolved (check execution order)
4. Enable debug logs in GameServiceBootstrapper

### Event Not Firing

**Problem:** Published event not received

**Solutions:**
1. Verify subscription happens before publish
2. Check OnDisable isn't unsubscribing too early
3. Ensure correct event type (exact type match required)
4. Check event bus was resolved successfully

### Save File Corrupted

**Problem:** LoadWorld fails or returns null

**Solutions:**
1. Check file exists at expected path
2. Verify JSON is valid (try manual deserialization)
3. Check compression/decompression is consistent
4. Use ValidateSaveFile() before loading
5. Restore from backup

---

## File Structure

```
Core/
├── GameServiceBootstrapper.cs          # Service registration
├── GameplaySceneInitializer.cs        # Gameplay scene setup
├── WorldPersistenceManager.cs         # Legacy scene transfer
│
├── DependencyInjection/
│   ├── ServiceContainer.cs            # DI container
│   └── IServiceProvider.cs            # Provider interface
│
├── Events/
│   ├── IEventBus.cs                   # Event bus interface
│   ├── EventBus.cs                    # Event bus implementation
│   └── [Event Classes].cs             # Individual event types
│
└── SaveSystem/
    ├── ISaveLoadService.cs            # Service interface
    ├── SaveLoadService.cs             # Main save/load logic
    ├── WorldSaveData.cs               # Save data structures
    ├── SeedConfig.cs                  # Seed configuration SO
    ├── SeedData.cs                    # Seed data structure
    ├── SaveExitButton.cs              # Example save button
    └── WorldSeedLoader.cs             # Example seed loader
```

---

## Best Practices

### ✅ DO
- Always use TryGet for safe service resolution
- Subscribe to events in OnEnable, unsubscribe in OnDisable
- Register services in GameServiceBootstrapper
- Use interfaces for services when possible
- Validate service resolution with null checks
- Document public APIs
- Use meaningful event names

### ❌ DON'T
- Don't use FindFirstObjectByType in gameplay code (use DI)
- Don't forget to unsubscribe from events
- Don't modify ServiceContainer at runtime (register at startup)
- Don't put game logic in Core system (keep it infrastructure only)
- Don't save sensitive data without encryption
- Don't block main thread with long I/O operations

---

## Future Enhancements

### Priority 1: High Impact
- ✅ Inventory/Equipment save implementation
- 🔄 Service lifetime management (scoped/transient)
- 🔄 Background thread for save I/O
- 🔄 Constructor injection option

### Priority 2: Nice to Have
- Save file encryption
- GZIP compression instead of Base64
- Cloud save integration
- Service dependency validation tool
- Event flow debugging tool

---

## Related Documentation

- [SAVE_LOAD_SYSTEM_DESIGN.md](../../SAVE_LOAD_SYSTEM_DESIGN.md) - Detailed save system design
- [CODEBASE_MERMAID_DIAGRAMS.md](../../CODEBASE_MERMAID_DIAGRAMS.md) - Visual architecture
- [CODEBASE_ARCHITECTURE_OVERVIEW.md](../../CODEBASE_ARCHITECTURE_OVERVIEW.md) - Full architecture
- [CODEBASE_DEPENDENCY_MAP.md](../../CODEBASE_DEPENDENCY_MAP.md) - Dependency relationships

---

**Last Updated:** February 16, 2026  
**System Status:** ✅ Production Ready
