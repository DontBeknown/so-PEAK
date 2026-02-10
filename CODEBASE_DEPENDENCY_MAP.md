# Codebase Dependency Map - "This is so PEAK"
**Last Updated:** February 4, 2026  
**Purpose:** Complete dependency visualization for architecture analysis

---

## Table of Contents
1. [Layer Architecture](#layer-architecture)
2. [System Overview](#system-overview)
3. [Core Infrastructure](#core-infrastructure)
4. [Player System Dependencies](#player-system-dependencies)
5. [Inventory & Equipment System](#inventory--equipment-system)
6. [UI System Dependencies](#ui-system-dependencies)
7. [Interaction System Dependencies](#interaction-system-dependencies)
8. [New Systems: Torch & Canteen](#new-systems-torch--canteen)
9. [Service Container Registry](#service-container-registry)
10. [Event Flow](#event-flow)
11. [Dependency Matrix](#dependency-matrix)

---

## Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  InventoryUI │  │  EquipmentUI │  │  CraftingUI  │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│  ┌──────▼───────┐  ┌──────▼───────┐  ┌──────▼───────┐      │
│  │   TooltipUI  │  │ ContextMenuUI│  │InteractionUI │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                   APPLICATION LAYER                          │
│  ┌────────────────────────────────────────────────┐         │
│  │         PlayerControllerRefactored             │         │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐    │         │
│  │  │ Walking  │  │ Climbing │  │ Falling  │    │         │
│  │  │  State   │  │  State   │  │  State   │    │         │
│  │  └──────────┘  └──────────┘  └──────────┘    │         │
│  └─────────┬──────────────────────────────────┬──┘         │
│            │                                   │            │
│  ┌─────────▼────────┐                ┌────────▼────────┐   │
│  │ PlayerInventory  │                │ Interaction     │   │
│  │    Facade        │                │   Detector      │   │
│  └──────────────────┘                └─────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                     DOMAIN LAYER                             │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐   │
│  │   Inventory   │  │   Equipment   │  │   Crafting    │   │
│  │    Manager    │  │    Manager    │  │    Manager    │   │
│  └───────┬───────┘  └───────┬───────┘  └───────┬───────┘   │
│          │                   │                   │           │
│  ┌───────▼───────────────────▼───────────────────▼───────┐  │
│  │            InventoryItem (ScriptableObject)           │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐            │  │
│  │  │Equipment │  │  Torch   │  │ Canteen  │            │  │
│  │  │   Item   │  │   Item   │  │   Item   │            │  │
│  │  └──────────┘  └──────────┘  └──────────┘            │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌───────────────┐  ┌───────────────┐                       │
│  │  PlayerStats  │  │  IInteractable│                       │
│  └───────────────┘  └───────────────┘                       │
└────────────────────────┬───────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                INFRASTRUCTURE LAYER                          │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐   │
│  │   Service     │  │   EventBus    │  │   Unity       │   │
│  │  Container    │  │               │  │  Integration  │   │
│  └───────────────┘  └───────────────┘  └───────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                  GAME INITIALIZATION SEQUENCE                    │
└─────────────────────────────────────────────────────────────────┘

1. GameServiceBootstrapper.Awake() [ExecutionOrder: -100]
   │
   ├─► ServiceContainer.Instance.Register<IEventBus>(EventBus)
   │
   ├─► ServiceContainer.Instance.Register<InventoryManager>()
   │   │
   │   ├─► IInventoryStorage
   │   ├─► IInventoryService
   │   ├─► IConsumableEffectSystem
   │   └─► IEventBus
   │
   ├─► ServiceContainer.Instance.Register<EquipmentManager>()
   │   │
   │   ├─► EquipmentSlot[] (Head, Body, Foot, Hand, HeldItem)
   │   └─► IEventBus
   │
   ├─► ServiceContainer.Instance.Register<CraftingManager>()
   │
   ├─► ServiceContainer.Instance.Register<PlayerStats>()
   │   │
   │   └─► IEventBus
   │
   ├─► ServiceContainer.Instance.Register<UIServiceProvider>()
   │   │
   │   ├─► UIPanelController
   │   ├─► CursorManager
   │   └─► All UI Panels
   │
   ├─► ServiceContainer.Instance.Register<PlayerControllerRefactored>()
   │   │
   │   ├─► PlayerModel
   │   ├─► PlayerPhysicsService
   │   ├─► PlayerAnimationService
   │   ├─► PlayerInputHandler
   │   ├─► PlayerInventoryFacade
   │   ├─► InteractionDetector
   │   └─► HeldItemBehaviorManager (NEW)
   │
   └─► ServiceContainer.Instance.Register<CinemachinePlayerCamera>()

2. PlayerController.Start()
   │
   └─► Enter WalkingState

3. All Systems Ready ✅
```

---

## Core Infrastructure

### Service Container Dependency Graph

```
┌────────────────────────────────────────────────────────────┐
│                    ServiceContainer                         │
│              (Singleton DI Container)                       │
└─────┬──────────────────────────────────────────────────────┘
      │
      ├─► IEventBus ──────────────────┐
      │                                │
      ├─► IInventoryService ───────┐  │
      │                             │  │
      ├─► IInventoryStorage ───────┤  │
      │                             │  │
      ├─► IConsumableEffectSystem ─┤  │
      │                             │  │
      ├─► InventoryManager ◄───────┴──┤
      │                                │
      ├─► EquipmentManager ◄───────────┤
      │                                │
      ├─► CraftingManager ◄────────────┤
      │                                │
      ├─► PlayerStats ◄─────────────────┤
      │                                │
      ├─► UIServiceProvider ◄───────────┤
      │   │                            │
      │   ├─► UIPanelController        │
      │   ├─► CursorManager            │
      │   └─► All UI Panels            │
      │                                │
      ├─► PlayerControllerRefactored ◄─┤
      │   │                            │
      │   ├─► PlayerPhysicsService     │
      │   ├─► PlayerAnimationService   │
      │   ├─► PlayerInputHandler       │
      │   └─► PlayerInventoryFacade    │
      │                                │
      ├─► InteractionPromptUI ◄────────┤
      │                                │
      ├─► HeldItemStateManager         │
      │                                │
      └─► CinemachinePlayerCamera ◄────┘
```

### EventBus Subscription Graph

```
┌────────────────────────────────────────┐
│            EventBus (IEventBus)        │
└────┬───────────────────────────────────┘
     │
     ├─► ItemEquippedEvent
     │   │
     │   ├─► Subscriber: EquipmentUI
     │   ├─► Subscriber: PlayerStats
     │   └─► Subscriber: InventoryUI
     │
     ├─► ItemUnequippedEvent
     │   │
     │   ├─► Subscriber: EquipmentUI
     │   ├─► Subscriber: PlayerStats
     │   └─► Subscriber: InventoryUI
     │
     ├─► ItemAddedEvent
     │   │
     │   ├─► Subscriber: InventoryUI
     │   ├─► Subscriber: NotificationUI
     │   └─► Subscriber: ItemNotificationUI
     │
     ├─► ItemRemovedEvent
     │   │
     │   ├─► Subscriber: InventoryUI
     │   └─► Subscriber: NotificationUI
     │
     ├─► ItemConsumedEvent
     │   │
     │   ├─► Subscriber: InventoryUI
     │   ├─► Subscriber: PlayerStats
     │   └─► Subscriber: NotificationUI
     │
     ├─► StaminaChangedEvent
     │   │
     │   ├─► Subscriber: SimpleStatsHUD
     │   └─► Subscriber: PlayerController
     │
     └─► ClimbingStaminaDepletedEvent
         │
         └─► Subscriber: PlayerController (ClimbingState)
```

---

## Player System Dependencies

```
┌─────────────────────────────────────────────────────────────┐
│           PlayerControllerRefactored (MonoBehaviour)        │
│                                                              │
│  Dependencies Injected via ServiceContainer:                │
│  ┌────────────────────────────────────────────────┐        │
│  │  1. IEventBus                                  │        │
│  │  2. UIServiceProvider                          │        │
│  │  3. InventoryManager                           │        │
│  │  4. EquipmentManager                           │        │
│  │  5. CraftingManager                            │        │
│  │  6. PlayerStats                                │        │
│  └────────────────────────────────────────────────┘        │
│                                                              │
│  Owns:                                                       │
│  ┌────────────────────────────────────────────────┐        │
│  │  PlayerModelRefactored                         │        │
│  │    ├─► PlayerConfig (ScriptableObject)        │        │
│  │    ├─► PlayerPhysicsService                   │        │
│  │    ├─► PlayerAnimationService                 │        │
│  │    ├─► PlayerInputHandler                     │        │
│  │    ├─► PlayerInventoryFacade                  │        │
│  │    ├─► InteractionDetector                    │        │
│  │    ├─► HeldItemBehaviorManager (NEW)         │        │
│  │    └─► Camera Reference                       │        │
│  └────────────────────────────────────────────────┘        │
│                                                              │
│  State Machine:                                              │
│  ┌────────────────────────────────────────────────┐        │
│  │  IPlayerState                                  │        │
│  │    ├─► WalkingState                           │        │
│  │    ├─► ClimbingState                          │        │
│  │    └─► FallingState                           │        │
│  └────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
           │                    │                    │
           │                    │                    │
           ▼                    ▼                    ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ Rigidbody        │  │ Input System     │  │ Animator         │
│ (Unity Physics)  │  │ (Unity)          │  │ (Unity)          │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

### Player Services Detail

```
PlayerPhysicsService (IPhysicsService)
├─► Depends On: Rigidbody component
├─► Methods:
│   ├─► Move(Vector3 velocity)
│   ├─► Jump(float force)
│   ├─► ApplyGravity()
│   └─► IsGrounded()
└─► Used By: All PlayerStates

PlayerAnimationService (IAnimationService)
├─► Depends On: Animator component
├─► Methods:
│   ├─► SetFloat(string param, float value)
│   ├─► SetBool(string param, bool value)
│   └─► SetTrigger(string param)
└─► Used By: All PlayerStates

PlayerInputHandler
├─► Depends On: Unity Input System
├─► Provides:
│   ├─► Movement input (Vector2)
│   ├─► Jump input (bool)
│   ├─► Interact input (bool)
│   └─► Inventory toggle (bool)
└─► Used By: PlayerController, PlayerStates

PlayerInventoryFacade
├─► Depends On:
│   ├─► InventoryManager
│   ├─► CraftingManager
│   └─► EquipmentManager
├─► Provides:
│   ├─► AddItem()
│   ├─► RemoveItem()
│   ├─► ConsumeItem()
│   ├─► Equip()
│   ├─► Unequip()
│   └─► Craft()
└─► Used By: PlayerController
```

---

## Inventory & Equipment System

### Inventory Manager Architecture

```
┌──────────────────────────────────────────────────────────────┐
│         InventoryManagerRefactored (MonoBehaviour)           │
│                 [Facade Pattern]                              │
└───┬──────────────────────────────────────────────────────────┘
    │
    ├─► IInventoryStorage (Data Layer)
    │   │
    │   ├─► InventorySlot[] slots
    │   ├─► int maxSlots
    │   └─► Methods:
    │       ├─► GetSlot(index)
    │       ├─► FindSlotWithItem(item)
    │       └─► GetAllSlots()
    │
    ├─► IInventoryService (Business Logic)
    │   │
    │   ├─► AddItem(item, quantity)
    │   ├─► RemoveItem(item, quantity)
    │   ├─► HasItem(item)
    │   ├─► GetItemCount(item)
    │   └─► ConsumeItem(item)
    │
    ├─► IConsumableEffectSystem (Strategy Pattern)
    │   │
    │   ├─► ConsumableEffectBase (Abstract)
    │   │   │
    │   │   ├─► HealthEffect
    │   │   ├─► HungerEffect
    │   │   ├─► ThirstEffect ◄──── Used by Canteen
    │   │   ├─► StaminaEffect
    │   │   └─► TemperatureEffect
    │   │
    │   └─► ApplyEffect(effect, target)
    │
    └─► IEventBus
        │
        ├─► Publishes: ItemAddedEvent
        ├─► Publishes: ItemRemovedEvent
        └─► Publishes: ItemConsumedEvent
```

### Equipment Manager Architecture

```
┌──────────────────────────────────────────────────────────────┐
│            EquipmentManager (MonoBehaviour)                  │
└───┬──────────────────────────────────────────────────────────┘
    │
    ├─► Dictionary<EquipmentSlotType, EquipmentSlot>
    │   │
    │   ├─► Head: EquipmentSlot
    │   ├─► Body: EquipmentSlot
    │   ├─► Foot: EquipmentSlot
    │   ├─► Hand: EquipmentSlot
    │   └─► HeldItem: EquipmentSlot ◄──── NEW (Torch/Canteen)
    │
    ├─► Methods:
    │   ├─► Equip(IEquippable item)
    │   ├─► Unequip(EquipmentSlotType slot)
    │   ├─► GetEquippedItem(slot)
    │   └─► IsSlotEmpty(slot)
    │
    ├─► Events:
    │   └─► OnEquipmentChanged(slot, item)
    │
    └─► IEventBus Integration
        │
        ├─► Publishes: ItemEquippedEvent
        └─► Publishes: ItemUnequippedEvent
```

### Equipment Slot Detail

```
EquipmentSlot (Class)
├─► EquipmentSlotType slotType
├─► IEquippable equippedItem
├─► Events:
│   ├─► OnItemEquipped
│   └─► OnItemUnequipped
└─► Methods:
    ├─► Equip(item) → returns previous item
    ├─► Unequip() → returns unequipped item
    └─► IsEmpty → bool
```

### Item Hierarchy

```
InventoryItem (ScriptableObject)
├─► Properties:
│   ├─► string itemName
│   ├─► Sprite icon
│   ├─► int maxStackSize
│   ├─► bool isConsumable
│   └─► ConsumableEffectBase[] effects
│
└─► Inheritance:
    │
    ├─► EquipmentItem (implements IEquippable)
    │   │
    │   ├─► EquipmentSlotType equipmentSlot
    │   ├─► StatModifier[] statModifiers
    │   └─► Methods:
    │       ├─► OnEquip()
    │       └─► OnUnequip()
    │   │
    │   └─► HeldEquipmentItem (NEW)
    │       │
    │       ├─► GameObject heldItemPrefab
    │       ├─► HeldItemState GetState()
    │       ├─► Abstract: CreateBehavior()
    │       ├─► Abstract: GetStateDescription()
    │       └─► Abstract: InitializeDefaultState()
    │       │
    │       ├─► TorchItem (NEW)
    │       │   │
    │       │   ├─► float maxDurabilitySeconds
    │       │   ├─► float durabilityDrainRate
    │       │   ├─► float warmthBonus
    │       │   ├─► float lightRadius
    │       │   ├─► float lightIntensity
    │       │   ├─► Color lightColor
    │       │   └─► AudioClip[] sounds
    │       │
    │       └─► CanteenItem (NEW)
    │           │
    │           ├─► int maxCharges
    │           ├─► float thirstRestorationPerSip
    │           ├─► float useCooldownSeconds
    │           ├─► float refillDurationSeconds
    │           ├─► Methods:
    │           │   ├─► CanDrink()
    │           │   ├─► Drink(playerStats)
    │           │   ├─► Refill()
    │           │   └─► IsFull()
    │           └─► AudioClip[] sounds
    │
    └─► CraftingRecipe (ScriptableObject)
        │
        ├─► InventoryItem resultItem
        ├─► int resultQuantity
        └─► RecipeIngredient[] ingredients
```

---

## UI System Dependencies

```
┌──────────────────────────────────────────────────────────────┐
│             UIServiceProvider (MonoBehaviour)                 │
│                    [Service Locator]                          │
└───┬──────────────────────────────────────────────────────────┘
    │
    ├─► UIPanelController
    │   │
    │   ├─► Dictionary<Type, IUIPanel> panels
    │   └─► Methods:
    │       ├─► OpenPanel<T>()
    │       ├─► ClosePanel<T>()
    │       └─► GetPanel<T>()
    │
    ├─► CursorManager (ICursorManager)
    │   │
    │   ├─► ShowCursor()
    │   ├─► HideCursor()
    │   └─► SetCursorState(state)
    │
    └─► UI Panels (via Adapters)
        │
        ├─► InventoryUI ──► InventoryUIAdapter
        │   │
        │   ├─► Depends On:
        │   │   ├─► InventoryManager
        │   │   ├─► EquipmentManager
        │   │   └─► TooltipUI
        │   │
        │   └─► Contains:
        │       └─► InventorySlotUI[] slots
        │
        ├─► EquipmentUI ──► EquipmentUIAdapter
        │   │
        │   ├─► Depends On:
        │   │   ├─► EquipmentManager
        │   │   └─► TooltipUI
        │   │
        │   └─► Contains:
        │       └─► EquipmentSlotUI[] slots (Head, Body, Foot, Hand, HeldItem)
        │
        ├─► CraftingUI ──► CraftingUIAdapter
        │   │
        │   ├─► Depends On:
        │   │   ├─► CraftingManager
        │   │   ├─► InventoryManager
        │   │   └─► TooltipUI
        │   │
        │   └─► Contains:
        │       └─► CraftingSlotUI[] recipeSlots
        │
        ├─► TabbedInventoryUI
        │   │
        │   ├─► Contains:
        │   │   ├─► InventoryUI (tab)
        │   │   ├─► EquipmentUI (tab)
        │   │   └─► CraftingUI (tab)
        │   │
        │   └─► Manages:
        │       └─► Tab switching logic
        │
        ├─► ContextMenuUI ◄──── Updated for Canteen "Drink" action
        │   │
        │   ├─► Depends On:
        │   │   ├─► InventoryManager
        │   │   ├─► EquipmentManager
        │   │   └─► PlayerStats
        │   │
        │   └─► Shows context actions:
        │       ├─► Equip / Unequip
        │       ├─► Consume
        │       ├─► Drink [X/5] ◄──── NEW for Canteen
        │       └─► Drop
        │
        ├─► TooltipUI
        │   │
        │   └─► Shows:
        │       ├─► Item name
        │       ├─► Item description
        │       ├─► Item stats
        │       └─► State (charges, durability) ◄──── NEW
        │
        ├─► NotificationUI
        │   │
        │   └─► Subscribes To:
        │       ├─► ItemAddedEvent
        │       ├─► ItemRemovedEvent
        │       └─► ItemConsumedEvent
        │
        ├─► ItemNotificationUI
        │   │
        │   └─► Shows: Floating item notifications
        │
        ├─► SimpleStatsHUD
        │   │
        │   ├─► Depends On:
        │   │   └─► PlayerStats
        │   │
        │   └─► Displays:
        │       ├─► Health bar
        │       ├─► Hunger bar
        │       ├─► Thirst bar ◄──── Affected by Canteen
        │       ├─► Stamina bar
        │       └─► Temperature ◄──── Affected by Torch
        │
        └─► InteractionPromptUI
            │
            ├─► Depends On:
            │   └─► InteractionDetector
            │
            ├─► Shows:
            │   ├─► Interaction prompt text
            │   └─► Progress bar ◄──── For hold-to-interact
            │
            └─► Methods:
                ├─► ShowPrompt(text)
                ├─► HidePrompt()
                ├─► ShowProgressBar()
                ├─► UpdateProgress(percent)
                └─► HideProgressBar()
```

---

## Interaction System Dependencies

```
┌──────────────────────────────────────────────────────────────┐
│         InteractionDetector (MonoBehaviour)                  │
│            [Observer Pattern]                                 │
└───┬──────────────────────────────────────────────────────────┘
    │
    ├─► Configuration:
    │   ├─► float detectionRadius = 3f
    │   ├─► LayerMask interactableLayer
    │   └─► float detectionInterval = 0.1f
    │
    ├─► Events:
    │   ├─► OnInteractableInRange(IInteractable)
    │   └─► OnNoInteractableInRange()
    │
    └─► Detection Logic:
        │
        ├─► Physics.OverlapSphere()
        ├─► GetHighestPriorityInteractable()
        └─► Highlight current target
```

### IInteractable Implementation Graph

```
IInteractable (Interface)
├─► Properties:
│   ├─► bool CanInteract
│   ├─► string InteractionPrompt
│   ├─► string InteractionVerb
│   ├─► float InteractionPriority
│   └─► Transform GetTransform()
│
├─► Methods:
│   ├─► OnHighlighted(bool highlighted)
│   └─► Interact(PlayerController player)
│
└─► Implementations:
    │
    ├─► ItemInteractable (Instant pickup)
    │   │
    │   ├─► Priority: 1.0
    │   ├─► Verb: "Press to"
    │   ├─► Prompt: "Pick up {itemName}"
    │   └─► Action: Add to inventory → Destroy
    │
    ├─► GatheringInteractable (Hold-to-interact)
    │   │
    │   ├─► Priority: 1.2
    │   ├─► Verb: "Hold to"
    │   ├─► Prompt: "Gather {resourceName}"
    │   ├─► Duration: 3 seconds (configurable)
    │   ├─► Locks player movement
    │   ├─► Shows progress bar
    │   ├─► Can be cancelled (release E)
    │   └─► Action: Give resources → Optional respawn
    │
    └─► WaterSourceInteractable (Hold-to-interact) ◄──── NEW
        │
        ├─► Priority: 1.2
        ├─► Verb: "Hold to"
        ├─► Prompt Logic:
        │   ├─► "Refill Canteen" ← canteen equipped & not full
        │   ├─► "Equip Canteen to Refill" ← canteen in inventory
        │   ├─► "No Canteen" ← no canteen exists
        │   └─► "Canteen Full" ← canteen already full
        │
        ├─► Duration: 3 seconds (configurable)
        ├─► Checks: Canteen equipped in HeldItem slot
        ├─► Locks player movement
        ├─► Shows progress bar
        ├─► Can be cancelled (release E)
        ├─► Action: Refill equipped canteen
        └─► Infinite uses (never depletes)
```

---

## New Systems: Torch & Canteen

### Held Item Infrastructure

```
┌──────────────────────────────────────────────────────────────┐
│         HeldItemBehaviorManager (MonoBehaviour)              │
│            [Lifecycle Manager]                                │
└───┬──────────────────────────────────────────────────────────┘
    │
    ├─► Attached To: Player GameObject
    │
    ├─► Depends On:
    │   ├─► EquipmentManager (via ServiceContainer)
    │   └─► Subscribes: OnEquipmentChanged event
    │
    ├─► Manages:
    │   └─► Dictionary<HeldEquipmentItem, IHeldItemBehavior>
    │
    └─► Lifecycle:
        │
        ├─► On Item Equipped (HeldItem slot):
        │   ├─► item.CreateBehavior(playerObject)
        │   ├─► behavior.OnEquipped()
        │   └─► Store in active behaviors
        │
        └─► On Item Unequipped (HeldItem slot):
            ├─► behavior.OnUnequipped()
            ├─► Destroy behavior component
            └─► Clear from active behaviors
```

### Held Item State Management

```
┌──────────────────────────────────────────────────────────────┐
│         HeldItemStateManager (Singleton MonoBehaviour)       │
│            [State Persistence]                                │
└───┬──────────────────────────────────────────────────────────┘
    │
    ├─► Dictionary<string, HeldItemState>
    │   │
    │   └─► Key: item.GetStateID() (usually itemName)
    │
    ├─► HeldItemState:
    │   │
    │   ├─► For Charge-Based Items (Canteen):
    │   │   ├─► int currentCharges
    │   │   ├─► int maxCharges
    │   │   └─► float lastUsedTime
    │   │
    │   └─► For Durability-Based Items (Torch):
    │       ├─► float currentDurability (seconds)
    │       └─► float maxDurability (seconds)
    │
    └─► Methods:
        ├─► GetOrCreateState(itemID)
        ├─► RemoveState(itemID)
        └─► HasState(itemID)
```

### Torch System Dependencies

```
TorchItem (ScriptableObject)
└─► Extends: HeldEquipmentItem
    │
    ├─► Configuration:
    │   ├─► float maxDurabilitySeconds = 300
    │   ├─► float durabilityDrainRate = 1.0
    │   ├─► float warmthBonus = 10
    │   ├─► float lightRadius = 10
    │   ├─► float lightIntensity = 2
    │   ├─► Color lightColor = Orange
    │   └─► float lowDurabilityThreshold = 0.2
    │
    └─► CreateBehavior() → TorchBehavior

TorchBehavior (MonoBehaviour)
└─► Implements: IHeldItemBehavior
    │
    ├─► Components Created:
    │   ├─► Light (Point Light)
    │   │   ├─► range = torch.lightRadius
    │   │   ├─► intensity = torch.lightIntensity
    │   │   ├─► color = torch.lightColor
    │   │   └─► shadows = Soft
    │   │
    │   ├─► AudioSource (looping)
    │   │   └─► clip = torch.cracklingSoundLoop
    │   │
    │   └─► Visual Prefab Instance
    │       └─► position = Hand (forward + up)
    │
    ├─► OnEquipped():
    │   ├─► Create light component
    │   ├─► Apply warmth bonus (PlayerStats.ModifyTemperature)
    │   ├─► Spawn visual prefab
    │   ├─► Play ignite sound
    │   └─► Start looping crackling sound
    │
    ├─► UpdateBehavior() [Every Frame]:
    │   ├─► Deplete durability (Time.deltaTime * drainRate)
    │   ├─► Update light intensity:
    │   │   └─► If durability < 20%: Flicker effect
    │   └─► Check if durability = 0:
    │       └─► Destroy torch from inventory
    │
    └─► OnUnequipped():
        ├─► Destroy light component
        ├─► Remove warmth bonus (PlayerStats.ModifyTemperature)
        ├─► Destroy visual prefab
        └─► Stop audio
```

### Canteen System Dependencies

```
CanteenItem (ScriptableObject)
└─► Extends: HeldEquipmentItem
    │
    ├─► Configuration:
    │   ├─► int maxCharges = 5
    │   ├─► float thirstRestorationPerSip = 20
    │   ├─► float useCooldownSeconds = 2
    │   └─► float refillDurationSeconds = 3
    │
    ├─► Methods:
    │   ├─► CanDrink() → bool
    │   │   ├─► Check: currentCharges > 0
    │   │   └─► Check: Time.time - lastUsedTime >= cooldown
    │   │
    │   ├─► Drink(PlayerStats stats) → bool
    │   │   ├─► Consume 1 charge
    │   │   ├─► Update lastUsedTime
    │   │   ├─► stats.Drink(thirstRestoration)
    │   │   └─► Play drink sound
    │   │
    │   └─► Refill()
    │       ├─► currentCharges = maxCharges
    │       └─► Play refill sound
    │
    └─► CreateBehavior() → CanteenBehavior

CanteenBehavior (MonoBehaviour)
└─► Implements: IHeldItemBehavior
    │
    ├─► OnEquipped():
    │   └─► Spawn visual prefab (at hip/belt position)
    │
    ├─► UpdateBehavior():
    │   └─► (No per-frame updates needed)
    │
    └─► OnUnequipped():
        └─► Destroy visual prefab

WaterSourceInteractable (MonoBehaviour)
└─► Implements: IInteractable
    │
    ├─► Depends On:
    │   ├─► EquipmentManager (get equipped canteen)
    │   └─► InteractionPromptUI (progress bar)
    │
    ├─► CanInteract:
    │   ├─► Check: Canteen equipped in HeldItem slot
    │   └─► Check: Canteen not full
    │
    ├─► Interact():
    │   ├─► Lock player movement
    │   ├─► Show progress bar
    │   ├─► Start refilling coroutine (3 seconds)
    │   └─► Monitor button hold (can cancel)
    │
    └─► On Complete:
        ├─► canteen.Refill()
        ├─► Unlock player movement
        ├─► Hide progress bar
        └─► Play refill sound
```

### Canteen Context Menu Integration

```
ContextMenuUI.ShowInventoryMenu()
└─► Check: Is item a CanteenItem?
    │
    ├─► YES:
    │   │
    │   ├─► canteen.CanDrink()?
    │   │   │
    │   │   ├─► YES: Add button "Drink [X/5]"
    │   │   │   └─► OnClick: canteen.Drink(PlayerStats)
    │   │   │
    │   │   └─► NO:
    │   │       │
    │   │       ├─► canteen.IsEmpty()?
    │   │       │   └─► Add disabled button "Empty - Equip to Refill"
    │   │       │
    │   │       └─► On Cooldown:
    │   │           └─► Add disabled button "On Cooldown"
    │   │
    │   └─► Continue with normal Equip/Unequip/Drop buttons
    │
    └─► NO: Normal equipment/consumable handling
```

---

## Service Container Registry

### Complete Registration Map

```
ServiceContainer.Instance
│
├─► IEventBus
│   └─► Implementation: EventBus (singleton)
│       └─► Used By: ALL systems for event pub/sub
│
├─► IInventoryService
│   └─► Implementation: InventoryService
│       └─► Delegates To: InventoryManagerRefactored
│
├─► IInventoryStorage
│   └─► Implementation: InventoryStorage
│       └─► Manages: InventorySlot array
│
├─► IConsumableEffectSystem
│   └─► Implementation: ConsumableEffectSystem
│       └─► Applies: ConsumableEffectBase effects
│
├─► InventoryManager (component instance)
│   └─► Registered By: GameServiceBootstrapper
│
├─► EquipmentManager (component instance)
│   └─► Registered By: GameServiceBootstrapper
│
├─► CraftingManager (component instance)
│   └─► Registered By: GameServiceBootstrapper
│
├─► PlayerStats (component instance)
│   └─► Registered By: GameServiceBootstrapper
│
├─► UIServiceProvider (component instance)
│   ├─► Registered By: GameServiceBootstrapper
│   └─► Provides Access To: All UI panels
│
├─► PlayerControllerRefactored (component instance)
│   └─► Registered By: GameServiceBootstrapper
│
├─► InteractionPromptUI (component instance)
│   ├─► Registered By: GameServiceBootstrapper (optional)
│   └─► Used By: All hold-to-interact interactables
│
└─► CinemachinePlayerCamera (component instance)
    └─► Registered By: GameServiceBootstrapper
```

---

## Event Flow

### Equipment Change Event Flow

```
1. User Action: Right-click item → "Equip"
   │
   ▼
2. ContextMenuUI.Equip()
   │
   ▼
3. EquipmentManager.Equip(item)
   │
   ├─► EquipmentSlot.Equip(item)
   │   │
   │   ├─► Store previous item
   │   ├─► Set new equipped item
   │   └─► Fire EquipmentSlot.OnItemEquipped
   │
   ├─► Fire EquipmentManager.OnEquipmentChanged
   │   │
   │   ├─► Listener: HeldItemBehaviorManager (if HeldItem slot)
   │   │   └─► Create & activate behavior component
   │   │
   │   └─► Listener: PlayerStats (apply stat modifiers)
   │
   └─► EventBus.Publish(ItemEquippedEvent)
       │
       ├─► Subscriber: EquipmentUI → Update slot display
       ├─► Subscriber: InventoryUI → Update slot display
       └─► Subscriber: TooltipUI → Refresh if showing item
```

### Canteen Drink Event Flow

```
1. User Action: Right-click canteen → "Drink [X/5]"
   │
   ▼
2. ContextMenuUI → canteen.Drink(PlayerStats)
   │
   ├─► Check: CanDrink()?
   │   ├─► currentCharges > 0?
   │   └─► Off cooldown?
   │
   ├─► Consume 1 charge
   ├─► Update lastUsedTime
   │
   ├─► PlayerStats.Drink(thirstRestoration)
   │   │
   │   ├─► Modify thirst stat
   │   └─► EventBus.Publish(ThirstChangedEvent) ← If exists
   │
   ├─► Play drink sound
   │
   └─► Return to ContextMenuUI
       │
       └─► InventoryUI.UpdateAllSlots()
           └─► Refresh display (shows updated charges)
```

### Torch Durability Depletion Flow

```
Every Frame While Torch Equipped:
│
▼
TorchBehavior.UpdateBehavior()
│
├─► Get state: HeldItemStateManager.GetState(torchID)
│
├─► Deplete durability:
│   └─► state.currentDurability -= Time.deltaTime * drainRate
│
├─► Update light intensity:
│   │
│   ├─► Calculate durability percentage
│   │
│   └─► If < 20%:
│       └─► Apply flicker effect (Perlin noise)
│
└─► Check destruction:
    │
    └─► If currentDurability <= 0:
        │
        ├─► TorchBehavior.DestroyTorch()
        │   │
        │   ├─► OnUnequipped() (cleanup light, warmth, audio)
        │   │
        │   ├─► IInventoryService.RemoveItem(torch, 1)
        │   │   │
        │   │   └─► EventBus.Publish(ItemRemovedEvent)
        │   │       └─► InventoryUI updates
        │   │
        │   ├─► HeldItemStateManager.RemoveState(torchID)
        │   │
        │   └─► Destroy(TorchBehavior component)
        │
        └─► EquipmentManager automatically handles empty slot
```

### Water Source Refill Flow

```
1. Player approaches WaterSourceInteractable
   │
   ▼
2. InteractionDetector detects it
   │
   ├─► Fire: OnInteractableInRange(waterSource)
   │
   └─► InteractionPromptUI shows:
       │
       ├─► Canteen equipped & not full: "Hold to Refill Canteen"
       ├─► Canteen in inventory: "Equip Canteen to Refill"
       ├─► No canteen: "No Canteen"
       └─► Canteen full: "Canteen Full"
   │
   ▼
3. Player holds E button
   │
   ▼
4. WaterSourceInteractable.Interact()
   │
   ├─► Check: CanInteract?
   │   ├─► Canteen equipped in HeldItem slot?
   │   └─► Canteen not full?
   │
   ├─► Lock player movement: player.SetInputBlocked(true)
   │
   ├─► Show progress bar: promptUI.ShowProgressBar()
   │
   ├─► Start coroutine: RefillingProcess()
   │   │
   │   ├─► For 3 seconds:
   │   │   ├─► Update progress: promptUI.UpdateProgress(%)
   │   │   └─► Check: player still holding E?
   │   │       └─► If released: Cancel & cleanup
   │   │
   │   └─► On complete:
   │       │
   │       ├─► canteen.Refill()
   │       │   ├─► Set currentCharges = maxCharges
   │       │   └─► Play refill sound
   │       │
   │       └─► CleanupRefilling()
   │           ├─► Hide progress bar
   │           ├─► Unlock player movement
   │           └─► Clear references
   │
   └─► (Can be cancelled if player releases E)
```

---

## Dependency Matrix

### System-to-System Dependencies

| System | Depends On | Used By | Publishes Events | Subscribes To Events |
|--------|-----------|---------|------------------|---------------------|
| **ServiceContainer** | None | ALL | None | None |
| **EventBus** | ServiceContainer | ALL | N/A (is event system) | N/A |
| **InventoryManager** | ServiceContainer, EventBus | PlayerController, UI, Commands | ItemAdded, ItemRemoved, ItemConsumed | None |
| **EquipmentManager** | ServiceContainer, EventBus | PlayerController, UI, HeldItemBehaviorMgr | ItemEquipped, ItemUnequipped | None |
| **CraftingManager** | ServiceContainer, InventoryManager | PlayerController, UI | None | None |
| **PlayerStats** | ServiceContainer, EventBus | PlayerController, UI, Consumables, Torch, Canteen | StaminaChanged, HealthChanged | None |
| **PlayerController** | ServiceContainer, ALL Managers, PlayerModel | None (entry point) | None | StaminaChanged, ClimbingStaminaDepleted |
| **HeldItemBehaviorMgr** | ServiceContainer, EquipmentManager | None | None | OnEquipmentChanged |
| **UIServiceProvider** | ServiceContainer, ALL UI Panels | PlayerController | None | None |
| **InventoryUI** | InventoryManager, TooltipUI, ContextMenuUI | UIServiceProvider | None | ItemAdded, ItemRemoved |
| **EquipmentUI** | EquipmentManager, TooltipUI, ContextMenuUI | UIServiceProvider | None | ItemEquipped, ItemUnequipped |
| **ContextMenuUI** | EquipmentManager, PlayerStats, CanteenItem | InventoryUI, EquipmentUI | None | None |
| **InteractionDetector** | None | PlayerController | OnInteractableInRange, OnNoInteractableInRange | None |
| **InteractionPromptUI** | InteractionDetector | All IInteractables | None | OnInteractableInRange, OnNoInteractableInRange |
| **TorchItem** | HeldItemStateManager | EquipmentManager | None | None |
| **TorchBehavior** | ServiceContainer, PlayerStats, InventoryService, TorchItem | HeldItemBehaviorMgr | None | None |
| **CanteenItem** | HeldItemStateManager, PlayerStats | EquipmentManager, ContextMenuUI | None | None |
| **CanteenBehavior** | CanteenItem | HeldItemBehaviorMgr | None | None |
| **WaterSourceInteractable** | ServiceContainer, EquipmentManager, InteractionPromptUI, CanteenItem | InteractionDetector | None | None |
| **HeldItemStateManager** | None | TorchItem, CanteenItem | None | None |

### Circular Dependency Detection

✅ **No Circular Dependencies Detected**

All dependencies flow in one direction following the layer architecture:
- Infrastructure → Domain → Application → Presentation
- No system depends on systems above it in the hierarchy

---

## Data Flow Diagrams

### Item Pickup Flow

```
World: ItemPickup
    │
    │ [Player approaches]
    │
    ▼
InteractionDetector ──► "Press to Pick up Item"
    │
    │ [Player presses E]
    │
    ▼
ItemPickup.Interact()
    │
    ├─► IInventoryService.AddItem(item, quantity)
    │   │
    │   ├─► IInventoryStorage.FindEmptyOrStackableSlot()
    │   ├─► IInventoryStorage.AddToSlot()
    │   └─► EventBus.Publish(ItemAddedEvent)
    │       │
    │       ├─► InventoryUI updates display
    │       └─► NotificationUI shows pickup message
    │
    └─► Destroy(ItemPickup GameObject)
```

### Crafting Flow

```
Player: Opens CraftingUI
    │
    ▼
CraftingUI displays recipes from CraftingManager
    │
    │ [Player clicks recipe]
    │
    ▼
CraftingManager.CanCraft(recipe)
    │
    ├─► Check: Has required ingredients?
    │   └─► IInventoryService.HasItem() for each ingredient
    │
    └─► YES:
        │
        ├─► Remove ingredients:
        │   └─► IInventoryService.RemoveItem() for each
        │       └─► EventBus.Publish(ItemRemovedEvent) for each
        │
        └─► Add result:
            └─► IInventoryService.AddItem(resultItem, quantity)
                └─► EventBus.Publish(ItemAddedEvent)
```

### Combat/Damage Flow (if applicable)

```
Enemy attacks Player
    │
    ▼
PlayerStats.TakeDamage(amount)
    │
    ├─► currentHealth -= amount
    │
    ├─► EventBus.Publish(HealthChangedEvent)
    │   │
    │   └─► SimpleStatsHUD updates health bar
    │
    ├─► Check: currentHealth <= 0?
    │   └─► EventBus.Publish(PlayerDiedEvent)
    │
    └─► If gathering: GatheringInteractable.OnPlayerDamaged()
        └─► Cancel gathering
```

---

## Performance Considerations

### Per-Frame Updates

| System | Update Frequency | Cost | Notes |
|--------|-----------------|------|-------|
| PlayerController | Every frame | Medium | State machine updates |
| TorchBehavior | Every frame (when equipped) | Low | Durability drain + light flicker |
| InteractionDetector | Every 0.1s | Low | Uses timer to reduce checks |
| SimpleStatsHUD | On stat change | Very Low | Event-driven updates |
| InventoryUI | On demand | Very Low | Only updates when inventory changes |

### Memory Allocations

| System | Allocations | Notes |
|--------|------------|-------|
| ServiceContainer | One-time | Dictionary created at startup |
| EventBus | Per subscription | Minimal, reuses delegates |
| InventoryStorage | One-time | Fixed-size array |
| HeldItemStateManager | Per item type | Dictionary grows with unique items |
| Command History | Per command | Bounded by max history size |

### Optimization Opportunities

1. **InteractionDetector**: Already optimized with 0.1s interval
2. **TorchBehavior**: Could reduce light intensity update frequency
3. **EventBus**: Consider object pooling for frequent events
4. **UI Updates**: Already event-driven (optimal)
5. **State Management**: Uses dictionaries (O(1) lookup)

---

## Testing Dependencies

### Unit Test Isolation

**Easily Testable (Low Dependencies):**
- InventoryService (pure logic)
- ConsumableEffectSystem (Strategy pattern)
- Command classes (isolated)
- PlayerStates (depend on interfaces)
- CanteenItem logic methods

**Moderately Testable (Some Dependencies):**
- EquipmentManager (requires EventBus mock)
- CraftingManager (requires InventoryManager mock)
- PlayerInventoryFacade (requires manager mocks)

**Hard to Test (MonoBehaviour-heavy):**
- PlayerController (complex MonoBehaviour)
- UI Components (Unity UI dependencies)
- Behavior components (require GameObject)

### Integration Test Points

1. **Inventory → Equipment → UI** chain
2. **Interaction → Inventory → Notification** chain
3. **Equip Torch → Light + Warmth + Durability** chain
4. **Use Canteen → Thirst + Charges → UI** chain
5. **Refill Canteen → Water Source → Equip Check** chain

---

## Conclusion

This dependency map shows a **well-structured architecture** with:

✅ **Clear separation of concerns** across layers  
✅ **Dependency Inversion** via ServiceContainer and interfaces  
✅ **Event-driven communication** via EventBus (no static events)  
✅ **No circular dependencies** detected  
✅ **New systems (Torch/Canteen)** integrate cleanly  
✅ **SOLID principles** followed throughout  

### Key Architectural Strengths:

1. **Unidirectional flow**: Infrastructure → Domain → Application → Presentation
2. **Interface-based design**: Easy to mock and test
3. **Event decoupling**: Systems don't know about each other directly
4. **Service registration**: All dependencies resolved at startup
5. **Extensibility**: New items/systems can be added without modifications

### Areas for Future Enhancement:

1. **ViewModel layer** for UI (MVVM pattern)
2. **Unit test coverage** for business logic
3. **Performance profiling** for per-frame updates
4. **Save/load system** for HeldItemState persistence
5. **Documentation** of public APIs

---

**Total Systems Analyzed:** 25+  
**Total Dependencies Mapped:** 100+  
**Architecture Pattern Compliance:** SOLID ✅  
**Circular Dependencies:** 0 ✅  
**Integration Points:** 15+  

---

*This dependency map should be updated when new systems are added or major refactoring occurs.*
