using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory.Effects
{
    /// <summary>
    /// Factory for creating consumable effects from data
    /// Follows Factory Pattern
    /// Converts legacy ConsumableEffect to new IConsumableEffect
    /// </summary>
    public static class ConsumableEffectFactory
    {
        /// <summary>
        /// Creates effect instances from consumable effect data
        /// </summary>
        public static List<IConsumableEffect> CreateEffects(List<ConsumableEffect> effects)
        {
            var result = new List<IConsumableEffect>();
            
            if (effects == null)
                return result;
            
            foreach (var effect in effects)
            {
                var consumableEffect = CreateEffect(effect);
                if (consumableEffect != null)
                {
                    result.Add(consumableEffect);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a single effect from data
        /// </summary>
        public static IConsumableEffect CreateEffect(ConsumableEffect effect)
        {
            if (effect == null)
                return null;
            
            switch (effect.statType)
            {
                case StatType.Health:
                    return new HealthEffect(effect.value);
                    
                case StatType.Hunger:
                    return new HungerEffect(effect.value);
                    
                case StatType.Thirst:
                    return new ThirstEffect(effect.value);
                    
                case StatType.Stamina:
                    return new StaminaEffect(effect.value);
                    
                case StatType.Temperature:
                    return new TemperatureEffect(effect.value);
                    
                default:
                    Debug.LogWarning($"[ConsumableEffectFactory] Unknown stat type: {effect.statType}");
                    return null;
            }
        }
    }
}
