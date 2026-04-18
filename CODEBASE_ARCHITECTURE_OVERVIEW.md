# Codebase Architecture Overview — "This is so PEAK"

**Last Updated:** April 18, 2026  
**Purpose:** Comprehensive architecture reference for all systems, patterns, and entry points.  
**Total scripts:** 286 C# files under `Assets/Game/Script/`

---

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture Patterns](#architecture-patterns)
3. [System Catalog](#system-catalog)
4. [Service Container Registry](#service-container-registry)
5. [Initialization Sequence](#initialization-sequence)
6. [Two-Gate Scene Loading](#two-gate-scene-loading)
7. [Event Bus Reference](#event-bus-reference)
8. [Layer Architecture](#layer-architecture)
9. [AI Analysis Guide](#ai-analysis-guide)

---

## Project Overview

Unity 3D survival/climbing game. Player climbs procedurally generated terrain, manages survival stats (health, hunger, thirst, stamina, temperature, fatigue), collects items, and interacts with the world.

### Directory Map
```
Assets/Game/Script/
  Collectable/    — CollectableManager, CollectableItem, CollectableZoneUnlockTrigger
  Core/           — DI (ServiceContainer), Events (EventBus), SaveSystem, GameServiceBootstrapper,
                    AsyncLoadCoordinator, GameplaySceneInitializer, WorldPersistenceManager
  Dialog/         — DialogManager + world triggers
  Editor/         — editor-only inspector utilities
  Environment/    — DayNight, Landslide, Tornado, Temperature
  Interaction/    — IInteractable, InteractionDetector, HoldInteractableBase, interactables
  Menu/           — MainMenuUI, WorldSelectionUI, WorldCreateUI, world slots
  Mountain/       — terrain mesh + falloff generation (in-script terrain)
  Player/         — PlayerControllerRefactored, 6 states, inventory, stats, services
  Progression/    — LevelBonusCollectableService, StarterCollectableService
  Sound/          — SoundService, SoundLibrary, SoundConfig, SoundEvents
  Tutorial/       — TutorialManager, step data
  UI/             — UIServiceProvider, all panels, adapters, HUD, loading UI

Assets/HJB/           — HJB pathfinding solver/backtracker/visualizer
Assets/TerrainGenerator/ — RenderController, WorldDataManager (procedural terrain chunks)
```

---

## Architecture Patterns

### 1. Dependency Injection — ServiceContainer

`Assets/Game/Script/Core/DependencyInjection/ServiceContainer.cs`

Type-keyed dictionary singleton. All game services are registered here at startup.

```csharp
// Register
ServiceContainer.Instance.Register<IEventBus>(new EventBus());
ServiceContainer.Instance.Register(myComponent);

// Resolve (throws InvalidOperationException if missing)
var bus = ServiceContainer.Instance.Get<IEventBus>();

// Safe resolve (returns null if missing)
var stats = ServiceContainer.Instance.TryGet<PlayerStats>();

// Check / remove
ServiceContainer.Instance.Has<T>();
ServiceContainer.Instance.Unregister<T>();
```

**Rule:** Always register in `GameServiceBootstrapper`. Retrieve via `Get<T>()` or `TryGet<T>()`. Never use `FindFirstObjectByType` at runtime.

---

### 2. Event Bus — IEventBus / EventBus

`Assets/Game/Script/Core/Events/EventBus.cs`

Type-safe publish/subscribe bus. All cross-system communication goes through EventBus. Static C# events are forbidden.

```csharp
_eventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
_eventBus.Publish(new ItemAddedEvent(item, qty));
_eventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
```

**Event files:**
- `Core/Events/GameEvents.cs` — `PlayerDiedEvent`, general game lifecycle
- `Core/Events/InventoryEvents.cs` — inventory lifecycle
- `Core/Events/DayNightEvents.cs` — time-of-day transitions
- `Player/Inventory/Events/InventoryEvents.cs` — `ItemAddedEvent`, `ItemRemovedEvent`, `ItemEquippedEvent`, etc.
- `Sound/Events/SoundEvents.cs` — play/stop/crossfade requests

---

### 3. State Machine — Player States

`Assets/Game/Script/Player/PlayerState/`

`PlayerControllerRefactored` owns the state machine. States implement `IPlayerState`:

| State | File | Key Behaviour |
|-------|------|---------------|
| `WalkingState` | `WalkingState.cs` | Ground movement, Tobler slope speed, fatigue penalty |
| `RunningState` | `RunningState.cs` | Stamina drain, speed ramp-up |
| `ClimbingState` | `ClimbingState.cs` | Wall attachment, stamina drain |
| `MantlingState` | `MantlingState.cs` | ClimbUp animation, ledge snap at 70% progress |
| `FallingState` | `FallingState.cs` | Air control 30%, gravity, coyote time |
| `TiedState` | `TiedState.cs` | Reduced speed within anchor radius, triggered by `TiedInteractable` |

Transition via `PlayerControllerRefactored.TransitionTo<TState>()`.

---

### 4. Command Pattern — Inventory Commands

`Assets/Game/Script/Player/Inventory/Commands/`

All inventory mutations go through command objects for undo/redo support.

```
IInventoryCommand
  PickupItemCommand   — add item from world
  DropItemCommand     — remove item to world
  UseItemCommand      — consume item (non-undoable)
  CraftItemCommand    — craft recipe
```

`InventoryCommandInvoker` maintains a history stack. Never mutate inventory state directly.

---

### 5. Template Method — HoldInteractableBase

`Assets/Game/Script/Interaction/Core/HoldInteractableBase.cs`

All timed hold-to-interact objects extend this class. It handles progress tracking, input checking, player locking, audio, and cancellation. Subclasses only implement `OnHoldComplete()`.

---

### 6. Strategy Pattern — Consumable Effects

`Assets/Game/Script/Player/Inventory/Effects/`

`ConsumableEffectBase` → concrete effect strategies (HealthEffect, HungerEffect, ThirstEffect, StaminaEffect, TemperatureEffect). `ConsumableEffectSystem` applies them.

---

### 7. Adapter Pattern — UI Adapters

`Assets/Game/Script/UI/Adapters/`

Adapters wrap legacy UI classes to implement `IUIPanel`, enabling gradual migration without breaking existing code.

---

## System Catalog

### Core (`Core/`)

| Component | Purpose |
|-----------|---------|
| `GameServiceBootstrapper` | ExecutionOrder -100. Registers all services at startup. |
| `ServiceContainer` | Type-keyed DI singleton. |
| `EventBus` | Type-safe pub/sub bus for all cross-system events. |
| `AsyncLoadCoordinator` | Two-gate background loading orchestrator (see below). |
| `SharedLoadingState` | ScriptableObject data bus between TerrainGenDemo and loading room scene. |
| `GameplaySceneInitializer` | Restores player/world state after terrain load completes. |
| `WorldPersistenceManager` | ScriptableObject — carries world GUID, name, spawn position across scene loads. |
| `SaveLoadService` | JSON+backup save system. DontDestroyOnLoad singleton. Auto-saves every 300 s. |

---

### Player (`Player/`)

| Component | Purpose |
|-----------|---------|
| `PlayerControllerRefactored` | Main player MonoBehaviour. Owns state machine. |
| `PlayerModelRefactored` | Aggregate root: transform, CharacterController, stats, config references. |
| `PlayerConfig` | ScriptableObject — movement speeds, jump force, coyote time, etc. |
| `PlayerPhysicsService` | Wraps CharacterController movement. |
| `PlayerAnimationService` | Wraps Animator. |
| `PlayerInputHandler` | Reads Unity Input System. |
| `PlayerInventoryFacade` | Single interface to inventory/crafting/equipment for PlayerController. |
| `PlayerStats` | Health, Hunger, Thirst, Stamina, Fatigue, Temperature stats. |
| `FootIKControllerRefactored` | Ground foot IK via IK strategies. |
| `HandIKControllerRefactored` | Climbing hand IK. |

**Player/Stat/Assessment/** — `LearningAssessmentService`, `PlayerStatsTrackerService`, `StandardAssessmentCalculator`, `AssessmentScore`, `PerformanceMetrics`, `RiskTracker`, `PathTracker`, etc.

---

### Inventory (`Player/Inventory/`)

| Component | Purpose |
|-----------|---------|
| `InventoryManagerRefactored` | Self-registers IInventoryService, IInventoryStorage, IConsumableEffectSystem. |
| `GridInventoryStorage` | 10×6 grid backend. |
| `EquipmentManager` | Slot management (Head/Body/Foot/Hand/HeldItem). |
| `CraftingManager` | Recipe matching and crafting. |
| `HeldItemBehaviorManager` | Creates/destroys behavior components when HeldItem slot changes. |
| `HeldItemStateManager` | Persists durability/charges across equip/unequip. |
| `TorchItem` / `TorchBehavior` | Durability-based held light source. |
| `CanteenItem` / `CanteenBehavior` | Charge-based hydration item. |
| `WorldItemSpawner` | Spawns dropped items into the world. |

---

### Sound (`Sound/`)

| Component | Purpose |
|-----------|---------|
| `SoundService` | Main audio service. AudioMixer groups (SFX/UI/Music/Ambient). Object-pooled AudioSources. Crossfade for music/ambient. |
| `SoundLibrary` | ScriptableObject registry of named audio clips. |
| `SoundConfig` | ScriptableObject — pool sizes, default volumes. |
| `SoundCategory` | Enum: SFX, UI, Music, Ambient. |
| `SoundEvents` | EventBus payload types: `PlaySoundEvent`, `StopSoundEvent`, `PlayMusicEvent`, etc. |
| `SoundEventListener` | MonoBehaviour — subscribes to SoundEvents and forwards to SoundService. |
| `SoundSettingsManager` | Reads/writes volume settings to/from AudioMixer. |

Sounds should be triggered by publishing `SoundEvents` through EventBus. Never call `AudioSource.Play()` directly from game code.

---

### UI (`UI/`)

| Component | Purpose |
|-----------|---------|
| `UIServiceProvider` | Central UI service — exposes UIPanelController, CursorManager, PlayerInputBlocker. |
| `TabbedInventoryUI` | Grid, Equipment, Crafting tabs. |
| `GridInventoryUI` | Drag-and-drop 10×6 grid. |
| `EquipmentUI` | Equipment slot display. |
| `CraftingUI` | Recipe list + craft button. |
| `SimpleStatsHUD` | Real-time health/hunger/thirst/stamina/temperature bars. |
| `DeathScreenUI` | Death panel with respawn options. |
| `EndingScreenUI` | Game completion / assessment reveal. |
| `AssessmentReportUI` | Detailed performance report. |
| `PlayerStatsTrackerUI` | Time-series stat graphs. |
| `LoadingProgressUI` | Reads `SharedLoadingState.progress`, drives Slider + label. |
| `DebugGameplayConfirm` | Shows confirm prompt when Gate 1 passes; Y key sets Gate 2. |
| `DialogUI` | Dialog bubble UI. |
| `TutorialUI` | Tutorial step overlay. |
| `CollectablesHubUI` | Hub panel listing collectables. |
| `DocumentPageUI` | Full-page collectable document viewer. |
| `BlurOverlay/` | `VolumeBlurController`, `DOTweenBlurEffect`, `FallImpactFeedback`, `LowHealthHeartbeatFeedback`, `LowStaminaBreathingFeedback`, `TemperaturePostProcessFeedback` — post-process visual feedback. |
| `BillboardText` | World-space text that always faces camera. |
| `FloatingNumber` | Floating damage/heal number popup. |

---

### Interaction (`Interaction/`)

| Component | Purpose |
|-----------|---------|
| `InteractionDetector` | OverlapSphere every 0.1 s. Priority ranks candidates. Fires `OnNearestInteractableChanged`. |
| `IInteractable` | Contract: `CanInteract`, `InteractionPrompt`, `InteractionPriority`, `Interact(player)`. |
| `HoldInteractableBase` | Abstract template for all timed interactions. |
| `ItemInteractable` | Instant pickup → PickupItemCommand. |
| `GatheringInteractable_Refactored` | Hold-gather → fires `GatheringDiscoveryDialogTrigger`. |
| `CollectableInteractable` | Interact → CollectableManager.Unlock(). |
| `WaterSourceInteractable` | Hold-refill canteen. |
| `ResourceCollectorInteractable` | Multi-resource node collector. |
| `AssessmentTerminalInteractable` | Opens learning terminal. |
| `TiedInteractable` | Transitions player to `TiedState`. |
| `RandomEquipmentRewardInteractable` | Grants random equipment on interact. |
| `RandomCollectableUnlockInteractable` | Unlocks random collectable. |
| `DebugLandslideHoldInteractable` | Dev-only: manually trigger landslide. |
| `DebugTornadoHoldInteractable` | Dev-only: manually trigger tornado. |

---

### Dialog (`Dialog/`)

| Component | Purpose |
|-----------|---------|
| `DialogManager` | `IDialogManager`. Queues and displays dialog sequences. Initialized with EventBus. |
| `DialogData` | ScriptableObject — array of `DialogLine` entries. |
| `DialogLine` | Single dialog entry (speaker, text, portrait). |
| `DialogOnStart` | Triggers a `DialogData` when the scene starts. |
| `GatheringDiscoveryDialogTrigger` | Fires dialog the first time a specific resource type is gathered. |
| `WorldDialogTrigger` | Fires dialog on world-condition trigger. |

---

### Tutorial (`Tutorial/`)

| Component | Purpose |
|-----------|---------|
| `TutorialManager` | `ITutorialManager`. Drives step-by-step tutorial flow. Initialized with EventBus, SaveLoadService, PlayerController, CinemachinePlayerCamera. |
| `TutorialData` | ScriptableObject — ordered array of `TutorialStepData`. |
| `TutorialStepData` | Per-step data (type, text, target, condition). |
| `TutorialStepType` | Enum of step kinds. |

---

### Collectable (`Collectable/`)

| Component | Purpose |
|-----------|---------|
| `CollectableManager` | `ICollectableManager`. HashSet of unlocked IDs. Publishes `CollectableUnlockedEvent`. |
| `CollectableItem` | ScriptableObject — id, display name, description, icon, document pages. |
| `CollectableType` | Enum categorizing collectables. |
| `CollectableZoneUnlockTrigger` | Trigger volume that auto-unlocks a collectable on enter. |

---

### Progression (`Progression/`)

| Component | Purpose |
|-----------|---------|
| `StarterCollectableService` | Grants starter collectables to new players. Registered in ServiceContainer. |
| `LevelBonusCollectableService` | Grants collectables when levelling up. Initialized with EventBus, CollectableManager, SaveLoadService, StarterCollectableService. |
| `StarterCollectableConfig` | ScriptableObject — list of starter collectable items. |
| `LevelBonusCollectableConfig` | ScriptableObject — per-level bonus collectable lists. |

---

### Environment (`Environment/`)

**DayNight:**
- `DayNightCycleManager` (`IDayNightCycleService`) — 24-hour cycle, directional light rotation, skybox blending, events.
- `DayNightConfig` — ScriptableObject with lighting/fog settings per period.
- `SkyboxBlender` — smooth cubemap skybox crossfade.
- `TimeOfDay` enum: Morning/Day/Evening/Night.

**Landslide:**
- `LandslideRockSpawner` — spawns rocks on trigger.
- `LandslideRockBehavior` — individual rock physics.
- `LandslideRockBehaviorConfig` — ScriptableObject.
- `LandslideShakeController` — camera shake on landslide.
- `LandslideDecalService` — places decals where rocks land.

**Tornado:**
- `TornadoMovement`, `TornadoPhaseController`, `TornadoPlayerPull`, `TornadoProximityFeedback`.
- `TornadoConfig` — ScriptableObject.

**Temperature:**
- `ITemperatureSource` — interface for objects that affect player temperature.

---

### Menu (`Menu/`)

`MainMenuUI`, `WorldSelectionUI`, `WorldCreateUI`, `WorldSlotUI`, `WorldData`, `MenuPanelAnimator`, `ButtonHoverScale`, `ButtonUnderlineAnimator`, `ConfirmationDialogUI`, `LoadingPanelUI`.

---

### Mountain / Terrain (`Mountain/`)

`TerrainGeneration`, `TerrainMeshGenerator`, `Falloff`, `TextureRigid` — in-script procedural terrain. Separate from `Assets/TerrainGenerator/` which handles chunked streaming terrain.

---

## Service Container Registry

Complete list of services registered at startup (April 2026):

**Registered by `GameServiceBootstrapper`:**

| Key Type | Implementation | Notes |
|----------|---------------|-------|
| `IEventBus` | `EventBus` | Registered first — others depend on it |
| `PlayerControllerRefactored` | scene instance | |
| `PlayerStats` | scene instance | |
| `CraftingManager` | scene instance | |
| `TabbedInventoryUI` | scene instance | |
| `CinemachinePlayerCamera` | scene instance | |
| `EquipmentManager` | scene instance | also self-registered by InventoryMgr |
| `InventoryUI` | scene instance | legacy fallback |
| `TooltipUI` | scene instance | |
| `ContextMenuUI` | scene instance | |
| `InteractionDetector` | scene instance | |
| `InteractionPromptUI` | scene instance | |
| `ItemNotificationUI` | scene instance | |
| `SimpleStatsHUD` | scene instance | |
| `SoundService` | scene instance | `Initialize()` called before registration |
| `IDayNightCycleService` | `DayNightCycleManager` | |
| `DayNightCycleManager` | scene instance | `Initialize(eventBus, soundService, equipment)` |
| `PlayerStatsTrackerUI` | scene instance | |
| `AssessmentReportUI` | scene instance | |
| `EndingScreenUI` | scene instance | |
| `LearningAssessmentService` | scene instance | |
| `PlayerStatsTrackerService` | scene instance | |
| `ISaveLoadService` | `SaveLoadService` | `Initialize()` called |
| `SaveLoadService` | DontDestroyOnLoad | |
| `ICollectableManager` | `CollectableManager` | `Initialize(eventBus)` called |
| `CollectableManager` | scene instance | |
| `IDialogManager` | `DialogManager` | `Initialize(eventBus)` called |
| `DialogManager` | scene instance | |
| `ITutorialManager` | `TutorialManager` | `Initialize(eventBus, saveLoadService, player, camera)` called |
| `TutorialManager` | scene instance | |
| `UIServiceProvider` | scene instance | `EnsureInitialized()` called |
| `StarterCollectableService` | scene instance | |
| `LevelBonusCollectableService` | scene instance | `Initialize(eventBus, cm, saveLoadService, starterService)` |

**Self-registered by `InventoryManagerRefactored.Awake()`:**

| Key Type | Implementation |
|----------|---------------|
| `IInventoryService` | `InventoryService` |
| `IInventoryStorage` | `GridStorageAdapter` (wraps `GridInventoryStorage`) |
| `IConsumableEffectSystem` | `ConsumableEffectSystem` |
| `InventoryManagerRefactored` | self |
| `EquipmentManager` | scene instance |

---

## Initialization Sequence

```
1. DontDestroyOnLoad objects persist from Menu scene:
   └─► SaveLoadService (DDOL singleton)

2. GameServiceBootstrapper.Awake() [ExecutionOrder -100]
   └─► Registers all services listed above

3. InventoryManagerRefactored.Awake() [default order]
   └─► Self-registers IInventoryService, IInventoryStorage, IConsumableEffectSystem

4. PlayerControllerRefactored.Awake()
   └─► InitializeModel → InitializeServices → InitializeInventory
   └─► Resolves all services from ServiceContainer

5. AsyncLoadCoordinator.Start() [TerrainGenDemo]
   └─► Two-gate loading flow begins (see below)

6. GameplaySceneInitializer.Start() [waits on PlayerSpawnComplete]
   └─► Restores player stats, inventory, world state

7. PlayerControllerRefactored enters WalkingState → game ready
```

---

## Two-Gate Scene Loading

The terrain scene uses a two-gate deferred spawn flow:

| Gate | Condition | Set By | Opens When |
|------|-----------|--------|------------|
| **Gate 1** | `SharedLoadingState.isChunksReady` | `RenderController.FinalizeChunk()` | All terrain chunks finalized |
| **Gate 2** | `SharedLoadingState.playerConfirmed` | `DebugGameplayConfirm` | Player presses Y |

**Flow:**
1. Menu scene loads save data → transitions to TerrainGenDemo.
2. `AsyncLoadCoordinator` disables terrain camera, additively loads `Scene_Debug_Gameplay`.
3. `RenderController` generates chunks, reports progress (10%→95%) to `SharedLoadingState`.
4. Gate 1 opens → status becomes "Press Y to enter world".
5. (Optional) HJB path calculation runs between Gate 1 and Gate 2.
6. Player presses Y → Gate 2 opens.
7. `AsyncLoadCoordinator` unloads `Scene_Debug_Gameplay`, re-enables terrain camera, calls `RenderController.SpawnPlayerNow()`.
8. `GameplaySceneInitializer` restores full world state.

`SharedLoadingState` is a ScriptableObject — both scenes reference the same asset with no direct coupling.

---

## Event Bus Reference

Key events by system:

**Inventory:**
- `ItemAddedEvent(item, qty)` — published by InventoryService
- `ItemRemovedEvent(item, qty)`
- `ItemConsumedEvent(item)`
- `InventoryChangedEvent`
- `ItemEquippedEvent(item, slotType)`
- `ItemUnequippedEvent(slotType)`

**Collectable:**
- `CollectableUnlockedEvent(collectable)` — published by CollectableManager

**Day/Night:**
- `TimeOfDayChangedEvent(prev, next, currentTime)`
- `DayCompletedEvent(dayNumber)`

**Sound:**
- `PlaySoundEvent`, `StopSoundEvent`, `PlayMusicEvent`, `StopMusicEvent`, `PlayAmbientEvent`

**Player:**
- `StaminaChangedEvent`, `HealthChangedEvent`, `PlayerDiedEvent`

---

## Layer Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    PRESENTATION                          │
│  Menu / All UI panels / HUD / Dialog / Tutorial overlays │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│                    APPLICATION                           │
│  PlayerControllerRefactored + 6 States                   │
│  PlayerInventoryFacade · InteractionDetector             │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│                      DOMAIN                              │
│  Inventory · Equipment · Crafting · PlayerStats          │
│  CollectableManager · DialogManager · TutorialManager    │
│  DayNightCycleManager · SoundService · LevelBonusService │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│                   INFRASTRUCTURE                         │
│  ServiceContainer · EventBus · SaveLoadService           │
│  SharedLoadingState · AsyncLoadCoordinator               │
└─────────────────────────────────────────────────────────┘
```

Dependency direction: Infrastructure → Domain → Application → Presentation. No layer depends on a layer above it.

---

## AI Analysis Guide

### Adding a New System

1. Create a service interface only if you have a concrete swap scenario (e.g. mock for tests). Otherwise, register the concrete class.
2. Register in `GameServiceBootstrapper.FindAndRegisterServices()`.
3. Call any needed `Initialize(...)` method immediately after registration.
4. Communicate cross-system via EventBus. Define new event types in the relevant `Events/` file.
5. Do not use `FindFirstObjectByType` at runtime (bootstrap is the only exception by design).

### Adding a New Interactable

- Timed hold interaction → extend `HoldInteractableBase`, override `OnHoldComplete()`.
- Instant interaction → implement `IInteractable` directly.
- Register any needed services in the DI container and resolve via `ServiceContainer.Instance.TryGet<T>()` in `Start()` or `Awake()`.

### Adding a New Player State

- Implement `IPlayerState`.
- Call `TransitionTo<NewState>()` (or pass a constructed instance) from the PlayerController or the current state.

### Debug Queries

```bash
# All service registrations
grep -r "Register<\|Register(" Assets/Game/Script/Core/GameServiceBootstrapper.cs

# All EventBus subscriptions
grep -rn "Subscribe<" Assets/Game/Script/

# All interactable types
grep -rn "IInteractable\|HoldInteractableBase" Assets/Game/Script/Interaction/

# All player states
grep -rn "IPlayerState" Assets/Game/Script/Player/PlayerState/
```

---

**Related per-system docs:**
- `Assets/Game/Script/CODEBASE_ARCHITECTURE_OVERVIEW.md` — extended narrative + refactoring history
- `Assets/Game/Script/Core/ASYNC_LOADING_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/Interaction/INTERACTION_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/Player/PLAYER_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/Player/Inventory/INVENTORY_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/Sound/SOUND_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/UI/UI_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/Menu/MENU_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/Environment/Landslide/LANDSLIDE_SYSTEM_OVERVIEW.md`
- `Assets/Game/Script/UI/BlurOverlay/BLUR_OVERLAY_SYSTEM.md`
- `Assets/Game/Script/UI/Components/BILLBOARD_TEXT_SYSTEM_OVERVIEW.md`
