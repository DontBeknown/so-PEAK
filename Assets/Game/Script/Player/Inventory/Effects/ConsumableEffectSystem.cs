using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Inventory.Effects
{
    /// <summary>
    /// Applies consumable effects using Strategy pattern
    /// Extensible without modification (Open/Closed Principle)
    /// </summary>
    public class ConsumableEffectSystem : IConsumableEffectSystem
    {
        private readonly Dictionary<StatType, IEffectStrategy> _strategies;
        private readonly PlayerStats _playerStats;

        public ConsumableEffectSystem(PlayerStats playerStats)
        {
            _playerStats = playerStats;
            _strategies = new Dictionary<StatType, IEffectStrategy>();
            
            // Register default strategies
            RegisterDefaultStrategies();
        }

        private void RegisterDefaultStrategies()
        {
            RegisterEffectStrategy(StatType.Health, new HealthEffectStrategy());
            RegisterEffectStrategy(StatType.Hunger, new HungerEffectStrategy());
            RegisterEffectStrategy(StatType.Stamina, new StaminaEffectStrategy());
        }

        public void RegisterEffectStrategy(StatType statType, IEffectStrategy strategy)
        {
            if (_strategies.ContainsKey(statType))
            {
                Debug.LogWarning($"[ConsumableEffectSystem] Overwriting strategy for {statType}");
            }
            
            _strategies[statType] = strategy;
            //Debug.Log($"[ConsumableEffectSystem] Registered strategy for {statType}");
        }

        public bool CanApplyEffect(ConsumableEffect effect)
        {
            if (effect == null) return false;
            if (_playerStats == null) return false;
            
            return _strategies.ContainsKey(effect.statType);
        }

        public void ApplyEffect(ConsumableEffect effect, PlayerStats stats)
        {
            if (!CanApplyEffect(effect))
            {
                Debug.LogWarning($"[ConsumableEffectSystem] Cannot apply effect for {effect.statType}");
                return;
            }

            var strategy = _strategies[effect.statType];
            strategy.Apply(effect, stats);
        }
    }
}
