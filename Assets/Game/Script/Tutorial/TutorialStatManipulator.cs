using Game.Core.DI;
using Game.Player.Inventory;
using UnityEngine;

namespace Game.Tutorial
{
    /// <summary>
    /// Trigger volume that manipulates player stats when entered.
    /// Used in the tutorial level to create hunger/thirst/damage scenarios.
    /// Requires a Trigger Collider on the same GameObject.
    /// </summary>
    public class TutorialStatManipulator : MonoBehaviour
    {
        [Header("Hunger")]
        [SerializeField] private bool drainHunger = false;
        [Tooltip("Target hunger value to set (0–100). Player hunger is drained to this level.")]
        [SerializeField, Range(0f, 100f)] private float hungerTarget = 20f;

        [Header("Thirst")]
        [SerializeField] private bool drainThirst = false;
        [Tooltip("Target thirst value to set (0–100). Player thirst is drained to this level.")]
        [SerializeField, Range(0f, 100f)] private float thirstTarget = 15f;

        [Header("Damage")]
        [SerializeField] private bool dealDamage = false;
        [SerializeField] private float damageAmount = 30f;

        [Header("Canteen")]
        [SerializeField] private bool drainCanteenCharges = false;

        [Header("Behaviour")]
        [SerializeField] private bool oneTimeOnly = true;

        private bool _triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (oneTimeOnly && _triggered) return;

            var stats = other.GetComponentInParent<PlayerStats>();
            if (stats == null) return;

            _triggered = true;

            if (drainHunger)
            {
                float delta = hungerTarget - stats.Hunger;
                if (delta < 0f) stats.Eat(delta);
            }

            if (drainThirst)
            {
                float delta = thirstTarget - stats.Thirst;
                if (delta < 0f) stats.Drink(delta);
            }

            if (dealDamage)
                stats.TakeDamage(damageAmount);

            if (drainCanteenCharges)
            {
                var storage = ServiceContainer.Instance.TryGet<IInventoryStorage>();
                if (storage != null)
                {
                    foreach (var slot in storage.GetAllSlots())
                    {
                        if (slot.item is CanteenItem canteen)
                        {
                            canteen.DrainCharges();
                            break;
                        }
                    }
                }
            }

            if (oneTimeOnly)
                enabled = false;
        }
    }
}
