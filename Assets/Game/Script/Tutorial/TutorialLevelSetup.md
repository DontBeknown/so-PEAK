# Tutorial Level Setup Guide

Complete step-by-step guide to building the tutorial scene from the flow below.

---

## Full Flow

```
Look Around  →  WASD  →  Sprint
    ↓
[Gate 1 — opens after Sprint step]
    ↓
Player walks into Food Zone
  • Hunger drained
  • Step advances → "Gather food"
Player completes gathering interactable
    ↓
[Gate 2 — opens after Gather step]
    ↓
Player walks into Water Zone
  • Thirst drained + canteen charges set to 0
  • Step advances → "Refill canteen"
Player drinks from canteen
    ↓
[Gate 3 — opens after Drink step]
    ↓
Player walks into Assessment Zone
  • Health reduced
  • Step advances → "Use Assessment Terminal"
Player interacts with the terminal
    ↓
Step advances → "Reach the lighthouse"
Player interacts with lighthouse  →  Tutorial complete
```

---

## TutorialData ScriptableObject

Create one `TutorialData` asset at `Resources/Tutorial/TutorialData` (right-click → Create → Tutorial → Tutorial Data).

Add 7 `TutorialStepData` assets (Create → Tutorial → Tutorial Step Data) and assign them in order:

| Index | Step ID | Completion Type | Threshold | Instruction Text | Input Hint |
|---|---|---|---|---|---|
| 0 | `look_around` | `LookAround` | `180` | "Look around to get your bearings" | "Move your mouse" |
| 1 | `walk` | `WalkDistance` | `10` | "Use WASD to move" | "W A S D" |
| 2 | `sprint` | `Sprint` | `3` | "Hold Shift to run" | "Hold Shift" |
| 3 | `wait_food` | `WaitForTrigger` | *(any)* | *(leave blank — UI hides automatically)* | *(leave blank)* |
| 4 | `gather_food` | `ConsumeFood` | `1` | "You're hungry — find something to eat" | "Right-click item → Consume" |
| 5 | `wait_water` | `WaitForTrigger` | *(any)* | *(leave blank)* | *(leave blank)* |
| 6 | `refill_canteen` | `RefillCanteen` | `1` | "Your canteen is empty — refill it at the water source" | "Hold E on water source" |
| 7 | `drink_canteen` | `DrinkFromCanteen` | `1` | "Now drink from your canteen" | "Right-click canteen → Drink" |
| 8 | `wait_terminal` | `WaitForTrigger` | *(any)* | *(leave blank)* | *(leave blank)* |
| 9 | `use_terminal` | `InteractTerminal` | `1` | "You're wounded — use the Assessment Terminal to recover" | "Hold E" |
| 10 | `lighthouse` | `InteractLighthouse` | `1` | "Find the highest peak and interact with the Lighthouse" | "Hold E" |

> `completionThreshold` for `LookAround` is degrees, for `WalkDistance` is Unity units, for `Sprint` is seconds. `WaitForTrigger` steps never auto-complete — only `TutorialAreaTrigger` (with **Complete Current Step = true**) can advance them. UI hides automatically on these steps.

---

## Scene Hierarchy

```
TutorialScene
├─ [Normal gameplay objects — player spawn, terrain, etc.]
│
├─ TutorialManager          ← MonoBehaviour, registered by GameServiceBootstrapper
│
├─ ── ZONES ──
│
├─ MovementZone             ← No trigger needed; steps 0–2 complete by polling
│
├─ FoodZone
│  ├─ FoodZone_Gate         ← TutorialAreaGate  (blocks entry until step 2 done)
│  ├─ FoodZone_Trigger      ← TutorialAreaTrigger + TutorialStatManipulator
│  └─ GatheringInteractable ← Any HoldInteractableBase object (apple tree, bush, etc.)
│
├─ WaterZone
│  ├─ WaterZone_Gate        ← TutorialAreaGate  (blocks entry until step 3 done)
│  ├─ WaterZone_Trigger     ← TutorialAreaTrigger + TutorialStatManipulator
│  └─ WaterSourceInteractable ← WaterSourceInteractable (for canteen refill)
│
├─ AssessmentZone
│  ├─ AssessmentZone_Gate   ← TutorialAreaGate  (blocks entry until step 4 done)
│  ├─ AssessmentZone_Trigger ← TutorialAreaTrigger + TutorialStatManipulator
│  └─ AssessmentTerminal    ← AssessmentTerminalInteractable
│
└─ LighthouseZone
   ├─ LighthouseZone_Gate   ← TutorialAreaGate  (blocks entry until step 5 done)
   └─ Lighthouse            ← HoldInteractableBase (or existing lighthouse interactable)
```

---

## Component Setup Per Object

### FoodZone_Gate
Component: `TutorialAreaGate`
```
Step Index To Unlock:  3      ← unlocks after WaitForTrigger step (index 3) completes
Use Animator:          ☐
```
Add a collider marked **Is Trigger = false** so it physically blocks the player.

---

### FoodZone_Trigger
Add **Box Collider** → check **Is Trigger**.
Size it to cover the entrance of the food area.

**TutorialAreaTrigger**
```
Area Name:             "Food Zone"
One Time Only:         ✓
Debug Logs:            ✓
Complete Current Step: ✓     ← completes WaitForTrigger (step 3) → shows Gather Food (step 4)
Publish Custom Event:  ☐
Area Type:             FoodZone
```

**TutorialStatManipulator** (same GameObject)
```
Drain Hunger:          ✓
Hunger Target:         20
Drain Thirst:          ☐
Deal Damage:           ☐
Drain Canteen Charges: ☐
One Time Only:         ✓
```

---

### WaterZone_Gate
Component: `TutorialAreaGate`
```
Step Index To Unlock:  4      ← unlocks after Gather Food step (index 4) completes
```

---

### WaterZone_Trigger
Add **Box Collider** → check **Is Trigger**.

**TutorialAreaTrigger**
```
Area Name:             "Water Zone"
One Time Only:         ✓
Debug Logs:            ✓
Complete Current Step: ✓     ← completes WaitForTrigger (step 5) → shows Drink Canteen (step 6)
Publish Custom Event:  ☐
Area Type:             WaterZone
```

**TutorialStatManipulator** (same GameObject)
```
Drain Hunger:          ☐
Drain Thirst:          ✓
Thirst Target:         15
Deal Damage:           ☐
Drain Canteen Charges: ✓     ← empties the canteen so player must refill
One Time Only:         ✓
```

---

### AssessmentZone_Gate
Component: `TutorialAreaGate`
```
Step Index To Unlock:  7      ← unlocks after Drink Canteen step (index 7) completes
```

---

### AssessmentZone_Trigger
Add **Box Collider** → check **Is Trigger**.

**TutorialAreaTrigger**
```
Area Name:             "Assessment Zone"
One Time Only:         ✓
Debug Logs:            ✓
Complete Current Step: ✓     ← completes WaitForTrigger (step 7) → shows Use Terminal (step 8)
Publish Custom Event:  ☐
Area Type:             RestZone
```

**TutorialStatManipulator** (same GameObject)
```
Drain Hunger:          ☐
Drain Thirst:          ☐
Deal Damage:           ✓
Damage Amount:         30
Drain Canteen Charges: ☐
One Time Only:         ✓
```

---

### LighthouseZone_Gate
Component: `TutorialAreaGate`
```
Step Index To Unlock:  9      ← unlocks after Use Terminal step (index 9) completes
```

---

### Gathering Interactable (food object)
Use an existing `HoldInteractableBase` object or any interactable that publishes `HoldInteractCompletedEvent`. No special tutorial config needed — `TutorialManager` listens to that event automatically and completes step 3.

---

### Water Source
Use `WaterSourceInteractable`. It already handles canteen refill when held. After refilling, the player uses the canteen from inventory → fires `ItemConsumedEvent` → completes step 4.

---

### Assessment Terminal
Use `AssessmentTerminalInteractable`. Completing the hold fires `HoldInteractCompletedEvent` → completes step 5.

---

### Lighthouse
Any `HoldInteractableBase` that publishes `HoldInteractCompletedEvent` → completes step 6 → tutorial marked complete.

---

## Checklist Before Playtesting

- [ ] `TutorialData` asset exists at `Resources/Tutorial/TutorialData`
- [ ] All 7 `TutorialStepData` assets are assigned in order
- [ ] `TutorialManager` GameObject is in the scene and wired up by `GameServiceBootstrapper`
- [ ] Each gate's `Step Index To Unlock` matches the table above
- [ ] Each trigger has **Is Trigger** checked on its collider
- [ ] Each zone's gate collider blocks the player physically (Is Trigger = false on the gate mesh)
- [ ] `WaterSourceInteractable` is present in the water zone
- [ ] `AssessmentTerminalInteractable` is present in the assessment zone
- [ ] Lighthouse has a `HoldInteractableBase` component

## Debugging

Enable `debugLogs` on every `TutorialAreaTrigger` and watch the Console. You should see:

```
[TutorialAreaTrigger] Player entered: Food Zone
[TutorialAreaTrigger] Completing current step for area: Food Zone
[TutorialManager] Tutorial started from step 0.
```

If a gate does not open, check that `Step Index To Unlock` matches the index of the step that just completed (0-based).
