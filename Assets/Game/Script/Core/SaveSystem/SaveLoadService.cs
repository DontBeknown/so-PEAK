using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Game.Core.DI;

public class SaveLoadService : MonoBehaviour, ISaveLoadService
{
    // Singleton instance
    public static SaveLoadService Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private SeedConfig seedConfig;
    //[SerializeField] private bool enableEncryption = false;
    [SerializeField] private bool enableCompression = true;
    
    [Header("Auto-Save")]
    [SerializeField] private bool autoSaveEnabled = false;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    
    [Header("Backup")]
    [SerializeField] private int maxBackupCount = 5;
    [SerializeField] private bool createBackupOnSave = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;
    
    // Events
    public event Action<WorldSaveData> OnWorldSaved;
    public event Action<WorldSaveData> OnWorldLoaded;
    public event Action<string> OnWorldDeleted;
    
    // Public API
    public WorldSaveData CurrentWorldSave => currentWorldSave;
    
    // Paths
    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
    private string BackupDirectory => Path.Combine(Application.persistentDataPath, "Backups");
    private string MetadataFile => Path.Combine(SaveDirectory, "metadata.json");
    
    // Current save
    private WorldSaveData currentWorldSave;
    private float autoSaveTimer;
    
    // Singleton instance (backward compatibility for local field)
    private static SaveLoadService instance;
    
    // Constants
    private const string SAVE_FILE_EXTENSION = ".sav";
    private const int CURRENT_SAVE_VERSION = 1;
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            //Debug.LogWarning("Duplicate SaveLoadService found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
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
    
    public WorldSaveData CreateNewWorld(string worldName, SeedData seedData, int level = 1)
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
            worldState = CreateDefaultWorldState(level)
        };
        
        currentWorldSave = newWorld;
        SaveWorld(newWorld);
        UpdateMetadata(newWorld);
        
        if (enableDebug) Debug.Log($"Created new world: {worldName} (Level {level}) with seed: {seedData.FullSeed}");
        
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
            
            if (enableDebug) Debug.Log($"Saved world: {saveData.worldName}");
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
            
            if (enableDebug) Debug.Log($"Loaded world: {saveData.worldName}");
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
            
            if (enableDebug) Debug.Log($"Deleted world: {worldGuid}");
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
            UpdatePlayerDataFromGame();
            
            if (enableDebug) Debug.Log("Auto-saving...");
            SaveWorld(currentWorldSave);
        }
    }
    
    /// <summary>
    /// Updates current world save with player data from game state
    /// Uses ServiceContainer to resolve player services
    /// </summary>
    private void UpdatePlayerDataFromGame()
    {
        if (currentWorldSave == null) return;
        
        try
        {
            var container = ServiceContainer.Instance;
            
            // Get player controller for position/rotation
            var player = container.TryGet<Game.Player.PlayerControllerRefactored>();
            if (player != null)
            {
                var pos = player.transform.position;
                var rot = player.transform.rotation;
                
                currentWorldSave.playerData.position = new float[] { pos.x, pos.y, pos.z };
                currentWorldSave.playerData.rotation = new float[] { rot.x, rot.y, rot.z, rot.w };
                
                if (enableDebug) Debug.Log($"Updated player position: {pos}");
            }
            
            // Get player stats
            var stats = container.TryGet<PlayerStats>();
            if (stats != null)
            {
                currentWorldSave.playerData.health = stats.Health;
                currentWorldSave.playerData.maxHealth = stats.MaxHealth;
                currentWorldSave.playerData.hunger = stats.Hunger;
                currentWorldSave.playerData.maxHunger = stats.MaxHunger;
                currentWorldSave.playerData.stamina = stats.Stamina;
                currentWorldSave.playerData.maxStamina = stats.MaxStamina;
                //currentWorldSave.playerData.temperature = stats.Temperature;
                
                if (enableDebug) Debug.Log($"Updated player stats: HP={stats.Health}/{stats.MaxHealth}");
            }
            
            // Update inventory items
            var inventoryManager = container.TryGet<Game.Player.Inventory.InventoryManagerRefactored>();
            if (inventoryManager != null)
            {
                var placements = inventoryManager.GetAllPlacements();
                currentWorldSave.playerData.inventoryItems = new List<InventoryItemSaveData>();
                foreach (var p in placements)
                {
                    currentWorldSave.playerData.inventoryItems.Add(new InventoryItemSaveData
                    {
                        itemId = p.Item.name,
                        quantity = 1,
                        gridX = p.Position.x,
                        gridY = p.Position.y,
                        isRotated = p.Rotated
                    });
                }
                if (enableDebug) Debug.Log($"[SaveLoadService] Saved {placements.Count} inventory items");
            }

            // Update equipped items
            var equipmentManager = container.TryGet<EquipmentManager>();
            if (equipmentManager != null)
            {
                currentWorldSave.playerData.equippedItems = new List<EquipmentSlotSaveData>();
                foreach (EquipmentSlotType slotType in System.Enum.GetValues(typeof(EquipmentSlotType)))
                {
                    var equipped = equipmentManager.GetEquippedItem(slotType);
                    var equippedObj = equipped as UnityEngine.Object;
                    if (equippedObj != null)
                    {
                        currentWorldSave.playerData.equippedItems.Add(new EquipmentSlotSaveData
                        {
                            slotType = slotType.ToString(),
                            itemId = equippedObj.name
                        });
                    }
                }
                if (enableDebug) Debug.Log($"[SaveLoadService] Saved {currentWorldSave.playerData.equippedItems.Count} equipped items");
            }
            
            // Update world state: time of day and day number
            var dayNightService = container.TryGet<Game.Environment.DayNight.IDayNightCycleService>();
            if (dayNightService != null)
            {
                if (currentWorldSave.worldState == null)
                    currentWorldSave.worldState = CreateDefaultWorldState();
                currentWorldSave.worldState.currentTimeOfDay = dayNightService.CurrentTime;
                currentWorldSave.worldState.dayNumber = dayNightService.CurrentDay;
                
                if (enableDebug) Debug.Log($"[SaveLoadService] Saved time={dayNightService.CurrentTime:F1}h, day={dayNightService.CurrentDay}");
            }
            
            // Update play time
            currentWorldSave.totalPlayTime += Time.deltaTime;
            
            if (enableDebug) Debug.Log("[SaveLoadService] Player data updated from game state");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveLoadService] Could not update player data: {e.Message}");
        }
    }
    
    #endregion
    
    #region Public API - Data Access
    
    /// <summary>
    /// Gets the saved player position from current world save
    /// </summary>
    public Vector3 GetSavedPlayerPosition()
    {
        if (currentWorldSave?.playerData?.position != null && currentWorldSave.playerData.position.Length >= 3)
        {
            var pos = currentWorldSave.playerData.position;
            return new Vector3(pos[0], pos[1], pos[2]);
        }
        
        Debug.LogWarning("[SaveLoadService] No saved position available, returning default");
        return new Vector3(0, 10, 0); // Default spawn position
    }
    
    /// <summary>
    /// Gets the saved player rotation from current world save
    /// </summary>
    public Quaternion GetSavedPlayerRotation()
    {
        if (currentWorldSave?.playerData?.rotation != null && currentWorldSave.playerData.rotation.Length >= 4)
        {
            var rot = currentWorldSave.playerData.rotation;
            return new Quaternion(rot[0], rot[1], rot[2], rot[3]);
        }
        
        Debug.LogWarning("[SaveLoadService] No saved rotation available, returning default");
        return Quaternion.identity;
    }
    
    /// <summary>
    /// Gets the saved player stats from current world save
    /// </summary>
    public PlayerSaveData GetSavedPlayerData()
    {
        if (currentWorldSave?.playerData != null)
        {
            return currentWorldSave.playerData;
        }
        
        Debug.LogWarning("[SaveLoadService] No player data available, returning default");
        return CreateDefaultPlayerData();
    }
    
    /// <summary>
    /// Check if this is a brand new world (first time playing)
    /// Returns true if world was just created and never played
    /// </summary>
    public bool IsNewWorld()
    {
        if (currentWorldSave == null) return false;
        
        //Debug.Log($"[SaveLoadService] Checking if new world: TotalPlayTime={currentWorldSave.totalPlayTime}");
        // Simple and reliable: new world has 0 play time
        return currentWorldSave.totalPlayTime == 0f;
    }
    
    /// <summary>
    /// Check if player should spawn at default position (new world) or saved position (existing world)
    /// </summary>
    public bool ShouldUseDefaultSpawn()
    {
        return IsNewWorld();
    }

    /// <summary>
    /// Increments the world level by 1 and immediately saves.
    /// </summary>
    public void ProgressToNextLevel()
    {
        if (currentWorldSave == null) return;

        if (currentWorldSave.worldState == null)
            currentWorldSave.worldState = CreateDefaultWorldState();

        currentWorldSave.worldState.level++;
        SaveWorld(currentWorldSave);

        if (enableDebug) Debug.Log($"[SaveLoadService] Progressed to level {currentWorldSave.worldState.level}");
    }

    /// <summary>
    /// Returns the current world level from the active save.
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentWorldSave?.worldState?.level ?? 1;
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
            
            if (enableDebug) Debug.Log($"Created backup: {backupPath}");
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
            
            if (enableDebug) Debug.Log($"Restored from backup: {backupDate}");
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
    
    private WorldStateSaveData CreateDefaultWorldState(int level = 1)
    {
        return new WorldStateSaveData
        {
            currentTimeOfDay = 6f, // Start at morning
            dayNumber = 1,
            currentWeather = "Clear",
            temperature = 20f,
            level = level,
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
