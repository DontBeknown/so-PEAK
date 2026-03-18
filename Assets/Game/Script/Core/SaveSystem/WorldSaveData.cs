using System;
using System.Collections.Generic;

[Serializable]
public class WorldSaveData
{
    // World Identity
    public string worldName;
    public string worldGuid;
    public DateTime createdDate;
    public DateTime lastPlayedDate;
    public float totalPlayTime; // in seconds
    
    // Seed Information
    public SeedData seedData;
    
    // Player Data
    public PlayerSaveData playerData;
    
    // World State
    public WorldStateSaveData worldState;

    // Tutorial State
    public TutorialSaveData tutorial;
    
    // Meta Information
    public string gameVersion;
    public int saveVersion;
}

[Serializable]
public class TutorialSaveData
{
    public bool isCompleted;
    public int lastCompletedStep;
}

[Serializable]
public class PlayerSaveData
{
    // Position & Rotation
    public float[] position; // [x, y, z]
    public float[] rotation; // [x, y, z, w]
    
    // Stats
    public float health;
    public float maxHealth;
    public float hunger;
    public float maxHunger;
    public float stamina;
    public float maxStamina;
    public float temperature;
    
    // Inventory
    public List<InventoryItemSaveData> inventoryItems;
    public List<EquipmentSlotSaveData> equippedItems;
}

[Serializable]
public class InventoryItemSaveData
{
    public string itemId; // ScriptableObject name
    public int quantity;
    public int slotIndex;
    
    // Grid position
    public int gridX;
    public int gridY;
    public bool isRotated;
    
    // Held item state (for torch, canteen, etc.)
    public HeldItemStateSaveData heldItemState;
}

[Serializable]
public class HeldItemStateSaveData
{
    public bool isActive;
    public float fuelRemaining;
    public float waterRemaining;
    public float durability;
}

[Serializable]
public class EquipmentSlotSaveData
{
    public string slotType; // "Head", "Chest", etc.
    public string itemId;
}

[Serializable]
public class WorldStateSaveData
{
    // Time
    public float currentTimeOfDay; // 0-24 hours
    public int dayNumber;
    
    // Environment
    public string currentWeather;
    public float temperature;
    public int level;
    
    // Interactables
    public List<InteractableStateSaveData> interactableStates;
    
    // Resources (gathered berry bushes, ore nodes, etc.)
    public List<ResourceNodeSaveData> resourceNodes;

    // Collectables/Dialog
    public List<string> unlockedCollectables = new List<string>();
    public List<string> triggeredDialogs = new List<string>();
}

[Serializable]
public class InteractableStateSaveData
{
    public string interactableGuid;
    public bool isUsed;
    public float respawnTimer;
}

[Serializable]
public class ResourceNodeSaveData
{
    public string nodeGuid;
    public bool isDepleted;
    public float regrowthTimer;
    public int remainingResources;
}

[Serializable]
public class SaveMetadata
{
    public string worldGuid;
    public string worldName;
    public DateTime lastPlayedDate;
    public float totalPlayTime;
    public string thumbnailPath; // Screenshot
    
    // Quick preview info
    public float playerHealth;
    public string seed1;
    public string seed2;
    public string seed3;
}

[Serializable]
public class SaveMetadataList
{
    public List<SaveMetadata> worlds;
}
