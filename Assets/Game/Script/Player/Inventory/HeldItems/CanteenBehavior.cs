using UnityEngine;
using Game.Player.Inventory.HeldItems;

/// <summary>
/// Runtime behavior for canteen when equipped.
/// Primarily visual - the canteen can be used without being equipped.
/// Follows Single Responsibility Principle.
/// </summary>
public class CanteenBehavior : MonoBehaviour, IHeldItemBehavior
{
    // Injected by HeldItemBehaviorManager (no Inspector assignment needed)
    [SerializeField] private Transform rightHandBone;
    
    private CanteenItem canteenItem;
    private GameObject visualPrefabInstance;
    private bool isEquipped = false;

    public void Initialize(CanteenItem item)
    {
        canteenItem = item;
    }

    public void OnEquipped()
    {
        if (canteenItem == null)
        {
            Debug.LogError("[CanteenBehavior] CanteenItem is null!");
            return;
        }

        isEquipped = true;

        // Spawn visual prefab
        SpawnVisualPrefab();

        //Debug.Log($"[CanteenBehavior] Canteen equipped - charges: {canteenItem.GetStateDescription()}");
    }

    public void OnUnequipped()
    {
        isEquipped = false;

        // Destroy visual prefab
        DestroyVisualPrefab();

        //Debug.Log("[CanteenBehavior] Canteen unequipped");
    }

    public void UpdateBehavior()
    {
        // Canteen doesn't need per-frame updates when equipped
        // Drinking and refilling are handled externally
    }

    public string GetStateDescription()
    {
        return canteenItem?.GetStateDescription() ?? "N/A";
    }

    public bool IsUsable()
    {
        return canteenItem != null;
    }

    private void SpawnVisualPrefab()
    {
        if (canteenItem.HeldItemPrefab != null)
        {
            // Instantiate in world space first (no parent)
            visualPrefabInstance = Instantiate(canteenItem.HeldItemPrefab);
            
            // Parent to right hand bone if found, otherwise use player transform
            if (rightHandBone != null)
            {
                visualPrefabInstance.transform.SetParent(rightHandBone);
                visualPrefabInstance.transform.localPosition = Vector3.zero;
                visualPrefabInstance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Fallback if hand bone not assigned - use belt/hip position
                visualPrefabInstance.transform.SetParent(transform);
                visualPrefabInstance.transform.localPosition = Vector3.right * 0.4f + Vector3.forward * 0.2f + Vector3.up * -0.2f;
                visualPrefabInstance.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                Debug.LogWarning("[CanteenBehavior] rightHandBone not assigned! Using fallback hip position");
            }
        }
    }

    private void DestroyVisualPrefab()
    {
        if (visualPrefabInstance != null)
        {
            Destroy(visualPrefabInstance);
            visualPrefabInstance = null;
        }
    }

    private void OnDestroy()
    {
        // Cleanup if destroyed unexpectedly
        if (isEquipped)
        {
            OnUnequipped();
        }
    }
}
