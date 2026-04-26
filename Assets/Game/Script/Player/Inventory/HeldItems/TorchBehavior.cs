using UnityEngine;
using Game.Player.Inventory.HeldItems;
using Game.Core.DI;
using Game.Environment.Temperature;

/// <summary>
/// Runtime behavior for torch - manages light, durability depletion, and cold resistance.
/// Attached to player when torch is equipped.
/// Follows Single Responsibility Principle.
/// </summary>
public class TorchBehavior : MonoBehaviour, IHeldItemBehavior, IHeldItemColdResistanceSource
{
    // Injected by HeldItemBehaviorManager (no Inspector assignment needed)
    [SerializeField] private Transform rightHandBone;
    
    private TorchItem torchItem;
    private HeldItemState _state;
    private Light torchLight;
    private AudioSource loopingAudio;
    private GameObject visualPrefabInstance;
    private bool isEquipped = false;

    public HeldItemState CurrentState => _state;
    public float ColdResistanceBonus => torchItem != null ? torchItem.ColdResistance : 0f;
    public bool IsActive => isEquipped && torchItem != null && _state != null && _state.currentDurability > 0f;

    public void Initialize(TorchItem item, HeldItemState state)
    {
        torchItem = item;
        _state = state;
    }

    public void OnEquipped()
    {
        if (torchItem == null)
        {
            Debug.LogError("[TorchBehavior] TorchItem is null!");
            return;
        }

        isEquipped = true;

        // Create light component
        CreateLight();

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
        if (_state == null || _state.maxDurability <= 0f) return "N/A";
        float pct = (_state.currentDurability / _state.maxDurability) * 100f;
        return $"{Mathf.RoundToInt(pct)}%";
    }

    public bool IsUsable()
    {
        return torchItem != null && _state != null && _state.currentDurability > 0f;
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
        _state.currentDurability -= Time.deltaTime * torchItem.DurabilityDrainRate;
        _state.currentDurability = Mathf.Max(0f, _state.currentDurability);
    }

    private void UpdateLightIntensity()
    {
        if (torchLight == null)
            return;

        float durabilityPercentage = _state.maxDurability > 0f
            ? _state.currentDurability / _state.maxDurability
            : 0f;

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
        if (_state.currentDurability <= 0f)
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
