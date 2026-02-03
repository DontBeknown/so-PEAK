using UnityEngine;

namespace Game.Inventory.Effects
{
    /// <summary>
    /// Restores health when consumed
    /// Follows Single Responsibility - only affects health
    /// </summary>
    public class HealthEffect : ConsumableEffectBase
    {
        public HealthEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.Heal(_value);
                //Debug.Log($"[HealthEffect] Restored {_value} health");
            }
        }
        
        public override string GetDescription()
        {
            return $"+{_value} Health";
        }
        
        public override bool CanApply(object target)
        {
            return target is PlayerStats;
        }
    }
    
    /// <summary>
    /// Restores hunger when consumed
    /// </summary>
    public class HungerEffect : ConsumableEffectBase
    {
        public HungerEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.Eat(_value);
                //Debug.Log($"[HungerEffect] Restored {_value} hunger");
            }
        }
        
        public override string GetDescription() => $"+{_value} Hunger";
        public override bool CanApply(object target) => target is PlayerStats;
    }
    
    /// <summary>
    /// Restores thirst when consumed
    /// </summary>
    public class ThirstEffect : ConsumableEffectBase
    {
        public ThirstEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.Drink(_value);
                //Debug.Log($"[ThirstEffect] Restored {_value} thirst");
            }
        }
        
        public override string GetDescription() => $"+{_value} Thirst";
        public override bool CanApply(object target) => target is PlayerStats;
    }
    
    /// <summary>
    /// Restores stamina when consumed
    /// </summary>
    public class StaminaEffect : ConsumableEffectBase
    {
        public StaminaEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.RestoreStamina(_value);
                //Debug.Log($"[StaminaEffect] Restored {_value} stamina");
            }
        }
        
        public override string GetDescription() => $"+{_value} Stamina";
        public override bool CanApply(object target) => target is PlayerStats;
    }
    
    /// <summary>
    /// Modifies temperature when consumed
    /// </summary>
    public class TemperatureEffect : ConsumableEffectBase
    {
        public TemperatureEffect(float value) : base(value) { }
        
        public override void Apply(object target)
        {
            if (target is PlayerStats stats)
            {
                stats.ModifyTemperature(_value);
                //Debug.Log($"[TemperatureEffect] Modified temperature by {_value}");
            }
        }
        
        public override string GetDescription() => $"{(_value > 0 ? "+" : "")}{_value} Temperature";
        public override bool CanApply(object target) => target is PlayerStats;
    }
}
