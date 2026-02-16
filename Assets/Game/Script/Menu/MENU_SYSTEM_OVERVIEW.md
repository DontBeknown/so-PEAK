# Menu System Overview

**Location:** `Assets/Game/Script/Menu/`  
**Last Updated:** February 16, 2026

---

## What is This System?

The **Menu System** handles all menu-related functionality including:
- **World Selection** - Browse, create, load, delete worlds
- **Save Management** - Display save metadata, playtime, health
- **World Creation** - Name worlds, generate seeds
- **Scene Transitions** - Menu ↔ Gameplay scene flow
- **WorldPersistenceManager** - Carries world data between scenes

This system bridges the **Menu Scene** and **Gameplay Scene**, managing save/load operations.

---

## Key Components

### 1. MainMenuUI

**File:** `Menu/MainMenuUI.cs`

**Purpose:** Entry point for the menu system.

**Features:**
```csharp
[Header("UI Panels")]
[SerializeField] private GameObject mainMenuPanel;
[SerializeField] private WorldSelectionUI worldSelectionUI;

[Header("Main Menu Buttons")]
[SerializeField] private Button playButton;
[SerializeField] private Button settingsButton;
[SerializeField] private Button quitButton;
```

**Key Methods:**
```csharp
private void OnPlayClicked()
{
    ShowWorldSelection();
}

private void ShowWorldSelection()
{
    worldSelectionUI.ShowWorldSelection(true);
    worldSelectionUI.RefreshWorldList();
    mainMenuPanel.SetActive(false);
}

public void ShowMainMenu()
{
    mainMenuPanel.SetActive(true);
    worldSelectionUI.ShowWorldSelection(false);
}
```

### 2. WorldSelectionUI

**File:** `Menu/WorldSelectionUI.cs`

**Purpose:** Displays saved worlds and handles world creation/loading.

**Features:**
- World list display with metadata
- Create new world button
- Load/delete world buttons
- Save file metadata preview

**Key Methods:**
```csharp
public void ShowWorldSelection(bool show)
{
    worldSelectionPanel.SetActive(show);
    if (show) RefreshWorldList();
}

public void RefreshWorldList()
{
    ClearWorldList();
    
    var saveService = SaveLoadService.Instance;
    var worlds = saveService.GetAllWorlds();
    
    foreach (var world in worlds)
    {
        CreateWorldEntry(world);
    }
}

private void CreateWorldEntry(SaveMetadata metadata)
{
    var entry = Instantiate(worldEntryPrefab, worldListContainer);
    var entryUI = entry.GetComponent<WorldEntryUI>();
    entryUI.Setup(metadata, this);
}

public void OnLoadWorld(SaveMetadata metadata)
{
    // Load save data
    var saveData = saveLoadService.LoadWorld(metadata.worldGuid);
    
    // Setup world persistence
    var persistence = WorldPersistenceManager.Instance;
    persistence.SetWorldData(
        saveData.worldGuid,
        saveData.worldName,
        saveData.seedData,
        false // not a new world
    );
    
    // Load gameplay scene
    SceneManager.LoadScene("Gameplay");
}

public void OnCreateNewWorld()
{
    worldCreationPanel.SetActive(true);
    worldSelectionPanel.SetActive(false);
}

public void OnDeleteWorld(string worldGuid)
{
    if (ConfirmDelete())
    {
        saveLoadService.DeleteWorld(worldGuid);
        RefreshWorldList();
    }
}
```

### 3. WorldEntryUI

**File:** `Menu/WorldEntryUI.cs`

**Purpose:** Individual world entry in the list.

**Features:**
- Display world name, seed, playtime, health
- Load button
- Delete button
- Hover highlight

**Display:**
```csharp
public void Setup(SaveMetadata metadata, WorldSelectionUI parent)
{
    worldNameText.text = metadata.worldName;
    
    // Format playtime
    TimeSpan time = TimeSpan.FromSeconds(metadata.totalPlayTime);
    playtimeText.text = $"Playtime: {time.Hours}h {time.Minutes}m";
    
    // Show health
    healthText.text = $"Health: {metadata.playerHealth:F0}/100";
    
    // Show seed parts
    seedText.text = $"Seed: {metadata.seed1}/{metadata.seed2}/{metadata.seed3}";
    
    // Show last played
    lastPlayedText.text = $"Last played: {metadata.lastPlayedDate:MM/dd/yyyy}";
    
    // Setup buttons
    loadButton.onClick.AddListener(() => parent.OnLoadWorld(metadata));
    deleteButton.onClick.AddListener(() => parent.OnDeleteWorld(metadata.worldGuid));
}
```

### 4. WorldCreationPanel

**File:** `Menu/WorldCreationPanel.cs`

**Purpose:** Create new world with custom name and seed.

**Features:**
- World name input field
- Seed input (3 parts: word1/word2/word3)
- Random seed generation
- Create button with validation

**Key Methods:**
```csharp
public void OnCreateButtonClicked()
{
    string worldName = worldNameInputField.text.Trim();
    
    // Validate world name
    if (string.IsNullOrEmpty(worldName))
    {
        ShowError("World name cannot be empty");
        return;
    }
    
    // Get or generate seed
    SeedData seedData = GetSeedData();
    
    // Create world save
    var saveService = SaveLoadService.Instance;
    var newWorld = saveService.CreateNewWorld(worldName, seedData);
    
    // Setup world persistence
    var persistence = WorldPersistenceManager.Instance;
    persistence.SetWorldData(
        newWorld.worldGuid,
        newWorld.worldName,
        newWorld.seedData,
        true // is new world
    );
    
    // Load gameplay scene
    SceneManager.LoadScene("Gameplay");
}

private SeedData GetSeedData()
{
    string seed1 = seed1InputField.text.Trim();
    string seed2 = seed2InputField.text.Trim();
    string seed3 = seed3InputField.text.Trim();
    
    // Use input seeds or generate random
    if (string.IsNullOrEmpty(seed1))
        seed1 = SeedGenerator.GenerateRandomWord();
    if (string.IsNullOrEmpty(seed2))
        seed2 = SeedGenerator.GenerateRandomWord();
    if (string.IsNullOrEmpty(seed3))
        seed3 = SeedGenerator.GenerateRandomWord();
    
    return new SeedData(seed1, seed2, seed3);
}

public void OnRandomSeedClicked()
{
    var randomSeed = SeedGenerator.GenerateRandomSeed();
    seed1InputField.text = randomSeed.seed1;
    seed2InputField.text = randomSeed.seed2;
    seed3InputField.text = randomSeed.seed3;
}
```

### 5. WorldPersistenceManager

**File:** `Menu/WorldPersistenceManager.cs`

**Purpose:** Carries world data between Menu and Gameplay scenes.

**Pattern:** Singleton with DontDestroyOnLoad

**Data:**
```csharp
public class WorldPersistenceManager : MonoBehaviour
{
    public static WorldPersistenceManager Instance { get; private set; }
    
    // World data to pass to gameplay scene
    public string currentWorldGuid;
    public string currentWorldName;
    public SeedData seedData;
    public bool isNewWorld;
    public bool shouldLoadWorld;
    
    // Spawn position (set by terrain generator or default)
    public Vector3 playerStartPosition;
    public Quaternion playerStartRotation;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SetWorldData(string guid, string name, SeedData seed, bool newWorld)
    {
        currentWorldGuid = guid;
        currentWorldName = name;
        seedData = seed;
        isNewWorld = newWorld;
        shouldLoadWorld = true;
    }
    
    public int GetSeedAsInt()
    {
        return seedData.FullSeed.GetHashCode();
    }
    
    public void ClearWorldData()
    {
        shouldLoadWorld = false;
        currentWorldGuid = null;
        currentWorldName = null;
    }
}
```

### 6. SeedGenerator

**File:** `Menu/SeedGenerator.cs`

**Purpose:** Generate random seeds for world creation.

**Implementation:**
```csharp
public static class SeedGenerator
{
    private static readonly string[] wordList = new string[]
    {
        "Mountain", "Forest", "Ocean", "Desert", "Valley",
        "Peak", "River", "Lake", "Canyon", "Glacier",
        "Storm", "Sunset", "Dawn", "Frost", "Ember",
        "Crystal", "Shadow", "Light", "Wind", "Thunder"
        // ... more words ...
    };
    
    public static SeedData GenerateRandomSeed()
    {
        string seed1 = GenerateRandomWord();
        string seed2 = GenerateRandomWord();
        string seed3 = GenerateRandomWord();
        
        return new SeedData(seed1, seed2, seed3);
    }
    
    public static string GenerateRandomWord()
    {
        int index = Random.Range(0, wordList.Length);
        return wordList[index];
    }
}
```

---

## How It Works in Game

### Menu → Gameplay Flow (New World)

```
1. Player clicks "Play" in MainMenuUI
   │
   ▼
2. WorldSelectionUI.ShowWorldSelection(true)
   │
   ▼
3. Player clicks "Create New World"
   │
   ▼
4. WorldCreationPanel opens
   │
   ├─► Player enters world name
   ├─► Player enters seed (or randomize)
   └─► Player clicks "Create"
   │
   ▼
5. WorldCreationPanel.OnCreateButtonClicked()
   │
   ├─► SaveLoadService.CreateNewWorld(name, seed)
   │   ├─► Create WorldSaveData with GUID
   │   ├─► Save to disk
   │   └─► Update metadata.json
   │
   └─► WorldPersistenceManager.SetWorldData(guid, name, seed, isNew=true)
   │
   ▼
6. SceneManager.LoadScene("Gameplay")
   │
   ▼
7. GameplaySceneInitializer.Start()
   │
   ├─► Check: WorldPersistenceManager.isNewWorld == true
   │
   └─► InitializeNewWorld()
       ├─► Generate terrain with seed
       ├─► Spawn player at default position
       └─► Initialize default world state (time = 6 AM)
```

### Menu → Gameplay Flow (Load World)

```
1. Player clicks "Play" in MainMenuUI
   │
   ▼
2. WorldSelectionUI.RefreshWorldList()
   │
   ├─► SaveLoadService.GetAllWorlds()
   └─► Display world entries
   │
   ▼
3. Player clicks "Load" on a world entry
   │
   ▼
4. WorldSelectionUI.OnLoadWorld(metadata)
   │
   ├─► SaveLoadService.LoadWorld(worldGuid)
   │   └─► Returns WorldSaveData
   │
   └─► WorldPersistenceManager.SetWorldData(guid, name, seed, isNew=false)
   │
   ▼
5. SceneManager.LoadScene("Gameplay")
   │
   ▼
6. GameplaySceneInitializer.Start()
   │
   ├─► Check: WorldPersistenceManager.isNewWorld == false
   │
   └─► InitializeLoadedWorld()
       ├─► Generate terrain with saved seed
       ├─► Spawn player at saved position
       └─► Restore world state (time, inventory, equipment)
```

### Gameplay → Menu Flow

```
1. Player presses ESC or clicks "Save & Exit"
   │
   ▼
2. SaveExitButton.SaveAndExitToMenu()
   │
   ├─► SaveLoadService.PerformAutoSave()
   │   ├─► UpdatePlayerDataFromGame()
   │   ├─► SaveWorld(currentWorldSave)
   │   └─► Create backup
   │
   └─► SceneManager.LoadScene("Menu")
   │
   ▼
3. MainMenuUI loads
   │
   └─► WorldPersistenceManager.ClearWorldData()
```

### Delete World Flow

```
1. Player clicks "Delete" on world entry
   │
   ▼
2. ConfirmationDialog shows: "Delete world 'MyWorld'?"
   │
   ├─► Player clicks "Cancel" → Dialog closes
   │
   └─► Player clicks "Confirm"
       │
       ▼
       WorldSelectionUI.OnDeleteWorld(worldGuid)
       │
       ├─► SaveLoadService.DeleteWorld(worldGuid)
       │   ├─► Delete save file
       │   ├─► Delete backups
       │   └─► Update metadata.json
       │
       └─► RefreshWorldList()
           └─► World entry removed from display
```

---

## How to Use

### Creating Menu Scene

1. **Setup scene hierarchy:**
```
MenuScene
├── Canvas
│   ├── MainMenuPanel
│   │   ├── PlayButton
│   │   ├── SettingsButton
│   │   └── QuitButton
│   │
│   ├── WorldSelectionPanel
│   │   ├── WorldListScrollView
│   │   ├── CreateNewWorldButton
│   │   └── BackButton
│   │
│   └── WorldCreationPanel
│       ├── WorldNameInputField
│       ├── Seed1InputField
│       ├── Seed2InputField
│       ├── Seed3InputField
│       ├── RandomSeedButton
│       └── CreateButton
│
├── SaveLoadService (DontDestroyOnLoad)
└── WorldPersistenceManager (DontDestroyOnLoad)
```

2. **Assign references in Inspector:**
   - MainMenuUI → panels and buttons
   - WorldSelectionUI → prefabs and containers
   - Wire up button onClick events

### Accessing World Data in Gameplay

```csharp
// In GameplaySceneInitializer or terrain generator
public class TerrainGenerator : MonoBehaviour
{
    private void Start()
    {
        var persistence = WorldPersistenceManager.Instance;
        
        if (persistence != null && persistence.shouldLoadWorld)
        {
            // Get seed for terrain generation
            int seed = persistence.GetSeedAsInt();
            
            // Generate terrain
            GenerateTerrain(seed);
            
            // Check if new world
            if (persistence.isNewWorld)
            {
                Debug.Log("Generating fresh terrain");
            }
            else
            {
                Debug.Log("Loading existing world");
            }
        }
    }
}
```

### Adding Custom World Metadata

1. **Extend SaveMetadata:**
```csharp
[Serializable]
public class SaveMetadata
{
    // ... existing fields ...
    
    // NEW: Custom metadata
    public string gameMode; // "Survival", "Creative"
    public int playerLevel;
    public string currentBiome;
}
```

2. **Update in SaveLoadService:**
```csharp
private void UpdateMetadata(WorldSaveData saveData)
{
    // ... existing code ...
    
    metadata.gameMode = saveData.worldState.gameMode;
    metadata.playerLevel = saveData.playerData.level;
}
```

3. **Display in WorldEntryUI:**
```csharp
public void Setup(SaveMetadata metadata, WorldSelectionUI parent)
{
    // ... existing code ...
    
    gameModeText.text = $"Mode: {metadata.gameMode}";
    levelText.text = $"Level: {metadata.playerLevel}";
}
```

---

## How to Expand

### Adding World Templates

**Example: Quick start worlds**

```csharp
public class WorldTemplateSelector : MonoBehaviour
{
    [SerializeField] private Button survivalButton;
    [SerializeField] private Button creativeButton;
    
    private void Start()
    {
        survivalButton.onClick.AddListener(() => CreateFromTemplate("Survival"));
        creativeButton.onClick.AddListener(() => CreateFromTemplate("Creative"));
    }
    
    private void CreateFromTemplate(string templateName)
    {
        var template = GetTemplate(templateName);
        
        // Create world with template settings
        var saveService = SaveLoadService.Instance;
        var newWorld = saveService.CreateNewWorld(
            $"New {templateName} World",
            template.seed
        );
        
        // Apply template-specific settings
        newWorld.worldState.gameMode = templateName;
        newWorld.playerData.inventoryItems = template.startingItems;
        
        saveService.SaveWorld(newWorld);
        
        // Load into game
        LoadWorld(newWorld);
    }
}
```

### Adding World Preview Screenshots

1. **Capture screenshot on save:**
```csharp
// In SaveLoadService
public void CaptureWorldThumbnail(string worldGuid)
{
    string screenshotPath = Path.Combine(SaveDirectory, $"{worldGuid}_thumb.png");
    ScreenCapture.CaptureScreenshot(screenshotPath);
    
    // Update metadata
    var metadata = GetMetadata(worldGuid);
    metadata.thumbnailPath = screenshotPath;
    UpdateMetadata(metadata);
}
```

2. **Display in WorldEntryUI:**
```csharp
public void Setup(SaveMetadata metadata, WorldSelectionUI parent)
{
    // ... existing code ...
    
    if (!string.IsNullOrEmpty(metadata.thumbnailPath))
    {
        LoadThumbnail(metadata.thumbnailPath);
    }
}

private void LoadThumbnail(string path)
{
    if (File.Exists(path))
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        
        thumbnailImage.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }
}
```

### Adding World Sorting/Filtering

```csharp
public class WorldSelectionUI : MonoBehaviour
{
    [SerializeField] private Dropdown sortDropdown;
    
    public void RefreshWorldList()
    {
        var worlds = saveLoadService.GetAllWorlds();
        
        // Sort based on dropdown
        switch (sortDropdown.value)
        {
            case 0: // Last played
                worlds = worlds.OrderByDescending(w => w.lastPlayedDate).ToList();
                break;
            case 1: // Playtime
                worlds = worlds.OrderByDescending(w => w.totalPlayTime).ToList();
                break;
            case 2: // Name A-Z
                worlds = worlds.OrderBy(w => w.worldName).ToList();
                break;
        }
        
        DisplayWorlds(worlds);
    }
}
```

---

## Architecture Patterns

### Singleton Pattern

**WorldPersistenceManager** uses singleton:
- Single instance across scene transitions
- DontDestroyOnLoad persistence
- Global access point

### Facade Pattern

**WorldSelectionUI** acts as facade:
- Simplifies save/load operations
- Hides SaveLoadService complexity
- Provides user-friendly interface

---

## Performance Considerations

### World List Loading
- **Metadata only:** Don't load full save files
- **Lazy loading:** Load thumbnails on demand
- **Pagination:** Show 10-20 worlds per page for large saves

### Scene Transitions
- **Async loading:** Use SceneManager.LoadSceneAsync()
- **Loading screen:** Show progress bar during load

---

## Troubleshooting

### World Not Loading

**Check:**
1. WorldPersistenceManager exists in scene?
2. SaveLoadService persisted from menu scene?
3. World GUID correct?
4. Save file exists?

**Debug:**
```csharp
Debug.Log($"World GUID: {persistence.currentWorldGuid}");
Debug.Log($"Is new world: {persistence.isNewWorld}");
Debug.Log($"Should load: {persistence.shouldLoadWorld}");
```

### Duplicate WorldPersistenceManager

**Cause:** Multiple instances created across scenes

**Fix:**
```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Debug.LogWarning("Duplicate WorldPersistenceManager destroyed");
        Destroy(gameObject);
        return;
    }
    
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### Save List Not Updating

**Check:**
1. RefreshWorldList() called after create/delete?
2. Metadata.json updated correctly?
3. File permissions OK?

**Debug:**
```csharp
var worlds = saveLoadService.GetAllWorlds();
Debug.Log($"Found {worlds.Count} worlds");
foreach (var world in worlds)
{
    Debug.Log($"World: {world.worldName} ({world.worldGuid})");
}
```

---

## Best Practices

### ✅ DO
- Use WorldPersistenceManager for scene data transfer
- Clear WorldPersistenceManager after gameplay scene loads
- Show confirmation dialog before deleting worlds
- Validate world names (no special characters, max length)
- Sort worlds by last played date by default
- Display playtime in human-readable format
- Handle missing save files gracefully

### ❌ DON'T
- Don't use static variables for scene transitions (use persistence manager)
- Don't load full save files for world list (use metadata)
- Don't forget to unsubscribe from button events
- Don't allow empty world names
- Don't block UI thread during save/load operations
- Don't delete worlds without confirmation
- Don't forget to update metadata after save

---

## File Structure

```
Menu/
├── MainMenuUI.cs                   # Main menu controller
├── WorldSelectionUI.cs             # World list display
├── WorldEntryUI.cs                 # Individual world entry
├── WorldCreationPanel.cs           # New world creation
├── WorldPersistenceManager.cs      # Scene data transfer
├── SeedGenerator.cs                # Random seed generation
└── ConfirmationDialog.cs           # Delete confirmation
```

---

## Integration Points

### With Core System
- SaveLoadService for save/load operations
- Scene management for transitions
- WorldPersistenceManager persists between scenes

### With Gameplay System
- GameplaySceneInitializer reads WorldPersistenceManager
- Terrain generator uses seed from persistence
- Player spawner uses saved position

---

## Related Documentation

- [CORE_SYSTEM_OVERVIEW.md](../Core/CORE_SYSTEM_OVERVIEW.md) - SaveLoadService details
- [SAVE_LOAD_SYSTEM_DESIGN.md](../../../SAVE_LOAD_SYSTEM_DESIGN.md) - Save file format

---

**Last Updated:** February 16, 2026  
**System Status:** ✅ Production Ready  
**Architecture Quality:** ⭐⭐⭐⭐ (Clean separation, good UX)
