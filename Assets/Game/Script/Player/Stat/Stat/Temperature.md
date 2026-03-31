# Temperature Stat — Design Spec

## Overview

`Temperature` tracks the player's body temperature in **degrees Celsius (0–100)**.

| Value | Meaning |
|-------|---------|
| 0 °C | Critically cold |
| 12 °C | Cold damage threshold (`tempColdThreshold`) |
| **37 °C** | **Comfort zone — no effects** |
| 42 °C | Heat damage threshold (`tempHotThreshold`) |
| 100 °C | Critically hot |

---

## How It Works

Each frame `PlayerStats.Update()` calls (in order):

1. `temperature.GatherHeatSources(transform.position)` — `OverlapSphereNonAlloc` scan for nearby `ITemperatureSource` objects  
2. `temperature.SetEnvironmentTarget(ambient)` — samples `PlayerConfig.temperatureDayCurve` at `IDayNightCycleService.DayProgress`  
3. `temperature.Tick(dt)` — drifts body temperature toward the **effective target**

**Effective target formula:**
```
effectiveTarget = Clamp(envTarget + weatherOffset + heatSourceBonus, 0, 100)
effectiveTarget = Lerp(effectiveTarget, 37°C, warmthInsulation)
```
Body temperature then drifts toward `effectiveTarget` at `tempDriftRate` °C/s.

---

## Effects at Temperature Extremes

### Cold Progression

| Temperature | Effect | Threshold | Detail |
|-------------|--------|-----------|--------|
| 24 °C and below | **Speed penalty** | `tempColdSpeedPenaltyThreshold` | Linearly from 100% at 24°C → 65% at 0°C |
| 18 °C and below | **Hunger drain** | `tempColdHungerPenaltyThreshold` | Multiplier 1.0 at 18°C → 1.3× at 0°C (shivering) |
| 12 °C and below | **Health damage** | `tempColdThreshold` | `tempColdDPS` (1.5 HP/s default) — `DeathCause.Freezing` |

### Hot Progression

| Temperature | Effect | Threshold | Detail |
|-------------|--------|-----------|--------|
| 38 °C and above | **Thirst drain** | `tempHotThirstPenaltyThreshold` | Multiplier 1.0 at 38°C → 1.5× at 100°C (sweating) |
| 42 °C and above | **Health damage** | `tempHotThreshold` | `tempHotDPS` (2 HP/s default) — `DeathCause.Heatstroke` |

---

## Inputs

| Source | How |
|--------|-----|
| Time of day | `AnimationCurve temperatureDayCurve` in `PlayerConfig` |
| Heat sources (campfire, hot spring) | `ITemperatureSource` on scene objects. Scanned via `OverlapSphereNonAlloc`. |
| Weather | `PlayerStats.SetWeatherTemperatureOffset(float)` — external API |
| Equipment | `PlayerStats.SetTemperatureInsulation(float)` — driven by `StatModifierApplicator` (`WarmthInsulation` type) |

---

## File Map

| File | Role |
|------|------|
| `TemperatureStat.cs` | All stat logic, heat-source scanning, penalty queries |
| `ITemperatureSource.cs` | Interface for heat-emitting scene objects |
| `PlayerConfig.cs` | All designer-tunable config fields |
| `PlayerStats.cs` | Orchestration, damage application, public API |
| `WalkingState.cs` / `RunningState.cs` | Reads `GetColdSpeedPenalty()` in multiplier stack |
| `HungerStat.cs` / `ThirstStat.cs` | `SetTemperatureMultiplier()` called each frame |
| `DeathCause.cs` | `Freezing`, `Heatstroke` entries |
| `WorldSaveData.cs` / `SaveLoadService.cs` | `PlayerSaveData.temperature` persisted |

---

## Adding a Heat Source

Attach a script implementing `ITemperatureSource` to any scene object:

```csharp
public class Campfire : MonoBehaviour, ITemperatureSource
{
    [SerializeField] private float warmth = 20f;
    public float TemperatureBonus => warmth;
    public bool  IsActive => isLit;

    private bool isLit;
    // ... lit/extinguished logic
}
```

Make sure the object is on the layer matching `TemperatureStat.heatSourceLayer` in the Inspector.
