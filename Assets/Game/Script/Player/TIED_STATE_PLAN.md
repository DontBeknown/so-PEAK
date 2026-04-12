# Tied State — Implementation Plan

**Status:** Approved, ready for execution  
**Last Updated:** 2026-04-12

---

## Overview

Add a **`TiedState`** player state where the player is tethered to a world object by a rope.
Entry is triggered by a `HoldInteractableBase` subclass (`TiedInteractable`) on any scene object.

**Behaviour:**
- The player **can still move** but at a reduced speed (`tiedSpeedMultiplier × walkSpeed`).
- Movement is clamped: the player cannot travel further than `maxTiedRadius` from the anchor.
- The state has **3 animation phases**: *Start* (enter trigger), *During* (loop bool), *Stop* (exit trigger).
- **Untie mechanic**: re-approach the anchor object and hold Interact again → `ExitTiedState()` → `WalkingState`.
- **Ownership safety**: only the same player who tied to this anchor can untie through this interactable.

---

## Files to Create / Modify

### 1. [NEW] `TiedState.cs`
**Path:** `Assets/Game/Script/Player/PlayerState/TiedState.cs`

**Class:** `public class TiedState : IPlayerState`

**Constructor:**
```csharp
public TiedState(IStateTransitioner transitioner, Transform anchor, float radius, float speedMultiplier)
```
Stores all four arguments as private readonly fields.

**`Enter(PlayerModelRefactored model)`**
- `animService.TriggerTiedStart()` — fires the Start trigger (plays bind animation once)
- `animService.SetTied(true)` — enables the During loop bool
- `animService.SetWalking(false)` — clear any stale walking blend
- `animService.SetRunning(false)`

**`FixedUpdate(PlayerModelRefactored model, Vector2 input)`**
1. Get camera-relative move direction via `model.GetCameraProvider().GetWorldDirection(input)`
2. Compute `velocity = moveDir * (model.WalkSpeed * _speedMultiplier)`
3. Apply gravity with `model.ApplyGravity(-9.81f)` so the player stays grounded
4. Call `model.Move(new Vector3(velocity.x, model.Velocity.y, velocity.z))`
5. **Radius clamp** — after move, read `model.Transform.position`; compute horizontal delta from `_anchor.position`;
   if `delta.magnitude > _radius`, project back onto the circle boundary via:
   `Controller.enabled = false → Transform.position = clampedPos → Controller.enabled = true`
6. Rotate player toward `moveDir` when input is non-zero (same `Slerp` pattern as `WalkingState`)
7. Drive animation: `animService.UpdateMovement(velocity, model.WalkSpeed * _speedMultiplier)`

**`HandleInput(PlayerModelRefactored model, Vector2 input)`** — empty

**`Exit(PlayerModelRefactored model)`**
- `animService.SetTied(false)`
- `animService.TriggerTiedStop()` — fires the Stop trigger (plays unbind animation once)

**Runtime guard:**
- If `_anchor` becomes null while tied, immediately transition to `WalkingState` (null-safe fallback).

**`OnJump`** — intentionally no-op (no jumping while tied)  
**`OnClimb`** — intentionally no-op

---

### 2. [NEW] `TiedInteractable.cs`
**Path:** `Assets/Game/Script/Interaction/TiedInteractable.cs`

**Class:** `public class TiedInteractable : HoldInteractableBase`

**Serialized fields:**
```csharp
[Header("Tied Settings")]
[SerializeField] private float tiedSpeedMultiplier = 0.35f;
[SerializeField] private float maxTiedRadius = 4f;
[SerializeField] private Transform anchorTransform; // defaults to this.transform in Awake
```

**Internal state:**
```csharp
private bool _playerIsTied = false;
private PlayerControllerRefactored _tiedPlayer;
```

**`Awake`:** `if (anchorTransform == null) anchorTransform = transform;`

**`CanInteract`** — `!isCurrentlyHolding` (allow tie/untie while preventing re-entry during active hold)

**`InteractionPrompt`:**
```csharp
public override string InteractionPrompt => _playerIsTied ? "Untie rope" : "Tie rope";
```

**`InteractionVerb`:**
```csharp
public override string InteractionVerb => "Hold to";
```

**`OnHoldComplete()`:**
```csharp
protected override void OnHoldComplete()
{
    if (!_playerIsTied)
    {
        _playerIsTied = true;
        _tiedPlayer = currentPlayer;
        currentPlayer.EnterTiedState(anchorTransform, maxTiedRadius, tiedSpeedMultiplier);
    }
    else if (currentPlayer == _tiedPlayer)
    {
        _playerIsTied = false;
        _tiedPlayer?.ExitTiedState();
        _tiedPlayer = null;
    }
}
```

**Ownership guard:** if tied and `currentPlayer != _tiedPlayer`, ignore the untie request.

**`OnHoldCancel`** — no-op (partial hold does nothing)

**Edge case — anchor object destroyed while player is tied:**
```csharp
public void ForceUntie()
{
    if (_playerIsTied && _tiedPlayer != null)
    {
        _tiedPlayer.ExitTiedState();
        _tiedPlayer = null;
        _playerIsTied = false;
    }
}

private void OnDestroy() => ForceUntie();
```

> **Note on input flow:** `HoldInteractableBase.CompleteHolding()` invokes `OnHoldComplete()` first,
> then calls `Cleanup()` (which unlocks input and re-enables interaction detection).
> `TiedState` does not need explicit input unblocking; base cleanup handles it.

---

### 3. [MODIFY] `IAnimationService.cs`
**Path:** `Assets/Game/Script/Player/Interfaces/IAnimationService.cs`

Add three new method signatures at the end of the interface:

```csharp
/// <summary>Sets the tied-to-object loop animation state (isTied bool).</summary>
void SetTied(bool isTied);

/// <summary>Fires the TiedStart trigger to play the rope-bind start animation.</summary>
void TriggerTiedStart();

/// <summary>Fires the TiedStop trigger to play the rope-unbind stop animation.</summary>
void TriggerTiedStop();
```

---

### 4. [MODIFY] `PlayerAnimationService.cs`
**Path:** `Assets/Game/Script/Player/Services/PlayerAnimationService.cs`

**Add hash constants** alongside existing ones:
```csharp
private static readonly int IsTiedHash    = Animator.StringToHash("isTied");
private static readonly int TiedStartHash = Animator.StringToHash("TiedStart");
private static readonly int TiedStopHash  = Animator.StringToHash("TiedStop");
```

**Implement the three new interface methods:**
```csharp
public void SetTied(bool isTied)
{
    if (_animator == null) return;
    _animator.SetBool(IsTiedHash, isTied);
}

public void TriggerTiedStart()
{
    if (_animator == null) return;
    _animator.SetTrigger(TiedStartHash);
}

public void TriggerTiedStop()
{
    if (_animator == null) return;
    _animator.SetTrigger(TiedStopHash);
}
```

---

### 5. [MODIFY] `PlayerControllerRefactored.cs`
**Path:** `Assets/Game/Script/Player/PlayerControllerRefactored.cs`

**Add two new public methods** inside `#region Public API`:

```csharp
/// <summary>
/// Transitions the player into TiedState, tethering them to the given anchor.
/// Called by TiedInteractable.OnHoldComplete().
/// </summary>
public void EnterTiedState(Transform anchor, float radius, float speedMultiplier)
{
    if (anchor == null)
    {
        Debug.LogWarning("[PlayerControllerRefactored] EnterTiedState called with null anchor.");
        return;
    }

    float safeRadius = Mathf.Max(0.1f, radius);
    float safeSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.01f, 1f);
    TransitionTo(new TiedState(this, anchor, safeRadius, safeSpeedMultiplier));
}

/// <summary>
/// Exits TiedState and returns the player to WalkingState.
/// Called by TiedInteractable when the player holds Interact again to untie.
/// </summary>
public void ExitTiedState()
{
    if (_currentState is TiedState)
    {
        TransitionTo(new WalkingState(this));
    }
}
```

**Modify `HandleAutomaticTransitions()`** — extend the existing guard at the top:

```csharp
// Before (existing):
if (_currentState is ClimbingState || _currentState is MantlingState)
    return;

// After:
if (_currentState is ClimbingState || _currentState is MantlingState || _currentState is TiedState)
    return;
```

---

## Animator Setup (Unity Editor — manual steps)

| Parameter | Type | Purpose |
|-----------|------|---------|
| `isTied` | Bool | Drives the looping During animation while tethered |
| `TiedStart` | Trigger | Plays once on entering tied state |
| `TiedStop` | Trigger | Plays once on exiting tied state |

**Suggested Animator state machine layout:**
```
Any State ──[TiedStart trigger]──► TiedStart clip (play once)
TiedStart ──[Exit Time 1.0]──────► TiedLoop  (condition: isTied == true)
TiedLoop  ──[isTied == false]────► TiedStop clip (play once)
TiedStop  ──[Exit Time 1.0]──────► Walking blend tree
```

---

## Verification Checklist

- [ ] Place `TiedInteractable` on a world object → prompt "Hold to Tie rope" appears when nearby
- [ ] Hold Interact → bar fills → `TiedStart` trigger fires, `isTied` bool goes true
- [ ] Player moves at ≈35% walk speed while tied
- [ ] Walking past `maxTiedRadius` clamps the player at the boundary
- [ ] Walking back near anchor → prompt switches to "Hold to Untie rope"
- [ ] Hold Interact again → `TiedStop` trigger fires, `isTied` cleared → returns to WalkingState
- [ ] Auto-transitions (sprint, fall) are suppressed while in TiedState
- [ ] Destroying the anchor object mid-tie calls `ForceUntie()` → player exits cleanly
- [ ] A different player cannot untie another player's active tie via this interactable
- [ ] Null anchor input to `EnterTiedState` is ignored safely (warning only)
