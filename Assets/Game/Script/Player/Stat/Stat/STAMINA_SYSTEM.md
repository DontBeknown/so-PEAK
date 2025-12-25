# Stamina & Fatigue System Documentation

## Overview
This document explains the comprehensive stamina and fatigue system that governs player endurance, movement speed, and recovery mechanics based on terrain and activity. The system uses Tobler's hiking function for realistic terrain-based movement and a multi-component fatigue model.

---

## Table of Contents
1. [Stamina System](#stamina-system)
2. [Fatigue System](#fatigue-system)
3. [Terrain Effects & Tobler's Function](#terrain-effects--toblers-function)
4. [Configuration Parameters](#configuration-parameters)
5. [System Integration](#system-integration)
6. [Examples & Scenarios](#examples--scenarios)

---

## Stamina System

### What is Stamina?
Stamina represents the player's immediate energy reserves used for actions like:
- Walking/Running
- Jumping (instant cost)
- Climbing
- Moving on slopes

### Stamina Mechanics

#### Drain Conditions
- **Walking on any terrain**: Constant base drain rate
- **Climbing**: Fixed drain rate per second
- **Jumping**: Instant stamina cost
- **Fatigue multiplier**: 1x to 2x based on current fatigue level

#### Regeneration
- **Active when**: Standing still or walking (after cooldown)
- **Blocked by**: Recent stamina drain actions
- **Cooldown**: 1 second after last drain before regen starts
- **Rate**: 15 stamina per second (configurable)

#### Formula
```
Stamina Drain = Base Drain × Fatigue Multiplier
Fatigue Multiplier = 1 + (currentFatigue / maxFatigue)  // 1x at 0%, 2x at 100%
```

---

## Fatigue System

### What is Fatigue?
Fatigue represents accumulated exhaustion over time. Unlike stamina which regenerates quickly, fatigue builds up gradually during activity and requires rest or sleep to fully recover.

### Mathematical Model

The fatigue system uses a three-component formula:

```
fatigue = fatigue_rate_time × Δt + fatigue_rate_elev × |slope| × Δt + fatigue_rate_speed × speed × Δt
```

Where:
- **fatigue_rate_time** = Base time-dependent fatigue rate (default: 0.12/sec)
- **fatigue_rate_elev** = Elevation/slope fatigue coefficient (default: 0.0005)
- **fatigue_rate_speed** = Speed-dependent fatigue coefficient (default: 0.1)
- **|slope|** = Absolute value of slope gradient
- **speed** = Current movement speed (after Tobler adjustment)
- **Δt** = Time delta (frame time)

### Fatigue Components

#### 1. Time Component
```
Time_Fatigue = 0.12 × Δt
```
Constant fatigue accumulation when moving, representing basic exertion.

#### 2. Elevation Component
```
Elevation_Fatigue = 0.0005 × |slope_gradient| × Δt
```
Additional fatigue from climbing or descending slopes. Uses absolute gradient so both uphill and downhill cause fatigue.

#### 3. Speed Component
```
Speed_Fatigue = 0.1 × actual_speed × Δt
```
Fatigue from movement speed - faster movement causes more fatigue.

### Fatigue Recovery

**No Automatic Recovery**: Fatigue does not decrease automatically when standing still.

**Full Rest Only**: Call `FullRest()` method to clear fatigue completely (e.g., sleeping in bed, sitting by campfire).

```csharp
playerStats.FullRest();  // Clears all fatigue
```

### Fatigue Effects

#### 1. Movement Speed Penalty
Fatigue reduces movement speed when it exceeds the threshold using a travel time formula.

**Threshold:** 70% fatigue (configurable)

**Formula:**
```
travel_time += (f_local - fatigue_limit) × 5.0

If Fatigue < Threshold:
    Speed Multiplier = 1.0 (full speed)
    
If Fatigue ≥ Threshold:
    Excess_Fatigue = (currentFatigue - threshold) / maxFatigue
    Additional_Time = Excess_Fatigue × 5.0
    Speed Multiplier = Max(0.2, 1 / (1 + Additional_Time))
```

**Speed Reduction:**
- 70% fatigue: 100% speed (no penalty)
- 80% fatigue: 67% speed
- 90% fatigue: 50% speed
- 100% fatigue: 40% speed (minimum 20%)

#### 2. Stamina Drain Multiplier
Higher fatigue increases stamina consumption.

**Formula:**
```
Fatigue_Multiplier = 1 + (currentFatigue / maxFatigue)
Stamina_Drain = Base_Drain × Fatigue_Multiplier
```

**Drain Increase:**
- 0% fatigue: 1.0x stamina drain
- 50% fatigue: 1.5x stamina drain
- 100% fatigue: 2.0x stamina drain

---

## Terrain Effects & Tobler's Function

### Tobler's Hiking Function

The system uses Tobler's hiking function to calculate realistic movement speed based on terrain slope:

```
speed = base_speed × exp(-3.5 × |slope_gradient + 0.05|)
```

This formula models real-world hiking speeds where:
- Slight downhill (gradient ≈ -0.05) is optimal
- Flat ground is slightly slower
- Both steep uphill and downhill significantly reduce speed

### Speed Multiplier Remapping

To prevent extreme slowdown, the Tobler result is remapped:

```
flat_ground_value = exp(-3.5 × 0.05) ≈ 0.839
normalized_value = tobler_result / flat_ground_value
final_multiplier = Lerp(minSpeed, maxSpeed, Clamp01(normalized_value))
```

**Default Range:** 0.7 to 1.0 (configurable via `minSlopeSpeedMultiplier` and `maxSlopeSpeedMultiplier`)

### Combined Speed Calculation

Slope and fatigue penalties combine **additively**:

```
tobler_reduction = 1 - tobler_multiplier
fatigue_reduction = 1 - fatigue_multiplier
total_reduction = Clamp01(tobler_reduction + fatigue_reduction)
final_speed = base_speed × (1 - total_reduction)
```

**Example:**
- Tobler: 0.8 (20% reduction)
- Fatigue: 0.7 (30% reduction)
- Combined: 50% reduction → **50% final speed**

### Slope Detection
The system detects terrain slope using raycasting:

1. **Raycast downward** from player position
2. **Get ground normal** vector
3. **Calculate angle** between ground normal and up vector
4. **Convert to gradient** using tangent

```csharp
float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
float slopeGradient = Tan(slopeAngle × Deg2Rad);
```

### Movement Direction
Determines if player is moving uphill or downhill:

```csharp
float movementDot = Vector3.Dot(movement.normalized, groundNormal);
bool isMovingUphill = movementDot < 0f;

// Apply sign to gradient
if (!isMovingUphill)
    slopeGradient = -slopeGradient;
```

- **Negative dot product** = Uphill (positive gradient)
- **Positive dot product** = Downhill (negative gradient)

---

## Configuration Parameters

### PlayerConfig Values

#### Movement
```csharp
[Header("Movement")]
walkSpeed = 3f                           // Base walking speed
minSlopeSpeedMultiplier = 0.7f          // Minimum speed on steep slopes (70%)
maxSlopeSpeedMultiplier = 1.0f          // Maximum speed on flat/downhill (100%)
```

#### Stamina Settings
```csharp
[Header("Stamina")]
jumpStaminaCost = 20f                    // Instant cost per jump
sprintStaminaDrainPerSecond = 25f        // Drain rate when sprinting
climbStaminaDrainPerSecond = 10f         // Drain rate when climbing

[Header("Stamina Regen")]
staminaRegenPerSecond = 15f              // Recovery rate
staminaDrainCooldown = 1f                // Delay before regen starts
```

#### Terrain Stamina Drain
```csharp
[Header("Terrain Stamina Drain")]
baseMovementStaminaDrain = 0.5f          // Constant drain when moving
movementThreshold = 0.1f                 // Min speed to trigger drain
```

#### Fatigue System
```csharp
[Header("Fatigue System")]
maxFatigue = 100f                        // Maximum fatigue value
fatigueRateTime = 0.12f                  // Time component rate
fatigueRateElev = 0.0005f                // Elevation component coefficient
fatigueRateSpeed = 0.1f                  // Speed component coefficient
fatigueSpeedPenaltyThreshold = 70f       // When speed penalty starts (%)
```

---

## System Integration

### Update Flow

```
Every FixedUpdate (WalkingState):
1. Get movement input and calculate base velocity
2. Calculate slope effects via raycast
3. Apply Tobler's function for slope speed
4. Calculate fatigue speed penalty
5. Combine penalties additively
6. Pass drain and parameters to StaminaStat
7. Apply final movement

Every Frame (StaminaStat.Tick):
1. Calculate fatigue components (time + elevation + speed)
2. Accumulate fatigue if moving
3. Apply stamina drain with fatigue multiplier
4. Regenerate stamina after cooldown (if walking)
```

### State Transitions

**Entering Walking State:**
- Enable stamina regeneration
- Fatigue accumulation begins when moving

**Exiting Walking State:**
- Disable stamina regeneration
- Fatigue accumulation stops

**Standing Still:**
- Stamina regenerates (after cooldown)
- Fatigue does NOT recover automatically

**Full Rest:**
- Call `FullRest()` to clear all fatigue
- Triggered by: sleeping, camping, resting at specific locations

---

## Examples & Scenarios

### Scenario 1: Flat Ground Walking (30 seconds)
```
Base Speed: 5.0 m/s
Tobler Multiplier: 1.0x (flat ground)
Fatigue Speed Penalty: 1.0x (below threshold)
Final Speed: 5.0 m/s

Stamina: 100 → 85 (drain: 0.5/sec)
Fatigue Components:
  - Time: 0.12/sec × 30s = 3.6
  - Elevation: 0 (flat)
  - Speed: 0.1 × 5.0 × 30s = 15
  - Total: 18.6 fatigue
```

### Scenario 2: Steep Uphill 30° (30 seconds)
```
Base Speed: 5.0 m/s
Slope Gradient: ~0.577
Tobler Multiplier: 0.75x
Fatigue Speed Penalty: 1.0x (below threshold)
Final Speed: 3.75 m/s

Stamina: 100 → 85 (drain: 0.5/sec, then × fatigue multiplier)
Fatigue Components:
  - Time: 0.12/sec × 30s = 3.6
  - Elevation: 0.0005 × 0.577 × 30s = 0.009
  - Speed: 0.1 × 3.75 × 30s = 11.25
  - Total: 14.86 fatigue
```

### Scenario 3: Extended Climb with Fatigue Buildup (120 seconds)
```
Base Speed: 5.0 m/s
Initial: Tobler 0.75x, Fatigue 1.0x → Speed: 3.75 m/s

After 60s (Fatigue ~30):
  Stamina Drain Multiplier: 1.3x
  Speed: Still 3.75 m/s (below threshold)

After 90s (Fatigue ~75, EXCEEDS 70% threshold):
  Fatigue Speed Penalty: 0.83x
  Combined: 0.75 - 0.25 + 0.83 - 0.17 = 0.58x
  Speed: 2.9 m/s
  Stamina Drain: 0.5 × 1.75 = 0.875/sec

After 120s (Fatigue ~95):
  Fatigue Speed Penalty: 0.44x
  Combined: 0.75 - 0.25 + 0.44 - 0.56 = 0.38x
  Speed: 1.9 m/s (significantly slowed)
  Stamina: Nearly depleted
```

### Scenario 4: Rest & Recovery
```
Duration: Full rest (sleep/campfire)
Call: playerStats.FullRest()
Fatigue: 95 → 0 (instant)
Stamina: Regenerates at 15/sec
Recovery Time: ~7 seconds to full stamina
```

---

## Key Differences: Stamina vs Fatigue

| Aspect | Stamina | Fatigue |
|--------|---------|---------|
| **Purpose** | Immediate energy | Long-term endurance |
| **Recovery Speed** | Fast (15/sec) | No auto-recovery |
| **Drain Speed** | Constant + fatigue multiplier | 3-component accumulation |
| **Reset Time** | ~7 seconds | Only via FullRest() |
| **Affects** | Action availability | Speed & stamina drain multiplier |
| **Terrain Impact** | Indirect (via fatigue) | Direct (elevation component) |
| **Cooldown** | Yes (1 sec) | No cooldown |

---

## Debug & Monitoring

Use `TerrainSlopeDebugger` component to monitor in real-time:

### Displayed Information
- **Raycast Hit**: Ground detection status
- **Slope Angle**: Current terrain angle in degrees
- **Direction**: Uphill ↗ or Downhill ↘
- **Tobler Multiplier**: Slope speed reduction
- **Base/Theoretical/Actual Speed**: Speed breakdown
- **Slope Gradient**: Numerical gradient value
- **Stamina**: Current/Max values

### Fatigue Breakdown
- **Time Component**: 0.120/sec constant
- **Elevation Component**: Based on slope gradient
- **Speed Component**: Based on actual movement speed
- **Total Rate**: Sum of all components per second

### Color Coding
- 🟢 **Green**: < 50% fatigue (healthy)
- 🟡 **Yellow**: 50-70% fatigue (tired)
- 🔴 **Red**: > 70% fatigue (exhausted, speed penalty active)

---

## Tuning Guidelines

### For Easier Gameplay:
```
baseMovementStaminaDrain = 0.3
fatigueRateTime = 0.08
fatigueRateElev = 0.0003
fatigueRateSpeed = 0.05
fatigueSpeedPenaltyThreshold = 80
minSlopeSpeedMultiplier = 0.8
```

### For Realistic Simulation:
```
baseMovementStaminaDrain = 0.5
fatigueRateTime = 0.12
fatigueRateElev = 0.0005
fatigueRateSpeed = 0.1
fatigueSpeedPenaltyThreshold = 70
minSlopeSpeedMultiplier = 0.7
```

### For Hardcore Challenge:
```
baseMovementStaminaDrain = 1.0
fatigueRateTime = 0.2
fatigueRateElev = 0.001
fatigueRateSpeed = 0.15
fatigueSpeedPenaltyThreshold = 60
minSlopeSpeedMultiplier = 0.5
maxFatigue = 150
```

---

## Technical Implementation

### Key Methods

**StaminaStat.cs:**
```csharp
void Tick(float deltaTime)                                          // Main update loop
void ApplyTerrainDrain(float drain, float gradient, float speed)  // Apply slope effects
float GetFatigueSpeedPenalty(float threshold)                      // Calculate speed penalty
void FullRest()                                                     // Clear all fatigue
```

**WalkingState.cs:**
```csharp
float CalculateSlopeEffects(...)  // Detect terrain and calculate Tobler multiplier
```

**TerrainSlopeDebugger.cs:**
```csharp
void CalculateSlopeInfo()  // Real-time slope and speed calculations
void OnGUI()               // Display debug information
```

---

## Version History

**v2.0** - December 24, 2025
- Implemented Tobler's hiking function for realistic slope-based speed
- Changed to 3-component fatigue formula (time + elevation + speed)
- Removed automatic fatigue recovery (FullRest() only)
- Updated speed penalty to use travel time formula
- Added configurable min/max speed multipliers
- Changed to additive penalty combination
- Added fatigue multiplier to stamina drain (1x to 2x)
- Enhanced TerrainSlopeDebugger with component breakdown

**v1.0** - December 23, 2025
- Initial implementation with basic slope-based stamina drain

---

## Credits
Based on Tobler's hiking function (1993) and real-world fatigue modeling principles adapted for game mechanics to provide realistic yet enjoyable endurance management.
