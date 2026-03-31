using UnityEngine;
using System;
using Game.Environment.Temperature;

/// <summary>
/// Tracks the player's body temperature in degrees Celsius (0–100).
/// Comfort zone: ~37°C. Below coldDamageThreshold → freezing damage + movement/hunger penalties.
/// Above hotDamageThreshold → heat damage + thirst penalty.
///
/// Integration:
///   Each frame PlayerStats.Update() calls:
///     1. GatherHeatSources(transform.position)  — scans nearby ITemperatureSource objects
///     2. SetEnvironmentTarget(ambientCelsius)    — sets ambient target from DayNightCycle
///     3. Tick(deltaTime)                         — drifts current toward effective target
///
/// The effective target = environment + heat sources, then blended toward 37°C by insulation.
/// </summary>
[Serializable]
public class TemperatureStat : Stat
{
    // ── Config (all set via Init() from PlayerConfig — NOT serialized) ────
    private float driftRate             = 2f;
    private float coldDamageThreshold   = 15f;
    private float hotDamageThreshold    = 42f;
    private float coldDPS               = 1.5f;
    private float hotDPS                = 2f;
    private float coldSpeedPenaltyThreshold = 15f;
    private float coldHungerPenaltyThreshold = 15f;
    private float hotThirstPenaltyThreshold  = 42f;
    private float coldSpeedMinMultiplier = 0.65f;
    private float hungerColdMaxMultiplier = 1.3f;
    private float thirstHotMaxMultiplier  = 1.5f;

    // ── Heat source scan ──────────────────────────────────────────────
    private float _heatSourceRadius = 5f;
    private LayerMask _heatSourceLayer;
    private readonly Collider[] _heatBuffer = new Collider[8];

    // ── Runtime state ─────────────────────────────────────────────────
    private float _environmentTarget   = 37f;
    private float _weatherOffset       = 0f;
    private float _heatSourceBonus     = 0f;
    private float _warmthInsulation    = 0f;

    // ── Debug read-only (used by PlayerStatsEditor) ───────────────────
    public float DebugEnvironmentTarget => _environmentTarget;
    public float DebugWeatherOffset     => _weatherOffset;
    public float DebugHeatBonus         => _heatSourceBonus;
    public float DebugInsulation        => _warmthInsulation;
    public float DebugColdThreshold     => coldDamageThreshold;
    public float DebugHotThreshold      => hotDamageThreshold;
    public float DebugColdSpeedPenaltyThreshold => coldSpeedPenaltyThreshold;
    public float DebugColdHungerPenaltyThreshold => coldHungerPenaltyThreshold;
    public float DebugHotThirstPenaltyThreshold  => hotThirstPenaltyThreshold;
    public bool  IsInitialized          => _initialized;

    // ── Init ──────────────────────────────────────────────────────────

    private bool _initialized = false;

    /// <summary>
    /// Called by PlayerStats.Awake() to configure thresholds from PlayerConfig.
    /// </summary>
    public void Init(
        float coldThreshold, float hotThreshold,
        float coldDps, float hotDps,
        float drift,
        float coldSpeedThreshold,
        float coldHungerThreshold,
        float hotThirstThreshold,
        float coldSpeedMin, float hungerColdMax, float thirstHotMax,
        float heatScanRadius = 5f, int heatScanLayer = 0)
    {
        coldDamageThreshold     = coldThreshold;
        hotDamageThreshold      = hotThreshold;
        coldDPS                 = coldDps;
        hotDPS                  = hotDps;
        driftRate               = drift;
        coldSpeedPenaltyThreshold  = coldSpeedThreshold > 0f ? coldSpeedThreshold : coldDamageThreshold;
        coldHungerPenaltyThreshold = coldHungerThreshold > 0f ? coldHungerThreshold : coldDamageThreshold;
        hotThirstPenaltyThreshold  = hotThirstThreshold > 0f ? hotThirstThreshold : hotDamageThreshold;
        coldSpeedMinMultiplier  = coldSpeedMin;
        hungerColdMaxMultiplier = hungerColdMax;
        thirstHotMaxMultiplier  = thirstHotMax;
        _heatSourceRadius       = heatScanRadius;
        _heatSourceLayer        = heatScanLayer;

        // Force current to comfort temperature, bypassing all serialization.
        max     = 100f;
        current = 37f;
        RaiseChanged();

        _initialized = true;
    }

    // ── Per-frame inputs ──────────────────────────────────────────────

    /// <summary>Set the ambient temperature target (°C) based on time-of-day curve.</summary>
    public void SetEnvironmentTarget(float celsius) => _environmentTarget = celsius;

    /// <summary>
    /// Expose a weather offset API so a future weather system can shift the ambient
    /// temperature (e.g. blizzard = -15, heatwave = +10) without refactoring.
    /// </summary>
    public void SetWeatherTemperatureOffset(float offsetCelsius) => _weatherOffset = offsetCelsius;

    /// <summary>Update equipment insulation. 0 = no insulation, 1 = perfect insulation (stays at 37°C).</summary>
    public void SetInsulation(float insulation) => _warmthInsulation = Mathf.Clamp01(insulation);

    /// <summary>
    /// Scans nearby ITemperatureSource objects and accumulates their bonus.
    /// Call once per frame BEFORE Tick(). Mirrors InteractionDetector's OverlapSphere pattern.
    /// </summary>
    public void GatherHeatSources(Vector3 worldPosition)
    {
        _heatSourceBonus = 0f;
        int count = Physics.OverlapSphereNonAlloc(
            worldPosition, _heatSourceRadius, _heatBuffer, _heatSourceLayer);

        for (int i = 0; i < count; i++)
        {
            if (_heatBuffer[i] != null &&
                _heatBuffer[i].TryGetComponent<ITemperatureSource>(out var src) &&
                src.IsActive)
            {
                _heatSourceBonus += src.TemperatureBonus;
            }
        }
    }

    // ── Core tick ─────────────────────────────────────────────────────

    /// <summary>
    /// Drifts body temperature toward the effective target.
    /// Call after GatherHeatSources() and SetEnvironmentTarget().
    /// </summary>
    public override void Tick(float deltaTime)
    {
        if (!_initialized) return; // wait for Init() — prevents drift before Awake completes

        // Effective ambient = environment + weather shift + heat sources, clamped to stat range
        float effectiveTarget = Mathf.Clamp(
            _environmentTarget + _weatherOffset + _heatSourceBonus,
            0f, max);

        // Warm clothing blends effective target toward comfort zone (37°C)
        effectiveTarget = Mathf.Lerp(effectiveTarget, 37f, _warmthInsulation);

        // Drift current temperature toward effective target
        float drift = (effectiveTarget - current) * driftRate * deltaTime;
        SetCurrent(current + drift);

        // Reset heat-source bonus — re-gathered next frame
        _heatSourceBonus = 0f;
    }

    // ── Damage queries ────────────────────────────────────────────────

    /// <summary>True when temperature is below the cold damage threshold.</summary>
    public bool IsFreezing    => _initialized && current <= coldDamageThreshold;
    /// <summary>True when temperature is above the heat damage threshold.</summary>
    public bool IsOverheating => _initialized && current >= hotDamageThreshold;
    /// <summary>Damage per second when freezing.</summary>
    public float ColdDPS      => coldDPS;
    /// <summary>Damage per second when overheating.</summary>
    public float HotDPS       => hotDPS;

    // ── Penalty queries ───────────────────────────────────────────────

    /// <summary>
    /// Movement speed multiplier from cold.
    /// 1.0 = no penalty. Scales down to coldSpeedMinMultiplier as temperature approaches 0°C.
    /// No penalty from heat.
    /// </summary>
    public float GetColdSpeedPenalty()
    {
        if (current >= coldSpeedPenaltyThreshold) return 1f;
        if (coldSpeedPenaltyThreshold <= 0f) return coldSpeedMinMultiplier;
        float t = current / coldSpeedPenaltyThreshold; // 0 (absolute cold) → 1 (at threshold)
        return Mathf.Lerp(coldSpeedMinMultiplier, 1f, t);
    }

    /// <summary>
    /// Extra hunger drain multiplier when freezing (shivering burns more calories).
    /// 1.0 = normal rate. Scales up to hungerColdMaxMultiplier at 0°C.
    /// </summary>
    public float GetHungerDrainMultiplier()
    {
        if (current >= coldHungerPenaltyThreshold) return 1f;
        if (coldHungerPenaltyThreshold <= 0f) return hungerColdMaxMultiplier;
        float t = 1f - (current / coldHungerPenaltyThreshold); // 0 at threshold → 1 at 0°C
        return Mathf.Lerp(1f, hungerColdMaxMultiplier, t);
    }

    /// <summary>
    /// Extra thirst drain multiplier when overheating (sweating).
    /// 1.0 = normal rate. Scales up to thirstHotMaxMultiplier at max temperature.
    /// </summary>
    public float GetThirstDrainMultiplier()
    {
        if (current <= hotThirstPenaltyThreshold) return 1f;
        float range = max - hotThirstPenaltyThreshold;
        if (range <= 0f) return thirstHotMaxMultiplier;
        float t = (current - hotThirstPenaltyThreshold) / range; // 0 at threshold → 1 at max
        return Mathf.Lerp(1f, thirstHotMaxMultiplier, t);
    }
}
