using UnityEngine;

namespace Game.Inventory.Effects
{
    /// <summary>
    /// Base class for consumable effects
    /// Provides common functionality
    /// </summary>
    public abstract class ConsumableEffectBase : IConsumableEffect
    {
        protected float _value;
        
        protected ConsumableEffectBase(float value)
        {
            _value = value;
        }
        
        public abstract void Apply(object target);
        public abstract string GetDescription();
        
        public virtual bool CanApply(object target)
        {
            return target != null;
        }
    }
}
