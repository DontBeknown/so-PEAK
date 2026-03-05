using UnityEngine;

[CreateAssetMenu(fileName = "WorldPersistence", menuName = "Game/World Persistence Manager")]
public class WorldPersistenceManager : ScriptableObject
{
    [Header("Current World")]
    public string currentWorldGuid;
    public string currentWorldName;
    public SeedData currentSeedData;
    public int level = 1;
    
    [Header("World State")]
    public bool isNewWorld;
    public bool shouldLoadWorld;
    
    [Header("Player Start")]
    public Vector3 playerStartPosition = new Vector3(0, 10, 0);
    public Quaternion playerStartRotation = Quaternion.identity;
    
    // Set when creating new world
    public void PrepareNewWorld(string worldName, SeedData seedData, string worldGuid, int level = 1)
    {
        currentWorldGuid = worldGuid;
        currentWorldName = worldName;
        currentSeedData = seedData;
        this.level = level;
        isNewWorld = true;
        shouldLoadWorld = false;
        
        /*Debug.Log($"Prepared new world: {worldName}");
        Debug.Log($"Seed: {seedData}");*/
    }
    
    // Set when loading existing world
    public void PrepareLoadWorld(WorldSaveData saveData)
    {
        currentWorldGuid = saveData.worldGuid;
        currentWorldName = saveData.worldName;
        currentSeedData = saveData.seedData;
        this.level = saveData.worldState != null ? saveData.worldState.level : 1;
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
        
        //Debug.Log($"Prepared to load world: {saveData.worldName}");
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
