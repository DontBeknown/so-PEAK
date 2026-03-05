using UnityEngine;

/// <summary>
/// Example script: Loads current world seed from SaveLoadService
/// Use this to get seed data for terrain generation or other systems
/// </summary>
public class WorldSeedLoader : MonoBehaviour
{
    private void Start()
    {
        LoadCurrentWorldSeed();
    }
    
    private void LoadCurrentWorldSeed()
    {
        var saveService = SaveLoadService.Instance;
        
        if (saveService == null)
        {
            Debug.LogError("[WorldSeedLoader] SaveLoadService not found!");
            return;
        }
        
        // Get current world save data
        WorldSaveData currentWorld = saveService.CurrentWorldSave;
        
        if (currentWorld == null)
        {
            Debug.LogError("[WorldSeedLoader] No world is currently loaded!");
            return;
        }
        

        int currentLevel = currentWorld.worldState.level;
        // Access seed data
        SeedData seedData = currentWorld.seedData;
        
        // Get seed parts
        string seed1 = seedData.seed1;
        string seed2 = seedData.seed2;
        string seed3 = seedData.seed3;
        
        // Get full combined seed
        string fullSeed = seedData.FullSeed;
        
        Debug.Log($"[WorldSeedLoader] Loaded World: {currentWorld.worldName}");
        Debug.Log($"[WorldSeedLoader] Seed Part 1: {seed1}");
        Debug.Log($"[WorldSeedLoader] Seed Part 2: {seed2}");
        Debug.Log($"[WorldSeedLoader] Seed Part 3: {seed3}");
        Debug.Log($"[WorldSeedLoader] Full Seed: {fullSeed}");
        Debug.Log($"[WorldSeedLoader] Current Level: {currentLevel}");
        
        // Use the seed for terrain generation
        GenerateTerrainWithSeed(fullSeed);
    }
    
    private void GenerateTerrainWithSeed(string seed)
    {
        // Example: Convert seed to int for Unity's Random
        int seedValue = seed.GetHashCode();
        
        Debug.Log($"[WorldSeedLoader] Using seed value: {seedValue} for generation");
        
        // TODO: Pass to your terrain generator
        // terrainGenerator.GenerateTerrain(seedValue);
    }
}
