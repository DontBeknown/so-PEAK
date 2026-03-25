using System.Collections;
using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;
using Game.Environment.DayNight;
using Game.Player.Inventory;
using Game.Interaction;
using Game.Tutorial;
public class GameplaySceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldPersistenceManager worldPersistence;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private SaveLoadService saveLoadService; // Optional: will use persisted instance if not set
    
    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;
    private void Awake()
    {
       //Debug.Log($"[GameplaySceneInitializer] Awake called.");
    }
    private void Start()
    {
        //Debug.Log($"[GameplaySceneInitializer] Start called.");
        // Get save load service (from Inspector or persisted instance)
        if (saveLoadService == null)
        {
            saveLoadService = SaveLoadService.Instance;
        }
        
        // Fallback to ServiceContainer if available
        if (saveLoadService == null)
        {
            var container = ServiceContainer.Instance;
            if (container != null)
            {
                saveLoadService = container.Get<ISaveLoadService>() as SaveLoadService;
            }
        }
        
        if (saveLoadService == null)
        {
            Debug.LogError("SaveLoadService not found! Ensure it persists from menu scene.");
            return;
        }
        
        if (worldPersistence == null)
        {
            Debug.LogError("WorldPersistenceManager not assigned!");
            return;
        }

        
        
        InitializeWorld();
    }
    
    private void InitializeWorld()
    {
        StartCoroutine(InitializeWorldCoroutine());
    }
    
    private IEnumerator InitializeWorldCoroutine()
    {
        WorldSaveData resolvedSaveData = null;

        if (worldPersistence.isNewWorld)
        {
            if (enableDebug) Debug.Log($"Initializing new world: {worldPersistence.currentWorldName}");
            InitializeDefaultWorldState();
            resolvedSaveData = saveLoadService.CurrentWorldSave;

            var renderController = FindFirstObjectByType<RenderController>();
            if (renderController != null)
            {
                yield return new WaitUntil(() => renderController.PlayerSpawnComplete);
            }
        }
        else if (worldPersistence.shouldLoadWorld)
        {
            if (enableDebug) Debug.Log($"Loading world: {worldPersistence.currentWorldName}");

            WorldSaveData saveData = saveLoadService.LoadWorld(worldPersistence.currentWorldGuid);
            if (saveData == null)
            {
                Debug.LogError("Failed to load world save data!");
                yield break;
            }

            resolvedSaveData = saveData;

            // Restore day/night immediately — does not require the player
            if (saveData.worldState != null)
            {
                var dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
                if (dayNightManager != null)
                {
                    dayNightManager.SetTime(saveData.worldState.currentTimeOfDay);
                    dayNightManager.SetDay(saveData.worldState.dayNumber);
                }
            }

            // Wait until RenderController has fully completed the player spawn sequence
            // (terrain loaded → player instantiated → services updated)
            var renderController = FindFirstObjectByType<RenderController>();
            yield return new WaitUntil(() =>
                renderController != null && renderController.PlayerSpawnComplete);

            // Restore player-dependent state
            var playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
            if (playerStats != null && saveData.playerData != null)
                RestorePlayerStats(playerStats, saveData.playerData);

            RestoreInventory(saveData.playerData);
            RestoreEquipment(saveData.playerData);
            RestoreResourceNodes(saveData.worldState);

            if (enableDebug) Debug.Log("World state restored successfully");
        }
        else
        {
            Debug.LogError("No world data to initialize!");
        }

        if (enableDebug) Debug.Log("Starting tutorial if needed...");
        TryStartTutorial(resolvedSaveData);
    }

    private void TryStartTutorial(WorldSaveData saveData)
    {
        Debug.Log($"[GameplaySceneInitializer] Checking tutorial status to determine if tutorial should start...");
        if (saveData == null)
        {
            Debug.LogWarning("No save data available to check tutorial status. Skipping tutorial start.");
            saveData = saveLoadService.CurrentWorldSave;
        }

        if (saveData == null)
        {
            Debug.LogError("Still no save data available to check tutorial status. Cannot start tutorial.");
            return;
        }

        saveData.tutorial ??= new TutorialSaveData();

        if (saveData.tutorial.isCompleted)
        {
            Debug.Log("Tutorial already completed according to save data. Skipping tutorial start.");
            return;
        }

        var tutorialManager = ServiceContainer.Instance.TryGet<ITutorialManager>();
        tutorialManager?.StartTutorial();
    }
    
    private void SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        /*if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, position, rotation);
            Debug.Log($"Spawned player at {position}");
        }*/
    }
    
    private void InitializeDefaultWorldState()
    {
        // Set time to morning, day 1
        var dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
        if (dayNightManager != null)
        {
            dayNightManager.SetTime(6f); // 6 AM
            dayNightManager.SetDay(1);
        }
    }
    
    private void RestorePlayerStats(PlayerStats playerStats, PlayerSaveData playerData)
    {
        // Restore health by calculating difference
        float healthDiff = playerData.health - playerStats.Health;
        if (healthDiff > 0)
            playerStats.Heal(healthDiff);
        else if (healthDiff < 0)
            playerStats.TakeDamage(-healthDiff);
        
        // Restore hunger by calculating difference
        float hungerDiff = playerData.hunger - playerStats.Hunger;
        if (hungerDiff > 0)
            playerStats.Eat(hungerDiff);
        else if (hungerDiff < 0)
            playerStats.Eat(hungerDiff); // Hunger stat handles negative values
        
        // Restore stamina by calculating difference
        float staminaDiff = playerData.stamina - playerStats.Stamina;
        if (staminaDiff > 0)
            playerStats.RestoreStamina(staminaDiff);
        else if (staminaDiff < 0)
            playerStats.ConsumeStamina(-staminaDiff);
    }
    
    private void RestoreInventory(PlayerSaveData playerData)
    {
        var inventoryManager = ServiceContainer.Instance.TryGet<InventoryManagerRefactored>();
        if (inventoryManager == null || playerData.inventoryItems == null) return;
        
        foreach (var itemData in playerData.inventoryItems)
        {
            InventoryItem item = Resources.Load<InventoryItem>($"Items/{itemData.itemId}");
            if (item == null) continue;

            // Restore at saved grid position; fall back to auto-place if the slot is taken
            var placement = inventoryManager.PlaceItemAt(item, new Vector2Int(itemData.gridX, itemData.gridY));
            if (placement == null)
            {
                inventoryManager.AddItem(item, 1);
            }
            else if (itemData.isRotated)
            {
                inventoryManager.RotateItem(placement);
            }
        }
    }
    
    private void RestoreEquipment(PlayerSaveData playerData)
    {
        var equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();
        if (equipmentManager == null || playerData.equippedItems == null) return;
        
        foreach (var equipData in playerData.equippedItems)
        {
            InventoryItem item = Resources.Load<InventoryItem>($"Items/{equipData.itemId}");
            if (item != null && item is IEquippable equippable)
            {
                equipmentManager.Equip(equippable);
            }
        }
    }
    
    private void RestoreResourceNodes(WorldStateSaveData worldState)
    {

    }
}
