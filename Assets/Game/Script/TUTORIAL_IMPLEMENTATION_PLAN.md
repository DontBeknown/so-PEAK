# Tutorial System - Implementation Plan

**Author:** Copilot  
**Date:** March 17, 2026  
**Status:** Ready to implement

---

## Table of Contents

1. [Overview](#overview)
2. [Confirmed Requirements](#confirmed-requirements)
3. [Architecture](#architecture)
4. [Implementation Steps](#implementation-steps)
5. [Tutorial Step List](#tutorial-step-list)
6. [File Checklist](#file-checklist)
7. [Verification](#verification)

---

## Overview

This plan adds a first-time-player tutorial system that starts automatically for new worlds and guides the player through the core gameplay loop with a lightweight UI overlay.

The tutorial is intentionally scoped to:

- Basic movement: walk, look, jump, sprint
- Interaction basics: press interact and hold interact
- Inventory opening
- First crafting action
- Awareness of survival stats

The tutorial should be:

- Automatically started on first-ever play for a new world
- Skippable from the tutorial UI
- Persistent per world through the existing save system
- Built on the existing event bus, UI panel patterns, and service registration flow

---

## Confirmed Requirements

| Requirement | Final Decision |
|---|---|
| Tutorial start | Automatically on first game start for a new world |
| Delivery style | Step-by-step UI prompts using an overlay panel |
| Skipping | Yes, via skip button in the tutorial UI |
| Movement step | Included |
| Camera step | Included |
| Jump step | Included |
| Sprint step | Included |
| Press interact step | Included as "Press E to interact with an item" |
| Hold interact step | Included |
| Press interact visibility condition | Show this step only when an interactable item enters range for the first time |
| Hold interact visibility condition | Show this step only when a hold-gather interactable is in range |
| Post-hold auto-next | After hold interact is completed, advance immediately to Inventory, then Crafting |
| Inventory step | Included, but shown only after first item is obtained |
| Context menu tutorial | Included: right-click an item to open context menu |
| Crafting step | Included |
| Survival stats step | Included |
| Climbing tutorial | Excluded |

---

## Architecture

```
Assets/Game/Script/
|-- Tutorial/
|   |-- TutorialStepType.cs          (enum for completion conditions)
|   |-- TutorialStepData.cs          (ScriptableObject for one step)
|   |-- TutorialData.cs              (ScriptableObject for ordered step list)
|   |-- ITutorialManager.cs          (interface)
|   `-- TutorialManager.cs           (MonoBehaviour : ITutorialManager)
|
|-- UI/
|   `-- Tutorial/
|       `-- TutorialUI.cs            (overlay panel implementing IUIPanel)
|
`-- Core/
    |-- Events/GameEvents.cs         (add tutorial events)
    |-- SaveSystem/WorldSaveData.cs  (add tutorial progress data)
    |-- GameServiceBootstrapper.cs   (register tutorial manager)
    `-- GameplaySceneInitializer.cs  (start tutorial for first-time worlds)
```

The tutorial system should follow existing project patterns:

- Use `GameServiceBootstrapper` for registration
- Use `EventBus` for UI and gameplay coordination
- Use `WorldSaveData` for per-world tutorial persistence
- Use `IUIPanel` and existing UI registration flow for the overlay
- Avoid invasive changes to player control code when simple polling or existing events are enough

---

## Implementation Steps

### Phase 1 - Data Layer

**Step 1.1 - Create `TutorialStepType` enum**  
`Assets/Game/Script/Tutorial/TutorialStepType.cs`

Values:

- `AutoAdvance`
- `WalkDistance`
- `LookAround`
- `Jump`
- `Sprint`
- `PressInteract`
- `HoldInteract`
- `OpenInventory`
- `OpenContextMenu`
- `CompleteCraft`

This keeps step completion logic explicit and easy to extend later.

**Step 1.2 - Create `TutorialStepData` ScriptableObject**  
`Assets/Game/Script/Tutorial/TutorialStepData.cs`

Fields:

- `stepId`
- `title`
- `instructionText`
- `inputHintText`
- `completionType`
- `completionThreshold`

`completionThreshold` is reused for numeric conditions such as distance, angle, seconds, or counts.

**Step 1.3 - Create `TutorialData` ScriptableObject**  
`Assets/Game/Script/Tutorial/TutorialData.cs`

Fields:

- `tutorialId`
- `List<TutorialStepData> steps`

The tutorial manager loads this single asset and runs its steps in order.

**Step 1.4 - Create tutorial asset in Resources**  
`Assets/Game/Resources/Tutorial/`

Create one `TutorialData` asset for the main onboarding flow. Loading from `Resources` avoids scene-specific setup and keeps bootstrap simple.

---

### Phase 2 - Events

**Step 2.1 - Add tutorial events to `GameEvents.cs`**  
`Assets/Game/Script/Core/Events/GameEvents.cs`

Add:

```csharp
public class TutorialStartedEvent
{
    public string TutorialId { get; }
    public int StepIndex { get; }
    public TutorialStepData StepData { get; }

    public TutorialStartedEvent(string tutorialId, int stepIndex, TutorialStepData stepData)
    {
        TutorialId = tutorialId;
        StepIndex = stepIndex;
        StepData = stepData;
    }
}

public class TutorialStepCompletedEvent
{
    public string TutorialId { get; }
    public int StepIndex { get; }

    public TutorialStepCompletedEvent(string tutorialId, int stepIndex)
    {
        TutorialId = tutorialId;
        StepIndex = stepIndex;
    }
}

public class TutorialCompletedEvent
{
    public string TutorialId { get; }

    public TutorialCompletedEvent(string tutorialId)
    {
        TutorialId = tutorialId;
    }
}

public class TutorialSkippedEvent
{
    public string TutorialId { get; }

    public TutorialSkippedEvent(string tutorialId)
    {
        TutorialId = tutorialId;
    }
}
```

These events are enough to drive UI updates, analytics, or later tutorial hooks.

---

### Phase 3 - Save Data

**Step 3.1 - Add tutorial state to `WorldSaveData.cs`**  
`Assets/Game/Script/Core/SaveSystem/WorldSaveData.cs`

Add a serializable tutorial payload:

```csharp
[System.Serializable]
public class TutorialSaveData
{
    public bool isCompleted;
    public int lastCompletedStep;
}
```

Then add a `tutorial` field to `WorldSaveData`.

This should remain world-scoped so each save slot can independently complete or skip onboarding.

---

### Phase 4 - Tutorial Manager

**Step 4.1 - Create `ITutorialManager`**  
`Assets/Game/Script/Tutorial/ITutorialManager.cs`

Suggested contract:

```csharp
namespace Game.Tutorial
{
    public interface ITutorialManager
    {
        bool IsActive { get; }
        bool IsCompleted { get; }
        int CurrentStepIndex { get; }

        void StartTutorial();
        void SkipTutorial();
    }
}
```

**Step 4.2 - Create `TutorialManager`**  
`Assets/Game/Script/Tutorial/TutorialManager.cs`

Responsibilities:

- Load the `TutorialData` asset
- Track the current step index
- Publish tutorial lifecycle events
- Detect completion conditions
- Persist completion state into world save data
- Stop all tracking cleanly when skipped or completed

Recommended implementation approach:

- Use polling in `Update()` for:
  - `WalkDistance`
  - `LookAround`
  - `Jump`
  - `Sprint`
- Use event subscriptions for:
  - `OpenInventory` via `PanelOpenedEvent`
    - `OpenContextMenu` via a context menu open event published by `ContextMenuUI`
  - `CompleteCraft` via `CraftingCompletedEvent`
  - `PressInteract` via the most reliable existing interaction-complete event in the project
  - `HoldInteract` via the existing gather or hold-complete event path

Step appearance gating rules:

- `PressInteract` step is hidden until the first time an item interactable is in range
- `HoldInteract` step is hidden until a hold-gather interactable is in range
- `OpenInventory` step is hidden until the first time the player obtains any item
- When `HoldInteract` completes, advance immediately to `OpenInventory` (or wait silently until first item is obtained if needed)
- After `OpenInventory` completes, advance immediately to `OpenContextMenu`
- After `OpenContextMenu` completes, advance immediately to `CompleteCraft`

Recommended event sources for gating:

- Use `ItemInRangeChangedEvent` (or equivalent detector event) to detect in-range availability
- Use item-added or item-picked-up event (for example `ItemPickedUpEvent` / inventory add event) to detect first obtained item
- For first-time appearance, keep per-step flags like `hasSeenPressInteractOpportunity` and `hasSeenHoldInteractOpportunity`
- If a range event cannot distinguish regular vs hold-gather interactables, extend the event payload with interactable type or source reference
- If context menu has no open event yet, publish a new `ContextMenuOpenedEvent` from `ContextMenuUI.ShowInventoryMenu` and `ContextMenuUI.ShowGridItemMenu`

Important constraints:

- Do not hard-block the player unless a specific step needs it
- Keep the implementation additive rather than rewriting player systems
- Unsubscribe from step-specific events when advancing to the next step
- Do not show impossible actions early; gating must prevent "Press E" or "Hold E" prompts when no valid target is in range
- Do not show inventory/context-menu guidance before the player has at least one item

---

### Phase 5 - Tutorial UI

**Step 5.1 - Create `TutorialUI` overlay panel**  
`Assets/Game/Script/UI/Tutorial/TutorialUI.cs`

Responsibilities:

- Show current instruction text
- Show short input hint text
- Show current step progress indicator
- Expose a skip button
- Animate in and out using the existing UI style

Suggested UI elements:

- `titleText`
- `instructionText`
- `inputHintText`
- `progressContainer`
- `skipButton`

Behavior:

- Subscribe to `TutorialStartedEvent`, `TutorialStepCompletedEvent`, `TutorialCompletedEvent`, and `TutorialSkippedEvent`
- Open when tutorial starts
- Update text when step changes
- Close when tutorial ends or is skipped

Keep the layout compact and unobtrusive so it does not fight with the rest of the HUD.

---

### Phase 6 - Bootstrap and Startup Hook

**Step 6.1 - Register tutorial manager in `GameServiceBootstrapper.cs`**  
`Assets/Game/Script/Core/GameServiceBootstrapper.cs`

Register `ITutorialManager` and ensure the instance is available to UI and scene startup systems.

**Step 6.2 - Auto-start from `GameplaySceneInitializer.cs`**  
`Assets/Game/Script/Core/GameplaySceneInitializer.cs`

After world load completes:

- Check `worldSaveData.tutorial.isCompleted`
- If `false`, call `tutorialManager.StartTutorial()`
- If `true`, do nothing

This keeps tutorial startup world-aware and prevents repeat onboarding for completed saves.

---

## Tutorial Step List

| # | Name | Instruction | Completion Type | Threshold |
|---|---|---|---|---|
| 1 | Welcome | "Welcome. Let's learn the basics." | AutoAdvance | 3 seconds |
| 2 | Move | "Use WASD to move." | WalkDistance | 3 meters |
| 3 | Camera | "Move the mouse to look around." | LookAround | 90 degrees total |
| 4 | Jump | "Press Space to jump." | Jump | 1 jump |
| 5 | Sprint | "Hold Shift to sprint." | Sprint | 1.5 seconds |
| 6 | Press Interact | "Press E to interact with an item." | PressInteract | 1 interaction |
| 7 | Hold Interact | "Hold E to gather a resource." | HoldInteract | 1 completion |
| 8 | Inventory | "Press Tab to open your inventory and check your first item." | OpenInventory | panel opened (appears only after first item obtained) |
| 9 | Context Menu | "Right-click an item to open its context menu." | OpenContextMenu | 1 context menu open |
| 10 | Craft | "Craft your first item." | CompleteCraft | 1 craft (auto-next chain after inventory and context menu) |
| 11 | Survival Stats | "Watch your Hunger, Stamina, and Temperature." | AutoAdvance | 5 seconds |

These steps are short on purpose. The UI prompt should tell the player exactly one thing at a time.

Condition behavior note:

- Steps 6 and 7 should not appear until their matching in-range opportunity exists
- Step 8 should not appear until the first item is obtained
- Step 7 completion should transition to Step 8 as soon as the first-item condition is satisfied
- Step 8 completion should auto-transition to Step 9 (context menu), then Step 10 (crafting)

---

## File Checklist

### New Files

- `Assets/Game/Script/Tutorial/TutorialStepType.cs`
- `Assets/Game/Script/Tutorial/TutorialStepData.cs`
- `Assets/Game/Script/Tutorial/TutorialData.cs`
- `Assets/Game/Script/Tutorial/ITutorialManager.cs`
- `Assets/Game/Script/Tutorial/TutorialManager.cs`
- `Assets/Game/Script/UI/Tutorial/TutorialUI.cs`

### Modified Files

- `Assets/Game/Script/Core/Events/GameEvents.cs`
- `Assets/Game/Script/Core/SaveSystem/WorldSaveData.cs`
- `Assets/Game/Script/Core/GameServiceBootstrapper.cs`
- `Assets/Game/Script/Core/GameplaySceneInitializer.cs`
- `Assets/Game/Script/UI/HIUD/ContextMenuUI.cs`

### New Asset

- `Assets/Game/Resources/Tutorial/TutorialData.asset`

---

## Verification

1. Create a brand-new world and enter gameplay
2. Confirm the tutorial UI appears automatically
3. Confirm Step 6 does not appear until an interactable item is in range for the first time
4. Confirm Step 7 does not appear until a hold-gather interactable is in range
5. Confirm Step 8 does not appear until the first item is obtained
6. Complete hold interact and confirm flow continues to Step 8 when first-item condition is met
7. Open inventory and confirm Step 9 appears and requests right-click context menu
8. Right-click an item and confirm context menu tutorial completes and advances to Crafting
9. Skip the tutorial and confirm it closes immediately
10. Reload the same world and confirm the tutorial does not restart
11. Create another new world and confirm the tutorial starts there
12. Complete the full tutorial and confirm completion state persists

---

## Notes

- Prefer existing events over new player-side wiring when possible
- Keep save changes minimal and backward-compatible
- The first implementation should optimize for reliability, not tutorial branching
- If a specific interaction event does not exist for `PressInteract` or `HoldInteract`, add a focused event rather than coupling directly to UI state
- If context menu open has no event in the current code, add a minimal `ContextMenuOpenedEvent` publish point in `ContextMenuUI`