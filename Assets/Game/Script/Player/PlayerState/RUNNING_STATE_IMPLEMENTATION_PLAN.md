# Running State Implementation Plan

**Feature:** Player Sprint/Run State with Stamina-Based Speed Degradation  
**Date Created:** February 19, 2026  
**Status:** 📋 Planning Phase  
**Priority:** Medium  
**Estimated Complexity:** Low-Medium (2-4 hours)

---

## Table of Contents
1. [Overview](#overview)
2. [Design Decisions](#design-decisions)
3. [Architecture](#architecture)
4. [Implementation Steps](#implementation-steps)
5. [Code Patterns](#code-patterns)
6. [Testing & Verification](#testing--verification)
7. [Integration Points](#integration-points)
8. [Future Enhancements](#future-enhancements)

---

## Overview

### Feature Description
Add a running/sprint state to the player that allows faster movement while draining stamina. The running speed will **gradually degrade** as stamina depletes, creating a smooth transition from run to walk rather than an abrupt cutoff.

### Key Mechanics
- **Input:** Left Shift (keyboard) or Left Stick Press (gamepad)
- **Base Run Speed:** 4.5f (1.5x walk speed of 3f)
- **Stamina Drain:** 25 points/second (already configured)
- **Speed Scaling:** Linear interpolation between walk speed (3f) and run speed (4.5f) based on stamina percentage
- **Stamina Regeneration:** Disabled while running (only regenerates when walking/idle)
- **Entry Condition:** Can always start running (no minimum stamina requirement)
- **Exit Condition:** Sprint released, stamina depleted, or stopped moving

### User Experience Flow
```
1. Player holds Left Shift while moving
   ↓
2. State transitions: WalkingState → RunningState
   ↓
3. Player accelerates to 4.5f speed (100% stamina)
   ↓
4. Stamina drains at 25/s, speed gradually reduces
   ↓
5. At 50% stamina: speed = 3.75f (halfway between walk and run)
   At 25% stamina: speed = 3.375f
   At 0% stamina: speed = 3f (walk speed)
   ↓
6. Player releases Left Shift OR stamina depletes
   ↓
7. State transitions: RunningState → WalkingState
   ↓
8. Player continues at walk speed (3f)
```

---

## Design Decisions

### 1. Speed Scaling Approach
**Decision:** Gradual speed reduction using linear interpolation

**Rationale:**
- Provides smooth player experience without jarring transitions
- No need for additional "exhausted" or "tired" states
- Simple to implement: `Mathf.Lerp(walkSpeed, runSpeed, staminaPercent)`
- Matches common game design patterns (e.g., Red Dead Redemption 2, Skyrim)

**Alternative Considered:**
- Instant transition at stamina depletion (rejected - feels too abrupt)
- Multiple speed tiers (rejected - too complex for initial implementation)

### 2. Sprint Entry Requirements
**Decision:** Allow sprinting at any stamina level

**Rationale:**
- Better player agency - let them decide when to sprint
- Speed scaling naturally handles low stamina scenarios
- Avoids "stamina-locked" frustration
- Simpler state transition logic

**Alternative Considered:**
- Require minimum stamina (e.g., 25 points) to start sprinting (rejected - too restrictive)

### 3. Stamina Regeneration
**Decision:** No regeneration while running

**Rationale:**
- Creates meaningful resource management
- Forces players to plan sprint usage
- Consistent with existing walking/climbing stamina behavior
- Simple to implement: `stamina.SetRunning(false)` disables regen

**Technical Detail:**
- Walking state: `stamina.SetWalking(true)` → enables regen
- Running state: `stamina.SetWalking(false)` → disables regen
- Climbing state: `stamina.SetClimbing(true)` → disables regen

### 4. State Architecture
**Decision:** Create dedicated `RunningState` class (not a parameter in WalkingState)

**Rationale:**
- Follows existing State Pattern architecture
- Clean separation of concerns
- Easy to extend with run-specific mechanics (e.g., momentum, sliding)
- Consistent with WalkingState/ClimbingState/FallingState design

**Code Reuse:**
- RunningState inherits same structure as WalkingState
- Shares slope physics, fatigue, and rotation logic
- Only differences: speed calculation and stamina behavior

---

## Architecture

### State Diagram
```
┌─────────────┐
│ WalkingState│◄──────────────────┐
└──────┬──────┘                   │
       │                          │
       │ Sprint Held + Moving      │ Sprint Released
       │ (detected in FixedUpdate) │ OR Stopped Moving
       ▼                          │
┌─────────────┐                   │
│ RunningState│───────────────────┘
└──────┬──────┘
       │                    ┌─────────────┐
       │ Not Grounded       │ FallingState│
       └───────────────────►│             │
                            └──────┬──────┘
                                   │
                                   │ Grounded + Sprint Held → RunningState
                                   │ Grounded + Sprint Not Held → WalkingState
                                   ▼
                            ┌─────────────┐
                            │ WalkingState│
                            │ or          │
                            │ RunningState│
                            └─────────────┘
```

**Key Change from Original Plan:** Sprint entry is handled inside `WalkingState.FixedUpdate()` and `FallingState` landing logic, NOT in `HandleAutomaticTransitions()`. This avoids competing transition logic.

### Component Dependencies
```
RunningState
├─► Depends On:
│   ├─► IStateTransitioner (PlayerController)
│   ├─► PlayerModelRefactored (data + services)
│   ├─► IPhysicsService (Rigidbody operations)
│   ├─► IAnimationService (animation control)
│   ├─► ICameraProvider (camera-relative input)
│   ├─► PlayerConfig (speed/stamina settings)
│   └─► PlayerStats (stamina drain)
│
├─► Modifies:
│   ├─► Player velocity
│   ├─► Player rotation
│   ├─► Animator parameters
│   └─► Stamina value
│
└─► Transitions To:
    ├─► WalkingState (sprint released, stamina depleted, stopped)
    ├─► ClimbingState (climb input detected)
    └─► FallingState (automatic - not grounded)
```

### File Impact Map
```
Files to Create (1):
└─► RunningState.cs                      [NEW FILE - ~280 lines]

Files to Modify (8):
├─► PlayerConfig.cs                      [+2 lines: baseRunSpeed, runAcceleration]
├─► PlayerModelRefactored.cs            [+1 line: RunSpeed property]
├─► PlayerInputHandler.cs               [+4 lines: IsSprintHeld with input buffer]
├─► IAnimationService.cs                [+2 lines: SetRunning, SetSpeedMultiplier]
├─► PlayerAnimationService.cs           [+12 lines: running + blend animation support]
├─► PlayerControllerRefactored.cs       [~5 lines: remove sprint from auto-transitions]
├─► WalkingState.cs                     [+8 lines: sprint → RunningState detection]
└─► FallingState.cs                     [+5 lines: sprint-held landing logic]

Unity Assets to Modify (Manual):
├─► Player Animator Controller          [Add: isRunning bool, SpeedMultiplier float]
└─► Animator Transitions                [Walk↔Run blends using SpeedMultiplier]
```

---

## Implementation Steps

### Step 1: Configuration Setup
**File:** `Assets/Game/Script/Player/PlayerConfig.cs`

**Changes:**
```csharp
[Header("Movement - Base Values")]
public float baseWalkSpeed = 3f;
public float baseRunSpeed = 4.5f;    // ADD: max sprint speed
public float runAcceleration = 8f;   // ADD: how fast speed ramps up/down (higher = snappier)
public float baseClimbSpeed = 2f;
public float jumpForce = 5f;
```

**Notes:**
- Existing `sprintStaminaDrainPerSecond = 25f` already configured
- `runAcceleration` controls how quickly speed transitions between walk and run speeds (used in `Mathf.Lerp(_currentSpeed, targetSpeed, dt * runAcceleration)`)
- A value of 8 gives ~0.1s to reach full speed; adjust in Inspector for feel

---

### Step 2: Player Model Expansion
**File:** `Assets/Game/Script/Player/PlayerModelRefactored.cs`

**Changes:**
```csharp
// In properties section (around line 50-80)

public float WalkSpeed => _config.baseWalkSpeed;
public float RunSpeed => _config.baseRunSpeed;  // ADD THIS LINE
public float ClimbSpeed => _config.baseClimbSpeed;
```

**Notes:**
- Simple property accessor following existing pattern
- No logic needed, just exposes config value

---

### Step 3: Input Handling
**File:** `Assets/Game/Script/Player/Services/PlayerInputHandler.cs`

**Changes:**
```csharp
// Add with other input properties (around line 30-50)

public bool IsJumpPressed => !IsInputBlocked() && _inputActions.Player.Jump.WasPressedThisFrame();
public bool IsSprintHeld => !IsInputBlocked() && _inputActions.Player.Sprint.IsPressed();  // ADD THIS
public bool IsClimbPressed => !IsInputBlocked() && _inputActions.Player.Climb.WasPressedThisFrame();
```

**Improved Implementation with Input Buffering:**
```csharp
// Add with other private fields
private float _sprintPressTime = -1f;
private const float SprintInputBufferTime = 0.15f;

// Add with other input properties
public bool IsSprintHeld
{
    get
    {
        if (IsInputBlocked()) return false;
        if (_inputActions.Player.Sprint.IsPressed())
        {
            _sprintPressTime = Time.time;
            return true;
        }
        // Buffer: treat as held briefly after release to smooth stop-start jitter
        return Time.time - _sprintPressTime < SprintInputBufferTime;
    }
}
```

**Notes:**
- Sprint Input Action already exists in `Assets/InputSystem_Actions.inputactions`
- Keyboard: Left Shift
- Gamepad: Left Stick Press (L3)
- XR: Trigger
- Input buffer (0.15s) prevents jitter when sprint is tapped rapidly
- Respects `IsInputBlocked()` for UI/interaction systems

---

### Step 4: Animation Interface
**File:** `Assets/Game/Script/Player/Interfaces/IAnimationService.cs`

**Changes:**
```csharp
public interface IAnimationService
{
    void UpdateMovement(Vector3 velocity, float maxSpeed);
    
    void SetWalking(bool isWalking);
    void SetRunning(bool isRunning);  // ADD THIS LINE
    void SetClimbing(bool isClimbing);
    void SetFalling(bool isFalling);
    void SetGrounded(bool isGrounded);
    
    void SetFootIKEnabled(bool enabled);
}
```

**Notes:**
- Follows existing naming convention
- Consistent with `SetWalking()`, `SetClimbing()`, etc.

---

### Step 5: Animation Service Implementation
**File:** `Assets/Game/Script/Player/Services/PlayerAnimationService.cs`

**Changes:**

**Part A: Add animator parameter hashes (around line 12-20)**
```csharp
private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
private static readonly int VerticalHash = Animator.StringToHash("Vertical");
private static readonly int IsClimbingHash = Animator.StringToHash("isClimbing");
private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
private static readonly int IsRunningHash = Animator.StringToHash("isRunning");          // ADD THIS
private static readonly int SpeedMultiplierHash = Animator.StringToHash("SpeedMultiplier"); // ADD THIS
private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
private static readonly int IsGroundedHash = Animator.StringToHash("isGround");
```

**Part B: Add SetRunning and SetSpeedMultiplier methods (around line 70-90)**
```csharp
public void SetWalking(bool isWalking)
{
    _animator.SetBool(IsWalkingHash, isWalking);
}

// ADD THESE TWO METHODS
public void SetRunning(bool isRunning)
{
    _animator.SetBool(IsRunningHash, isRunning);
}

/// <summary>Drives blend between walk/run animations. 0=walk speed, 1=full run speed.</summary>
public void SetSpeedMultiplier(float multiplier)
{
    _animator.SetFloat(SpeedMultiplierHash, multiplier);
}

public void SetClimbing(bool isClimbing)
{
    _animator.SetBool(IsClimbingHash, isClimbing);
}
```

**Unity Animator Setup:**
1. Add `isRunning` Bool parameter
2. Add `SpeedMultiplier` Float parameter (default: 0)
3. In the Walk → Run transition: condition `isRunning == true`
4. In the Run → Walk transition: condition `isRunning == false`
5. In the Run blend tree: use `SpeedMultiplier` to blend walk/run animation clips
6. Blend times: 0.15 seconds for smooth transitions

---

### Step 6: Create RunningState Class
**File:** `Assets/Game/Script/Player/PlayerState/RunningState.cs` (NEW FILE)

**Full Implementation:**
```csharp
using UnityEngine;

namespace Game.Player.PlayerState
{
    /// <summary>
    /// Sprint/Run state with stamina-based speed degradation.
    /// Speed linearly scales from runSpeed to walkSpeed as stamina depletes.
    /// Entry: triggered from WalkingState when sprint is held + moving.
    /// Exit:  sprint released, stopped moving, or not grounded.
    /// </summary>
    public class RunningState : IPlayerState
    {
        private readonly IStateTransitioner _stateTransitioner;
        private float _currentSpeed; // Tracks accelerating/decelerating speed

        public RunningState(IStateTransitioner stateTransitioner)
        {
            _stateTransitioner = stateTransitioner;
        }

        public void Enter(PlayerModelRefactored model)
        {
            var animService = model.GetAnimationService();
            animService.SetRunning(true);
            animService.SetGrounded(true);

            // Initialize speed at current walk speed for smooth acceleration
            _currentSpeed = model.WalkSpeed;

            // Disable stamina regen while running
            model.Stats?.SetWalking(false);

            // Clear downward velocity when entering from fall
            if (model.Velocity.y < 0f)
            {
                Vector3 vel = model.Velocity;
                vel.y = 0f;
                model.Velocity = vel;
            }
        }

        public void Exit(PlayerModelRefactored model)
        {
            var animService = model.GetAnimationService();
            animService.SetRunning(false);
            animService.SetSpeedMultiplier(0f);
            // Note: WalkingState.Enter() will re-enable stamina regen via SetWalking(true)
        }

        public void HandleInput(PlayerModelRefactored model, Vector2 input)
        {
            // Input is processed in FixedUpdate
        }

        public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
        {
            var config          = model.GetConfig();
            var physicsService  = model.GetPhysicsService();
            var animService     = model.GetAnimationService();
            var cameraProvider  = model.GetCameraProvider();
            var inputHandler    = model.GetInputHandler();

            float inputMagnitude = input.magnitude;

            // === EXIT CONDITIONS ===
            // Return to walking if sprint released or stopped moving
            if (!inputHandler.IsSprintHeld || inputMagnitude < config.movementThreshold)
            {
                _stateTransitioner?.TransitionTo(new WalkingState(_stateTransitioner));
                return;
            }

            // === STAMINA PERCENTAGE ===
            float staminaPercent = 1f;
            if (model.Stats?.StaminaStat != null)
            {
                staminaPercent = Mathf.Clamp01(
                    model.Stats.StaminaStat.CurrentValue / model.Stats.StaminaStat.MaxValue);
            }
            // State stays active at 0 stamina - player moves at walk speed until sprint released

            // === SPEED WITH ACCELERATION ===
            float targetSpeed = Mathf.Lerp(model.WalkSpeed, model.RunSpeed, staminaPercent);
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.fixedDeltaTime * config.runAcceleration);

            // === MOVEMENT DIRECTION ===
            Vector3 moveDir  = cameraProvider.GetWorldDirection(input);
            Vector3 horizontal = moveDir * _currentSpeed;

            // === SLOPE PHYSICS ===
            if (physicsService.IsGrounded())
            {
                Vector3 groundNormal = physicsService.GetGroundNormal();
                float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);

                if (slopeAngle > 0.1f)
                {
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized;
                    float alignment = Vector3.Dot(slopeDirection, moveDir);

                    if (alignment > 0.1f) // Uphill
                    {
                        float normalizedSlope = Mathf.Clamp01(slopeAngle / 45f);
                        horizontal *= config.slopeCurve.Evaluate(normalizedSlope);
                    }
                    else if (alignment < -0.1f) // Downhill
                    {
                        float normalizedSlope = Mathf.Clamp01(slopeAngle / 45f);
                        horizontal *= Mathf.Lerp(1f, config.downhillSpeedMultiplier, normalizedSlope);
                    }
                }
            }

            // === FATIGUE MULTIPLIER ===
            if (model.Stats?.FatigueStat != null)
                horizontal *= model.Stats.FatigueStat.GetSpeedMultiplier();

            // === APPLY MOVEMENT ===
            model.Move(new Vector3(horizontal.x, model.Velocity.y, horizontal.z));

            // === ROTATION ===
            if (moveDir.sqrMagnitude > 0.01f)
            {
                model.Transform.forward = Vector3.Slerp(
                    model.Transform.forward,
                    moveDir,
                    Time.fixedDeltaTime * model.RotationSmoothness);
            }

            // === ANIMATION ===
            animService.UpdateMovement(horizontal, _currentSpeed);
            animService.SetSpeedMultiplier(staminaPercent); // Drives walk/run blend tree

            // === STAMINA DRAIN ===
            // Only drain when actually moving
            Vector3 horizontalVelocity = new Vector3(model.Velocity.x, 0f, model.Velocity.z);
            if (model.Stats != null && horizontalVelocity.magnitude > config.movementThreshold)
            {
                float drainPerSecond = config.sprintStaminaDrainPerSecond;

                // Fatigue multiplier on drain
                if (model.Stats.FatigueStat != null)
                    drainPerSecond *= model.Stats.FatigueStat.GetStaminaDrainMultiplier();

                // Slope multiplier on drain: uphill drains more, downhill less
                if (physicsService.IsGrounded())
                {
                    Vector3 groundNormal = physicsService.GetGroundNormal();
                    float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
                    Vector3 slopeDir = Vector3.ProjectOnPlane(moveDir, groundNormal).normalized;
                    float alignment = Vector3.Dot(slopeDir, moveDir);

                    if (alignment > 0.1f) // Uphill: up to +50% drain at 45°
                        drainPerSecond *= 1f + (slopeAngle / 45f) * 0.5f;
                    else if (alignment < -0.1f) // Downhill: up to -30% drain at 45°
                        drainPerSecond *= Mathf.Max(0.3f, 1f - (slopeAngle / 45f) * 0.3f);
                }

                model.Stats.StaminaStat.ApplyTerrainDrain(drainPerSecond * Time.fixedDeltaTime);
            }

            // === GRAVITY ===
            if (physicsService.IsGrounded())
            {
                Vector3 velocity = model.Velocity;
                velocity.y = -2f;
                model.Velocity = velocity;
            }
        }

        public void OnJump(PlayerModelRefactored model, Vector2 input)
        {
            var config         = model.GetConfig();
            var physicsService = model.GetPhysicsService();

            if (!physicsService.IsGrounded()) return;

            if (model.Stats != null &&
                !model.Stats.StaminaStat.TryConsume(config.jumpStaminaCost))
            {
                return; // Not enough stamina
            }

            // Preserve horizontal running momentum through the jump
            Vector3 currentVelocity = model.Velocity;
            physicsService.Jump(config.jumpForce);
            model.Velocity = new Vector3(currentVelocity.x, model.Velocity.y, currentVelocity.z);
            // FallingState will be entered automatically via HandleAutomaticTransitions
        }

        public void OnClimb(PlayerModelRefactored model)
        {
            _stateTransitioner?.TransitionTo(new ClimbingState(_stateTransitioner));
        }
    }
}
```

**Key Changes from Original Plan:**

1. **Acceleration:** `_currentSpeed` lerps to `targetSpeed` each frame instead of snapping instantly
2. **Jump velocity:** Horizontal momentum preserved through the jump (`currentVelocity.x/z` restored)
3. **Slope stamina drain:** Uphill +50% drain, downhill -30% drain at 45° slope
4. **SpeedMultiplier:** Drives walk/run blend tree in Animator (smoother than bool alone)
5. **0-stamina behavior:** State intentionally stays active - player moves at walk speed until sprint released
6. **Null safety:** All transitions use `?.` or explicit null check

---

### Step 7: Update Player Controller Transitions
**File:** `Assets/Game/Script/Player/PlayerControllerRefactored.cs`

**Changes:**

**Modify HandleAutomaticTransitions() - REMOVE sprint detection (moved to WalkingState):**

```csharp
private void HandleAutomaticTransitions()
{
    // Don't interrupt climbing
    if (_currentState is ClimbingState)
        return;

    // Check for falling
    if (!_physicsService.IsGrounded())
    {
        if (!(_currentState is FallingState))
        {
            // TransitionTo(new FallingState(this)); // Uncomment when ready
        }
    }
    // Return to walking when landed (from any non-walking grounded state)
    // NOTE: FallingState handles its own landing logic including sprint-held check
    else if (!(_currentState is WalkingState) && !(_currentState is RunningState))
    {
        TransitionTo(new WalkingState(this));
    }
    // Sprint check is intentionally NOT here - it lives in WalkingState.FixedUpdate()
}
```

**Why this is better:**
- Avoids competing logic where both controller AND running state manage the same transition
- Keeps state-specific transitions inside the state itself (SRP)
- `HandleAutomaticTransitions` only handles physics-driven transitions (falling/landing)

**Add using statement at top of file if missing:**
```csharp
using Game.Player.PlayerState;
```

---

### Step 8: Add Sprint Detection to WalkingState
**File:** `Assets/Game/Script/Player/PlayerState/WalkingState.cs`

**Changes:**

At the end of `FixedUpdate()`, after movement is applied, add the sprint check:

```csharp
// In WalkingState.FixedUpdate() - add at the END, after all movement logic
void IPlayerState.FixedUpdate(PlayerModelRefactored model, Vector2 input)
{
    // ... existing movement, slope, fatigue, animation, stamina logic ...

    // === SPRINT CHECK ===
    // Transition to running if sprint held and player is actively moving
    var inputHandler = model.GetInputHandler();
    var config = model.GetConfig();
    if (inputHandler.IsSprintHeld && input.magnitude > config.movementThreshold)
    {
        _stateTransitioner?.TransitionTo(new RunningState(_stateTransitioner));
    }
}
```

**Notes:**
- Sprint check placed AFTER movement logic so one walk frame plays before transitioning
- Uses same `movementThreshold` as WalkingState for consistency
- `_stateTransitioner` is already available in WalkingState from its constructor

---

### Step 9: Update FallingState Landing Logic
**File:** `Assets/Game/Script/Player/PlayerState/FallingState.cs`

**Changes:**

In the grounded landing check inside `FixedUpdate()`, add sprint-landing logic:

```csharp
// In FallingState.FixedUpdate() - modify the landing detection block
// BEFORE:
if (physicsService.IsGrounded())
{
    _stateTransitioner?.TransitionTo(new WalkingState(_stateTransitioner));
}

// AFTER:
if (physicsService.IsGrounded())
{
    var inputHandler = model.GetInputHandler();
    var input = model.GetLastInput(); // or pass input from parameter
    var config = model.GetConfig();

    // If sprint held and moving on landing, go straight to running
    if (inputHandler.IsSprintHeld && input.magnitude > config.movementThreshold)
    {
        _stateTransitioner?.TransitionTo(new RunningState(_stateTransitioner));
    }
    else
    {
        _stateTransitioner?.TransitionTo(new WalkingState(_stateTransitioner));
    }
}
```

**Player Experience Benefit:**
Holding sprint while falling lands directly in RunningState instead of flickering Walk → Run.

---

## Code Patterns

### Pattern 1: State Entry/Exit Lifecycle
```csharp
public void Enter(PlayerModelRefactored model)
{
    // 1. Set animator flags
    animService.SetRunning(true);
    animService.SetGrounded(true);
    
    // 2. Configure systems (stamina, etc.)
    model.Stats?.SetWalking(false); // Disable regen
    
    // 3. Initialize physics state
    if (model.Velocity.y < 0f)
    {
        model.Velocity = new Vector3(model.Velocity.x, 0f, model.Velocity.z);
    }
}

public void Exit(PlayerModelRefactored model)
{
    // 1. Clear animator flags
    animService.SetRunning(false);
    
    // 2. Restore systems handled by next state
    // (WalkingState will call SetWalking(true))
}
```

### Pattern 2: Safe State Transitions
```csharp
// Always null-check and pass self-reference
if (_stateTransitioner != null)
{
    _stateTransitioner.TransitionTo(new WalkingState(_stateTransitioner));
}
```

### Pattern 3: Stamina Drain with Fatigue
```csharp
if (model.Stats != null && isMoving)
{
    float baseDrain = config.sprintStaminaDrainPerSecond;
    
    // Apply fatigue multiplier
    if (model.Stats.FatigueStat != null)
    {
        baseDrain *= model.Stats.FatigueStat.GetStaminaDrainMultiplier();
    }
    
    // Drain per frame
    model.Stats.StaminaStat.ApplyTerrainDrain(baseDrain * Time.fixedDeltaTime);
}
```

### Pattern 4: Speed Scaling with Stamina
```csharp
// Get stamina percentage (0.0 to 1.0)
float staminaPercent = 1f;
if (model.Stats?.StaminaStat != null)
{
    staminaPercent = Mathf.Clamp01(
        model.Stats.StaminaStat.CurrentValue / model.Stats.StaminaStat.MaxValue);
}
// Note: state stays active at 0% - player moves at walk speed until sprint released

// Target speed based on stamina
float targetSpeed = Mathf.Lerp(model.WalkSpeed, model.RunSpeed, staminaPercent);
```

### Pattern 5: Smooth Acceleration
```csharp
// Instance field - persists between FixedUpdate calls
private float _currentSpeed;

// Initialize in Enter()
_currentSpeed = model.WalkSpeed;

// Smooth toward target each physics frame
float targetSpeed = Mathf.Lerp(model.WalkSpeed, model.RunSpeed, staminaPercent);
_currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.fixedDeltaTime * config.runAcceleration);

// Use _currentSpeed for movement (not targetSpeed directly)
Vector3 horizontal = moveDir * _currentSpeed;
```

### Pattern 6: Preserve Jump Momentum
```csharp
// Capture horizontal velocity BEFORE applying jump force
Vector3 currentVelocity = model.Velocity;

// Apply jump (only changes Y)
physicsService.Jump(config.jumpForce);

// Restore horizontal - player keeps running momentum mid-air
model.Velocity = new Vector3(currentVelocity.x, model.Velocity.y, currentVelocity.z);
```

---

## Testing & Verification

### Manual Testing Checklist

#### Basic Functionality
- [ ] Press Left Shift while moving → player accelerates smoothly to run speed
- [ ] Release Left Shift → player decelerates back to walk speed
- [ ] Hold Left Shift with no movement input → stays in walking state (no sprint transition)
- [ ] Sprint while moving → stamina bar drains continuously
- [ ] Stamina depletes to 0% → player continues moving at walk speed (state stays active)
- [ ] Stop sprinting and stand still → stamina regenerates
- [ ] Stop sprinting and keep walking → stamina regenerates
- [ ] Acceleration feels smooth (no instant speed snap)

#### Speed Scaling
- [ ] Full stamina (100%) → max run speed (4.5f)
- [ ] Half stamina (50%) → medium speed (~3.75f)
- [ ] Low stamina (25%) → slow run (~3.375f)
- [ ] Empty stamina (0%) → walk speed (3.0f)
- [ ] Speed transition is smooth (no stuttering)

#### State Transitions
- [ ] Walk → Run: Smooth transition when sprint held + moving (detected in WalkingState.FixedUpdate)
- [ ] Run → Walk: Clean transition when sprint released
- [ ] Run → Fall: Works when walking off edge while sprinting
- [ ] Fall → Run: Lands in RunningState when holding sprint on landing
- [ ] Fall → Walk: Lands in WalkingState when NOT holding sprint
- [ ] Run → Climb: Can initiate climb while sprinting
- [ ] Climb → Walk: Returns to walk (not run) after climb ends
- [ ] No transition flicker (Walk→Run→Walk in same frame)

#### Stamina Integration
- [ ] Running disables stamina regeneration
- [ ] Walking enables stamina regeneration
- [ ] Idle enables stamina regeneration
- [ ] Fatigue increases stamina drain during running
- [ ] Stamina drains faster on uphill slopes (via fatigue)

#### Slope Physics
- [ ] Running uphill → noticeable speed reduction
- [ ] Running downhill → slight speed boost
- [ ] Running on flat ground → normal speed
- [ ] Slope effects combined with stamina scaling feel natural

#### Animation (requires Unity Animator setup)
- [ ] `isRunning` parameter → true when running
- [ ] `isRunning` parameter → false when not running
- [ ] `Horizontal` and `Vertical` values update correctly
- [ ] Smooth blend between walk and run animations

#### Edge Cases
- [ ] Sprint while at 0 stamina → moves at walk speed
- [ ] Jump while sprinting → works, costs stamina
- [ ] Jump while sprinting with no stamina → doesn't jump
- [ ] Interact/open UI while sprinting → sprint canceled (input blocked)
- [ ] Resume sprinting after interaction → works normally

#### Input Devices
- [ ] Keyboard: Left Shift works
- [ ] Gamepad: Left Stick Press (L3) works
- [ ] Can sprint and look around simultaneously
- [ ] Sprint input respects input blocking (UI open, interactions)

### Performance Testing
- [ ] No frame drops during sprint state
- [ ] State transitions have no noticeable lag
- [ ] Stamina calculations don't cause GC allocations
- [ ] Animator updates don't cause performance issues

### Debug Verification

Add temporary debug logs to verify:

```csharp
// In RunningState.FixedUpdate()
Debug.Log($"Running - Stamina: {staminaPercent:P0}, Target: {targetSpeed:F2}, Actual: {_currentSpeed:F2}");

// In WalkingState.FixedUpdate() - sprint check
Debug.Log($"[WalkingState] Sprint detected → transitioning to RunningState");
```

Expected console output:
```
[WalkingState] Sprint detected → transitioning to RunningState
Running - Stamina: 100%, Target: 4.50, Actual: 3.10  (accelerating)
Running - Stamina: 100%, Target: 4.50, Actual: 4.12  (still accelerating)
Running - Stamina: 100%, Target: 4.50, Actual: 4.48  (nearly full speed)
Running - Stamina: 95%,  Target: 4.43, Actual: 4.45
...
Running - Stamina: 0%,   Target: 3.00, Actual: 3.02
Running - Stamina: 0%,   Target: 3.00, Actual: 3.00  (walk speed, state stays active)
[WalkingState] entering (sprint released)
```

---

## Integration Points

### Existing Systems Integration

#### 1. Stamina System
**File:** `Assets/Game/Script/Player/Stat/PlayerStats.cs`

**Current Integration:**
- `SetWalking(true/false)` controls stamina regen
- `StaminaStat.ApplyTerrainDrain()` drains stamina
- `FatigueStat.GetStaminaDrainMultiplier()` scales drain

**Running State Usage:**
- `Enter()`: `SetWalking(false)` → disable regen
- `Exit()`: Implicitly handled by WalkingState
- `FixedUpdate()`: `ApplyTerrainDrain(25 * dt * fatigueMultiplier)`

**No Changes Required** - existing API sufficient

#### 2. Animation System
**Files:** 
- `Assets/Game/Script/Player/Services/PlayerAnimationService.cs`
- `Assets/Game/Script/Player/Interfaces/IAnimationService.cs`

**Changes Required:**
- ✅ Add `SetRunning(bool)` method
- ✅ Add `IsRunningHash` parameter
- ✅ Add `SetSpeedMultiplier(float)` method (drives blend tree)
- ✅ Add `SpeedMultiplierHash` parameter

**Unity Animator Setup (Manual):**
1. Open Player Animator Controller
2. Add Bool Parameter: `isRunning`
3. Add Float Parameter: `SpeedMultiplier` (default: 0)
4. Create transitions:
   - Walk → Run: When `isRunning == true`
   - Run → Walk: When `isRunning == false`
5. Inside Run state: use Blend Tree driven by `SpeedMultiplier` (0=walk anim, 1=run anim)
6. Blend times: 0.15 seconds for smooth transitions

#### 3. Input System
**File:** `Assets/Game/Script/Player/Services/PlayerInputHandler.cs`

**Current Status:**
- Sprint action exists in `InputSystem_Actions.inputactions`
- Not wired up in PlayerInputHandler

**Changes Required:**
- ✅ Add `IsSprintHeld` property

**Existing Input Actions:**
- Move: WASD / Left Stick
- Jump: Space / A Button
- Sprint: Left Shift / L3 ← **Already configured!**
- Climb: E / B Button

#### 4. Physics Service
**File:** `Assets/Game/Script/Player/Services/PlayerPhysicsService.cs`

**Current Integration:**
- `IsGrounded()` - State exit condition check
- `GetGroundNormal()` - Slope angle calculation
- `Jump()` - Available while running

**No Changes Required** - existing API sufficient

#### 5. Camera System
**File:** `Assets/Game/Script/Player/Services/CinemachineCameraProvider.cs`

**Current Integration:**
- `GetWorldDirection(Vector2)` - Converts input to world space

**No Changes Required** - existing API sufficient

#### 6. Fatigue System
**File:** `Assets/Game/Script/Player/Stat/FatigueStat.cs`

**Current Integration:**
- `GetSpeedMultiplier()` - Reduces movement speed
- `GetStaminaDrainMultiplier()` - Increases stamina drain

**Running State Usage:**
- Speed penalty applied after base speed calculation
- Stamina drain multiplier applied to sprint drain rate

**No Changes Required** - existing API sufficient

---

## Future Enhancements

### Priority 1: Polish & Feel
- **Momentum System**: Acceleration/deceleration curves for smoother feel
- **Footstep Audio**: Different footstep sounds for walk vs run
- **Camera FOV**: Slight FOV increase while sprinting (e.g., 60° → 65°)
- **Dust Particles**: Ground dust/snow kicked up while running
- **Animation Improvements**: Speed-matched footsteps, lean animations

### Priority 2: Gameplay Features
- **Sliding Mechanic**: Press crouch while sprinting downhill
- **Stamina HUD Warning**: Visual/audio cue when stamina < 25%
- **Sprint Toggle Option**: Allow toggle sprint in settings
- **Sprint Cooldown**: Brief cooldown after stamina depletes (e.g., 2 seconds)
- **Weather Effects**: Reduced run speed in rain/snow

### Priority 3: Advanced Systems
- **Weapon Weight**: Equipped items affect run speed
- **Injury System**: Leg injuries reduce max run speed
- **Terrain-Based Stamina**: Different drain rates for sand, mud, snow
- **Buff/Debuff System**: Potions/foods that affect sprint speed/stamina
- **Achievement Integration**: Track total distance sprinted

### Priority 4: Multiplayer Considerations
- **Network Prediction**: Client-side prediction for run state
- **Lag Compensation**: Stamina sync across network
- **Animation Sync**: Ensure run animations sync properly

---

## Appendix

### A. Configuration Reference

```csharp
// PlayerConfig.cs values relevant to running
public float baseWalkSpeed = 3f;                    // Walk speed
public float baseRunSpeed = 4.5f;                   // Max run speed (NEW)
public float runAcceleration = 8f;                  // Speed ramp-up rate (NEW)
public float sprintStaminaDrainPerSecond = 25f;     // Already exists
public float jumpStaminaCost = 20f;                 // Jump cost
public float movementThreshold = 0.1f;              // Minimum input to move
public float rotationSmoothness = 10f;              // Rotation speed
public AnimationCurve slopeCurve;                   // Tobler's function
public float downhillSpeedMultiplier = 1.2f;        // Downhill speed boost
```

### B. Animator Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Horizontal` | Float | X-axis movement speed |
| `Vertical` | Float | Z-axis movement speed |
| `isWalking` | Bool | True when in WalkingState |
| `isRunning` | Bool | True when in RunningState (NEW) |
| `SpeedMultiplier` | Float | 0=walk speed, 1=full run speed; drives blend tree (NEW) |
| `isClimbing` | Bool | True when in ClimbingState |
| `isFalling` | Bool | True when in FallingState |
| `isGround` | Bool | True when grounded |

### C. State Responsibilities Matrix

| Responsibility | WalkingState | RunningState | ClimbingState | FallingState |
|----------------|--------------|--------------|---------------|--------------|
| Stamina Regen | ✅ Enabled | ❌ Disabled | ❌ Disabled | N/A |
| Stamina Drain | Slow (moving) | Fast (25f/s) | Medium (10f/s) | None |
| Jump Allowed | ✅ Yes | ✅ Yes | ❌ No | ❌ No |
| Slope Physics | ✅ Yes | ✅ Yes | N/A | N/A |
| Fatigue Effects | ✅ Yes | ✅ Yes | ✅ Yes | N/A |
| Rotation | ✅ Smooth | ✅ Smooth | ✅ Toward wall | ✅ Maintains |

### D. Useful Debug Commands

```csharp
// Add to PlayerStats.cs for testing
[ContextMenu("Set Stamina to 50%")]
private void DebugSetStaminaHalf()
{
    StaminaStat.SetValue(StaminaStat.MaxValue * 0.5f);
}

[ContextMenu("Set Stamina to 10%")]
private void DebugSetStaminaLow()
{
    StaminaStat.SetValue(StaminaStat.MaxValue * 0.1f);
}

[ContextMenu("Refill Stamina")]
private void DebugRefillStamina()
{
    StaminaStat.SetValue(StaminaStat.MaxValue);
}
```

### E. Common Issues & Solutions

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| Speed doesn't change | Config not saved | Check Inspector, save scene |
| Sprint key doesn't work | Input not wired | Verify `IsSprintHeld` property added |
| Stamina not draining | Stats null | Check PlayerStats registered in ServiceContainer |
| Animation not playing | Parameter missing | Add `isRunning` bool to Animator Controller |
| State stuck in running | Transition logic error | Check exit conditions in HandleAutomaticTransitions |
| Sliding on stop | Velocity not cleared | Verify Exit() method implementation |

---

## Implementation Checklist

### Code Changes
- [ ] Add `baseRunSpeed = 4.5f` and `runAcceleration = 8f` to PlayerConfig.cs
- [ ] Add `RunSpeed` property to PlayerModelRefactored.cs
- [ ] Add `IsSprintHeld` property with input buffer to PlayerInputHandler.cs
- [ ] Add `SetRunning()` and `SetSpeedMultiplier()` to IAnimationService.cs
- [ ] Add `IsRunningHash`, `SpeedMultiplierHash`, `SetRunning()`, `SetSpeedMultiplier()` to PlayerAnimationService.cs
- [ ] Create RunningState.cs with full implementation (including `_currentSpeed` field)
- [ ] Add sprint check at END of WalkingState.FixedUpdate() (Step 8)
- [ ] Add sprint-held landing logic to FallingState landing detection (Step 9)
- [ ] Update HandleAutomaticTransitions() in PlayerControllerRefactored.cs (remove sprint check)

### Unity Editor Setup
- [ ] Open Player Animator Controller
- [ ] Add `isRunning` Bool parameter
- [ ] Add `SpeedMultiplier` Float parameter (default: 0)
- [ ] Create Walk → Run transition (condition: isRunning == true, blend: 0.15s)
- [ ] Create Run → Walk transition (condition: isRunning == false, blend: 0.15s)
- [ ] Set up Blend Tree inside Run state using SpeedMultiplier
- [ ] Test animation transitions in Animator window

### Testing
- [ ] Run through Manual Testing Checklist
- [ ] Test with keyboard (Left Shift)
- [ ] Test with gamepad (L3)
- [ ] Verify stamina drain rate (should be ~25/s on flat ground)
- [ ] Verify uphill drains faster (~37.5/s at 45°)
- [ ] Verify speed scaling (smooth curve from 4.5f → 3.0f as stamina depletes)
- [ ] Verify acceleration (no instant speed snap)
- [ ] Test jump horizontal momentum preservation while running
- [ ] Test landing while holding sprint (should enter RunningState)
- [ ] Test all state transitions
- [ ] Profile performance (no frame drops expected)

### Documentation
- [ ] Update PLAYER_SYSTEM_OVERVIEW.md with RunningState
- [ ] Add RunningState to CODEBASE_DEPENDENCY_MAP.md
- [ ] Update CODEBASE_MERMAID_DIAGRAMS.md state machine diagram
- [ ] Mark this plan as ✅ Implemented

---

**End of Implementation Plan**

**Last Updated:** February 20, 2026 (improved)  
**Status:** 📋 Ready for Implementation  
**Estimated Time:** 2-4 hours (code) + 1 hour (testing/polish)
