# Codebase Mermaid Diagrams - "This is so PEAK"

**Last Updated:** February 16, 2026  
**Purpose:** Visual architecture diagrams using Mermaid for quick understanding

---

## Table of Contents
1. [System Architecture Overview](#system-architecture-overview)
2. [Save/Load System](#saveload-system)
3. [Player System](#player-system)
4. [Inventory System](#inventory-system)
5. [Interaction System](#interaction-system)
6. [Event Flow Diagrams](#event-flow-diagrams)
7. [Service Container Registry](#service-container-registry)

---

## System Architecture Overview

### Layer Architecture

```mermaid
graph TB
    subgraph Presentation["🎨 Presentation Layer"]
        UI[UI Systems]
        Menu[Menu Systems]
        Prompts[Interaction Prompts]
    end
    
    subgraph Application["⚙️ Application Layer"]
        Player[Player Controller]
        Services[Player Services]
        Facades[Inventory Facade]
    end
    
    subgraph Domain["📦 Domain Layer"]
        Inventory[Inventory Manager]
        Equipment[Equipment Manager]
        Crafting[Crafting Manager]
        Stats[Player Stats]
        Items[Item Data - SO]
    end
    
    subgraph Infrastructure["🔧 Infrastructure Layer"]
        DI[Service Container]
        Events[Event Bus]
        Unity[Unity Integration]
        SaveSystem[Save/Load Service]
    end
    
    Presentation --> Application
    Application --> Domain
    Domain --> Infrastructure
    
    style Presentation fill:#e1f5ff
    style Application fill:#fff4e1
    style Domain fill:#f0ffe1
    style Infrastructure fill:#ffe1f5
```

---

## Save/Load System

### Save System Architecture

```mermaid
graph TB
    subgraph Menu["📋 Menu Scene"]
        WorldCreate[WorldCreateUI]
        WorldSelect[WorldSelectionUI]
        WorldSlot[WorldSlotUI]
    end
    
    subgraph Gameplay["🎮 Gameplay Scene"]
        TerrainGen[Terrain Generator]
        PlayerSpawn[Player Spawner]
        WorldLoader[World Seed Loader]
    end
    
    subgraph Core["💾 Save Load Service - Singleton"]
        SaveService[SaveLoadService]
        CurrentSave[currentWorldSave]
        AutoSave[Auto-Save Timer]
    end
    
    subgraph Data["📊 Data Layer"]
        WorldData[WorldSaveData]
        PlayerData[PlayerSaveData]
        SeedData[SeedData]
        WorldState[WorldStateSaveData]
    end
    
    subgraph Storage["💿 Storage"]
        JSON[JSON Files]
        Backups[Backup Files]
        Metadata[metadata.json]
    end
    
    subgraph DI["🔌 Dependency Injection"]
        SC[Service Container]
        PlayerController[PlayerController]
        PlayerStats[PlayerStats]
    end
    
    Menu --> SaveService
    Gameplay --> SaveService
    SaveService --> CurrentSave
    SaveService --> AutoSave
    SaveService --> Data
    Data --> Storage
    SaveService <--> DI
    AutoSave -.->|captures| DI
    
    style SaveService fill:#4a90e2,color:#fff
    style CurrentSave fill:#7ed321
    style Storage fill:#f5a623
```

### Save Flow Sequence

```mermaid
sequenceDiagram
    participant User
    participant Button as Save Button
    participant Service as SaveLoadService
    participant Container as ServiceContainer
    participant Player as PlayerController
    participant Stats as PlayerStats
    participant File as File System
    
    User->>Button: Click "Save & Exit"
    Button->>Service: PerformAutoSave()
    activate Service
    
    Service->>Service: UpdatePlayerDataFromGame()
    Service->>Container: TryGet<PlayerController>()
    Container-->>Service: PlayerController instance
    
    Service->>Player: Get transform.position
    Player-->>Service: Vector3 position
    
    Service->>Player: Get transform.rotation
    Player-->>Service: Quaternion rotation
    
    Service->>Container: TryGet<PlayerStats>()
    Container-->>Service: PlayerStats instance
    
    Service->>Stats: Get Health, Hunger, Stamina
    Stats-->>Service: Stats data
    
    Service->>Service: Update currentWorldSave
    Service->>Service: SaveWorld(currentWorldSave)
    Service->>File: WriteAllText(json)
    File-->>Service: Success
    
    Service->>File: UpdateMetadata()
    Service->>File: CreateBackup() [optional]
    
    deactivate Service
    Service-->>Button: Save complete
    Button->>User: Load Menu Scene
```

### Load/Spawn Decision Flow

```mermaid
flowchart TD
    Start([Gameplay Scene Start])
    GetService[Get SaveLoadService.Instance]
    CheckSave{CurrentWorldSave<br/>exists?}
    CheckNew{IsNewWorld?<br/>totalPlayTime == 0}
    
    DefaultSpawn[Use Default Spawn<br/>PlayerSpawner.TeleportToSpawn]
    RestoreSpawn[Restore From Save<br/>GetSavedPlayerPosition]
    
    DisableCC[Disable CharacterController]
    SetPosition[Set player.transform.position]
    EnableCC[Enable CharacterController]
    
    RestoreStats[Restore Player Stats<br/>health, hunger, stamina]
    
    End([Player Ready])
    
    Start --> GetService
    GetService --> CheckSave
    CheckSave -->|No| DefaultSpawn
    CheckSave -->|Yes| CheckNew
    CheckNew -->|Yes<br/>New World| DefaultSpawn
    CheckNew -->|No<br/>Existing World| RestoreSpawn
    
    DefaultSpawn --> End
    
    RestoreSpawn --> DisableCC
    DisableCC --> SetPosition
    SetPosition --> EnableCC
    EnableCC --> RestoreStats
    RestoreStats --> End
    
    style CheckNew fill:#ff9800
    style DefaultSpawn fill:#4caf50
    style RestoreSpawn fill:#2196f3
```

### Public API Reference

```mermaid
classDiagram
    class SaveLoadService {
        +WorldSaveData CurrentWorldSave
        +CreateNewWorld(name, seed) WorldSaveData
        +SaveWorld(saveData) bool
        +LoadWorld(worldGuid) WorldSaveData
        +DeleteWorld(worldGuid) bool
        +GetAllWorlds() List~SaveMetadata~
        +EnableAutoSave(seconds)
        +DisableAutoSave()
        +PerformAutoSave()
        +GetSavedPlayerPosition() Vector3
        +GetSavedPlayerRotation() Quaternion
        +GetSavedPlayerData() PlayerSaveData
        +IsNewWorld() bool
        +ShouldUseDefaultSpawn() bool
        +CreateBackup(worldGuid) bool
        +RestoreFromBackup(worldGuid, date) bool
        -UpdatePlayerDataFromGame()
    }
    
    class WorldSaveData {
        +string worldName
        +string worldGuid
        +DateTime createdDate
        +DateTime lastPlayedDate
        +float totalPlayTime
        +SeedData seedData
        +PlayerSaveData playerData
        +WorldStateSaveData worldState
    }
    
    class PlayerSaveData {
        +float[] position
        +float[] rotation
        +float health
        +float hunger
        +float stamina
        +List~InventoryItemSaveData~ inventoryItems
        +List~EquipmentSlotSaveData~ equippedItems
    }
    
    class SeedData {
        +string seed1
        +string seed2
        +string seed3
        +string FullSeed
        +IsValid() bool
    }
    
    SaveLoadService --> WorldSaveData
    WorldSaveData --> PlayerSaveData
    WorldSaveData --> SeedData
```

---

## Player System

### Player Controller Architecture

```mermaid
graph TB
    subgraph PlayerController["🎮 PlayerControllerRefactored"]
        Controller[Main Controller]
        Model[PlayerModelRefactored]
        StateMachine[State Machine]
    end
    
    subgraph States["🚶 Player States"]
        Walking[WalkingState]
        Climbing[ClimbingState]
        Falling[FallingState]
    end
    
    subgraph Services["⚙️ Player Services"]
        Physics[PlayerPhysicsService]
        Animation[PlayerAnimationService]
        Input[PlayerInputHandler]
        InvFacade[PlayerInventoryFacade]
    end
    
    subgraph Dependencies["📦 Dependencies - DI"]
        EventBus[IEventBus]
        UIService[UIServiceProvider]
        Inventory[InventoryManager]
        Equipment[EquipmentManager]
        Crafting[CraftingManager]
        Stats[PlayerStats]
    end
    
    Controller --> Model
    Controller --> StateMachine
    StateMachine --> Walking
    StateMachine --> Climbing
    StateMachine --> Falling
    
    Model --> Services
    Model --> Dependencies
    
    Services --> Physics
    Services --> Animation
    Services --> Input
    Services --> InvFacade
    
    style Controller fill:#4a90e2,color:#fff
    style StateMachine fill:#7ed321
```

### State Transition Diagram

```mermaid
stateDiagram-v2
    [*] --> Walking
    
    Walking --> Climbing: Detect Climbable<br/>Press Forward
    Walking --> Falling: Not Grounded<br/>Not Jumping
    Walking --> Walking: Grounded
    
    Climbing --> Falling: Release Climb<br/>OR Stamina Depleted
    Climbing --> Walking: Reach Top<br/>OR Reach Bottom
    
    Falling --> Walking: Land on Ground
    Falling --> Climbing: Grab Climbable<br/>Mid-air
    
    Walking --> [*]: Player Disabled
    
    note right of Climbing
        Consumes stamina
        Can move up/down
        Can jump off
    end note
    
    note right of Falling
        Air control enabled
        Gravity applied
        Can be cancelled
    end note
```

---

## Inventory System

### Inventory System Architecture

```mermaid
graph TB
    subgraph UI["🖥️ UI Layer"]
        InventoryUI[TabbedInventoryUI]
        EquipmentUI[EquipmentUI]
        CraftingUI[CraftingUI]
        Tooltip[TooltipUI]
        ContextMenu[ContextMenuUI]
    end
    
    subgraph Facade["🎭 Facade Layer"]
        InvFacade[PlayerInventoryFacade]
        Commands[Command Pattern]
    end
    
    subgraph Managers["📦 Manager Layer"]
        InventoryMgr[InventoryManagerRefactored]
        EquipmentMgr[EquipmentManager]
        CraftingMgr[CraftingManager]
    end
    
    subgraph Services["⚙️ Service Layer"]
        InvService[IInventoryService]
        InvStorage[IInventoryStorage]
        EffectSystem[IConsumableEffectSystem]
    end
    
    subgraph Data["📊 Data Layer"]
        Items[InventoryItem - SO]
        Slots[InventorySlot]
        Effects[ConsumableEffectBase]
    end
    
    UI --> InvFacade
    UI --> Managers
    
    InvFacade --> Commands
    Commands --> Managers
    
    Managers --> Services
    Services --> Data
    
    style InvFacade fill:#4a90e2,color:#fff
    style Commands fill:#7ed321
```

### Command Pattern Flow

```mermaid
sequenceDiagram
    participant Player
    participant Facade as PlayerInventoryFacade
    participant Cmd as AddItemCommand
    participant Service as IInventoryService
    participant Storage as IInventoryStorage
    participant EventBus
    
    Player->>Facade: AddItem(item, count)
    activate Facade
    
    Facade->>Cmd: new AddItemCommand(…)
    Facade->>Cmd: Execute()
    activate Cmd
    
    Cmd->>Service: AddItem(item, count)
    activate Service
    
    Service->>Storage: CanAddItem(item, count)?
    Storage-->>Service: true/false
    
    alt Can Add
        Service->>Storage: AddToSlot(item, count)
        Storage-->>Service: Success
        Service->>EventBus: Publish(ItemAddedEvent)
        Service-->>Cmd: true
        Cmd-->Facade: true
        Facade->>Facade: commandHistory.Push(cmd)
    else Cannot Add
        Service-->>Cmd: false
        Cmd-->Facade: false
    end
    
    deactivate Service
    deactivate Cmd
    deactivate Facade
```

---

## Interaction System

### Interaction Detection Flow

```mermaid
graph LR
    subgraph Player["👤 Player"]
        PlayerPos[Player Position]
        Controller[PlayerController]
    end
    
    subgraph Detector["🔍 InteractionDetector"]
        SphereOverlap[Physics.OverlapSphere]
        Priority[Priority Checker]
        Events[Event System]
    end
    
    subgraph Interactables["🎯 IInteractables"]
        Item[ItemInteractable]
        Gather[GatheringInteractable]
        Water[WaterSourceInteractable]
        Terminal[AssessmentTerminal]
    end
    
    subgraph UI["💬 UI"]
        Prompt[InteractionPromptUI]
        ProgressBar[Progress Bar]
    end
    
    PlayerPos --> SphereOverlap
    SphereOverlap --> Priority
    Priority --> Events
    
    Events -->|OnInteractableInRange| Prompt
    Controller -->|Input.Interact| Interactables
    
    Interactables -->|UpdateProgress| ProgressBar
    
    style Detector fill:#4a90e2,color:#fff
    style Prompt fill:#7ed321
```

### Hold-to-Interact Template

```mermaid
classDiagram
    class IInteractable {
        <<interface>>
        +bool CanInteract
        +string InteractionPrompt
        +float InteractionPriority
        +Interact(player)
        +OnHighlighted(bool)
    }
    
    class HoldInteractableBase {
        <<abstract>>
        +float holdDuration
        +string progressVerb
        -float holdProgress
        +Interact(player)
        #OnHoldStart()
        #OnHoldComplete()
        #OnHoldCancel(reason)
        -UpdateHoldProgress()
    }
    
    class GatheringInteractable {
        +ResourceType resourceType
        +int gatherAmount
        #OnHoldComplete()
    }
    
    class WaterSourceInteractable {
        +int refillAmount
        #OnHoldStart()
        #OnHoldComplete()
        #OnHoldCancel(reason)
    }
    
    IInteractable <|.. HoldInteractableBase
    HoldInteractableBase <|-- GatheringInteractable
    HoldInteractableBase <|-- WaterSourceInteractable
    
    note for HoldInteractableBase "Template Method Pattern
    Eliminates code duplication
    Handles progress bar updates
    Manages cancellation logic"
```

---

## Event Flow Diagrams

### Equipment Change Event Flow

```mermaid
sequenceDiagram
    participant User
    participant Context as ContextMenuUI
    participant EquipMgr as EquipmentManager
    participant EventBus
    participant UI as EquipmentUI
    participant Stats as PlayerStats
    
    User->>Context: Right-click → "Equip"
    Context->>EquipMgr: Equip(item, slotType)
    activate EquipMgr
    
    EquipMgr->>EquipMgr: GetSlot(slotType)
    EquipMgr->>EquipMgr: previousItem = slot.Equip(item)
    
    alt Has Previous Item
        EquipMgr->>Stats: RemoveStatModifiers(previousItem)
    end
    
    EquipMgr->>Stats: ApplyStatModifiers(item)
    EquipMgr->>EventBus: Publish(ItemEquippedEvent)
    
    EventBus-->>UI: ItemEquippedEvent
    UI->>UI: UpdateSlotDisplay()
    
    deactivate EquipMgr
```

### Auto-Save Trigger Flow

```mermaid
flowchart TD
    Start([Game Running])
    Timer{Auto-Save<br/>Timer ≥ Interval?}
    
    subgraph UpdatePlayerData["📷 Capture Player State"]
        GetPlayer[Get PlayerController via DI]
        GetStats[Get PlayerStats via DI]
        CapturePos[Capture position & rotation]
        CaptureStats[Capture health, hunger, stamina]
        UpdateTime[Increment totalPlayTime]
    end
    
    SaveToFile[SaveWorld - Write JSON]
    CreateBackup[Create Backup - Optional]
    UpdateMetadata[Update Metadata File]
    ResetTimer[Reset Timer to 0]
    
    Start --> Timer
    Timer -->|No| Start
    Timer -->|Yes| UpdatePlayerData
    
    GetPlayer --> GetStats
    GetStats --> CapturePos
    CapturePos --> CaptureStats
    CaptureStats --> UpdateTime
    
    UpdatePlayerData --> SaveToFile
    SaveToFile --> CreateBackup
    CreateBackup --> UpdateMetadata
    UpdateMetadata --> ResetTimer
    ResetTimer --> Start
    
    style UpdatePlayerData fill:#4a90e2,color:#fff
    style SaveToFile fill:#7ed321
```

---

## Service Container Registry

### Dependency Injection Map

```mermaid
graph TB
    subgraph Bootstrap["🚀 GameServiceBootstrapper<br/>ExecutionOrder: -100"]
        Awake[Awake - RegisterServices]
    end
    
    subgraph Container["📦 ServiceContainer - Singleton"]
        Registry[Type → Instance Registry]
    end
    
    subgraph CoreServices["🔧 Core Services"]
        EventBus[IEventBus → EventBus]
        SaveService[ISaveLoadService → SaveLoadService]
    end
    
    subgraph PlayerServices["👤 Player Services"]
        Player[PlayerControllerRefactored]
        Stats[PlayerStats]
    end
    
    subgraph InventoryServices["🎒 Inventory Services"]
        InvService[IInventoryService]
        InvStorage[IInventoryStorage]
        EffectSys[IConsumableEffectSystem]
        InvMgr[InventoryManagerRefactored]
        EquipMgr[EquipmentManager]
        CraftMgr[CraftingManager]
    end
    
    subgraph UIServices["🖥️ UI Services"]
        UIProvider[UIServiceProvider]
        TabbedUI[TabbedInventoryUI]
        EquipUI[EquipmentUI]
        Tooltip[TooltipUI]
        ContextMenu[ContextMenuUI]
    end
    
    subgraph OtherServices["⚙️ Other Services"]
        Camera[CinemachinePlayerCamera]
        DayNight[DayNightCycleManager]
        Assessment[LearningAssessmentService]
    end
    
    Bootstrap --> Container
    Container --> CoreServices
    Container --> PlayerServices
    Container --> InventoryServices
    Container --> UIServices
    Container --> OtherServices
    
    style Bootstrap fill:#ff5722,color:#fff
    style Container fill:#4a90e2,color:#fff
```

### Service Resolution Flow

```mermaid
sequenceDiagram
    participant Client as Any Component
    participant Container as ServiceContainer
    participant Registry as services Dictionary
    
    Client->>Container: TryGet<PlayerStats>()
    activate Container
    
    Container->>Registry: TryGetValue(typeof(PlayerStats))
    
    alt Service Found
        Registry-->>Container: PlayerStats instance
        Container-->>Client: PlayerStats instance
        Note over Client: Use service normally
    else Service Not Found
        Registry-->>Container: null
        Container->>Container: Log Warning
        Container-->>Client: null
        Note over Client: Handle gracefully<br/>Don't crash
    end
    
    deactivate Container
```

---

## Complete System Integration

### Game Initialization Sequence

```mermaid
sequenceDiagram
    participant Unity
    participant Bootstrap as GameServiceBootstrapper
    participant Container as ServiceContainer
    participant EventBus
    participant SaveService as SaveLoadService
    participant Player as PlayerController
    
    Unity->>Bootstrap: Awake() [Order: -100]
    activate Bootstrap
    
    Bootstrap->>Container: Instance
    Container-->>Bootstrap: ServiceContainer
    
    Bootstrap->>EventBus: new EventBus()
    Bootstrap->>Container: Register<IEventBus>(eventBus)
    
    Bootstrap->>Bootstrap: FindFirstObjectByType<SaveLoadService>()
    Bootstrap->>Container: Register<ISaveLoadService>(saveService)
    Bootstrap->>Container: Register<SaveLoadService>(saveService)
    
    Bootstrap->>Bootstrap: FindAndRegisterAllServices()
    Note over Bootstrap: Finds and registers:<br/>- PlayerController<br/>- PlayerStats<br/>- InventoryManager<br/>- EquipmentManager<br/>- UI Services<br/>- etc.
    
    deactivate Bootstrap
    
    Unity->>SaveService: Awake()
    SaveService->>SaveService: EnsureDirectoriesExist()
    
    Unity->>Player: Start()
    Player->>Container: Resolve<IEventBus>()
    Player->>Container: Resolve<PlayerStats>()
    Player->>Container: Resolve<InventoryManager>()
    Player->>Player: EnterState(WalkingState)
    
    Note over Unity,Player: All Systems Ready ✅
```

### Gameplay Loop with Auto-Save

```mermaid
flowchart LR
    subgraph GameLoop["🎮 Gameplay Loop"]
        Update[Unity Update]
        PlayerInput[Process Input]
        StateUpdate[Update Player State]
        PhysicsCalc[Physics Calculations]
        UIUpdate[Update UI]
    end
    
    subgraph AutoSave["💾 Auto-Save System"]
        Timer{Timer ≥ 5min?}
        Capture[Capture Player Data]
        Save[Save to File]
        Backup[Create Backup]
    end
    
    subgraph Events["📡 Event System"]
        Publish[Publish Events]
        Subscribe[Handle Events]
    end
    
    Update --> PlayerInput
    PlayerInput --> StateUpdate
    StateUpdate --> PhysicsCalc
    PhysicsCalc --> UIUpdate
    UIUpdate --> Timer
    
    Timer -->|Yes| Capture
    Timer -->|No| Update
    Capture --> Save
    Save --> Backup
    Backup --> Update
    
    StateUpdate -.-> Publish
    Publish -.-> Subscribe
    Subscribe -.-> UIUpdate
    
    style AutoSave fill:#4caf50
    style Events fill:#ff9800
```

---

## Summary

This document provides visual representations of:
- ✅ System layer architecture
- ✅ Save/Load system flow and decision logic
- ✅ Player state machine and services
- ✅ Inventory command pattern
- ✅ Interaction detection and template method pattern
- ✅ Event-driven communication
- ✅ Service container dependency injection
- ✅ Complete game initialization sequence

**All diagrams use Mermaid syntax** and can be rendered in:
- GitHub
- GitLab
- VS Code (with Mermaid extension)
- Most modern markdown viewers

---

**Last Updated:** February 16, 2026
