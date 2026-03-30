using UnityEngine;
using Game.Player.Inventory.HeldItems;
using Game.Core.DI;
using Game.Core.Events;

/// <summary>
/// Canteen item - refillable water container with multiple charges.
/// Can be used (drunk) from inventory without equipping.
/// Must be equipped to refill at water sources.
/// Follows Single Responsibility Principle.
/// </summary>
[CreateAssetMenu(fileName = "New Canteen", menuName = "Inventory/Held Items/Canteen")]
public class CanteenItem : HeldEquipmentItem
{
    [Header("Canteen Settings")]
    [SerializeField] private int maxCharges = 5;
    [SerializeField] private float thirstRestorationPerSip = 20f;
    [SerializeField] private float useCooldownSeconds = 2f;
    [SerializeField] private float refillDurationSeconds = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip drinkSound;
    [SerializeField] private AudioClip refillSound;

    public int MaxCharges => maxCharges;
    public float ThirstRestorationPerSip => thirstRestorationPerSip;
    public float UseCooldownSeconds => useCooldownSeconds;
    public float RefillDurationSeconds => refillDurationSeconds;


    public override IHeldItemBehavior CreateBehavior(GameObject playerObject)
    {
        var behavior = playerObject.AddComponent<CanteenBehavior>();
        behavior.Initialize(this);
        return behavior;
    }

    public override string GetStateDescription()
    {
        EnsureStateInitialized();
        var state = GetState();
        return $"{state.currentCharges}/{state.maxCharges}";
    }

    protected override void InitializeDefaultState(HeldItemState state)
    {
        state.maxCharges = maxCharges;
        state.currentCharges = maxCharges; // Start full
        state.lastUsedTime = -useCooldownSeconds; // Can use immediately
    }

    /// <summary>
    /// Checks if canteen has charges and is off cooldown.
    /// </summary>
    public bool CanDrink()
    {
        EnsureStateInitialized();
        var state = GetState();
        
        bool hasCharges = state.currentCharges > 0;
        bool offCooldown = (Time.time - state.lastUsedTime) >= useCooldownSeconds;
        
        return hasCharges && offCooldown;
    }

    /// <summary>
    /// Consumes one charge and applies thirst effect.
    /// Returns true if successful.
    /// </summary>
    public bool Drink(PlayerStats playerStats)
    {
        if (!CanDrink())
            return false;

        EnsureStateInitialized();
        var state = GetState();

        // Consume charge
        state.currentCharges--;
        state.lastUsedTime = Time.time;

        // Apply thirst effect
        if (playerStats != null)
        {
            playerStats.Drink(thirstRestorationPerSip);
            //Debug.Log($"[CanteenItem] Drank from canteen - restored {thirstRestorationPerSip} thirst. Charges: {state.currentCharges}/{state.maxCharges}");
        }

        ServiceContainer.Instance
            .TryGet<IEventBus>()
            ?.Publish(new Game.Player.Inventory.Events.ItemConsumedEvent(this));

        return true;
    }

    /// <summary>
    /// Refills the canteen to max charges.
    /// </summary>
    public void Refill()
    {
        EnsureStateInitialized();
        var state = GetState();
        state.currentCharges = state.maxCharges;

        // Play refill sound
        if (refillSound != null)
        {
            AudioSource.PlayClipAtPoint(refillSound, Camera.main.transform.position);
        }

        //Debug.Log($"[CanteenItem] Canteen refilled to {state.maxCharges} charges");
    }

    /// <summary>
    /// Checks if canteen is full.
    /// </summary>
    public bool IsFull()
    {
        EnsureStateInitialized();
        var state = GetState();
        return state.currentCharges >= state.maxCharges;
    }

    /// <summary>
    /// Checks if canteen is empty.
    /// </summary>
    public bool IsEmpty()
    {
        EnsureStateInitialized();
        var state = GetState();
        return state.currentCharges <= 0;
    }

    /// <summary>
    /// Gets current charges.
    /// </summary>
    public int GetCurrentCharges()
    {
        EnsureStateInitialized();
        return GetState().currentCharges;
    }
}
