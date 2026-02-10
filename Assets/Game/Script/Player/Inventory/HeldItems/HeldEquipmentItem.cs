using UnityEngine;
using Game.Player.Inventory.HeldItems;

/// <summary>
/// Base class for held equipment items (torch, canteen) that have runtime state.
/// Extends EquipmentItem and is designed for HeldItem slot.
/// Follows Open/Closed Principle - open for extension, closed for modification.
/// </summary>
public abstract class HeldEquipmentItem : EquipmentItem
{
    [Header("Held Item Properties")]
    [SerializeField] protected GameObject heldItemPrefab; // Visual prefab to spawn on player

    /// <summary>
    /// Gets the unique state ID for this item instance.
    /// Override to provide instance-specific IDs if needed.
    /// </summary>
    public virtual string GetStateID()
    {
        // Default: use item name as ID
        // For multiple instances, this should be overridden to use instance IDs
        return itemName;
    }

    /// <summary>
    /// Gets the runtime state for this item.
    /// </summary>
    public HeldItemState GetState()
    {
        return HeldItemStateManager.Instance.GetOrCreateState(GetStateID());
    }

    /// <summary>
    /// Creates a behavior component for this item on the player.
    /// Must be implemented by derived classes.
    /// </summary>
    public abstract IHeldItemBehavior CreateBehavior(GameObject playerObject);

    /// <summary>
    /// Gets a description of the current state (for UI display).
    /// </summary>
    public abstract string GetStateDescription();

    /// <summary>
    /// Gets the visual prefab to spawn when this item is held.
    /// </summary>
    public GameObject HeldItemPrefab => heldItemPrefab;

    /// <summary>
    /// Initializes default state for this item.
    /// Called when item is first created or state doesn't exist.
    /// </summary>
    protected abstract void InitializeDefaultState(HeldItemState state);

    /// <summary>
    /// Ensures state is initialized.
    /// </summary>
    protected void EnsureStateInitialized()
    {
        var state = GetState();
        if (state.maxCharges == 0 && state.maxDurability == 0)
        {
            InitializeDefaultState(state);
        }
    }
}
