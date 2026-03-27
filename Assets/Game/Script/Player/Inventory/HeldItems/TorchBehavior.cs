using UnityEngine;
using Game.Player.Inventory.HeldItems;
using Game.Core.DI;

/// <summary>
/// Runtime behavior for torch - manages light, durability depletion, and warmth bonus.
/// Attached to player when torch is equipped.
/// Follows Single Responsibility Principle.
/// </summary>
public class TorchBehavior : MonoBehaviour, IHeldItemBehavior
{
    // Injected by HeldItemBehaviorManager (no Inspector assignment needed)
    [SerializeField] private Transform rightHandBone;
    
    private TorchItem torchItem;
    private Light torchLight;
    private AudioSource loopingAudio;
    private GameObject visualPrefabInstance;
    private PlayerStats playerStats;
    private bool isEquipped = false;

    public void Initialize(TorchItem item)
    {
        torchItem = item;
    }

    public void OnEquipped()
    {
        if (torchItem == null)
        {
            Debug.LogError("[TorchBehavior] TorchItem is null!");
            return;
        }

        isEquipped = true;

        // Get player stats for warmth bonus
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();

        // Create light component
        CreateLight();

        // Apply warmth bonus
        ApplyWarmthBonus();

        // Spawn visual prefab
        SpawnVisualPrefab();

        // Play ignite sound
        PlayIgniteSound();

        // Start looping crackling sound
        StartLoopingSound();

        //Debug.Log($"[TorchBehavior] Torch equipped - durability: {torchItem.GetStateDescription()}");
    }

    public void OnUnequipped()
    {
        isEquipped = false;

        // Remove light
        DestroyLight();

        // Remove warmth bonus
        RemoveWarmthBonus();

        // Destroy visual prefab
        DestroyVisualPrefab();

        // Stop looping sound
        StopLoopingSound();

        //Debug.Log("[TorchBehavior] Torch unequipped");
    }

    public void UpdateBehavior()
    {
        if (!isEquipped || torchItem == null)
            return;

        // Deplete durability
        DepleteDurability();

        // Update light intensity based on durability
        UpdateLightIntensity();

        // Check if torch should be destroyed
        CheckDestruction();
    }

    public string GetStateDescription()
    {
        return torchItem?.GetStateDescription() ?? "N/A";
    }

    public bool IsUsable()
    {
        return torchItem != null && torchItem.HasDurability();
    }

    private void Update()
    {
        if (isEquipped)
        {
            UpdateBehavior();
        }
    }

    private void CreateLight()
    
    {
        var lightObject = new GameObject("TorchLight");
        lightObject.transform.SetParent(transform);
        lightObject.transform.localPosition = new Vector3(1.21f, 2.385f, 1.325f);

        torchLight = lightObject.AddComponent<Light>();
        torchLight.type = LightType.Point;
        torchLight.range = torchItem.LightRadius;
        torchLight.intensity = torchItem.LightIntensity;
        torchLight.color = torchItem.LightColor;
        torchLight.shadows = LightShadows.Soft;
    }

    private void DestroyLight()
    {
        if (torchLight != null)
        {
            Destroy(torchLight.gameObject);
            torchLight = null;
        }
    }

    private void ApplyWarmthBonus()
    {
        if (playerStats != null)
        {
            playerStats.ModifyTemperature(torchItem.WarmthBonus);
            //Debug.Log($"[TorchBehavior] Applied warmth bonus: +{torchItem.WarmthBonus}");
        }
    }

    private void RemoveWarmthBonus()
    {
        if (playerStats != null)
        {
            playerStats.ModifyTemperature(-torchItem.WarmthBonus);
            //Debug.Log($"[TorchBehavior] Removed warmth bonus: -{torchItem.WarmthBonus}");
        }
    }

    private void SpawnVisualPrefab()
    {
        if (torchItem.HeldItemPrefab != null)
        {
            // Instantiate in world space first (no parent)
            visualPrefabInstance = Instantiate(torchItem.HeldItemPrefab);
            
            
            // Parent to hand bone if found, otherwise use player transform
            if (rightHandBone != null)
            {
                visualPrefabInstance.transform.SetParent(rightHandBone);
                visualPrefabInstance.transform.localPosition = Vector3.zero;
                visualPrefabInstance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Fallback if hand bone not assigned
                visualPrefabInstance.transform.SetParent(transform);
                visualPrefabInstance.transform.localPosition = Vector3.right * 0.5f + Vector3.forward * 0.3f;
                visualPrefabInstance.transform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
                Debug.LogWarning("[TorchBehavior] rightHandBone not assigned! Assign it in Inspector: Character Rig → RightHand bone");
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

    private void PlayIgniteSound()
    {
        if (torchItem.IgniteSound != null)
        {
            AudioSource.PlayClipAtPoint(torchItem.IgniteSound, transform.position);
        }
    }

    private void StartLoopingSound()
    {
        if (torchItem.CracklingSoundLoop != null)
        {
            loopingAudio = gameObject.AddComponent<AudioSource>();
            loopingAudio.clip = torchItem.CracklingSoundLoop;
            loopingAudio.loop = true;
            loopingAudio.spatialBlend = 0.5f; // Somewhat 3D
            loopingAudio.volume = 0.3f;
            loopingAudio.Play();
        }
    }

    private void StopLoopingSound()
    {
        if (loopingAudio != null)
        {
            loopingAudio.Stop();
            Destroy(loopingAudio);
            loopingAudio = null;
        }
    }

    private void DepleteDurability()
    {
        var state = torchItem.GetState();
        state.currentDurability -= Time.deltaTime * torchItem.DurabilityDrainRate;
        state.currentDurability = Mathf.Max(0, state.currentDurability);
    }

    private void UpdateLightIntensity()
    {
        if (torchLight == null)
            return;

        float durabilityPercentage = torchItem.GetDurabilityPercentage();

        // Flicker when low
        if (durabilityPercentage < torchItem.LowDurabilityThreshold)
        {
            float flicker = Mathf.PerlinNoise(Time.time * 10f, 0f) * 0.5f + 0.5f;
            torchLight.intensity = torchItem.LightIntensity * durabilityPercentage * flicker;
        }
        else
        {
            torchLight.intensity = torchItem.LightIntensity;
        }
    }

    private void CheckDestruction()
    {
        if (!torchItem.HasDurability())
        {
            //Debug.Log("[TorchBehavior] Torch durability depleted - destroying item");
            DestroyTorch();
        }
    }

    private void DestroyTorch()
    {
        // Unequip first
        OnUnequipped();

        // Remove item from inventory
        var inventoryService = ServiceContainer.Instance.TryGet<Game.Player.Inventory.IInventoryService>();
        if (inventoryService != null)
        {
            inventoryService.RemoveItem(torchItem, 1);
        }

        // Remove state
        HeldItemStateManager.Instance.RemoveState(torchItem.GetStateID());

        // Destroy this behavior component
        Destroy(this);
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
