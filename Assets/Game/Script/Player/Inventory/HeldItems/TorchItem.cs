using UnityEngine;
using Game.Player.Inventory.HeldItems;

/// <summary>
/// Torch item - provides light and warmth, depletes durability when equipped.
/// Destroyed when durability reaches 0.
/// Follows Single Responsibility Principle.
/// </summary>
[CreateAssetMenu(fileName = "New Torch", menuName = "Inventory/Held Items/Torch")]
public class TorchItem : HeldEquipmentItem
{
    [Header("Torch Settings")]
    [SerializeField] private float maxDurabilitySeconds = 300f; // 5 minutes
    [SerializeField] private float durabilityDrainRate = 1f; // seconds per second (1.0 = normal time)
    [SerializeField] private float warmthBonus = 10f;
    
    [Header("Light Settings")]
    [SerializeField] private float lightRadius = 10f;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private Color lightColor = new Color(1f, 0.6f, 0.2f); // Warm orange
    [SerializeField] private float lowDurabilityThreshold = 0.2f; // 20%
    
    [Header("Audio")]
    [SerializeField] private AudioClip igniteSound;
    [SerializeField] private AudioClip cracklingSoundLoop;

    public float MaxDurabilitySeconds => maxDurabilitySeconds;
    public float DurabilityDrainRate => durabilityDrainRate;
    public float WarmthBonus => warmthBonus;
    public float LightRadius => lightRadius;
    public float LightIntensity => lightIntensity;
    public Color LightColor => lightColor;
    public float LowDurabilityThreshold => lowDurabilityThreshold;
    public AudioClip IgniteSound => igniteSound;
    public AudioClip CracklingSoundLoop => cracklingSoundLoop;

    public override IHeldItemBehavior CreateBehavior(GameObject playerObject)
    {
        var behavior = playerObject.AddComponent<TorchBehavior>();
        behavior.Initialize(this);
        return behavior;
    }

    public override string GetStateDescription()
    {
        EnsureStateInitialized();
        var state = GetState();
        float percentage = (state.currentDurability / state.maxDurability) * 100f;
        return $"{Mathf.RoundToInt(percentage)}%";
    }

    protected override void InitializeDefaultState(HeldItemState state)
    {
        state.maxDurability = maxDurabilitySeconds;
        state.currentDurability = maxDurabilitySeconds;
    }

    /// <summary>
    /// Checks if torch has any durability left.
    /// </summary>
    public bool HasDurability()
    {
        EnsureStateInitialized();
        var state = GetState();
        return state.currentDurability > 0;
    }

    /// <summary>
    /// Gets the current durability percentage (0-1).
    /// </summary>
    public float GetDurabilityPercentage()
    {
        EnsureStateInitialized();
        var state = GetState();
        return state.currentDurability / state.maxDurability;
    }
}
