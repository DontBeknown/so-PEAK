using UnityEngine;

[CreateAssetMenu(menuName = "Config/PlayerConfig", fileName = "PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement - Base Values")]
    [Tooltip("Base walk speed on flat terrain")]
    public float baseWalkSpeed = 3f;
    [Tooltip("Max sprint speed (1.5x walk speed)")]
    public float baseRunSpeed = 4.5f;
    [Tooltip("How fast speed ramps between walk and run (higher = snappier)")]
    public float runAcceleration = 8f;
    [Tooltip("Base climb speed")]
    public float baseClimbSpeed = 2f;
    public float jumpForce = 5f;
    public float rotationSmoothness = 10f;
    
    [Header("Movement - Slope Modifiers")]
    [Tooltip("Minimum speed multiplier on steep slopes (0.7 = 70% speed)")]
    public float minSlopeSpeedMultiplier = 0.7f;
    [Tooltip("Maximum speed multiplier on flat/downhill (1.0 = 100% speed)")]
    public float maxSlopeSpeedMultiplier = 1f;
    
    [Header("Movement - Physics")]
    public float climbDetectionRange = 1f;
    [Tooltip("Multiplier of CharacterController radius used for climb detection sphere cast")]
    public float climbDetectionRadiusMultiplier = 0.9f;
    [Tooltip("Minimum wall angle from up vector required to be climbable (90 = perfectly vertical wall)")]
    [Range(0f, 180f)]
    public float minClimbableWallAngle = 70f;
    [Tooltip("Maximum wall angle from up vector allowed to be climbable")]
    [Range(0f, 180f)]
    public float maxClimbableWallAngle = 110f;
    [Tooltip("Maximum angle between player forward and wall inward direction to allow climbing")]
    [Range(0f, 180f)]
    public float maxClimbApproachAngle = 60f;
    public LayerMask climbableLayer;
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer = -1;

    [Header("Coyote Time")]
    [Tooltip("Grace period (seconds) after leaving the ground before transitioning to fall state")]
    public float coyoteTimeDuration = 0.15f;

    [Header("Stamina")]
    public float jumpStaminaCost = 20f;
    public float sprintStaminaDrainPerSecond = 25f;
    public float climbStaminaDrainPerSecond = 10f;
    [Tooltip("Speed multiplier applied when stamina reaches 0 (e.g. 0.4 = 40% of normal speed)")]
    public float staminaExhaustedSpeedMultiplier = 0.4f;

    [Header("Stamina Regen")]
    public float staminaRegenPerSecond = 15f;
    public float staminaDrainCooldown = 1f;

    [Header("Terrain Stamina Drain")]
    [Tooltip("Base stamina drain per meter moved on flat ground")]
    public float baseMovementStaminaDrain = 0.5f;
    [Tooltip("Angle (degrees) where uphill drain starts increasing")]
    public float slopeStartAngle = 15f;
    [Tooltip("Angle (degrees) where drain reaches maximum")]
    public float slopeMaxAngle = 45f;
    [Tooltip("Maximum multiplier for uphill movement")]
    public float uphillDrainMultiplier = 3f;
    [Tooltip("Multiplier for downhill movement (usually lower)")]
    public float downhillDrainMultiplier = 0.3f;
    [Tooltip("Minimum movement speed to drain stamina")]
    public float movementThreshold = 0.1f;

    [Header("Fatigue System")]
    [Tooltip("Maximum fatigue value (100 = completely exhausted)")]
    public float maxFatigue = 100f;
    [Tooltip("Fatigue rate over time (per second)")]
    public float fatigueRateTime = 0.12f;
    [Tooltip("Fatigue rate based on elevation/slope")]
    public float fatigueRateElev = 0.0005f;
    [Tooltip("Multiplier for fatigue accumulation on slopes")]
    public float slopeFatigueMultiplier = 2f;
    [Tooltip("Fatigue threshold where stamina regen is reduced (50 = 50% fatigue)")]
    public float fatigueStaminaPenaltyThreshold = 50f;
    [Tooltip("Fatigue threshold where speed is reduced (70 = 70% fatigue)")]
    public float fatigueSpeedPenaltyThreshold = 70f;

    [Header("Hunger")]
    public float hungerDrainPerSecond = 0.2f;
    public float hungerHurtThreshold = 30f;
    public float starvationDPS = 1f;
    [Tooltip("Hunger drains this many times faster while sprinting")]
    public float hungerSprintMultiplier = 2f;

    [Header("Fall Damage")]
    [Tooltip("Fall speed (m/s) below which no damage is taken")]
    public float fallDamageSafeSpeed = 8f;
    [Tooltip("Fall speed (m/s) at which maximum damage is dealt")]
    public float fallDamageLethalSpeed = 25f;
    [Tooltip("Maximum damage at lethal fall speed")]
    public float fallDamageMax = 100f;

    [Header("Thirst")]
    public float thirstDrainPerSecond = 0.35f;
    public float thirstHurtThreshold = 40f;
    public float dehydrationDPS = 2f;
    [Tooltip("Thirst drains this many times faster while sprinting")]
    public float thirstSprintMultiplier = 2.5f;

    [Header("Temperature — Thresholds & Damage")]
    [Tooltip("Body temperature (°C) below which cold damage and penalties apply")]
    public float tempColdThreshold = 12f;
    [Tooltip("Body temperature (°C) above which heat damage and penalties apply")]
    public float tempHotThreshold = 42f;
    [Tooltip("Health damage per second when freezing")]
    public float tempColdDPS = 1.5f;
    [Tooltip("Health damage per second when overheating")]
    public float tempHotDPS = 2f;
    [Tooltip("How fast (°C/s) body temperature drifts toward the environment temperature")]
    public float tempDriftRate = 2f;

    [Header("Temperature — Survival Penalties")]
    [Tooltip("Body temperature (°C) below which cold movement speed penalty starts.")]
    public float tempColdSpeedPenaltyThreshold = 24f;
    [Tooltip("Body temperature (°C) below which extra hunger drain from cold starts.")]
    public float tempColdHungerPenaltyThreshold = 18f;
    [Tooltip("Body temperature (°C) above which extra thirst drain from heat starts.")]
    public float tempHotThirstPenaltyThreshold = 38f;
    [Tooltip("Minimum walk/run speed multiplier at absolute cold (0°C). 0.65 = 35% slow.")]
    [Range(0.1f, 1f)]
    public float tempColdSpeedMinMultiplier = 0.65f;
    [Tooltip("Maximum hunger drain multiplier when freezing. 1.3 = 30% faster drain.")]
    [Range(1f, 3f)]
    public float tempHungerColdMaxMultiplier = 1.3f;
    [Tooltip("Maximum thirst drain multiplier when overheating. 1.5 = 50% faster drain.")]
    [Range(1f, 3f)]
    public float tempThirstHotMaxMultiplier = 1.5f;

    [Header("Temperature — Heat Sources")]
    [Tooltip("Radius in which the player detects ITemperatureSource objects (campfires, hot springs, etc.)")]
    public float tempHeatSourceRadius = 5f;
    [Tooltip("Layer(s) that can contain ITemperatureSource objects")]
    public LayerMask tempHeatSourceLayer;

    [Header("Temperature — Environment Curves by Level")]
    [Tooltip("Level 1: neutral environment all day. Keep at 37°C across the full 24-hour cycle.")]
    public AnimationCurve temperatureDayCurveLevel1 = new AnimationCurve(
        new Keyframe(0f,    50f),
        new Keyframe(0.25f, 50f),
        new Keyframe(0.5f,  50f),
        new Keyframe(0.75f, 50f),
        new Keyframe(1f,    50f)
    );

    [Tooltip("Level 2: cold at night, hot in daytime, but stays below critical thresholds by default.")]
    public AnimationCurve temperatureDayCurveLevel2 = new AnimationCurve(
        new Keyframe(0f,    18f),
        new Keyframe(0.25f, 24f),
        new Keyframe(0.5f,  39f),
        new Keyframe(0.75f, 28f),
        new Keyframe(1f,    18f)
    );

    [Tooltip("Level 3: always cold, with critical cold at night.")]
    public AnimationCurve temperatureDayCurveLevel3 = new AnimationCurve(
        new Keyframe(0f,    8f),
        new Keyframe(0.25f, 14f),
        new Keyframe(0.5f,  19f),
        new Keyframe(0.75f, 14f),
        new Keyframe(1f,    8f)
    );
}
