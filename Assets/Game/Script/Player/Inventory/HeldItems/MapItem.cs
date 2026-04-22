using UnityEngine;
using Game.Player.Inventory.HeldItems;

/// <summary>
/// Held equipment item that opens a map viewer when clicked while equipped.
/// </summary>
[CreateAssetMenu(fileName = "New Map Item", menuName = "Inventory/Held Items/Map")]
public class MapItem : HeldEquipmentItem
{
    [Header("Map Data")]
    [SerializeField] private HeldMapData mapData;

    public HeldMapData MapData => mapData;

    public override IHeldItemBehavior CreateBehavior(GameObject playerObject)
    {
        var behavior = playerObject.AddComponent<MapBehavior>();
        behavior.Initialize(this);
        return behavior;
    }

    public override string GetStateDescription()
    {
        return mapData != null ? mapData.MapTitle : itemName;
    }

    protected override void InitializeDefaultState(HeldItemState state)
    {
        if (state == null)
            return;

        state.maxCharges = 1;
        state.currentCharges = 1;
        state.maxDurability = 1f;
        state.currentDurability = 1f;
    }
}