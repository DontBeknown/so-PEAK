using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;

    [SerializeField] private HealthStat health;
    [SerializeField] private HungerStat hunger;
    [SerializeField] private ThirstStat thirst;
    [SerializeField] private StaminaStat stamina;

    public PlayerConfig Config => config;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action OnDeath;

    private bool isSprinting;

    private void Awake()
    {
        health ??= new HealthStat();
        hunger ??= new HungerStat();
        thirst ??= new ThirstStat();
        stamina ??= new StaminaStat();

        hunger.Init(config.hungerDrainPerSecond, config.hungerHurtThreshold, config.starvationDPS);
        thirst.Init(config.thirstDrainPerSecond, config.thirstHurtThreshold, config.dehydrationDPS);
        stamina.Init(config.staminaRegenPerSecond, config.staminaDrainCooldown, config.climbStaminaDrainPerSecond);
        stamina.InitFatigue(config.maxFatigue, config.fatigueRateTime, config.fatigueRateElev, config.fatigueRateSpeed);

        health.OnChanged += (c, m) => OnHealthChanged?.Invoke(c, m);
        stamina.OnChanged += (c, m) => OnStaminaChanged?.Invoke(c, m);
        health.OnDeath += () => OnDeath?.Invoke();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        hunger.Tick(dt);
        thirst.Tick(dt);
        stamina.Tick(dt);

        if (hunger.ShouldHurt)
        {
            health.Damage(hunger.StarveDPS * dt);
        }
            

        if (thirst.ShouldHurt)
        {
            health.Damage(thirst.DehydrateDPS * dt);
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

    public void OnSprint(bool sprinting)
    {
        isSprinting = sprinting;
    }

    public void ConsumeStamina(float amount) => stamina.Drain(amount);
    public void TakeDamage(float dmg) => health.Damage(dmg);
    public void Heal(float amount) => health.Heal(amount);
    public void Eat(float nutrition) => hunger.Add(nutrition);
    public void Drink(float water) => thirst.Add(water);

    public void ModifyTemperature(float amount)
    {
        // Add temperature stat if you haven't already, or modify existing temperature system
        // temperature.Add(amount);
        Debug.Log($"Temperature modified by {amount}");
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
        stamina.FullRest();
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
    
    // Expose stamina stat for advanced operations like terrain drain
    public StaminaStat StaminaStat => stamina;

    public IStat GetStat(StatType statType)
    {
        return statType switch
        {
            StatType.Health => health,
            StatType.Hunger => hunger,
            StatType.Thirst => thirst,
            StatType.Stamina => stamina,
            // StatType.Temperature => temperature, // Add when you implement temperature
            _ => null
        };
    }

}
