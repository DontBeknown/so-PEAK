#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Text;
using Game.Menu;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class SaveSystemEditorWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<SaveMetadata> allWorlds;
    private SaveLoadService saveLoadService;
    private WorldSelectionUI worldSelectionUI;
    private SeedConfig seedConfig;
    
    private string searchFilter = "";
    
    // New World Creation
    private string newWorldName = "New World";
    private string seed1 = "";
    private string seed2 = "";
    private string seed3 = "";
    private bool showCreateWorld = false;

    // Destroyed spawned-object debug cache (worldGuid -> destroyed count)
    private readonly Dictionary<string, int> destroyedSpawnedCountByWorld = new Dictionary<string, int>();
    private const int SpawnIdPreviewLimit = 20;
    
    [MenuItem("Tools/Save System Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<SaveSystemEditorWindow>("Save Manager");
        window.minSize = new Vector2(500, 600);
    }
    
    private void OnEnable()
    {
        // Don't search for scene objects during compilation or when no scene is loaded
        if (EditorApplication.isCompiling || !EditorApplication.isPlaying)
        {
            // Only find SaveLoadService if we're in play mode
            // In edit mode, we'll work directly with file system
            saveLoadService = null;
        }
        else
        {
            EnsureSaveLoadService();
        }
        
        // Load seed config (safe - it's an asset)
        try
        {
            seedConfig = AssetDatabase.LoadAssetAtPath<SeedConfig>(
                "Assets/Game/ScriptableObject/World/SeedConfig.asset");
       }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load SeedConfig: {e.Message}");
        }
        
        RefreshWorldList();
    }
    
    /// <summary>
    /// Ensures SaveLoadService is found when needed. Call this before any action that requires the service.
    /// </summary>
    private void EnsureSaveLoadService()
    {
        if (saveLoadService == null && EditorApplication.isPlaying && !EditorApplication.isCompiling)
        {
            saveLoadService = FindFirstObjectByType<SaveLoadService>();
            if (saveLoadService == null)
            {
                // Debug.LogWarning("SaveLoadService not found in scene! Save Manager will operate in file mode.");
            }
        }
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
            EnsureSaveLoadService();
            RefreshWorldList();
            RefreshWorldSelectionUI();
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
        
        // Load button (only enabled in Play mode)
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        if (GUILayout.Button("Load", GUILayout.Width(60)))
        {
            LoadWorldInEditor(world);
        }
        EditorGUI.EndDisabledGroup();
        
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

        if (GUILayout.Button("Debug Destroyed", GUILayout.Width(120)))
        {
            ShowDestroyedSpawnedDebug(world);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Details
        EditorGUILayout.LabelField($"Seed: {world.seed1}-{world.seed2}-{world.seed3}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Last Played: {world.lastPlayedDate:yyyy-MM-dd HH:mm}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Play Time: {FormatPlayTime(world.totalPlayTime)}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Health: {world.playerHealth:F0}", EditorStyles.miniLabel);

        int destroyedCount = 0;
        if (!string.IsNullOrEmpty(world.worldGuid))
        {
            destroyedSpawnedCountByWorld.TryGetValue(world.worldGuid, out destroyedCount);
        }
        EditorGUILayout.LabelField($"Destroyed Spawned: {destroyedCount}", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    private void RefreshWorldList()
    {
        EnsureSaveLoadService();
        
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

        RefreshDestroyedSpawnedCounts();
    }

    private void RefreshDestroyedSpawnedCounts()
    {
        destroyedSpawnedCountByWorld.Clear();

        if (allWorlds == null || allWorlds.Count == 0)
        {
            return;
        }

        foreach (SaveMetadata world in allWorlds)
        {
            if (world == null || string.IsNullOrEmpty(world.worldGuid))
            {
                continue;
            }

            if (TryLoadDestroyedSpawnIds(world.worldGuid, out List<string> spawnIds, out _))
            {
                destroyedSpawnedCountByWorld[world.worldGuid] = spawnIds.Count;
            }
            else
            {
                destroyedSpawnedCountByWorld[world.worldGuid] = 0;
            }
        }
    }

    private void ShowDestroyedSpawnedDebug(SaveMetadata world)
    {
        if (world == null || string.IsNullOrEmpty(world.worldGuid))
        {
            EditorUtility.DisplayDialog("Debug Destroyed", "World data is invalid or missing world GUID.", "OK");
            return;
        }

        if (!TryLoadDestroyedSpawnIds(world.worldGuid, out List<string> spawnIds, out string error))
        {
            EditorUtility.DisplayDialog(
                "Debug Destroyed",
                $"Failed to read destroyed spawned-object data for '{world.worldName}'.\n\nReason: {error}",
                "OK");
            return;
        }

        var preview = spawnIds.Take(SpawnIdPreviewLimit).ToList();
        string previewText = preview.Count > 0 ? string.Join("\n", preview) : "(none)";
        string truncatedText = spawnIds.Count > SpawnIdPreviewLimit
            ? $"\n\n...and {spawnIds.Count - SpawnIdPreviewLimit} more. Check Console for full list."
            : string.Empty;

        EditorUtility.DisplayDialog(
            "Debug Destroyed",
            $"World: {world.worldName}\nGUID: {world.worldGuid}\nDestroyed Count: {spawnIds.Count}\n\nPreview:\n{previewText}{truncatedText}",
            "OK");

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[SaveSystemEditor] Destroyed SpawnIds for world '{world.worldName}' ({world.worldGuid})");

        if (spawnIds.Count == 0)
        {
            logBuilder.AppendLine("(none)");
        }
        else
        {
            for (int i = 0; i < spawnIds.Count; i++)
            {
                logBuilder.Append(i + 1);
                logBuilder.Append(". ");
                logBuilder.AppendLine(spawnIds[i]);
            }
        }

        Debug.Log(logBuilder.ToString());
    }

    private bool TryLoadDestroyedSpawnIds(string worldGuid, out List<string> destroyedSpawnIds, out string error)
    {
        destroyedSpawnIds = new List<string>();
        error = string.Empty;

        if (string.IsNullOrEmpty(worldGuid))
        {
            error = "World GUID is empty.";
            return false;
        }

        string filePath = GetSaveFilePath(worldGuid);
        if (!File.Exists(filePath))
        {
            error = $"Save file not found at: {filePath}";
            return false;
        }

        string rawText;
        try
        {
            rawText = File.ReadAllText(filePath);
        }
        catch (Exception e)
        {
            error = $"Failed to read save file: {e.Message}";
            return false;
        }

        if (TryParseWorldSaveData(rawText, out WorldSaveData saveData))
        {
            destroyedSpawnIds = ExtractDestroyedSpawnIds(saveData);
            return true;
        }

        if (TryDecodeBase64ToJson(rawText, out string decodedJson) && TryParseWorldSaveData(decodedJson, out saveData))
        {
            destroyedSpawnIds = ExtractDestroyedSpawnIds(saveData);
            return true;
        }

        error = "Save data could not be parsed as raw JSON or Base64-encoded JSON.";
        return false;
    }

    private string GetSaveFilePath(string worldGuid)
    {
        return Path.Combine(Application.persistentDataPath, "Saves", $"{worldGuid}.sav");
    }

    private bool TryParseWorldSaveData(string json, out WorldSaveData saveData)
    {
        saveData = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            saveData = JsonUtility.FromJson<WorldSaveData>(json);
            return saveData != null;
        }
        catch
        {
            return false;
        }
    }

    private bool TryDecodeBase64ToJson(string encodedText, out string decodedJson)
    {
        decodedJson = string.Empty;

        if (string.IsNullOrWhiteSpace(encodedText))
        {
            return false;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(encodedText);
            decodedJson = Encoding.UTF8.GetString(bytes);
            return !string.IsNullOrWhiteSpace(decodedJson);
        }
        catch
        {
            return false;
        }
    }

    private List<string> ExtractDestroyedSpawnIds(WorldSaveData saveData)
    {
        if (saveData?.worldState?.spawnedObjectStates == null)
        {
            return new List<string>();
        }

        return saveData.worldState.spawnedObjectStates
            .Where(state => state != null && state.isDestroyed && !string.IsNullOrEmpty(state.spawnId))
            .Select(state => state.spawnId)
            .Distinct()
            .ToList();
    }

    private void RefreshWorldSelectionUI()
    {
        if (EditorApplication.isCompiling || !EditorApplication.isPlaying)
        {
            worldSelectionUI = null;
            return;
        }
        
        if (worldSelectionUI == null)
        {
            worldSelectionUI = FindFirstObjectByType<WorldSelectionUI>();
        }

        if (worldSelectionUI != null && worldSelectionUI.gameObject.activeInHierarchy)
        {
            worldSelectionUI.RefreshWorldList();    
        }
    }
    
    private void CreateNewWorld()
    {
        if (seedConfig == null)
        {
            EditorUtility.DisplayDialog("Error", "SeedConfig not found!", "OK");
            return;
        }
        
        EnsureSaveLoadService();
        
        if (saveLoadService == null)
        {
            EditorUtility.DisplayDialog("Error", "SaveLoadService not found in scene! Make sure you're in Play mode and the scene has SaveLoadService.", "OK");
            return;
        }
        
        SeedData seedData = new SeedData(seed1, seed2, seed3);
        saveLoadService.CreateNewWorld(newWorldName, seedData);
        
        RefreshWorldList();
        RefreshWorldSelectionUI();
        showCreateWorld = false;
        
        // Debug.Log($"Created world: {newWorldName}");
    }
    
    private void LoadWorldInEditor(SaveMetadata world)
    {
        // Validate we're in Play mode
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", 
                "Can only load worlds in Play mode! Enter Play mode first.", 
                "OK");
            return;
        }
        
        // Ensure SaveLoadService is found
        EnsureSaveLoadService();
        
        if (saveLoadService == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "SaveLoadService not found in scene! Make sure the scene has SaveLoadService.", 
                "OK");
            return;
        }
        
        // Load world data into SaveLoadService (this sets currentWorldSave)
        WorldSaveData saveData = saveLoadService.LoadWorld(world.worldGuid);
        
        if (saveData == null)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Failed to load world '{world.worldName}'. Check console for details.", 
                "OK");
            return;
        }
        
        // Load WorldPersistenceManager asset
        const string worldPersistencePath = "Assets/Game/ScriptableObject/World/WorldPersistence.asset";
        WorldPersistenceManager worldPersistence = AssetDatabase.LoadAssetAtPath<WorldPersistenceManager>(worldPersistencePath);
        
        if (worldPersistence == null)
        {
            EditorUtility.DisplayDialog("Error", 
                $"WorldPersistenceManager asset not found at path:\n{worldPersistencePath}\n\nPlease create it or update the path.", 
                "OK");
            return;
        }
        
        // Prepare world persistence for loading
        worldPersistence.PrepareLoadWorld(saveData);
        
        // Mark asset as dirty and save
        EditorUtility.SetDirty(worldPersistence);
        AssetDatabase.SaveAssets();
        
        // Debug.Log($"[SaveSystemEditor] Loaded world '{world.worldName}' into SaveLoadService and updated WorldPersistenceManager");
        
        // Get current scene
        Scene currentScene = SceneManager.GetActiveScene();
        
        // Confirm before reloading
        if (EditorUtility.DisplayDialog("Reload Scene", 
            $"World '{world.worldName}' loaded successfully!\n\nReload scene '{currentScene.name}' to initialize the world?\n\n" +
            "Warning: Any unsaved changes in the current scene will be lost.", 
            "Reload Scene", "Cancel"))
        {
            // Reload the current scene
            EditorSceneManager.LoadScene(currentScene.path, LoadSceneMode.Single);
            
            // Debug.Log($"[SaveSystemEditor] Scene '{currentScene.name}' reloaded. GameplaySceneInitializer will now initialize the loaded world.");
        }
    }
    
    private void EditWorld(SaveMetadata world)
    {
        WorldSaveDataEditorWindow.ShowWindow(world.worldGuid);
    }
    
    private void DeleteWorld(string worldGuid)
    {
        EnsureSaveLoadService();
        
        if (saveLoadService != null)
        {
            saveLoadService.DeleteWorld(worldGuid);
        }
        else
        {
            // Fallback to direct file system operation
            string savePath = Path.Combine(Application.persistentDataPath, "Saves", $"{worldGuid}.sav");
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }
        
        RefreshWorldList();
        RefreshWorldSelectionUI();
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
        RefreshWorldSelectionUI();
        // Debug.Log("All saves cleared!");
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
        if (saveLoadService == null && EditorApplication.isPlaying && !EditorApplication.isCompiling)
        {
            // Try again to find the service
            saveLoadService = FindFirstObjectByType<SaveLoadService>();
        }
        
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
            SaveWorldData();
        }
    }
    
    private void SaveWorldData()
    {
        var saveLoadService = FindFirstObjectByType<SaveLoadService>();
        if (saveLoadService == null && EditorApplication.isPlaying && !EditorApplication.isCompiling)
        {
            // Try again to find the service
            saveLoadService = FindFirstObjectByType<SaveLoadService>();
        }
        
        if (saveLoadService != null)
        {
            saveLoadService.SaveWorld(saveData);
            EditorUtility.DisplayDialog("Success", "World saved successfully!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "SaveLoadService not found in scene!", "OK");
        }
    }
}
#endif
