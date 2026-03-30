using UnityEngine;
using System.Collections.Generic;
using Game.Core.DI;
using Game.Player.Inventory.HeldItems;

/// <summary>
/// Manages runtime behaviors for held items (torch, canteen).
/// Listens for equipment changes and creates/destroys behavior components.
/// Follows Single Responsibility Principle.
/// </summary>
public class HeldItemBehaviorManager : MonoBehaviour
{
    private Dictionary<HeldEquipmentItem, IHeldItemBehavior> activeBehaviors = new Dictionary<HeldEquipmentItem, IHeldItemBehavior>();
    private EquipmentManager equipmentManager;
    private GameObject playerObject;
    
    // Cached bone references (found once)
    private Transform rightHandBone;
    private Transform leftHandBone;

    private void Awake()
    {
        // Get player object (this component should be on the player)
        playerObject = gameObject;
        
        // Find hand bones in character rig
        FindHandBones();
    }

    private void Start()
    {
        // Get equipment manager from ServiceContainer
        // Done in Start() to ensure EquipmentManager has been registered by GameServiceBootstrapper
        equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();
        
        if (equipmentManager != null)
        {
            // Subscribe to equipment events
            equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
        }
        else
        {
            Debug.LogWarning("[HeldItemBehaviorManager] EquipmentManager not found in ServiceContainer!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from equipment events
        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
        }
        
        // Clean up all active behaviors
        foreach (var behavior in activeBehaviors.Values)
        {
            if (behavior != null)
            {
                behavior.OnUnequipped();
            }
        }
        activeBehaviors.Clear();
    }

    private void OnEquipmentChanged(EquipmentSlotType slotType, IEquippable item)
    {
        // Only handle HeldItem slot
        if (slotType != EquipmentSlotType.HeldItem)
            return;

        // Check if item was unequipped (item is null)
        if (item == null)
        {
            HandleItemUnequipped();
            return;
        }

        // Check if item is a held equipment item
        HeldEquipmentItem heldItem = item as HeldEquipmentItem;
        if (heldItem != null)
        {
            HandleItemEquipped(heldItem);
        }
    }

    private void HandleItemEquipped(HeldEquipmentItem item)
    {
        // Clean up any existing behavior first
        HandleItemUnequipped();

        // Create new behavior
        IHeldItemBehavior behavior = item.CreateBehavior(playerObject);
        if (behavior != null)
        {
            activeBehaviors[item] = behavior;
            
            // Inject hand bone references into behavior
            InjectDependencies(behavior);
            
            behavior.OnEquipped();
            //Debug.Log($"[HeldItemBehaviorManager] Created and activated behavior for {item.itemName}");
        }
    }

    private void HandleItemUnequipped()
    {
        // Unequip and destroy all active behaviors
        foreach (var kvp in activeBehaviors)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnUnequipped();
                
                // Destroy the behavior component if it's a MonoBehaviour
                if (kvp.Value is MonoBehaviour behaviorMono)
                {
                    Destroy(behaviorMono);
                }
            }
        }
        activeBehaviors.Clear();
        //Debug.Log("[HeldItemBehaviorManager] Unequipped and destroyed all held item behaviors");
    }

    /// <summary>
    /// Gets the currently active behavior, if any.
    /// </summary>
    public IHeldItemBehavior GetActiveBehavior()
    {
        if (activeBehaviors.Count > 0)
        {
            foreach (var behavior in activeBehaviors.Values)
            {
                return behavior;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a specific item has an active behavior.
    /// </summary>
    public bool HasActiveBehavior(HeldEquipmentItem item)
    {
        return activeBehaviors.ContainsKey(item);
    }
    
    /// <summary>
    /// Finds hand bones in character rig hierarchy.
    /// Called once in Awake() to cache references.
    /// </summary>
    private void FindHandBones()
    {
        // Common hand bone names across different character rigs
        string[] rightHandNames = { "RightHandEquip" };
        string[] leftHandNames = { "LeftHandEquip" };
        
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        
        // Find right hand
        foreach (string boneName in rightHandNames)
        {
            foreach (Transform t in allTransforms)
            {
                if (t.name == boneName)
                {
                    rightHandBone = t;
                    //Debug.Log($"[HeldItemBehaviorManager] Found right hand bone: {t.name}");
                    break;
                }
            }
            if (rightHandBone != null) break;
        }
        
        // Find left hand
        foreach (string boneName in leftHandNames)
        {
            foreach (Transform t in allTransforms)
            {
                if (t.name == boneName)
                {
                    leftHandBone = t;
                    //Debug.Log($"[HeldItemBehaviorManager] Found left hand bone: {t.name}");
                    break;
                }
            }
            if (leftHandBone != null) break;
        }
        
        if (rightHandBone == null)
            Debug.LogWarning("[HeldItemBehaviorManager] Could not find right hand bone in character rig");
        if (leftHandBone == null)
            Debug.LogWarning("[HeldItemBehaviorManager] Could not find left hand bone in character rig");
    }
    
    /// <summary>
    /// Injects dependencies (hand bones) into behavior components.
    /// Uses reflection to find and set fields marked with [SerializeField].
    /// </summary>
    private void InjectDependencies(IHeldItemBehavior behavior)
    {
        if (behavior is TorchBehavior torchBehavior)
        {
            // Use reflection to set the private field
            var field = typeof(TorchBehavior).GetField("rightHandBone", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(torchBehavior, rightHandBone);
        }
        else if (behavior is CanteenBehavior canteenBehavior)
        {
            // Use reflection to set the private field
            var field = typeof(CanteenBehavior).GetField("rightHandBone", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(canteenBehavior, rightHandBone);
        }
    }
}
