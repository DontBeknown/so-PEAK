# Collectable & Dialog System — Implementation Plan

**Author:** Copilot  
**Date:** March 12, 2026  
**Status:** Ready to implement

---

## Table of Contents

1. [Overview](#overview)
2. [Key Design Decisions](#key-design-decisions)
3. [Architecture](#architecture)
4. [Implementation Steps](#implementation-steps)
5. [File Checklist](#file-checklist)
6. [Code Contracts (Signatures)](#code-contracts-signatures)

---

## Overview

This plan adds two new feature domains to the game:

- **Collectable System** — A separate domain from the Inventory System. Players interact with world objects to unlock lore documents, item pickups with descriptions, or narrative dialog triggers. Unlocked collectables persist across death/respawn.
- **Dialog System** — A sequential line-by-line dialog presenter, driven by `DialogData` ScriptableObjects. Can be triggered by collectables or by `WorldDialogTrigger` collider volumes in the scene.

Both systems follow the established codebase patterns: **ServiceContainer / interface registration, EventBus events, IInteractable, ScriptableObjects for data, and IUIPanel for UI**.

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| `ICollectableManager` and `IDialogManager` interfaces required | All services in this codebase are registered and consumed via interface (e.g. `IInventoryService`). Concrete-type registration creates tight coupling and prevents mocking. |
| Save data fields go on `WorldStateSaveData`, not `PlayerSaveData` | Collectables and triggered dialogs are **world-persistent**, not player-stats. A player death and respawn must not reset lore progress. |
| `WorldDialogTrigger` as the collider-based MonoBehaviour name | `CollectableType` already has a `DialogTrigger` enum value. Using the same name for both a type value and a class causes compiler ambiguity; the scene-placed MonoBehaviour is named `WorldDialogTrigger` to distinguish intent. |
| `dialogData` lives on `CollectableInteractable`, not on `CollectableItem` | A `CollectableItem` ScriptableObject for a lore note has no logical reason to reference dialog data. Keeping it scene-level (on the MonoBehaviour) makes the wiring explicit and keeps the data asset clean. |
| Managers expose `LoadState` / `GetState` methods; `SaveLoadService` drives hydration | Managers must not know about the save file format. `SaveLoadService` calls `LoadState(List<string>)` on load and reads `GetUnlockedIds()` / `GetTriggeredIds()` on save. |
| Input locking goes through `UIServiceProvider` | The existing pattern for blocking player input is `UIServiceProvider.BlockPlayerInput(true)`. `DialogManager` resolves this from `ServiceContainer` — it does not implement its own pause or time-scale mechanism. |

---

## Architecture

```
Assets/Game/Script/
├── Collectable/
│   ├── CollectableItem.cs          (ScriptableObject — data)
│   ├── CollectableType.cs          (enum, own file)
│   ├── ICollectableManager.cs      (interface)
│   └── CollectableManager.cs       (MonoBehaviour : ICollectableManager)
│
├── Dialog/
│   ├── DialogData.cs               (ScriptableObject — lines of dialog)
│   ├── DialogLine.cs               (serializable struct)
│   ├── IDialogManager.cs           (interface)
│   ├── DialogManager.cs            (MonoBehaviour : IDialogManager)
│   └── Triggers/
│       └── WorldDialogTrigger.cs   (MonoBehaviour, collider-based trigger)
│
├── Interaction/Interactables/
│   └── CollectableInteractable.cs  (MonoBehaviour : IInteractable)
│
├── UI/
│   ├── Collectable/
│   │   └── CollectablesUI.cs       (MonoBehaviour : IUIPanel)
│   └── Dialog/
│       └── DialogUI.cs             (MonoBehaviour : IUIPanel)
│
└── Core/
    ├── Events/GameEvents.cs        (add 3 new event structs)
    ├── SaveSystem/WorldSaveData.cs (add 2 fields to WorldStateSaveData)
    └── GameServiceBootstrapper.cs  (register 2 new services)
```

---

## Implementation Steps

### Phase 1 — Data Layer (no Unity run required to validate)

**Step 1.1 — Create `CollectableType` enum**
```
Assets/Game/Script/Collectable/CollectableType.cs
```
```csharp
namespace Game.Collectable
{
    public enum CollectableType
    {
        PureTextDocument,
        ItemWithDescription,
        DialogTrigger
    }
}
```

**Step 1.2 — Create `CollectableItem` ScriptableObject**
```
Assets/Game/Script/Collectable/CollectableItem.cs
```
Fields: `id`, `title`, `content`, `type`, `icon`, `associatedItem`.  
No `dialogData` — wired at the scene level on the interactable.

**Step 1.3 — Create `DialogLine` struct & `DialogData` ScriptableObject**
```
Assets/Game/Script/Dialog/DialogLine.cs
Assets/Game/Script/Dialog/DialogData.cs
```
`DialogLine` holds `speakerName` and `text`.  
`DialogData` holds `List<DialogLine> lines` and a `string dialogId` for save tracking.

---

### Phase 2 — Events

**Step 2.1 — Add new events to `GameEvents.cs`**
```
Assets/Game/Script/Core/Events/GameEvents.cs
```
Add inside `namespace Game.Core.Events`:

```csharp
public class CollectableUnlockedEvent
{
    public CollectableItem Collectable { get; }
    public CollectableUnlockedEvent(CollectableItem collectable) => Collectable = collectable;
}

public class DialogStartedEvent
{
    public DialogData Dialog { get; }
    public DialogStartedEvent(DialogData dialog) => Dialog = dialog;
}

public class DialogEndedEvent
{
    public string DialogId { get; }
    public DialogEndedEvent(string dialogId) => DialogId = dialogId;
}
```

---

### Phase 3 — Manager Interfaces and Implementations

**Step 3.1 — Create `ICollectableManager` interface**
```
Assets/Game/Script/Collectable/ICollectableManager.cs
```
```csharp
using System.Collections.Generic;

namespace Game.Collectable
{
    public interface ICollectableManager
    {
        bool IsUnlocked(string collectableId);
        void Unlock(CollectableItem collectable);
        IReadOnlyCollection<string> GetUnlockedIds();
        void LoadState(List<string> unlockedIds);
    }
}
```

**Step 3.2 — Create `CollectableManager` MonoBehaviour**
```
Assets/Game/Script/Collectable/CollectableManager.cs
```
- Internal `HashSet<string> _unlocked`
- `Unlock()`: checks for duplicate, adds to set, fires `CollectableUnlockedEvent`
- `LoadState()`: populates `_unlocked` from a list (called by `SaveLoadService`)
- `GetUnlockedIds()`: returns snapshot for saving

**Step 3.3 — Create `IDialogManager` interface**
```
Assets/Game/Script/Dialog/IDialogManager.cs
```
```csharp
namespace Game.Dialog
{
    public interface IDialogManager
    {
        bool IsActive { get; }
        bool HasTriggered(string dialogId);
        void StartDialog(DialogData data);
        void AdvanceLine();
        void EndDialog();
        void LoadState(System.Collections.Generic.List<string> triggeredIds);
        System.Collections.Generic.List<string> GetTriggeredIds();
    }
}
```

**Step 3.4 — Create `DialogManager` MonoBehaviour**
```
Assets/Game/Script/Dialog/DialogManager.cs
```
- Resolves `IEventBus` and `UIServiceProvider` from `ServiceContainer`
- On `StartDialog()`: checks if already triggered (guard), blocks player input via `UIServiceProvider`, publishes `DialogStartedEvent`
- Maintains current line index
- On `EndDialog()`: adds `dialogId` to triggered set, unblocks input, publishes `DialogEndedEvent`

---

### Phase 4 — Interactable

**Step 4.1 — Create `CollectableInteractable`**
```
Assets/Game/Script/Interaction/Interactables/CollectableInteractable.cs
```
- Implements `IInteractable` (same pattern as `ItemInteractable`)
- Fields: `CollectableItem collectableItem`, `DialogData dialogData`, `bool destroyOnInteract`
- `CanInteract`: resolves `ICollectableManager` from `ServiceContainer`, returns `!IsUnlocked(id)`
- `Interact()` dispatch table:

| `CollectableType` | Actions |
|---|---|
| `PureTextDocument` | unlock → destroy |
| `ItemWithDescription` | unlock → add `associatedItem` to inventory → destroy |
| `DialogTrigger` | unlock → `IDialogManager.StartDialog(dialogData)` → destroy |

---

### Phase 5 — UI

**Step 5.1 — Create `CollectablesUI`**
```
Assets/Game/Script/UI/Collectable/CollectablesUI.cs
```
- Implements `IUIPanel`
- On `Open()`: populate list from `ICollectableManager.GetUnlockedIds()`; load `CollectableItem` detail pane
- Subscribes to `CollectableUnlockedEvent` (via `IEventBus`) to refresh without closing
- Uses a `[SerializeField] private CollectableItem[] allCollectables` reference to look up full objects by id (assign in Inspector or load from Resources)

**Step 5.2 — Create `DialogUI`**
```
Assets/Game/Script/UI/Dialog/DialogUI.cs
```
- Implements `IUIPanel`
- Subscribes to `DialogStartedEvent` → opens panel, populates speaker/text
- Each frame: listens for `Space` or `Mouse0` → calls `IDialogManager.AdvanceLine()`
- Subscribes to `DialogEndedEvent` → closes panel
- Supports optional typewriter effect (additive, not blocking)

---

### Phase 6 — World Trigger

**Step 6.1 — Create `WorldDialogTrigger`**
```
Assets/Game/Script/Dialog/Triggers/WorldDialogTrigger.cs
```
- `[SerializeField] private DialogData dialogData`
- Uses `OnTriggerEnter` (tag check for "Player")
- Resolves `IDialogManager`, guards on `HasTriggered(dialogData.dialogId)`
- Calls `dialogManager.StartDialog(data)`

---

### Phase 7 — Save/Load Integration

**Step 7.1 — Modify `WorldSaveData.cs`**

Add to `WorldStateSaveData`:
```csharp
public List<string> unlockedCollectables = new List<string>();
public List<string> triggeredDialogs = new List<string>();
```

**Step 7.2 — Modify `SaveLoadService.cs`**

In the method that builds `PlayerSaveData`/`WorldStateSaveData` before writing to disk:
```csharp
var collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
if (collectableManager != null)
    worldState.unlockedCollectables = collectableManager.GetUnlockedIds().ToList();

var dialogManager = ServiceContainer.Instance.TryGet<IDialogManager>();
if (dialogManager != null)
    worldState.triggeredDialogs = dialogManager.GetTriggeredIds();
```

In the method that applies loaded data to running services:
```csharp
collectableManager?.LoadState(saveData.worldState.unlockedCollectables);
dialogManager?.LoadState(saveData.worldState.triggeredDialogs);
```

---

### Phase 8 — Registration

**Step 8.1 — Modify `GameServiceBootstrapper.cs`**

Add `[SerializeField]` references for both managers and register in `FindAndRegisterServices()`:
```csharp
[SerializeField] private CollectableManager collectableManager;
[SerializeField] private DialogManager dialogManager;

// In FindAndRegisterServices():
var cm = collectableManager ?? FindFirstObjectByType<CollectableManager>();
if (cm != null) container.Register<ICollectableManager>(cm);

var dm = dialogManager ?? FindFirstObjectByType<DialogManager>();
if (dm != null) container.Register<IDialogManager>(dm);
```

---

## File Checklist

| # | File | Action | Status |
|---|---|---|---|
| 1 | `Collectable/CollectableType.cs` | CREATE | ⬜ |
| 2 | `Collectable/CollectableItem.cs` | CREATE | ⬜ |
| 3 | `Collectable/ICollectableManager.cs` | CREATE | ⬜ |
| 4 | `Collectable/CollectableManager.cs` | CREATE | ⬜ |
| 5 | `Dialog/DialogLine.cs` | CREATE | ⬜ |
| 6 | `Dialog/DialogData.cs` | CREATE | ⬜ |
| 7 | `Dialog/IDialogManager.cs` | CREATE | ⬜ |
| 8 | `Dialog/DialogManager.cs` | CREATE | ⬜ |
| 9 | `Dialog/Triggers/WorldDialogTrigger.cs` | CREATE | ⬜ |
| 10 | `Interaction/Interactables/CollectableInteractable.cs` | CREATE | ⬜ |
| 11 | `UI/Collectable/CollectablesUI.cs` | CREATE | ⬜ |
| 12 | `UI/Dialog/DialogUI.cs` | CREATE | ⬜ |
| 13 | `Core/Events/GameEvents.cs` | MODIFY — add 3 events | ⬜ |
| 14 | `Core/SaveSystem/WorldSaveData.cs` | MODIFY — 2 fields on `WorldStateSaveData` | ⬜ |
| 15 | `Core/SaveSystem/SaveLoadService.cs` | MODIFY — capture & restore both managers | ⬜ |
| 16 | `Core/GameServiceBootstrapper.cs` | MODIFY — register 2 new services | ⬜ |

---

## Code Contracts (Signatures)

These are the minimal public surface areas. Implementations may add private members freely.

```csharp
// ICollectableManager
bool IsUnlocked(string id);
void Unlock(CollectableItem collectable);
IReadOnlyCollection<string> GetUnlockedIds();
void LoadState(List<string> unlockedIds);

// IDialogManager
bool IsActive { get; }
bool HasTriggered(string dialogId);
void StartDialog(DialogData data);
void AdvanceLine();
void EndDialog();
void LoadState(List<string> triggeredIds);
List<string> GetTriggeredIds();

// CollectableItem (ScriptableObject)
string id;
string title;
string content;          // [TextArea]
CollectableType type;
Sprite icon;
InventoryItem associatedItem;  // nullable — only for ItemWithDescription

// DialogLine (struct)
string speakerName;
string text;             // [TextArea]

// DialogData (ScriptableObject)
string dialogId;         // unique, used for save tracking
List<DialogLine> lines;

// CollectableInteractable (implements IInteractable)
// — no additional public API beyond IInteractable
```

---

## Implementation Order Recommendation

```
Phase 1 (Data)  →  Phase 2 (Events)  →  Phase 3 (Managers)
     →  Phase 4 (Interactable)  →  Phase 8 (Registration)
     →  Phase 7 (Save/Load)  →  Phase 5 & 6 (UI + Trigger)
```

Start phases 1–4+8 first so the core runtime loop works (collect → save → load) before building any UI. UI can always be wired last.
