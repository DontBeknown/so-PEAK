using UnityEngine;
using System;
using Game.Core.DI;
using Game.Core.Events;
using Game.Environment.DayNight;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private EquipmentManager equipmentManager;

    [SerializeField] private HealthStat health;
    [SerializeField] private HungerStat hunger;
    [SerializeField] private ThirstStat thirst;
    [SerializeField] private StaminaStat stamina;
    [SerializeField] private FatigueStat fatigue;
    // NOTE: Not [SerializeField] — always created fresh in Awake() to prevent
    // Unity's deserialization from overwriting the 37°C starting value.
    private TemperatureStat temperature;
    
    private IStatModifierCalculator statModifierCalculator;

    public PlayerConfig Config => config;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnTemperatureChanged;
    public event Action OnDeath;
    
    // Stat tracking events
    public event Action<float> OnStaminaDrained;
    public event Action<float> OnHealthDamaged;
    public event Action<float> OnFatigueChanged;
    public event Action<float> OnFallDamaged;

    [SerializeField] private float spawnImmunityDuration = 2.5f;
    private bool isImmune;

    private bool isSprinting;

    private IEventBus _eventBus;
    private IDayNightCycleService _dayNightService;
    private ISaveLoadService _saveLoadService;
    private DeathCause _lastDamageSource = DeathCause.Unknown;
    public DeathCause LastDamageSource => _lastDamageSource;

    private void Awake()
    {
        
        health ??= new HealthStat();
        hunger ??= new HungerStat();
        thirst ??= new ThirstStat();
        stamina ??= new StaminaStat();
        fatigue ??= new FatigueStat();
        // Always create fresh — bypasses any stale serialized state
        temperature = new TemperatureStat();

        hunger.Init(config.hungerDrainPerSecond, config.hungerHurtThreshold, config.starvationDPS, config.hungerSprintMultiplier);
        thirst.Init(config.thirstDrainPerSecond, config.thirstHurtThreshold, config.dehydrationDPS, config.thirstSprintMultiplier);
        stamina.Init(config.staminaRegenPerSecond, config.staminaDrainCooldown, config.climbStaminaDrainPerSecond);
        fatigue.Init(config.maxFatigue, config.fatigueRateTime, config.fatigueRateElev);
        temperature.Init(
            config.tempColdThreshold, config.tempHotThreshold,
            config.tempColdDPS,       config.tempHotDPS,
            config.tempDriftRate,
            config.tempColdSpeedPenaltyThreshold,
            config.tempColdHungerPenaltyThreshold,
            config.tempHotThirstPenaltyThreshold,
            config.tempColdSpeedMinMultiplier,
            config.tempHungerColdMaxMultiplier,
            config.tempThirstHotMaxMultiplier,
            config.tempHeatSourceRadius,
            config.tempHeatSourceLayer);

        health.OnChanged += (c, m) => OnHealthChanged?.Invoke(c, m);
        stamina.OnChanged += (c, m) => OnStaminaChanged?.Invoke(c, m);
        temperature.OnChanged += (c, m) => OnTemperatureChanged?.Invoke(c, m);

        
        // Subscribe to stat tracking events
        health.OnDamaged += (amount) => OnHealthDamaged?.Invoke(amount);
        stamina.OnDrained += (amount) => OnStaminaDrained?.Invoke(amount);
        fatigue.OnChanged += (c, m) => OnFatigueChanged?.Invoke(c);
    }

    private void Start()
    {
        _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        _dayNightService = ServiceContainer.Instance.TryGet<IDayNightCycleService>();
        _saveLoadService = ServiceContainer.Instance.TryGet<ISaveLoadService>();
        health.OnDeath += () =>
        {
            OnDeath?.Invoke();
            _eventBus?.Publish(new PlayerDeathEvent(_lastDamageSource));
        };
        // Auto-assign equipment manager if not set
        equipmentManager ??= GetComponent<EquipmentManager>();
        
        // Initialize stat modifier calculator
        if (equipmentManager != null)
        {
            statModifierCalculator = new StatModifierApplicator(equipmentManager);
        }

        // Force all survivial stats to full — clears any stale serialized values
        // from previous play sessions saved back into the prefab/scene.
        hunger.ResetToFull();
        thirst.ResetToFull();
        health.ResetToFull();
        stamina.ResetToFull();

        StartCoroutine(SpawnImmunityRoutine());
    }

    private System.Collections.IEnumerator SpawnImmunityRoutine()
    {
        isImmune = true;
        yield return new WaitForSeconds(spawnImmunityDuration);
        isImmune = false;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        hunger.Tick(dt);
        thirst.Tick(dt);
        stamina.Tick(dt);
        fatigue.Tick(dt);

        // Temperature: gather heat sources → set env target → tick
        temperature.GatherHeatSources(transform.position);
        if (_dayNightService != null)
        {
            int level = _saveLoadService?.GetCurrentLevel() ?? 1;
            AnimationCurve selectedCurve = level switch
            {
                2 => config.temperatureDayCurveLevel2,
                3 => config.temperatureDayCurveLevel3,
                _ => config.temperatureDayCurveLevel1
            };

            float ambient = selectedCurve != null
                ? selectedCurve.Evaluate(_dayNightService.DayProgress)
                : 37f;
            temperature.SetEnvironmentTarget(ambient);
        }
        temperature.Tick(dt);

        // Push temperature penalties into hunger/thirst each frame
        hunger.SetTemperatureMultiplier(temperature.GetHungerDrainMultiplier());
        thirst.SetTemperatureMultiplier(temperature.GetThirstDrainMultiplier());

        if (!isImmune && hunger.ShouldHurt)
        {
            _lastDamageSource = DeathCause.Starvation;
            health.Damage(hunger.StarveDPS * dt);
        }

        if (!isImmune && thirst.ShouldHurt)
        {
            _lastDamageSource = DeathCause.Dehydration;
            health.Damage(thirst.DehydrateDPS * dt);
        }

        // Temperature damage
        if (!isImmune && temperature.IsFreezing)
        {
            _lastDamageSource = DeathCause.Freezing;
            health.Damage(temperature.ColdDPS * dt);
        }

        if (!isImmune && temperature.IsOverheating)
        {
            _lastDamageSource = DeathCause.Heatstroke;
            health.Damage(temperature.HotDPS * dt);
        }

        if (isSprinting)
        {
            //stamina.Drain(sprintDrainPerSecond * dt);
        }
    }

    public void OnJump()
    {
        if (stamina.CanUse(config.jumpStaminaCost))
            stamina.Drain(config.jumpStaminaCost);
    }
    
    public void SetClimbing(bool climbing)
    {
        stamina.SetClimbing(climbing);
    }

    public void SetWalking(bool walking)
    {
        stamina.SetWalking(walking);
    }

    public void SetRunning(bool running)
    {
        stamina.SetRunning(running);
    }

    public void OnSprint(bool sprinting)
    {
        isSprinting = sprinting;
        hunger.SetSprinting(sprinting);
        thirst.SetSprinting(sprinting);
    }

    public void ConsumeStamina(float amount) => stamina.Drain(amount);
    public void TakeDamage(float dmg)
    {
        if (isImmune) return;
        _lastDamageSource = DeathCause.Damage;
        health.Damage(dmg);
    }

    public void TakeFallDamage(float dmg)
    {
        if (isImmune) return;
        _lastDamageSource = DeathCause.Falling;
        health.Damage(dmg);
        OnFallDamaged?.Invoke(dmg);
    }
    public void Heal(float amount) => health.Heal(amount);
    public void Eat(float nutrition) => hunger.Add(nutrition);
    public void Drink(float water) => thirst.Add(water);

    public void ModifyTemperature(float amount)
    {
        temperature.Add(amount);
    }

    /// <summary>Adjust ambient temperature offset from a weather system (e.g. blizzard = -15).</summary>
    public void SetWeatherTemperatureOffset(float offsetCelsius)
    {
        temperature.SetWeatherTemperatureOffset(offsetCelsius);
    }

    /// <summary>Set equipment insulation. 0 = none, 1 = perfect (stays at 37°C).</summary>
    public void SetTemperatureInsulation(float insulation)
    {
        temperature.SetInsulation(insulation);
    }

    public void RestoreStamina(float amount)
    {
        stamina.Add(amount);
    }

    /// <summary>
    /// Fully rest the player - clears all fatigue (e.g., sleeping, campfire rest)
    /// </summary>
    public void FullRest()
    {
        fatigue.FullRest();
    }

    public float Health => health.Current;
    public float MaxHealth => health.Max;
    public float HealthPercent => health.Percent;

    public float Hunger => hunger.Current;
    public float MaxHunger => hunger.Max;
    public float HungerPercent => hunger.Percent;

    public float Thirst => thirst.Current;
    public float MaxThirst => thirst.Max;
    public float ThirstPercent => thirst.Percent;

    public float Stamina => stamina.Current;
    public float MaxStamina => stamina.Max;
    public float StaminaPercent => stamina.Percent;
    
    public float Temperature        => temperature.Current;
    public float MaxTemperature     => temperature.Max;
    public float TemperaturePercent => temperature.Percent;
    
    public float Fatigue => fatigue.Current;
    public float MaxFatigue => fatigue.Max;
    public float FatiguePercent => fatigue.Percent;
    
    // Expose stat instances for advanced operations
    public StaminaStat StaminaStat => stamina;
    public FatigueStat FatigueStat => fatigue;
    public TemperatureStat TemperatureStat => temperature;

    public IStat GetStat(StatType statType)
    {
        return statType switch
        {
            StatType.Health      => health,
            StatType.Hunger      => hunger,
            StatType.Thirst      => thirst,
            StatType.Stamina     => stamina,
            StatType.Temperature => temperature,
            _ => null
        };
    }
    
    #region Equipment Stat Modifiers
    
    /// <summary>
    /// Gets the modified walk speed with equipment bonuses applied.
    /// </summary>
    public float GetModifiedWalkSpeed(bool isOnSlope = false)
    {
        float baseSpeed = config.baseWalkSpeed;
        
        if (statModifierCalculator != null)
        {
            // Apply universal walk speed modifier
            baseSpeed = statModifierCalculator.GetModifiedValue(StatModifierType.UniversalWalkSpeed, baseSpeed);
            
            // Apply normal or slope-specific modifier
            if (isOnSlope)
            {
                baseSpeed = statModifierCalculator.GetModifiedValue(StatModifierType.WalkSpeedSlope, baseSpeed);
            }
            else
            {
                baseSpeed = statModifierCalculator.GetModifiedValue(StatModifierType.NormalWalkSpeed, baseSpeed);
            }
        }
        
        return baseSpeed;
    }
    
    /// <summary>
    /// Gets the modified climb speed with equipment bonuses applied.
    /// </summary>
    public float GetModifiedClimbSpeed()
    {
        float baseSpeed = config.baseClimbSpeed;
        
        if (statModifierCalculator != null)
        {
            baseSpeed = statModifierCalculator.GetModifiedValue(StatModifierType.ClimbSpeed, baseSpeed);
        }
        
        return baseSpeed;
    }
    
    /// <summary>
    /// Gets the stamina drain multiplier with equipment reductions applied.
    /// Lower values mean less stamina drain.
    /// </summary>
    public float GetStaminaDrainMultiplier(bool isWalking = false, bool isClimbing = false)
    {
        float multiplier = 1f;
        
        if (statModifierCalculator != null)
        {
            // Universal stamina reduction applies to all activities
            multiplier = statModifierCalculator.GetModifiedValue(StatModifierType.UniversalStaminaReduce, multiplier);
            
            // Activity-specific reductions
            if (isWalking)
            {
                multiplier = statModifierCalculator.GetModifiedValue(StatModifierType.WalkStaminaReduce, multiplier);
            }
            else if (isClimbing)
            {
                multiplier = statModifierCalculator.GetModifiedValue(StatModifierType.ClimbStaminaReduce, multiplier);
            }
        }
        
        return multiplier;
    }
    
    /// <summary>
    /// Gets the fatigue accumulation multiplier with equipment reductions applied.
    /// Lower values mean slower fatigue accumulation.
    /// </summary>
    public float GetFatigueMultiplier(bool isOnSlope = false)
    {
        float multiplier = 1f;
        
        if (statModifierCalculator != null)
        {
            // Universal fatigue reduction
            multiplier = statModifierCalculator.GetModifiedValue(StatModifierType.UniversalFatigueReduce, multiplier);
            
            // Slope-specific reduction
            if (isOnSlope)
            {
            multiplier = statModifierCalculator.GetModifiedValue(StatModifierType.SlopeFatigueReduce, multiplier);
            }
        }
        
        return multiplier;
    }
    
    /// <summary>
    /// Gets the fatigue rest bonus with equipment applied.
    /// Higher values mean faster fatigue recovery when resting.
    /// </summary>
    public float GetFatigueRestBonus()
    {
        float bonus = 1f;
        
        if (statModifierCalculator != null)
        {
            bonus = statModifierCalculator.GetModifiedValue(StatModifierType.FatigueGainWhenRest, bonus);
        }
        
        return bonus;
    }
    
    #endregion

}
