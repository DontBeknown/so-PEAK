# Collectable & Dialog System - Revised Implementation Plan

**Author:** Copilot  
**Date:** March 16, 2026  
**Status:** Ready to implement

---

## Table of Contents

1. [Overview](#overview)
2. [Confirmed Requirements](#confirmed-requirements)
3. [Architecture](#architecture)
4. [Implementation Steps](#implementation-steps)
5. [File Checklist](#file-checklist)
6. [Code Contracts (Signatures)](#code-contracts-signatures)

---

## Overview

This revision replaces the previous scope and locks the feature to:

- **Collectable System** with exactly **2 types**:
  - `TextDocument`
  - `ScriptDialog`
- **Three UI surfaces**:
  1. **Document Page Tab**: spawns a document page prefab and shows header + body text
  2. **Dialog UI**: runs dialog while player can keep walking, advances via dedicated key bind (Mouse Right)
  3. **Collectables Hub Tab**: shows locked/unlocked entries inside inventory tab flow; unlocked entries can open document or replay dialog

Collectable unlock state and triggered dialog state are world-persistent and survive death/respawn.

---

## Confirmed Requirements

| Requirement | Final Decision |
|---|---|
| Collectable types | Only `TextDocument` and `ScriptDialog` |
| Item-with-description type | Removed |
| Hub placement | Hub is an inventory tab |
| Dialog advance key | Dedicated bind = Mouse Right |
| Dialog + movement | Player can walk while dialog runs |
| Dialog with other UI | Auto-hide and pause when another UI opens |
| Hub interactions | Unlocked entries can open document or replay dialog |

---

## Architecture

```
Assets/Game/Script/
|-- Collectable/
|   |-- CollectableItem.cs          (ScriptableObject - data)
|   |-- CollectableType.cs          (enum: 2 values only)
|   |-- ICollectableManager.cs      (interface)
|   `-- CollectableManager.cs       (MonoBehaviour : ICollectableManager)
|
|-- Dialog/
|   |-- DialogLine.cs               (serializable struct)
|   |-- DialogData.cs               (ScriptableObject - lines + dialogId)
|   |-- IDialogManager.cs           (interface)
|   |-- DialogManager.cs            (MonoBehaviour : IDialogManager)
|   `-- Triggers/
|       `-- WorldDialogTrigger.cs   (optional collider trigger)
|
|-- Interaction/Interactables/
|   `-- CollectableInteractable.cs  (MonoBehaviour : IInteractable)
|
|-- UI/
|   |-- Collectable/
|   |   |-- CollectablesHubUI.cs    (inventory tab content)
|   |   `-- DocumentPageUI.cs       (document viewer panel/tab content)
|   `-- Dialog/
|       `-- DialogUI.cs             (dialog presenter)
|
`-- Core/
    |-- Events/GameEvents.cs        (add collectable/dialog events)
    |-- SaveSystem/WorldSaveData.cs (add 2 world state fields)
    |-- SaveSystem/SaveLoadService.cs (capture + hydrate manager state)
    `-- GameServiceBootstrapper.cs  (register new services)
```

---

## Implementation Steps

### Phase 1 - Data Layer

**Step 1.1 - Create `CollectableType` enum (2 values only)**
`Assets/Game/Script/Collectable/CollectableType.cs`

```csharp
namespace Game.Collectable
{
    public enum CollectableType
    {
        TextDocument,
        ScriptDialog
    }
}
```

**Step 1.2 - Create `CollectableItem` ScriptableObject**
`Assets/Game/Script/Collectable/CollectableItem.cs`

Fields:
- `id`
- `headerName`
- `content`
- `type`
- `icon`

`associatedItem` is removed. Keep dialog data on scene-level interactable or dialog asset reference mapping.

**Step 1.3 - Create `DialogLine` struct + `DialogData` ScriptableObject**
`Assets/Game/Script/Dialog/DialogLine.cs`  
`Assets/Game/Script/Dialog/DialogData.cs`

- `DialogLine`: `speakerName`, `text`
- `DialogData`: `dialogId`, `List<DialogLine> lines`

---

### Phase 2 - Events

**Step 2.1 - Add events to `GameEvents.cs`**
`Assets/Game/Script/Core/Events/GameEvents.cs`

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

public class CollectableOpenRequestedEvent
{
    public CollectableItem Collectable { get; }
    public CollectableOpenRequestedEvent(CollectableItem collectable) => Collectable = collectable;
}
```

`CollectableOpenRequestedEvent` is used by Hub buttons to open document or replay dialog.

---

### Phase 3 - Managers

**Step 3.1 - Create `ICollectableManager`**
`Assets/Game/Script/Collectable/ICollectableManager.cs`

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

**Step 3.2 - Create `CollectableManager`**
`Assets/Game/Script/Collectable/CollectableManager.cs`

- Internal `HashSet<string> _unlocked`
- Guard duplicate unlocks
- Publish `CollectableUnlockedEvent`

**Step 3.3 - Create `IDialogManager`**
`Assets/Game/Script/Dialog/IDialogManager.cs`

```csharp
using System.Collections.Generic;

namespace Game.Dialog
{
    public interface IDialogManager
    {
        bool IsActive { get; }
        bool IsPaused { get; }
        bool HasTriggered(string dialogId);
        void StartDialog(DialogData data, bool isReplay = false);
        void AdvanceLine();
        void PauseDialog();
        void ResumeDialog();
        void EndDialog();
        void LoadState(List<string> triggeredIds);
        List<string> GetTriggeredIds();
    }
}
```

**Step 3.4 - Create `DialogManager`**
`Assets/Game/Script/Dialog/DialogManager.cs`

Behavior:
- Does **not** block player movement/input during normal dialog run
- Subscribes to panel-open events and if a non-dialog panel opens:
  - pauses dialog
  - requests dialog UI hide
- Advances line index on explicit `AdvanceLine()` only
- Replay from Hub starts from first line

---

### Phase 4 - Input Binding

**Step 4.1 - Add dedicated `AdvanceDialog` action**
`Assets/Game/Input_Action/Player.inputactions`

- Add action `AdvanceDialog`
- Bind to `Mouse Right`
- Regenerate input wrapper class if used

Dialog UI consumes this action only while dialog is active.

---

### Phase 5 - Interactable + Trigger

**Step 5.1 - Create `CollectableInteractable`**
`Assets/Game/Script/Interaction/Interactables/CollectableInteractable.cs`

- Implements `IInteractable`
- Fields: `CollectableItem collectableItem`, `DialogData dialogData`, `bool destroyOnInteract`
- `CanInteract`: `!collectableManager.IsUnlocked(id)`
- `Interact()` behavior:

| `CollectableType` | Actions |
|---|---|
| `TextDocument` | unlock -> publish open request (optional immediate open) -> destroy if configured |
| `ScriptDialog` | unlock -> start dialog -> destroy if configured |

**Step 5.2 - Optional `WorldDialogTrigger`**
`Assets/Game/Script/Dialog/Triggers/WorldDialogTrigger.cs`

Can still exist for non-collectable narrative zones.

---

### Phase 6 - UI (3 Surfaces)

**Step 6.1 - Document Page Tab / Viewer**
`Assets/Game/Script/UI/Collectable/DocumentPageUI.cs`

- Implements `IUIPanel` (or tab content adapter if inventory tab system requires)
- Spawns document page prefab on open
- Displays `headerName` and `content`

**Step 6.2 - Dialog UI**
`Assets/Game/Script/UI/Dialog/DialogUI.cs`

- Implements `IUIPanel`
- Shows speaker + text for current line
- Advances on dedicated `AdvanceDialog` (`Mouse Right`)
- When another UI opens: auto-hide and stay paused until resumed/reopened

**Step 6.3 - Collectables Hub Tab**
`Assets/Game/Script/UI/Collectable/CollectablesHubUI.cs`

- Implement as **inventory tab content**
- Shows locked and unlocked entries
- Unlocked entry actions:
  - Open document page (for `TextDocument`)
  - Replay dialog from line 1 (for `ScriptDialog`)
- Closing exits current viewer cleanly

---

### Phase 7 - Save/Load Integration

**Step 7.1 - Modify `WorldSaveData.cs`**
`Assets/Game/Script/Core/SaveSystem/WorldSaveData.cs`

Add to `WorldStateSaveData`:

```csharp
public List<string> unlockedCollectables = new List<string>();
public List<string> triggeredDialogs = new List<string>();
```

**Step 7.2 - Modify `SaveLoadService.cs`**
`Assets/Game/Script/Core/SaveSystem/SaveLoadService.cs`

On save:

```csharp
var collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
if (collectableManager != null)
    worldState.unlockedCollectables = collectableManager.GetUnlockedIds().ToList();

var dialogManager = ServiceContainer.Instance.TryGet<IDialogManager>();
if (dialogManager != null)
    worldState.triggeredDialogs = dialogManager.GetTriggeredIds();
```

On load:

```csharp
collectableManager?.LoadState(saveData.worldState.unlockedCollectables);
dialogManager?.LoadState(saveData.worldState.triggeredDialogs);
```

Also initialize both lists in default/new world state creation.

---

### Phase 8 - Service Registration

**Step 8.1 - Modify `GameServiceBootstrapper.cs`**
`Assets/Game/Script/Core/GameServiceBootstrapper.cs`

Register both managers as interfaces:

```csharp
[SerializeField] private CollectableManager collectableManager;
[SerializeField] private DialogManager dialogManager;

var cm = collectableManager ?? FindFirstObjectByType<CollectableManager>();
if (cm != null) container.Register<ICollectableManager>(cm);

var dm = dialogManager ?? FindFirstObjectByType<DialogManager>();
if (dm != null) container.Register<IDialogManager>(dm);
```

---

## File Checklist

| # | File | Action | Status |
|---|---|---|---|
| 1 | `Collectable/CollectableType.cs` | CREATE (2 values only) | ✅ |
| 2 | `Collectable/CollectableItem.cs` | CREATE (header/content/type/icon) | ✅ |
| 3 | `Collectable/ICollectableManager.cs` | CREATE | ✅ |
| 4 | `Collectable/CollectableManager.cs` | CREATE | ✅ |
| 5 | `Dialog/DialogLine.cs` | CREATE | ✅ |
| 6 | `Dialog/DialogData.cs` | CREATE | ✅ |
| 7 | `Dialog/IDialogManager.cs` | CREATE | ✅ |
| 8 | `Dialog/DialogManager.cs` | CREATE | ✅ |
| 9 | `Dialog/Triggers/WorldDialogTrigger.cs` | CREATE (optional) | ✅ |
| 10 | `Interaction/Interactables/CollectableInteractable.cs` | CREATE | ✅ |
| 11 | `UI/Collectable/CollectablesHubUI.cs` | CREATE (inventory tab content) | ✅ |
| 12 | `UI/Collectable/DocumentPageUI.cs` | CREATE (spawns prefab) | ✅ |
| 13 | `UI/Dialog/DialogUI.cs` | CREATE | ✅ |
| 14 | `Core/Events/GameEvents.cs` | MODIFY (collectable/dialog events) | ✅ |
| 15 | `Core/SaveSystem/WorldSaveData.cs` | MODIFY (2 fields) | ✅ |
| 16 | `Core/SaveSystem/SaveLoadService.cs` | MODIFY (capture + hydrate) | ✅ |
| 17 | `Core/GameServiceBootstrapper.cs` | MODIFY (register services) | ✅ |
| 18 | `Input_Action/Player.inputactions` | MODIFY (`AdvanceDialog` Mouse Right) | ⬜ |

---

## Code Contracts (Signatures)

```csharp
// ICollectableManager
bool IsUnlocked(string id);
void Unlock(CollectableItem collectable);
IReadOnlyCollection<string> GetUnlockedIds();
void LoadState(List<string> unlockedIds);

// IDialogManager
bool IsActive { get; }
bool IsPaused { get; }
bool HasTriggered(string dialogId);
void StartDialog(DialogData data, bool isReplay = false);
void AdvanceLine();
void PauseDialog();
void ResumeDialog();
void EndDialog();
void LoadState(List<string> triggeredIds);
List<string> GetTriggeredIds();

// CollectableItem (ScriptableObject)
string id;
string headerName;
string content;      // [TextArea]
CollectableType type;
Sprite icon;

// CollectableType (enum)
TextDocument,
ScriptDialog

// DialogLine (struct)
string speakerName;
string text;         // [TextArea]

// DialogData (ScriptableObject)
string dialogId;
List<DialogLine> lines;
```

---

## Implementation Order Recommendation

```
Phase 1 (Data)
 -> Phase 2 (Events)
 -> Phase 3 (Managers)
 -> Phase 8 (Registration)
 -> Phase 7 (Save/Load)
 -> Phase 4 (Input)
 -> Phase 5 (Interactable/Trigger)
 -> Phase 6 (UI: Hub + Document + Dialog)
```

Build and verify the runtime loop first (unlock -> save -> load), then finish UI behavior and tab integration.