using UnityEngine;

[CreateAssetMenu(menuName = "Config/PlayerConfig", fileName = "PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float climbSpeed = 2f;
    public float jumpForce = 5f;
    public float rotationSmoothness = 10f;
    public float climbDetectionRange = 1f;
    public LayerMask climbableLayer;
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer = -1;

    [Header("Stamina")]
    public float jumpStaminaCost = 20f;
    public float sprintStaminaDrainPerSecond = 25f;
    public float climbStaminaDrainPerSecond = 10f;

    [Header("Stamina Regen")]
    public float staminaRegenPerSecond = 15f;
    public float staminaDrainCooldown = 1f;

    [Header("Hunger")]
    public float hungerDrainPerSecond = 0.2f;
    public float hungerHurtThreshold = 30f;
    public float starvationDPS = 1f;

    [Header("Thirst")]
    public float thirstDrainPerSecond = 0.35f;
    public float thirstHurtThreshold = 40f;
    public float dehydrationDPS = 2f;


}
