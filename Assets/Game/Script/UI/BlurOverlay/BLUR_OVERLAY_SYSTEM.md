# Blur Overlay System Design

## Overview
A dynamic, survival-stat-driven blur that responds to the player's hunger and thirst. When either stat crosses its critical threshold, the effect fades in and intensifies; when the player recovers, it fades back out. The runtime is built on **URP post-processing Volumes**, not a UI `Image`.

## System Goals
1. Provide clear visual feedback when the player is starving or dehydrated.
2. Smooth, DOTween-driven fade-in/out transitions.
3. Intensity scales with severity (threshold between critical and severe).
4. Non-intrusive but noticeable enough to alert the player.
5. Zero asset coupling at design time — the controller spawns its own `Volume` GameObject at runtime from a designer-authored `VolumeProfile`.

---

## Technical Architecture

### Runtime orchestrator: `VolumeBlurController`
**File:** `Assets/Game/Script/UI/BlurOverlay/VolumeBlurController.cs`

This is the main component — a `MonoBehaviour` attached to the player (or any persistent object). It:

1. **Awake** — spawns a child GameObject named `SurvivalBlurVolume` with a URP `UnityEngine.Rendering.Volume` component (`isGlobal = true`, `weight = 0`, configurable `priority`). The volume's `profile` is set to the `customVolumeProfile` asset assigned in the Inspector (usually authored with Depth Of Field + Motion Blur overrides).
2. **Start** — resolves `PlayerStats` via `ServiceContainer.Instance.TryGet<PlayerStats>()`. If the service isn't registered, logs an error and disables itself.
3. Constructs a `SurvivalStatBlurCalculator` (plain C# class, constructor-injected with `PlayerStats`) and subscribes to its `OnIntensityChanged` event.
4. **Update** — polls every `updateInterval` seconds (default 0.1s) and calls `UpdateBlurEffect()`.
5. On intensity change, tweens `volume.weight` 0..1 with DOTween. Fade-in and fade-out use different eases (`InQuad` vs `OutQuad`) and different durations (`fadeInDuration` vs `fadeOutDuration`), scaled proportionally to the intensity delta so small changes don't take as long as large ones. The tween uses `SetUpdate(true)` (unscaled time) so pausing the game doesn't stall the fade.
6. **OnDestroy** — kills the tween, unsubscribes, destroys the spawned volume GameObject. The `VolumeProfile` asset is never destroyed (it is a shared project asset).

### Intensity calculator: `SurvivalStatBlurCalculator`
**File:** `Assets/Game/Script/UI/BlurOverlay/SurvivalStatBlurCalculator.cs`

Implements `IBlurIntensityCalculator`. Reads hunger + thirst from `PlayerStats`, applies the two-threshold model below, and fires `OnIntensityChanged(float)` when the computed target changes.

### Interfaces (retained — used by the strategy seam)
- **`IBlurIntensityCalculator`** — `CalculateIntensity() : float`, `UpdateIntensity()`, `Initialize()`, `Cleanup()`, `OnIntensityChanged` event.
- **`IBlurEffect`** — strategy seam for swapping out the visual layer (`SetTargetIntensity(float, bool)`, `CurrentIntensity`, `Initialize()`, `Cleanup()`). Historically implemented by `DOTweenBlurEffect.cs`, which animates a UI `Image`. **`VolumeBlurController` does NOT use `IBlurEffect` internally** — it tweens the volume weight directly. The interface and `DOTweenBlurEffect` remain for alternative visual implementations that target canvas UI rather than URP post-processing.

### Sibling feedback components (same folder)
These are independent behaviours that also react to stats but are **not** part of the blur pipeline — they plug into the same player context:
- `FallImpactFeedback.cs` — camera/screen reaction to hard landings.
- `LowHealthHeartbeatFeedback.cs` — pulsing overlay at low HP.
- `LowStaminaBreathingFeedback.cs` — breathing overlay at low stamina.
- `TemperaturePostProcessFeedback.cs` — tint/effect for temperature extremes.

Document and extend those individually; they share no controller with `VolumeBlurController`.

---

## Inspector configuration (on `VolumeBlurController`)

| Header | Field | Default | Purpose |
|--------|-------|---------|---------|
| Calculator | `hungerCriticalThreshold` | 50 | Hunger at/below which blur starts scaling in |
| Calculator | `thirstCriticalThreshold` | 50 | Thirst at/below which blur starts scaling in |
| Calculator | `hungerSevereThreshold` | 20 | Hunger at which blur reaches `maxBlurIntensity` |
| Calculator | `thirstSevereThreshold` | 20 | Thirst at which blur reaches `maxBlurIntensity` |
| Calculator | `maxBlurIntensity` | 1 | Ceiling on computed intensity (0..1) |
| Calculator | `useWorstStat` | true | `true` → max of hunger/thirst intensities; `false` → average |
| Effect | `fadeInDuration` | 0.67s | Base duration for fading up (scaled by intensity delta) |
| Effect | `fadeOutDuration` | 0.4s | Base duration for fading down (scaled by intensity delta) |
| Update | `updateInterval` | 0.1s | How often `UpdateBlurEffect` polls the calculator |
| Volume | `customVolumeProfile` | _(required)_ | The `VolumeProfile` asset that defines DOF/Motion Blur overrides |
| Volume | `volumePriority` | 1000 | Priority of the spawned global Volume (higher wins over other volumes) |
| Debug | `enableDebugLogs` | false | Verbose console logging |

> **Required:** `customVolumeProfile` MUST be assigned. `Awake` spawns the Volume GameObject unconditionally, but if no profile is assigned the controller logs an error and the volume has no overrides to apply.

---

## Intensity calculation

Per stat (hunger, thirst):
```csharp
if (stat > critical)       intensity = 0;
else if (stat <= severe)   intensity = maxBlurIntensity;
else {
    float t = (stat - severe) / (critical - severe);
    intensity = Mathf.Lerp(maxBlurIntensity, 0f, t);
}
```

Combining the two:
```csharp
target = useWorstStat
    ? Mathf.Max(hungerIntensity, thirstIntensity)
    : (hungerIntensity + thirstIntensity) * 0.5f;
```

Volume weight animation (in `VolumeBlurController.SetTargetIntensityInternal`):
```csharp
bool  isFadingIn          = target > currentWeight;
float delta               = Mathf.Abs(target - currentWeight);
float base                = isFadingIn ? fadeInDuration : fadeOutDuration;
float proportionalDuration = base * delta;        // small changes tween faster
Ease  ease                = isFadingIn ? Ease.InQuad : Ease.OutQuad;

weightTween = DOTween.To(
    () => currentWeight,
    v  => volume.weight = currentWeight = v,
    target,
    proportionalDuration
).SetEase(ease).SetUpdate(true);
```

Changes smaller than `0.01` weight are applied instantly without a tween.

---

## Behaviour flow

### Initialisation
1. `VolumeBlurController.Awake()` creates the `SurvivalBlurVolume` child, assigns `customVolumeProfile`, sets `weight = 0`.
2. `VolumeBlurController.Start()` resolves `PlayerStats` from `ServiceContainer` (warning + disable if missing).
3. Constructs `SurvivalStatBlurCalculator`, pushes thresholds into it, calls `Initialize()`, subscribes to `OnIntensityChanged`, fires one `UpdateBlurEffect()` to apply any already-critical stats on spawn.

### Every frame
- `updateTimer += Time.deltaTime`. When ≥ `updateInterval`, call `UpdateBlurEffect()`:
  - `intensityCalculator.UpdateIntensity()` — recomputes target from current `PlayerStats`.
  - If the target differs from the last observed value, kicks off a new tween in the correct direction.

### Teardown
`OnDestroy` kills the tween, unsubscribes, calls `intensityCalculator.Cleanup()`, destroys the volume GameObject. The `VolumeProfile` asset is left alone.

---

## Public API

```csharp
// Bypass the calculator (cutscenes, scripted events, debug)
controller.SetManualIntensity(0.5f, fadeIn: true);

// Current volume weight (0..1)
float w = controller.GetCurrentIntensity();

// Toggle the entire system. When disabled, weight tweens to 0.
controller.SetEnabled(false);

// Low-level access for advanced manipulation
Volume        v = controller.GetVolume();
VolumeProfile p = controller.GetVolumeProfile();
```

### Context menus (Editor only)
- **Test Max Blur** → `SetManualIntensity(1f, true)`
- **Test Medium Blur** → `SetManualIntensity(0.5f, true)`
- **Test Clear Blur** → `SetManualIntensity(0f, false)`
- **Force Update** → `UpdateBlurEffect()` once

---

## Setup Instructions

### 1. Create the `VolumeProfile` asset
1. `Create → Volume Profile` in the Project window.
2. Add the overrides you want the survival blur to drive — typically **Depth Of Field** (Gaussian or Bokeh) and **Motion Blur**. Configure them at their full-intensity target (weight=1) values; the controller will scale them in by ramping the volume's own `weight`, so you author them "at maximum" and let the tween fade them in.

### 2. Scene setup
1. Pick a persistent player/game object that lives for the session.
2. Add the `VolumeBlurController` component.
3. Inspector:
   - Drag the `VolumeProfile` asset into **Custom Volume Profile**.
   - Leave the thresholds at defaults or tune per your survival curve.
   - Leave **Volume Priority** high (default 1000) so this blur wins against gameplay volumes.
4. Ensure your URP renderer has **Post Processing** enabled and the camera's **Post Processing** flag is on. Without URP post-processing, the spawned volume has no effect.
5. Make sure **`PlayerStats`** is registered in `ServiceContainer` before the blur controller's `Start` runs (it is, under the default `GameServiceBootstrapper` order).

### 3. Test
Enter Play mode, then use the context-menu actions on the component (Test Max Blur / Test Clear Blur) or manipulate `PlayerStats` hunger/thirst through the debug UI.

---

## Configuration Recommendations

| Setting | Default | Notes |
|---------|---------|-------|
| Hunger Critical | 50 | Blur begins fading in below 50% |
| Hunger Severe | 20 | Full blur weight at 20% or below |
| Thirst Critical | 50 | — |
| Thirst Severe | 20 | — |
| Max Blur Intensity | 1 | Cap on volume weight; lower for a subtler max |
| Fade In | 0.67s | Gradual warning |
| Fade Out | 0.4s | Faster — reward recovery |
| Use Worst Stat | true | Single-stat focus; set false for cumulative |
| Volume Priority | 1000 | High so it wins against level volumes |

---

## Extending the system

### Different stats driving the blur
Write a new `IBlurIntensityCalculator` (same folder) and call its methods the same way `VolumeBlurController.InitializeIntensityCalculator()` does. You can subclass `VolumeBlurController` and override that method, or add a calculator-selection enum.

```csharp
public class HealthBlurCalculator : IBlurIntensityCalculator { /* ... */ }
```

### Different visual output (UI Image instead of post-processing)
Use `DOTweenBlurEffect.cs` plus your own lightweight controller that ties an `IBlurIntensityCalculator` to `IBlurEffect.SetTargetIntensity`. This is the original (pre-URP) path and is still supported for scenes without post-processing.

### Add more override effects to the same volume
Extend the `VolumeProfile` asset — add Vignette, Chromatic Aberration, Color Adjustments, etc. `VolumeBlurController` does not care what's in the profile; it just ramps `volume.weight`, which scales every override in the profile proportionally.

---

## Files in this folder

| File | Role |
|------|------|
| `VolumeBlurController.cs` | Main orchestrator — spawns the URP Volume, runs the calculator, tweens weight. |
| `SurvivalStatBlurCalculator.cs` | Default hunger/thirst → intensity calculator. |
| `IBlurIntensityCalculator.cs` | Calculator strategy interface. |
| `IBlurEffect.cs` | Legacy visual-strategy interface — used by `DOTweenBlurEffect`. |
| `DOTweenBlurEffect.cs` | Alternative visual path that animates a UI `Image` instead of a URP Volume. Not wired into `VolumeBlurController`. |
| `FallImpactFeedback.cs` | Independent — camera/screen reaction on landing. |
| `LowHealthHeartbeatFeedback.cs` | Independent — pulsing overlay at low HP. |
| `LowStaminaBreathingFeedback.cs` | Independent — breathing overlay at low stamina. |
| `TemperaturePostProcessFeedback.cs` | Independent — tint for temperature extremes. |

---

## Dependencies

- **URP:** `UnityEngine.Rendering`, `UnityEngine.Rendering.Universal` (for `Volume` + `VolumeProfile`). Camera must have post-processing on.
- **DOTween:** for the weight tween. Tween uses unscaled time (`SetUpdate(true)`).
- **ServiceContainer:** `PlayerStats` must be registered before `Start`.

---

## History note

An earlier revision of this doc described a `BlurOverlayController` that animated a full-screen UI `Image` via alpha or a `_BlurAmount` material property. That class no longer exists — the system moved to URP post-processing and lives in `VolumeBlurController`. If you're reading old code or Inspector references that mention `BlurOverlayController`, translate them to `VolumeBlurController`.
