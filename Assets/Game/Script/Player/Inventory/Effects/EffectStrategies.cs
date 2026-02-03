using UnityEngine;

namespace Game.Player.Inventory.Effects
{
    /// <summary>
    /// Strategy for Health stat modification
    /// </summary>
    public class HealthEffectStrategy : IEffectStrategy
    {
        public void Apply(ConsumableEffect effect, PlayerStats stats)
        {
            if (stats == null)
            {
                Debug.LogWarning("[HealthEffectStrategy] PlayerStats is null");
                return;
            }

            stats.Heal(effect.value);
            
            //Debug.Log($"[HealthEffectStrategy] Applied {effect.value} health. " + $"Current: {stats.Health}/{stats.MaxHealth}");
        }
    }

    /// <summary>
    /// Strategy for Hunger stat modification
    /// </summary>
    public class HungerEffectStrategy : IEffectStrategy
    {
        public void Apply(ConsumableEffect effect, PlayerStats stats)
        {
            if (stats == null)
            {
                Debug.LogWarning("[HungerEffectStrategy] PlayerStats is null");
                return;
            }

            stats.Eat(effect.value);
            
            //Debug.Log($"[HungerEffectStrategy] Applied {effect.value} hunger. " + $"Current: {stats.Hunger}/{stats.MaxHunger}");
        }
    }

    /// <summary>
    /// Strategy for Stamina stat modification
    /// </summary>
    public class StaminaEffectStrategy : IEffectStrategy
    {
        public void Apply(ConsumableEffect effect, PlayerStats stats)
        {
            if (stats == null)
            {
                Debug.LogWarning("[StaminaEffectStrategy] PlayerStats is null");
                return;
            }

            stats.RestoreStamina(effect.value);
            
            //Debug.Log($"[StaminaEffectStrategy] Applied {effect.value} stamina. " + $"Current: {stats.Stamina}/{stats.MaxStamina}");
        }
    }
}
