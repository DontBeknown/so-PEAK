# Save/Load System Design - "This is so PEAK"

**Last Updated:** February 16, 2026  
**Purpose:** Complete save/load system architecture with seed splitting and world persistence

---

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture](#architecture)
3. [Seed Management System](#seed-management-system)
4. [Save Data Structure](#save-data-structure)
5. [Save/Load Service](#saveload-service)
6. [World Persistence Manager](#world-persistence-manager)
7. [Editor Debug Tools](#editor-debug-tools)
8. [Integration Guide](#integration-guide)
9. [File Structure](#file-structure)

---

## System Overview

### Goals
- Save/load player worlds with persistent data
- Split large terrain seeds into 3 configurable parts for user-friendly display
- Transfer seed data between menu and gameplay scenes
- Provide editor tools for save management and debugging
- Support multiple save slots
- Auto-save functionality

### Key Features
- ✅ Seed splitting (1 long seed → 3 parts)
- ✅ Configurable seed digit count per part
- ✅ JSON-based save files
- ✅ Scene-persistent seed data using ScriptableObject
- ✅ Editor window for save management
- ✅ Backup and recovery system
- ✅ Save slot management

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      SAVE/LOAD SYSTEM                            │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐         ┌──────────────────┐
│  Menu Scene      │         │  Gameplay Scene  │
│                  │         │                  │
│  WorldCreateUI   │         │  TerrainGen      │
│  WorldSelectUI   │◄────────┤  PlayerProgress  │
│  WorldSlotUI     │  Seed   │  StatsTracker    │
└────────┬─────────┘  Transfer└────────┬─────────┘
         │                              │
         │       ┌──────────────────┐   │
         └──────►│ SaveLoadService  │◄──┘
                 │  (Singleton)     │
                 └────────┬─────────┘
                          │
         ┌────────────────┼────────────────┐
         │                │                │
         ▼                ▼                ▼
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│  WorldData  │  │  SeedConfig │  │ WorldPersist│
│    (SO)     │  │    (SO)     │  │ Manager (SO)│
└─────────────┘  └─────────────┘  └─────────────┘
         │                │                │
         └────────────────┴────────────────┘
                          │
                          ▼
              ┌────────────────────┐
              │  JSON Save Files   │
              │  (Persistent Data) │
              └────────────────────┘
```

---

## Seed Management System

### Seed Splitting Concept

**Problem:** Long terrain seed `123456789012345678901234` is hard to display/share  
**Solution:** Split into 3 user-friendly parts:
- Seed Part 1: `12345678` (8 digits)
- Seed Part 2: `90123456` (8 digits)
- Seed Part 3: `78901234` (8 digits)

### SeedConfig (ScriptableObject)

**Location:** `Assets/Game/Data/Config/SeedConfig.asset`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SeedConfig", menuName = "Game/Config/Seed Config")]
public class SeedConfig : ScriptableObject
{
    [Header("Seed Part Lengths")]
    [Tooltip("Number of digits in Seed Part 1")]
    [Range(4, 12)]
    public int seed1DigitCount = 8;
    
    [Tooltip("Number of digits in Seed Part 2")]
    [Range(4, 12)]
    public int seed2DigitCount = 8;
    
    [Tooltip("Number of digits in Seed Part 3")]
    [Range(4, 12)]
    public int seed3DigitCount = 8;
    
    [Header("Generation Settings")]
    [Tooltip("Use current time as seed if not specified")]
    public bool useTimeAsDefaultSeed = true;
    
    [Tooltip("Maximum seed value (Total digits)")]
    public int maxSeedLength = 24;
    
    // Calculated total length
    public int TotalDigitCount => seed1DigitCount + seed2DigitCount + seed3DigitCount;
    
    // Validation
    private void OnValidate()
    {
        if (TotalDigitCount > maxSeedLength)
        {
            Debug.LogWarning($"Total seed length ({TotalDigitCount}) exceeds maximum ({maxSeedLength})");
        }
    }
}
```

### SeedData (Data Structure)

```csharp
using System;

[Serializable]
public struct SeedData
{
    public string seed1;
    public string seed2;
    public string seed3;
    
    // Combined full seed
    public string FullSeed => seed1 + seed2 + seed3;
    
    // Constructor from full seed
    public SeedData(string fullSeed, SeedConfig config)
    {
        if (string.IsNullOrEmpty(fullSeed))
        {
            fullSeed = GenerateRandomSeed(config);
        }
        
        // Pad if needed
        int totalLength = config.TotalDigitCount;
        fullSeed = fullSeed.PadRight(totalLength, '0');
        
        // Split into parts
        int pos = 0;
        seed1 = fullSeed.Substring(pos, config.seed1DigitCount);
        pos += config.seed1DigitCount;
        seed2 = fullSeed.Substring(pos, config.seed2DigitCount);
        pos += config.seed2DigitCount;
        seed3 = fullSeed.Substring(pos, config.seed3DigitCount);
    }
    
    // Constructor from parts
    public SeedData(string part1, string part2, string part3)
    {
        seed1 = part1;
        seed2 = part2;
        seed3 = part3;
    }
    
    // Generate random seed
    public static string GenerateRandomSeed(SeedConfig config)
    {
        string seed = "";
        System.Random random = new System.Random();
        for (int i = 0; i < config.TotalDigitCount; i++)
        {
            seed += random.Next(0, 10).ToString();
        }
        return seed;
    }
    
    // Validate seed parts
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(seed1) && 
               !string.IsNullOrEmpty(seed2) && 
               !string.IsNullOrEmpty(seed3);
    }
    
    public override string ToString()
    {
        return $"Seed1: {seed1} | Seed2: {seed2} | Seed3: {seed3}";
    }
}
```

---

## Save Data Structure

### WorldSaveData (Complete Save File)

```csharp
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
    
    // Meta Information
    public string gameVersion;
    public int saveVersion;
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
    
    // Interactables
    public List<InteractableStateSaveData> interactableStates;
    
    // Resources (gathered berry bushes, ore nodes, etc.)
    public List<ResourceNodeSaveData> resourceNodes;
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
```

### SaveMetadata (Quick Load Info)

```csharp
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
```

---

## Save/Load Service

### ISaveLoadService (Interface)

```csharp
using System;
using System.Collections.Generic;

public interface ISaveLoadService
{
    // World Management
    WorldSaveData CreateNewWorld(string worldName, SeedData seedData);
    bool SaveWorld(WorldSaveData saveData);
    WorldSaveData LoadWorld(string worldGuid);
    bool DeleteWorld(string worldGuid);
    List<SaveMetadata> GetAllWorlds();
    
    // Auto-save
    void EnableAutoSave(float intervalSeconds);
    void DisableAutoSave();
    void PerformAutoSave();
    
    // Backup
    bool CreateBackup(string worldGuid);
    bool RestoreFromBackup(string worldGuid, DateTime backupDate);
    List<DateTime> GetBackups(string worldGuid);
    
    // Validation
    bool ValidateSaveFile(string worldGuid);
    
    // Events
    event Action<WorldSaveData> OnWorldSaved;
    event Action<WorldSaveData> OnWorldLoaded;
    event Action<string> OnWorldDeleted;
}
```

### SaveLoadService (Implementation)

**Location:** `Assets/Game/Script/Core/SaveSystem/SaveLoadService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveLoadService : MonoBehaviour, ISaveLoadService
{
    [Header("Configuration")]
    [SerializeField] private SeedConfig seedConfig;
    [SerializeField] private bool enableEncryption = false;
    [SerializeField] private bool enableCompression = true;
    
    [Header("Auto-Save")]
    [SerializeField] private bool autoSaveEnabled = false;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    
    [Header("Backup")]
    [SerializeField] private int maxBackupCount = 5;
    [SerializeField] private bool createBackupOnSave = true;
    
    // Events
    public event Action<WorldSaveData> OnWorldSaved;
    public event Action<WorldSaveData> OnWorldLoaded;
    public event Action<string> OnWorldDeleted;
    
    // Paths
    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
    private string BackupDirectory => Path.Combine(Application.persistentDataPath, "Backups");
    private string MetadataFile => Path.Combine(SaveDirectory, "metadata.json");
    
    // Current save
    private WorldSaveData currentWorldSave;
    private float autoSaveTimer;
    
    // Constants
    private const string SAVE_FILE_EXTENSION = ".sav";
    private const int CURRENT_SAVE_VERSION = 1;
    
    private void Awake()
    {
        EnsureDirectoriesExist();
    }
    
    private void Update()
    {
        if (autoSaveEnabled && currentWorldSave != null)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                PerformAutoSave();
                autoSaveTimer = 0f;
            }
        }
    }
    
    #region World Management
    
    public WorldSaveData CreateNewWorld(string worldName, SeedData seedData)
    {
        WorldSaveData newWorld = new WorldSaveData
        {
            worldName = worldName,
            worldGuid = Guid.NewGuid().ToString(),
            createdDate = DateTime.Now,
            lastPlayedDate = DateTime.Now,
            totalPlayTime = 0f,
            seedData = seedData,
            gameVersion = Application.version,
            saveVersion = CURRENT_SAVE_VERSION,
            playerData = CreateDefaultPlayerData(),
            worldState = CreateDefaultWorldState()
        };
        
        currentWorldSave = newWorld;
        SaveWorld(newWorld);
        UpdateMetadata(newWorld);
        
        Debug.Log($"Created new world: {worldName} with seed: {seedData.FullSeed}");
        
        return newWorld;
    }
    
    public bool SaveWorld(WorldSaveData saveData)
    {
        try
        {
            saveData.lastPlayedDate = DateTime.Now;
            
            string filePath = GetSaveFilePath(saveData.worldGuid);
            string json = JsonUtility.ToJson(saveData, true);
            
            // Optional: Encrypt or compress here
            if (enableCompression)
            {
                json = CompressString(json);
            }
            
            File.WriteAllText(filePath, json);
            
            // Update metadata
            UpdateMetadata(saveData);
            
            // Create backup
            if (createBackupOnSave)
            {
                CreateBackup(saveData.worldGuid);
            }
            
            OnWorldSaved?.Invoke(saveData);
            
            Debug.Log($"Saved world: {saveData.worldName}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save world: {e.Message}");
            return false;
        }
    }
    
    public WorldSaveData LoadWorld(string worldGuid)
    {
        try
        {
            string filePath = GetSaveFilePath(worldGuid);
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Save file not found: {worldGuid}");
                return null;
            }
            
            string json = File.ReadAllText(filePath);
            
            // Optional: Decrypt or decompress here
            if (enableCompression)
            {
                json = DecompressString(json);
            }
            
            WorldSaveData saveData = JsonUtility.FromJson<WorldSaveData>(json);
            
            // Validate
            if (!ValidateSaveData(saveData))
            {
                Debug.LogError("Save data validation failed!");
                return null;
            }
            
            currentWorldSave = saveData;
            OnWorldLoaded?.Invoke(saveData);
            
            Debug.Log($"Loaded world: {saveData.worldName}");
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load world: {e.Message}");
            return null;
        }
    }
    
    public bool DeleteWorld(string worldGuid)
    {
        try
        {
            string filePath = GetSaveFilePath(worldGuid);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            // Delete backups
            DeleteBackups(worldGuid);
            
            // Update metadata
            RemoveFromMetadata(worldGuid);
            
            OnWorldDeleted?.Invoke(worldGuid);
            
            Debug.Log($"Deleted world: {worldGuid}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete world: {e.Message}");
            return false;
        }
    }
    
    public List<SaveMetadata> GetAllWorlds()
    {
        try
        {
            if (!File.Exists(MetadataFile))
            {
                return new List<SaveMetadata>();
            }
            
            string json = File.ReadAllText(MetadataFile);
            SaveMetadataList metadataList = JsonUtility.FromJson<SaveMetadataList>(json);
            
            return metadataList?.worlds ?? new List<SaveMetadata>();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load metadata: {e.Message}");
            return new List<SaveMetadata>();
        }
    }
    
    #endregion
    
    #region Auto-Save
    
    public void EnableAutoSave(float intervalSeconds)
    {
        autoSaveEnabled = true;
        autoSaveInterval = intervalSeconds;
        autoSaveTimer = 0f;
    }
    
    public void DisableAutoSave()
    {
        autoSaveEnabled = false;
    }
    
    public void PerformAutoSave()
    {
        if (currentWorldSave != null)
        {
            Debug.Log("Auto-saving...");
            SaveWorld(currentWorldSave);
        }
    }
    
    #endregion
    
    #region Backup System
    
    public bool CreateBackup(string worldGuid)
    {
        try
        {
            string sourcePath = GetSaveFilePath(worldGuid);
            if (!File.Exists(sourcePath))
            {
                return false;
            }
            
            string backupFolder = GetBackupFolder(worldGuid);
            Directory.CreateDirectory(backupFolder);
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(backupFolder, $"backup_{timestamp}{SAVE_FILE_EXTENSION}");
            
            File.Copy(sourcePath, backupPath, true);
            
            // Limit backup count
            CleanupOldBackups(worldGuid);
            
            Debug.Log($"Created backup: {backupPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create backup: {e.Message}");
            return false;
        }
    }
    
    public bool RestoreFromBackup(string worldGuid, DateTime backupDate)
    {
        try
        {
            string backupFolder = GetBackupFolder(worldGuid);
            string timestamp = backupDate.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(backupFolder, $"backup_{timestamp}{SAVE_FILE_EXTENSION}");
            
            if (!File.Exists(backupPath))
            {
                Debug.LogError($"Backup not found: {backupPath}");
                return false;
            }
            
            string savePath = GetSaveFilePath(worldGuid);
            File.Copy(backupPath, savePath, true);
            
            Debug.Log($"Restored from backup: {backupDate}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to restore backup: {e.Message}");
            return false;
        }
    }
    
    public List<DateTime> GetBackups(string worldGuid)
    {
        List<DateTime> backups = new List<DateTime>();
        string backupFolder = GetBackupFolder(worldGuid);
        
        if (!Directory.Exists(backupFolder))
        {
            return backups;
        }
        
        string[] files = Directory.GetFiles(backupFolder, $"backup_*{SAVE_FILE_EXTENSION}");
        
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string timestamp = fileName.Replace("backup_", "");
            
            if (DateTime.TryParseExact(timestamp, "yyyyMMdd_HHmmss", null, 
                System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                backups.Add(date);
            }
        }
        
        return backups.OrderByDescending(d => d).ToList();
    }
    
    #endregion
    
    #region Validation
    
    public bool ValidateSaveFile(string worldGuid)
    {
        try
        {
            WorldSaveData saveData = LoadWorld(worldGuid);
            return ValidateSaveData(saveData);
        }
        catch
        {
            return false;
        }
    }
    
    private bool ValidateSaveData(WorldSaveData saveData)
    {
        if (saveData == null) return false;
        if (string.IsNullOrEmpty(saveData.worldGuid)) return false;
        if (string.IsNullOrEmpty(saveData.worldName)) return false;
        if (!saveData.seedData.IsValid()) return false;
        if (saveData.playerData == null) return false;
        
        return true;
    }
    
    #endregion
    
    #region Helper Methods
    
    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(SaveDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }
    
    private string GetSaveFilePath(string worldGuid)
    {
        return Path.Combine(SaveDirectory, $"{worldGuid}{SAVE_FILE_EXTENSION}");
    }
    
    private string GetBackupFolder(string worldGuid)
    {
        return Path.Combine(BackupDirectory, worldGuid);
    }
    
    private void UpdateMetadata(WorldSaveData saveData)
    {
        List<SaveMetadata> allMetadata = GetAllWorlds();
        
        SaveMetadata metadata = allMetadata.FirstOrDefault(m => m.worldGuid == saveData.worldGuid);
        if (metadata == null)
        {
            metadata = new SaveMetadata { worldGuid = saveData.worldGuid };
            allMetadata.Add(metadata);
        }
        
        metadata.worldName = saveData.worldName;
        metadata.lastPlayedDate = saveData.lastPlayedDate;
        metadata.totalPlayTime = saveData.totalPlayTime;
        metadata.seed1 = saveData.seedData.seed1;
        metadata.seed2 = saveData.seedData.seed2;
        metadata.seed3 = saveData.seedData.seed3;
        metadata.playerHealth = saveData.playerData?.health ?? 100f;
        
        SaveMetadataList metadataList = new SaveMetadataList { worlds = allMetadata };
        string json = JsonUtility.ToJson(metadataList, true);
        File.WriteAllText(MetadataFile, json);
    }
    
    private void RemoveFromMetadata(string worldGuid)
    {
        List<SaveMetadata> allMetadata = GetAllWorlds();
        allMetadata.RemoveAll(m => m.worldGuid == worldGuid);
        
        SaveMetadataList metadataList = new SaveMetadataList { worlds = allMetadata };
        string json = JsonUtility.ToJson(metadataList, true);
        File.WriteAllText(MetadataFile, json);
    }
    
    private void DeleteBackups(string worldGuid)
    {
        string backupFolder = GetBackupFolder(worldGuid);
        if (Directory.Exists(backupFolder))
        {
            Directory.Delete(backupFolder, true);
        }
    }
    
    private void CleanupOldBackups(string worldGuid)
    {
        List<DateTime> backups = GetBackups(worldGuid);
        
        if (backups.Count > maxBackupCount)
        {
            string backupFolder = GetBackupFolder(worldGuid);
            int toRemove = backups.Count - maxBackupCount;
            
            for (int i = backups.Count - 1; i >= backups.Count - toRemove; i--)
            {
                string timestamp = backups[i].ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupFolder, $"backup_{timestamp}{SAVE_FILE_EXTENSION}");
                
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }
    }
    
    private PlayerSaveData CreateDefaultPlayerData()
    {
        return new PlayerSaveData
        {
            position = new float[] { 0, 10, 0 },
            rotation = new float[] { 0, 0, 0, 1 },
            health = 100f,
            maxHealth = 100f,
            hunger = 100f,
            maxHunger = 100f,
            stamina = 100f,
            maxStamina = 100f,
            temperature = 20f,
            inventoryItems = new List<InventoryItemSaveData>(),
            equippedItems = new List<EquipmentSlotSaveData>()
        };
    }
    
    private WorldStateSaveData CreateDefaultWorldState()
    {
        return new WorldStateSaveData
        {
            currentTimeOfDay = 6f, // Start at morning
            dayNumber = 1,
            currentWeather = "Clear",
            temperature = 20f,
            interactableStates = new List<InteractableStateSaveData>(),
            resourceNodes = new List<ResourceNodeSaveData>()
        };
    }
    
    // Compression helpers (simple base64 for now)
    private string CompressString(string text)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }
    
    private string DecompressString(string compressed)
    {
        byte[] bytes = Convert.FromBase64String(compressed);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
    
    #endregion
}

[Serializable]
public class SaveMetadataList
{
    public List<SaveMetadata> worlds;
}
```

---

## World Persistence Manager

### WorldPersistenceManager (ScriptableObject - Scene Transfer)

**Location:** `Assets/Game/Data/Runtime/WorldPersistence.asset`

**Purpose:** Transfer world data between Menu scene and Gameplay scene

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "WorldPersistence", menuName = "Game/World Persistence Manager")]
public class WorldPersistenceManager : ScriptableObject
{
    [Header("Current World")]
    public string currentWorldGuid;
    public string currentWorldName;
    public SeedData currentSeedData;
    
    [Header("World State")]
    public bool isNewWorld;
    public bool shouldLoadWorld;
    
    [Header("Player Start")]
    public Vector3 playerStartPosition = new Vector3(0, 10, 0);
    public Quaternion playerStartRotation = Quaternion.identity;
    
    // Set when creating new world
    public void PrepareNewWorld(string worldName, SeedData seedData, string worldGuid)
    {
        currentWorldGuid = worldGuid;
        currentWorldName = worldName;
        currentSeedData = seedData;
        isNewWorld = true;
        shouldLoadWorld = false;
        
        Debug.Log($"Prepared new world: {worldName}");
        Debug.Log($"Seed: {seedData}");
    }
    
    // Set when loading existing world
    public void PrepareLoadWorld(WorldSaveData saveData)
    {
        currentWorldGuid = saveData.worldGuid;
        currentWorldName = saveData.worldName;
        currentSeedData = saveData.seedData;
        isNewWorld = false;
        shouldLoadWorld = true;
        
        // Set player start from save
        if (saveData.playerData != null && saveData.playerData.position != null)
        {
            playerStartPosition = new Vector3(
                saveData.playerData.position[0],
                saveData.playerData.position[1],
                saveData.playerData.position[2]
            );
            
            if (saveData.playerData.rotation != null)
            {
                playerStartRotation = new Quaternion(
                    saveData.playerData.rotation[0],
                    saveData.playerData.rotation[1],
                    saveData.playerData.rotation[2],
                    saveData.playerData.rotation[3]
                );
            }
        }
        
        Debug.Log($"Prepared to load world: {saveData.worldName}");
    }
    
    // Clear after scene transition
    public void Clear()
    {
        currentWorldGuid = string.Empty;
        currentWorldName = string.Empty;
        currentSeedData = new SeedData();
        isNewWorld = false;
        shouldLoadWorld = false;
    }
    
    // Get seed as integer for terrain generation
    public int GetSeedAsInt()
    {
        if (string.IsNullOrEmpty(currentSeedData.FullSeed))
        {
            return 0;
        }
        
        // Use hash code for consistent seed
        return currentSeedData.FullSeed.GetHashCode();
    }
}
```

### GameplaySceneInitializer (MonoBehaviour)

**Location:** `Assets/Game/Script/Core/GameplaySceneInitializer.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplaySceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldPersistenceManager worldPersistence;
    [SerializeField] private TerrainGeneration terrainGenerator;
    [SerializeField] private GameObject playerPrefab;
    
    [Header("Services")]
    private ISaveLoadService saveLoadService;
    
    private void Start()
    {
        // Get services
        saveLoadService = ServiceContainer.Instance.Resolve<ISaveLoadService>();
        
        if (worldPersistence == null)
        {
            Debug.LogError("WorldPersistenceManager not assigned!");
            return;
        }
        
        InitializeWorld();
    }
    
    private void InitializeWorld()
    {
        if (worldPersistence.isNewWorld)
        {
            InitializeNewWorld();
        }
        else if (worldPersistence.shouldLoadWorld)
        {
            InitializeLoadedWorld();
        }
        else
        {
            Debug.LogError("No world data to initialize!");
        }
    }
    
    private void InitializeNewWorld()
    {
        Debug.Log($"Initializing new world: {worldPersistence.currentWorldName}");
        
        // TODO: Generate terrain with seed (not yet implemented)
        // if (terrainGenerator != null)
        // {
        //     int seed = worldPersistence.GetSeedAsInt();
        //     terrainGenerator.GenerateTerrain(seed);
        //     Debug.Log($"Generated terrain with seed: {seed}");
        // }
        
        // Spawn player at start position
        SpawnPlayer(worldPersistence.playerStartPosition, worldPersistence.playerStartRotation);
        
        // Initialize default world state
        InitializeDefaultWorldState();
    }
    
    private void InitializeLoadedWorld()
    {
        Debug.Log($"Loading world: {worldPersistence.currentWorldName}");
        
        // Load save data
        WorldSaveData saveData = saveLoadService.LoadWorld(worldPersistence.currentWorldGuid);
        
        if (saveData == null)
        {
            Debug.LogError("Failed to load world save data!");
            return;
        }
        
        // TODO: Generate terrain with saved seed (not yet implemented)
        // if (terrainGenerator != null)
        // {
        //     int seed = worldPersistence.GetSeedAsInt();
        //     terrainGenerator.GenerateTerrain(seed);
        //     Debug.Log($"Generated terrain with saved seed: {seed}");
        // }
        
        // Spawn player at saved position
        SpawnPlayer(worldPersistence.playerStartPosition, worldPersistence.playerStartRotation);
        
        // Restore world state
        RestoreWorldState(saveData);
    }
    
    private void SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, position, rotation);
            Debug.Log($"Spawned player at {position}");
        }
    }
    
    private void InitializeDefaultWorldState()
    {
        // Set time to morning
        var dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
        if (dayNightManager != null)
        {
            dayNightManager.SetTime(6f); // 6 AM
        }
    }
    
    private void RestoreWorldState(WorldSaveData saveData)
    {
        // Restore player stats
        var playerStats = ServiceContainer.Instance.Resolve<PlayerStats>();
        if (playerStats != null && saveData.playerData != null)
        {
            playerStats.SetHealth(saveData.playerData.health);
            playerStats.SetHunger(saveData.playerData.hunger);
            playerStats.SetStamina(saveData.playerData.stamina);
        }
        
        // Restore inventory
        RestoreInventory(saveData.playerData);
        
        // Restore equipment
        RestoreEquipment(saveData.playerData);
        
        // Restore time of day
        if (saveData.worldState != null)
        {
            var dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
            if (dayNightManager != null)
            {
                dayNightManager.SetTime(saveData.worldState.currentTimeOfDay);
            }
        }
        
        // Restore resource nodes
        RestoreResourceNodes(saveData.worldState);
        
        Debug.Log("World state restored successfully");
    }
    
    private void RestoreInventory(PlayerSaveData playerData)
    {
        var inventoryManager = ServiceContainer.Instance.Resolve<InventoryManager>();
        if (inventoryManager == null || playerData.inventoryItems == null) return;
        
        foreach (var itemData in playerData.inventoryItems)
        {
            // Load item from Resources or AssetDatabase
            InventoryItem item = Resources.Load<InventoryItem>($"Items/{itemData.itemId}");
            if (item != null)
            {
                inventoryManager.AddItem(item, itemData.quantity);
            }
        }
    }
    
    private void RestoreEquipment(PlayerSaveData playerData)
    {
        var equipmentManager = ServiceContainer.Instance.Resolve<EquipmentManager>();
        if (equipmentManager == null || playerData.equippedItems == null) return;
        
        foreach (var equipData in playerData.equippedItems)
        {
            InventoryItem item = Resources.Load<InventoryItem>($"Items/{equipData.itemId}");
            if (item != null && item is IEquippable equippable)
            {
                equipmentManager.EquipItem(equippable);
            }
        }
    }
    
    private void RestoreResourceNodes(WorldStateSaveData worldState)
    {
        if (worldState?.resourceNodes == null) return;
        
        // Find all resource nodes in scene and restore their state
        var resourceNodes = FindObjectsByType<ResourceCollectorInteractable>(FindObjectsSortMode.None);
        
        foreach (var node in resourceNodes)
        {
            var savedNode = worldState.resourceNodes.Find(n => n.nodeGuid == node.GetGuid());
            if (savedNode != null)
            {
                node.RestoreState(savedNode.isDepleted, savedNode.remainingResources);
            }
        }
    }
}
```

---

## Editor Debug Tools

### SaveSystemEditorWindow

**Location:** `Assets/Game/Script/Editor/SaveSystemEditorWindow.cs`

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SaveSystemEditorWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<SaveMetadata> allWorlds;
    private SaveLoadService saveLoadService;
    private SeedConfig seedConfig;
    
    private string searchFilter = "";
    private int selectedWorldIndex = -1;
    
    // New World Creation
    private string newWorldName = "New World";
    private string seed1 = "";
    private string seed2 = "";
    private string seed3 = "";
    private bool showCreateWorld = false;
    
    [MenuItem("Tools/Save System Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<SaveSystemEditorWindow>("Save Manager");
        window.minSize = new Vector2(500, 600);
    }
    
    private void OnEnable()
    {
        // Find or create SaveLoadService in scene
        saveLoadService = FindFirstObjectByType<SaveLoadService>();
        
        // Load seed config
        seedConfig = AssetDatabase.LoadAssetAtPath<SeedConfig>(
            "Assets/Game/Data/Config/SeedConfig.asset");
        
        RefreshWorldList();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        
        DrawToolbar();
        EditorGUILayout.Space(10);
        
        DrawStats();
        EditorGUILayout.Space(10);
        
        if (showCreateWorld)
        {
            DrawCreateWorld();
        }
        else
        {
            DrawWorldList();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            RefreshWorldList();
        }
        
        if (GUILayout.Button("Clear All Saves", EditorStyles.toolbarButton, GUILayout.Width(120)))
        {
            if (EditorUtility.DisplayDialog("Clear All Saves", 
                "Are you sure you want to delete ALL save files? This cannot be undone!", 
                "Delete All", "Cancel"))
            {
                ClearAllSaves();
            }
        }
        
        if (GUILayout.Button("Open Save Folder", EditorStyles.toolbarButton, GUILayout.Width(120)))
        {
            OpenSaveFolder();
        }
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button(showCreateWorld ? "Back" : "Create New", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            showCreateWorld = !showCreateWorld;
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawStats()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Save System Statistics", EditorStyles.boldLabel);
        
        int totalWorlds = allWorlds?.Count ?? 0;
        string savePath = Path.Combine(Application.persistentDataPath, "Saves");
        long totalSize = GetDirectorySize(savePath);
        string sizeStr = FormatBytes(totalSize);
        
        EditorGUILayout.LabelField($"Total Worlds: {totalWorlds}");
        EditorGUILayout.LabelField($"Total Save Size: {sizeStr}");
        EditorGUILayout.LabelField($"Save Location: {savePath}");
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawCreateWorld()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Create New World", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        newWorldName = EditorGUILayout.TextField("World Name:", newWorldName);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Seed Configuration:", EditorStyles.boldLabel);
        
        if (seedConfig != null)
        {
            EditorGUILayout.LabelField($"Seed Part 1 ({seedConfig.seed1DigitCount} digits):");
            seed1 = EditorGUILayout.TextField(seed1);
            
            EditorGUILayout.LabelField($"Seed Part 2 ({seedConfig.seed2DigitCount} digits):");
            seed2 = EditorGUILayout.TextField(seed2);
            
            EditorGUILayout.LabelField($"Seed Part 3 ({seedConfig.seed3DigitCount} digits):");
            seed3 = EditorGUILayout.TextField(seed3);
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Generate Random Seed"))
            {
                string fullSeed = SeedData.GenerateRandomSeed(seedConfig);
                SeedData tempSeed = new SeedData(fullSeed, seedConfig);
                seed1 = tempSeed.seed1;
                seed2 = tempSeed.seed2;
                seed3 = tempSeed.seed3;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("SeedConfig not found! Please create one at Assets/Game/Data/Config/SeedConfig.asset", MessageType.Error);
        }
        
        EditorGUILayout.Space(10);
        
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newWorldName));
        if (GUILayout.Button("Create World", GUILayout.Height(30)))
        {
            CreateNewWorld();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawWorldList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Saved Worlds", EditorStyles.boldLabel);
        
        // Search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // World list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        if (allWorlds == null || allWorlds.Count == 0)
        {
            EditorGUILayout.HelpBox("No saved worlds found.", MessageType.Info);
        }
        else
        {
            var filteredWorlds = allWorlds.Where(w => 
                string.IsNullOrEmpty(searchFilter) || 
                w.worldName.ToLower().Contains(searchFilter.ToLower())
            ).ToList();
            
            for (int i = 0; i < filteredWorlds.Count; i++)
            {
                DrawWorldItem(filteredWorlds[i], i);
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawWorldItem(SaveMetadata world, int index)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(world.worldName, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Edit", GUILayout.Width(60)))
        {
            EditWorld(world);
        }
        
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("Delete World", 
                $"Are you sure you want to delete '{world.worldName}'?", 
                "Delete", "Cancel"))
            {
                DeleteWorld(world.worldGuid);
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Details
        EditorGUILayout.LabelField($"Seed: {world.seed1}-{world.seed2}-{world.seed3}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Last Played: {world.lastPlayedDate:yyyy-MM-dd HH:mm}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Play Time: {FormatPlayTime(world.totalPlayTime)}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Health: {world.playerHealth:F0}", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    private void RefreshWorldList()
    {
        if (saveLoadService != null)
        {
            allWorlds = saveLoadService.GetAllWorlds();
        }
        else
        {
            // Load directly from file system
            string metadataPath = Path.Combine(Application.persistentDataPath, "Saves", "metadata.json");
            if (File.Exists(metadataPath))
            {
                string json = File.ReadAllText(metadataPath);
                SaveMetadataList metadataList = JsonUtility.FromJson<SaveMetadataList>(json);
                allWorlds = metadataList?.worlds ?? new List<SaveMetadata>();
            }
            else
            {
                allWorlds = new List<SaveMetadata>();
            }
        }
    }
    
    private void CreateNewWorld()
    {
        if (seedConfig == null)
        {
            EditorUtility.DisplayDialog("Error", "SeedConfig not found!", "OK");
            return;
        }
        
        SeedData seedData = new SeedData(seed1, seed2, seed3);
        
        if (saveLoadService == null)
        {
            EditorUtility.DisplayDialog("Error", "SaveLoadService not found in scene!", "OK");
            return;
        }
        
        saveLoadService.CreateNewWorld(newWorldName, seedData);
        
        RefreshWorldList();
        showCreateWorld = false;
        
        Debug.Log($"Created world: {newWorldName}");
    }
    
    private void EditWorld(SaveMetadata world)
    {
        WorldSaveDataEditorWindow.ShowWindow(world.worldGuid);
    }
    
    private void DeleteWorld(string worldGuid)
    {
        if (saveLoadService != null)
        {
            saveLoadService.DeleteWorld(worldGuid);
        }
        else
        {
            string savePath = Path.Combine(Application.persistentDataPath, "Saves", $"{worldGuid}.sav");
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }
        
        RefreshWorldList();
    }
    
    private void ClearAllSaves()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "Saves");
        if (Directory.Exists(savePath))
        {
            Directory.Delete(savePath, true);
            Directory.CreateDirectory(savePath);
        }
        
        string backupPath = Path.Combine(Application.persistentDataPath, "Backups");
        if (Directory.Exists(backupPath))
        {
            Directory.Delete(backupPath, true);
            Directory.CreateDirectory(backupPath);
        }
        
        RefreshWorldList();
        Debug.Log("All saves cleared!");
    }
    
    private void OpenSaveFolder()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        EditorUtility.RevealInFinder(savePath);
    }
    
    private long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        
        DirectoryInfo dir = new DirectoryInfo(path);
        return dir.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }
    
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    private string FormatPlayTime(float seconds)
    {
        int hours = (int)(seconds / 3600);
        int minutes = (int)((seconds % 3600) / 60);
        return $"{hours}h {minutes}m";
    }
}

// Individual world editor window
public class WorldSaveDataEditorWindow : EditorWindow
{
    private string worldGuid;
    private WorldSaveData saveData;
    private Vector2 scrollPosition;
    
    public static void ShowWindow(string guid)
    {
        var window = GetWindow<WorldSaveDataEditorWindow>("Edit World");
        window.worldGuid = guid;
        window.LoadSaveData();
    }
    
    private void LoadSaveData()
    {
        var saveLoadService = FindFirstObjectByType<SaveLoadService>();
        if (saveLoadService != null)
        {
            saveData = saveLoadService.LoadWorld(worldGuid);
        }
    }
    
    private void OnGUI()
    {
        if (saveData == null)
        {
            EditorGUILayout.HelpBox("Failed to load save data!", MessageType.Error);
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("World Information", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        saveData.worldName = EditorGUILayout.TextField("World Name:", saveData.worldName);
        EditorGUILayout.LabelField($"GUID: {saveData.worldGuid}");
        EditorGUILayout.LabelField($"Created: {saveData.createdDate}");
        EditorGUILayout.LabelField($"Last Played: {saveData.lastPlayedDate}");
        saveData.totalPlayTime = EditorGUILayout.FloatField("Total Play Time (s):", saveData.totalPlayTime);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Seed Information", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        saveData.seedData.seed1 = EditorGUILayout.TextField("Seed Part 1:", saveData.seedData.seed1);
        saveData.seedData.seed2 = EditorGUILayout.TextField("Seed Part 2:", saveData.seedData.seed2);
        saveData.seedData.seed3 = EditorGUILayout.TextField("Seed Part 3:", saveData.seedData.seed3);
        EditorGUILayout.LabelField($"Full Seed: {saveData.seedData.FullSeed}");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Player Stats", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        saveData.playerData.health = EditorGUILayout.FloatField("Health:", saveData.playerData.health);
        saveData.playerData.hunger = EditorGUILayout.FloatField("Hunger:", saveData.playerData.hunger);
        saveData.playerData.stamina = EditorGUILayout.FloatField("Stamina:", saveData.playerData.stamina);
        saveData.playerData.temperature = EditorGUILayout.FloatField("Temperature:", saveData.playerData.temperature);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
        {
            SaveChanges();
        }
    }
    
    private void SaveChanges()
    {
        var saveLoadService = FindFirstObjectByType<SaveLoadService>();
        if (saveLoadService != null)
        {
            saveLoadService.SaveWorld(saveData);
            EditorUtility.DisplayDialog("Success", "World saved successfully!", "OK");
        }
    }
}
#endif
```

---

## Integration Guide

### Step 1: Create Configuration Assets

1. **Create SeedConfig:**
   - Right-click in Project → Create → Game → Config → Seed Config
   - Save as: `Assets/Game/Data/Config/SeedConfig.asset`
   - Configure digit counts (default: 8-8-8)

2. **Create WorldPersistence:**
   - Right-click in Project → Create → Game → World Persistence Manager
   - Save as: `Assets/Game/Data/Runtime/WorldPersistence.asset`

### Step 2: Setup SaveLoadService

1. Add `SaveLoadService` component to a persistent GameObject (e.g., GameServiceBootstrapper)
2. Assign `SeedConfig` reference
3. Configure auto-save settings
4. Register in ServiceContainer:

```csharp
// In GameServiceBootstrapper.cs
private void RegisterServices()
{
    var saveLoadService = GetComponent<SaveLoadService>();
    ServiceContainer.Instance.Register<ISaveLoadService>(saveLoadService);
}
```

### Step 3: Integrate with Menu UI

Update **WorldCreateUI.cs:**

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldCreateUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField worldNameInput;
    [SerializeField] private TMP_InputField seed1Input;
    [SerializeField] private TMP_InputField seed2Input;
    [SerializeField] private TMP_InputField seed3Input;
    [SerializeField] private Button createButton;
    [SerializeField] private Button randomSeedButton;
    
    [Header("Configuration")]
    [SerializeField] private SeedConfig seedConfig;
    [SerializeField] private WorldPersistenceManager worldPersistence;
    
    private ISaveLoadService saveLoadService;
    
    private void Start()
    {
        saveLoadService = ServiceContainer.Instance.Resolve<ISaveLoadService>();
        
        createButton.onClick.AddListener(OnCreateWorld);
        randomSeedButton.onClick.AddListener(OnRandomSeed);
        
        // Set placeholders based on config
        seed1Input.placeholder.GetComponent<TMP_Text>().text = new string('0', seedConfig.seed1DigitCount);
        seed2Input.placeholder.GetComponent<TMP_Text>().text = new string('0', seedConfig.seed2DigitCount);
        seed3Input.placeholder.GetComponent<TMP_Text>().text = new string('0', seedConfig.seed3DigitCount);
    }
    
    private void OnCreateWorld()
    {
        string worldName = worldNameInput.text;
        if (string.IsNullOrEmpty(worldName))
        {
            worldName = "New World";
        }
        
        // Get seed parts
        string seed1 = seed1Input.text;
        string seed2 = seed2Input.text;
        string seed3 = seed3Input.text;
        
        // Create seed data (will auto-generate if empty)
        string fullSeed = seed1 + seed2 + seed3;
        SeedData seedData = new SeedData(fullSeed, seedConfig);
        
        // Create world save
        WorldSaveData world = saveLoadService.CreateNewWorld(worldName, seedData);
        
        // Prepare for scene transition
        worldPersistence.PrepareNewWorld(worldName, seedData, world.worldGuid);
        
        // Load gameplay scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
    }
    
    private void OnRandomSeed()
    {
        string randomSeed = SeedData.GenerateRandomSeed(seedConfig);
        SeedData seedData = new SeedData(randomSeed, seedConfig);
        
        seed1Input.text = seedData.seed1;
        seed2Input.text = seedData.seed2;
        seed3Input.text = seedData.seed3;
    }
}
```

Update **WorldSelectionUI.cs:**

```csharp
private void LoadSelectedWorld()
{
    if (selectedWorldSlot == null) return;
    
    // Load world data
    WorldSaveData saveData = saveLoadService.LoadWorld(selectedWorldSlot.WorldGuid);
    
    if (saveData == null)
    {
        Debug.LogError("Failed to load world!");
        return;
    }
    
    // Prepare for scene transition
    worldPersistence.PrepareLoadWorld(saveData);
    
    // Load gameplay scene
    UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
}
```

### Step 4: Add Auto-Save to PlayerController

```csharp
// In PlayerControllerRefactored.cs
private ISaveLoadService saveLoadService;

private void Start()
{
    saveLoadService = ServiceContainer.Instance.Resolve<ISaveLoadService>();
    saveLoadService.EnableAutoSave(300f); // Auto-save every 5 minutes
}

private void OnApplicationQuit()
{
    // Save on quit
    saveLoadService.PerformAutoSave();
}
```

---

## File Structure

```
Assets/
├── Game/
│   ├── Data/
│   │   ├── Config/
│   │   │   └── SeedConfig.asset
│   │   └── Runtime/
│   │       └── WorldPersistence.asset
│   │
│   ├── Script/
│   │   ├── Core/
│   │   │   ├── SaveSystem/
│   │   │   │   ├── ISaveLoadService.cs
│   │   │   │   ├── SaveLoadService.cs
│   │   │   │   ├── SeedConfig.cs
│   │   │   │   ├── SeedData.cs
│   │   │   │   ├── WorldSaveData.cs
│   │   │   │   ├── PlayerSaveData.cs
│   │   │   │   └── SaveMetadata.cs
│   │   │   │
│   │   │   ├── WorldPersistenceManager.cs
│   │   │   └── GameplaySceneInitializer.cs
│   │   │
│   │   └── Editor/
│   │       ├── SaveSystemEditorWindow.cs
│   │       └── WorldSaveDataEditorWindow.cs
│
└── Persistent Data (Runtime)/
    ├── Saves/
    │   ├── {world-guid-1}.sav
    │   ├── {world-guid-2}.sav
    │   └── metadata.json
    │
    └── Backups/
        ├── {world-guid-1}/
        │   ├── backup_20260216_143000.sav
        │   └── backup_20260216_150000.sav
        └── {world-guid-2}/
            └── backup_20260216_144500.sav
```

---

## Usage Examples

### Creating a New World (Menu Scene)

```csharp
// User fills in:
// World Name: "My Adventure"
// Seed Part 1: 12345678
// Seed Part 2: 90123456
// Seed Part 3: 78901234

// System creates:
SeedData seed = new SeedData("12345678", "90123456", "78901234");
WorldSaveData world = saveLoadService.CreateNewWorld("My Adventure", seed);

// Prepare scene transition
worldPersistence.PrepareNewWorld("My Adventure", seed, world.worldGuid);

// Load gameplay scene with seed data available
SceneManager.LoadScene("Gameplay");
```

### Loading a World (Gameplay Scene)

```csharp
// GameplaySceneInitializer reads WorldPersistence
int seed = worldPersistence.GetSeedAsInt(); // Converts full seed to int hash

// TODO: Terrain generation will be integrated later
// terrainGenerator.GenerateTerrain(seed);

// Restore player state
playerController.transform.position = worldPersistence.playerStartPosition;
playerStats.SetHealth(saveData.playerData.health);
```

### Editor Debug

1. **Open Save Manager:** Tools → Save System Manager
2. **Create Test World:** Click "Create New", fill details, generate random seed
3. **Edit World:** Select world, click "Edit", modify stats
4. **Clear All Saves:** Toolbar → "Clear All Saves" (with confirmation)

---

## Future Enhancements

### Phase 2 Features
- 🔒 Save file encryption
- 📦 GZIP compression for save files
- ☁️ Cloud save support (Steam, Epic, etc.)
- 📸 World thumbnails/screenshots
- 🔄 Save file versioning and migration
- 🔐 Integrity checking (hash validation)

### Phase 3 Features
- 👥 Multiple player profiles
- 🏆 Achievement tracking
- 📊 Statistics and analytics
- 🌐 Cross-platform save sync
- 🎮 Controller-friendly save UI

---

## Testing Checklist

- [ ] Create new world with custom seed
- [ ] Create new world with random seed
- [ ] Load existing world
- [ ] Delete world
- [ ] Auto-save triggers correctly
- [ ] Backup creation works
- [ ] Restore from backup
- [ ] Clear all saves
- [ ] Editor window displays all worlds
- [ ] Edit world in editor
- [ ] Seed parts display correctly in UI
- [ ] Seed transfers between scenes
- [ ] Player position saves/loads
- [ ] Inventory saves/loads
- [ ] World time saves/loads

---

**End of Save/Load System Design**
