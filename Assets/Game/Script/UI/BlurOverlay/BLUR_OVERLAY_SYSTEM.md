# Blur Overlay System Design

## Overview
A dynamic blur overlay effect that responds to player survival stats (hunger and thirst), providing visual feedback when the player is in critical condition and gradually fading as they recover.

## System Goals
1. Provide clear visual feedback when player is starving or dehydrated
2. Smooth fade-in/fade-out transitions for immersive experience
3. Intensity scales with severity of the condition
4. Non-intrusive but noticeable enough to alert the player

## Technical Architecture

### SOLID Principles Implementation

This system follows **SOLID principles** for maintainability, testability, and extensibility:

- **Single Responsibility**: Each class has one clear purpose
  - `IBlurIntensityCalculator` → Calculate blur intensity
  - `IBlurEffect` → Render blur effect
  - `BlurOverlayController` → Orchestrate the system
  
- **Open/Closed**: Open for extension, closed for modification
  - Easy to add new calculators (e.g., health-based, fatigue-based)
  - Easy to add new effects (e.g., different animation styles)
  
- **Liskov Substitution**: Interface implementations are interchangeable
  - Any `IBlurIntensityCalculator` works with the controller
  - Any `IBlurEffect` works with the controller
  
- **Interface Segregation**: Focused, minimal interfaces
  - Each interface has only the methods needed for its purpose
  
- **Dependency Inversion**: Depends on abstractions, not concrete classes
  - Controller depends on interfaces, not concrete implementations
  - Components injected via constructors

### Components

#### 1. IBlurIntensityCalculator (Interface)
**Purpose:** Abstracts blur intensity calculation logic

**Methods:**
```csharp
float CalculateIntensity()    // Returns target intensity (0-1)
void Initialize()             // Setup and subscribe to events
void Cleanup()                // Unsubscribe and cleanup
```

#### 2. SurvivalStatBlurCalculator (Implementation)
**Purpose:** Calculates blur intensity based on hunger and thirst stats

**Responsibilities:**
- Monitor hunger and thirst values
- Calculate intensity using threshold-based algorithm
- Fire events when intensity changes

**Configuration:**
```csharp
- float hungerCriticalThreshold = 30f  // Below this, blur starts
- float thirstCriticalThreshold = 30f  // Below this, blur starts
- float hungerSevereThreshold = 10f    // Maximum blur
- float thirstSevereThreshold = 10f    // Maximum blur
- float minBlurIntensity = 0f          // Minimum intensity
- float maxBlurIntensity = 0.8f        // Maximum intensity
- bool useWorstStat = true             // Use max vs average
```

#### 3. IBlurEffect (Interface)
**Purpose:** Abstracts blur rendering/application

**Methods:**
```csharp
void SetTargetIntensity(float target, bool isFadingIn)  // Apply blur
float CurrentIntensity { get; }                         // Get current value
void Initialize()                                       // Setup effect
void Cleanup()                                          // Cleanup resources
```

#### 4. DOTweenBlurEffect (Implementation)
**Purpose:** Applies blur using DOTween for smooth animations

**Responsibilities:**
- Smooth transitions using DOTween
- Support multiple blur methods (alpha, material property, both)
- Different speeds/eases for fade-in vs fade-out

**Configuration:**
```csharp
- Image blurOverlayImage              // UI Image reference
- float fadeInDuration = 0.67f        // Fade-in time
- float fadeOutDuration = 0.4f        // Fade-out time
- Ease fadeInEase = InQuad            // Ease curve for fade-in
- Ease fadeOutEase = OutQuad          // Ease curve for fade-out
- BlurMethod blurMethod = Alpha       // How to apply blur
- string materialBlurProperty         // Material property name
```

**Blur Methods:**
- **Alpha**: Use Image alpha channel (simple, works with any blur texture)
- **MaterialProperty**: Control shader property (e.g., `_BlurAmount`)
- **Both**: Use both alpha and material property together

#### 5. BlurOverlayController (Orchestrator)
**Purpose:** Main controller that coordinates the system

**Responsibilities:**
- Initialize and configure components
- Coordinate between calculator and effect
- Provide public API for manual control
- Periodic updates

**Configuration:**
```csharp
- Image blurOverlayImage               // Reference to UI element
- All calculator settings              // Thresholds, intensity range
- All effect settings                  // Durations, eases, methods
- float updateInterval = 0.1f          // Update frequency
```

### Blur Intensity Calculation

#### Algorithm
```
1. Get normalized hunger and thirst values (0-100 scale)
2. Check if either stat is below critical threshold
3. Calculate intensity for each stat:
   - If stat > criticalThreshold: intensity = 0
   - If stat <= severeThreshold: intensity = maxBlurIntensity
   - Otherwise: intensity = lerp(maxBlurIntensity, 0, normalized value between severe and critical)
4. Combine intensities:
   - Option A (useWorstStat=true): Use maximum of both
   - Option B (useWorstStat=false): Use average of both
5. Smoothly transition currentIntensity towards targetIntensity
```

#### Formula
```csharp
// For each stat (in SurvivalStatBlurCalculator)
if (statValue > criticalThreshold)
{
    intensity = 0;
}
else if (statValue <= severeThreshold)
{
    intensity = maxBlurIntensity;
}
else
{
    float normalizedValue = (statValue - severeThreshold) / (criticalThreshold - severeThreshold);
    intensity = Mathf.Lerp(maxBlurIntensity, minBlurIntensity, normalizedValue);
}

// Combine intensities
if (useWorstStat)
    targetIntensity = Mathf.Max(hungerIntensity, thirstIntensity);
else
    targetIntensity = (hungerIntensity + thirstIntensity) / 2f;

// Smooth transition using DOTween (in DOTweenBlurEffect)
bool isFadingIn = targetIntensity > currentIntensity;
float duration = isFadingIn ? fadeInDuration : fadeOutDuration;
Ease ease = isFadingIn ? fadeInEase : fadeOutEase;

DOTween.To(
    () => currentIntensity,
    value => ApplyIntensity(value),
    targetIntensity,
    duration
).SetEase(ease);
```

## Behavior Flow

### Initialization
1. **BlurOverlayController.Awake()**
   - Gets PlayerStats component reference
   - Validates blur overlay Image reference
   - Calls `InitializeDependencies()`
2. **InitializeDependencies()**
   - Creates `SurvivalStatBlurCalculator` with PlayerStats
   - Configures calculator with thresholds and settings
   - Creates `DOTweenBlurEffect` with Image reference
   - Configures effect with durations, eases, and blur method
3. **BlurOverlayController.Start()**
   - Calls `Initialize()` on calculator and effect
   - Subscribes to intensity change events
   - Performs initial blur update

### Runtime - Stat Degradation
1. Player's hunger/thirst drops below critical threshold (30%)
2. **BlurOverlayController.Update()** periodically calls `UpdateBlurEffect()`
3. **SurvivalStatBlurCalculator.UpdateIntensity()** calculates new intensity
4. If intensity changed, fires `OnIntensityChanged` event
5. **BlurOverlayController.OnIntensityChanged()** receives new intensity
6. Calls **DOTweenBlurEffect.SetTargetIntensity()** with `isFadingIn = true`
7. DOTween creates smooth fade-in animation to target intensity
8. Blur gradually becomes more prominent as stats worsen

### Runtime - Stat Recovery
1. Player consumes food/water
2. Periodic update detects stat increase
3. **SurvivalStatBlurCalculator** calculates lower intensity
4. Fires `OnIntensityChanged` event
5. **DOTweenBlurEffect.SetTargetIntensity()** called with `isFadingIn = false`
6. DOTween creates faster fade-out animation (shorter duration)
7. Blur gradually fades away

### Edge Cases
- **Both stats critical:** Uses worst stat or average based on configuration
- **Rapid stat changes:** DOTween handles smoothing automatically, killing previous tweens
- **Death state:** Blur continues to operate; death screen handles overlay separately
- **Stat thresholds:** Coordinated with damage system thresholds
- **Manual control:** Public API allows bypassing calculator for cutscenes/events

## UI Integration

### Prerequisites
- Blur overlay already exists in scene (user-mentioned)
- Overlay should be on a full-screen canvas
- Material with blur shader assigned to Image component

### Setup Requirements
1. Canvas with blur overlay Image
2. Blur material/shader that supports intensity parameter
3. Image should be at highest render order (in front of UI)
4. Image set to full screen (anchors stretched)

### Shader Parameter Control
The controller will modify one of these (based on shader):
- `_BlurAmount` - Standard blur amount parameter
- `_Intensity` - Generic intensity parameter
- Image alpha channel - If using pre-blurred texture

## Configuration Recommendations

### Default Values
```csharp
Hunger Critical Threshold: 30f   // Matches ~45% below hurt threshold (70)
Thirst Critical Threshold: 30f   // Halfway below hurt threshold (60)
Hunger Severe Threshold: 10f     // Very low, maximum blur
Thirst Severe Threshold: 10f     // Very low, maximum blur
Max Blur Intensity: 0.8f         // Strong but not completely blinding
Fade In Speed: 1.5f              // Gradual warning
Fade Out Speed: 2.5f             // Quick relief feedback
Use Worst Stat: true             // Most critical condition takes priority
```

### Tuning Guidelines
- **Critical Threshold:** Should be slightly below or at hurt threshold for consistency
- **Severe Threshold:** Reserve maximum blur for truly critical situations (10-15%)
- **Max Intensity:** 0.7-0.9 range - should impair but not blind
- **Fade Speeds:** Fade-out should be faster for positive feedback
- **Use Worst Stat:** True for focused feedback, false for cumulative effect

## Testing Scenarios

### Test Cases
1. **Normal Conditions:** No blur when stats above 30%
2. **Single Stat Low:** Blur appears when hunger OR thirst drops below 30%
3. **Both Stats Low:** Blur intensity correctly calculated for both
4. **Gradual Degradation:** Smooth fade-in as stats decrease
5. **Rapid Recovery:** Smooth but quick fade-out when consuming items
6. **Critical State (10% or below):** Maximum blur intensity reached
7. **Threshold Boundaries:** Proper behavior at exactly 30% and 10%
8. **Performance:** No frame drops during rapid stat changes

## Performance Considerations
- **Event-driven updates**: Uses periodic checks (0.1s interval) instead of every frame
- **DOTween optimization**: Native tweening engine, highly optimized
- **Cached references**: All references cached in Awake()
- **Smart updates**: Only applies blur when intensity changes (not every frame)
- **Tween management**: Previous tweens automatically killed before starting new ones
- **No allocations**: No runtime allocations during blur updates
- **Minimal calculations**: Simple math operations (lerp, clamp, comparison)

## Setup Instructions

### 1. Scene Setup
1. Ensure you have a Canvas with a full-screen blur overlay Image
2. Assign a blur material/shader to the Image (or use alpha blending)
3. Set Image to full screen (anchors: stretch, offsets: 0)
4. Set Image raycast target to false (doesn't block input)

### 2. Component Setup
1. Find your Player GameObject with PlayerStats component
2. Add **BlurOverlayController** component to the same GameObject
3. Configure references:
   - Assign **Blur Overlay Image** in inspector
4. Configure thresholds:
   - Hunger Critical: 30, Severe: 10
   - Thirst Critical: 30, Severe: 10
   - Max Blur Intensity: 0.8
   - Use Worst Stat: ✓
5. Configure effect:
   - Fade In Duration: 0.67s
   - Fade Out Duration: 0.4s
   - Blur Method: Choose based on your setup
     - **Alpha**: If using pre-blurred texture
     - **Material Property**: If shader has blur parameter
     - **Both**: For combined effect

### 3. Material Setup (if using Material Property blur)
1. Ensure your blur shader has a float property (e.g., `_BlurAmount`)
2. Set **Material Blur Property** field to match shader property name
3. Test in play mode using context menu: "Test Max Blur"

## Usage

### Basic Usage
Simply attach the component - it works automatically!

### Manual Control (Advanced)
```csharp
BlurOverlayController controller = GetComponent<BlurOverlayController>();

// Manually set blur (bypasses stat calculation)
controller.SetManualIntensity(0.5f, fadeIn: true);

// Get current intensity
float current = controller.GetCurrentIntensity();

// Enable/disable system
controller.SetEnabled(false);
```

## Future Enhancements
- **Vignette Effect:** Add darkening edges with blur
- **Color Tint:** Add slight desaturation or color shift when critical
- **Pulse Effect:** Subtle pulse when at maximum blur
- **Audio Cue:** Heartbeat or ringing sound when blur is active
- **Multiple Stat Support:** Extend to fatigue or health if needed
- **Custom Curves:** Animation curves for non-linear fade transitions
- **Separate Blur Types:** Different blur styles for hunger vs thirst

## Implementation Files
- **IBlurIntensityCalculator.cs** - Interface for intensity calculation
- **SurvivalStatBlurCalculator.cs** - Hunger/thirst-based calculator implementation
- **IBlurEffect.cs** - Interface for blur rendering
- **DOTweenBlurEffect.cs** - DOTween-based effect implementation
- **BlurOverlayController.cs** - Main orchestrator component
- **BLUR_OVERLAY_SYSTEM.md** - This design document

## Dependencies
- **Unity Components:**
  - UnityEngine.UI (Image component)
  - DOTween (animation library)
- **Game Components:**
  - PlayerStats.cs
  - HungerStat.cs (accessed through PlayerStats)
  - ThirstStat.cs (accessed through PlayerStats)
- **Scene Setup:**
  - Canvas with blur overlay Image
  - Blur shader/material (optional, based on blur method)

## Extensibility Examples

### Custom Calculator
Create a calculator based on different stats:
```csharp
public class HealthBlurCalculator : IBlurIntensityCalculator
{
    private PlayerStats stats;
    
    public HealthBlurCalculator(PlayerStats stats)
    {
        this.stats = stats;
    }
    
    public float CalculateIntensity()
    {
        float healthPercent = stats.HealthPercent;
        return healthPercent < 0.2f ? (1f - healthPercent / 0.2f) * 0.9f : 0f;
    }
    
    // Implement other interface methods...
}
```

### Custom Effect
Create a different animation style:
```csharp
public class PulseBlurEffect : IBlurEffect
{
    // Implement pulsing effect instead of smooth transitions
    // Use DOTween.Sequence() for complex animations
}
```

### Using Custom Implementations
```csharp
public class CustomBlurController : BlurOverlayController
{
    protected override void InitializeDependencies()
    {
        intensityCalculator = new HealthBlurCalculator(playerStats);
        blurEffect = new PulseBlurEffect(blurOverlayImage);
    }
}
```
