using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveLoadService
{
    // World Management
    WorldSaveData CreateNewWorld(string worldName, SeedData seedData, int level = 1);
    bool SaveWorld(WorldSaveData saveData);
    WorldSaveData LoadWorld(string worldGuid);
    bool DeleteWorld(string worldGuid);
    List<SaveMetadata> GetAllWorlds();
    
    // Auto-save
    void EnableAutoSave(float intervalSeconds);
    void DisableAutoSave();
    void PerformAutoSave();
    void PerformAutoSave(Transform customSpawnPoint);
    
    // Backup
    bool CreateBackup(string worldGuid);
    bool RestoreFromBackup(string worldGuid, DateTime backupDate);
    List<DateTime> GetBackups(string worldGuid);
    
    // Level progression
    void ProgressToNextLevel();
    int GetCurrentLevel();
    
    // Validation
    bool ValidateSaveFile(string worldGuid);
    
    // Events
    event Action<WorldSaveData> OnWorldSaved;
    event Action<WorldSaveData> OnWorldLoaded;
    event Action<string> OnWorldDeleted;
}
