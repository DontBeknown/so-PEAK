using UnityEngine;

namespace Game.Player.Data
{
    /// <summary>
    /// Value object containing all player configuration data.
    /// Separates data from behavior for better testability.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        [Header("Movement Settings")]
        public float WalkSpeed = 3f;
        public float ClimbSpeed = 2f;
        public float JumpForce = 5f;
        public float RotationSmoothness = 10f;
        public float Gravity = -9.81f;

        [Header("Detection Settings")]
        public float ClimbDetectionRange = 1f;
        public float GroundCheckDistance = 0.3f;
        public LayerMask ClimbableLayer;
        public LayerMask GroundLayer;

        [Header("Stamina Settings")]
        public float JumpStaminaCost = 20f;
        public float SprintStaminaDrainPerSecond = 25f;
        public float ClimbStaminaDrainPerSecond = 10f;
        public float StaminaRegenPerSecond = 15f;
        public float StaminaDrainCooldown = 1f;

        [Header("Survival Settings")]
        public float HungerDrainPerSecond = 0.2f;
        public float HungerHurtThreshold = 30f;
        public float StarvationDPS = 1f;
        public float ThirstDrainPerSecond = 0.35f;
        public float ThirstHurtThreshold = 40f;
        public float DehydrationDPS = 2f;

        /// <summary>
        /// Creates PlayerData from a PlayerConfig ScriptableObject
        /// </summary>
        public static PlayerData FromConfig(PlayerConfig config)
        {
            return new PlayerData
            {
                WalkSpeed = config.baseWalkSpeed,
                ClimbSpeed = config.baseClimbSpeed,
                JumpForce = config.jumpForce,
                RotationSmoothness = config.rotationSmoothness,
                Gravity = config.gravity,
                ClimbDetectionRange = config.climbDetectionRange,
                GroundCheckDistance = config.groundCheckDistance,
                ClimbableLayer = config.climbableLayer,
                GroundLayer = config.groundLayer,
                JumpStaminaCost = config.jumpStaminaCost,
                SprintStaminaDrainPerSecond = config.sprintStaminaDrainPerSecond,
                ClimbStaminaDrainPerSecond = config.climbStaminaDrainPerSecond,
                StaminaRegenPerSecond = config.staminaRegenPerSecond,
                StaminaDrainCooldown = config.staminaDrainCooldown,
                HungerDrainPerSecond = config.hungerDrainPerSecond,
                HungerHurtThreshold = config.hungerHurtThreshold,
                StarvationDPS = config.starvationDPS,
                ThirstDrainPerSecond = config.thirstDrainPerSecond,
                ThirstHurtThreshold = config.thirstHurtThreshold,
                DehydrationDPS = config.dehydrationDPS
            };
        }

        /// <summary>
        /// Creates a deep copy of this PlayerData
        /// </summary>
        public PlayerData Clone()
        {
            return (PlayerData)this.MemberwiseClone();
        }
    }
}
