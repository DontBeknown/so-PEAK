using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.STP;




[System.Serializable]
public struct NoiseSource
{
    public NoiseType RequiredNoiseType; // e.g., TreeNoise
    public MapGenerator Generator;      // The GameObject doing the math
}

[System.Serializable]
public class WorldLevelProfile
{
    public string levelName;
    public NoiseTranslator generator;
    public Color fieldColor;


    [Header("Stage Specific Spawning")]
    public List<SpawnConfig> spawnConfigs;
    public SpawnConfig lighthouseConfig;
    public List<NoiseSource> resourceNoises;
}

public class WorldDataManager : MonoBehaviour
{
    [Header("Level Setup")]
    public WorldLevel currentLevel = WorldLevel.Forest;
    public List<WorldLevelProfile> levelProfiles = new List<WorldLevelProfile>();

    [Header("Global Data Outputs")]
    [HideInInspector] public NoiseTranslator activeGen;
    public float[,] globalHeightMap;
    public Color[,] globalColorMap;
    public Color fieldColor;

    // REMOVED: Global resourceNoiseGenerators, lighthouseConfig, and worldSpawnConfigs
    // These now live inside the WorldLevelProfile above!

    [HideInInspector] public int activeLevelSeed, seed1, seed2, seed3;
    public enum WorldLevel { Forest, Desert, Tundra }
    public Dictionary<Vector2Int, List<PlacedObject>> masterSpawnGrid;

    public void GenerateWorldData(int chunkSize)
    {
        Debug.Log($"[WorldDataManager] Generating {currentLevel} Data...");

        // 1. Find the profile (Case-Insensitive for safety)
        WorldLevelProfile profile = levelProfiles.Find(p =>
            p.levelName.Equals(currentLevel.ToString(), System.StringComparison.OrdinalIgnoreCase));

        if (profile == null || profile.generator == null)
        {
            Debug.LogError($"[WorldDataManager] Missing Profile or Generator for {currentLevel}!");
            return;
        }

        // 2. Set ACTIVE references
        activeGen = profile.generator;
        this.fieldColor = profile.fieldColor;
        LoadSeed();

        activeLevelSeed = currentLevel switch
        {
            WorldLevel.Forest => seed1,
            WorldLevel.Desert => seed2,
            WorldLevel.Tundra => seed3,
            _ => seed1
        };

        // 3. Generate Terrain
        activeGen.TerrainDrawing(activeLevelSeed);
        globalHeightMap = activeGen.completeMap;
        globalColorMap = activeGen.colorMap;

        float[,] expandedRoadMask = new float[activeGen.mapWidth + activeGen.bufferLength, activeGen.mapLength + activeGen.bufferLength];
        BufferGen.GenRoadMaskWithBuffer(activeGen.roadRidge, expandedRoadMask, activeGen.bufferLength);

        // 4. Generate Resource Noise Maps (Using PROFILE specific list)
        Dictionary<NoiseType, float[,]> availableNoiseMaps = new Dictionary<NoiseType, float[,]>();
        int noiseIndex = 0;

        foreach (NoiseSource source in profile.resourceNoises)
        {
            if (source.Generator != null)
            {
                source.Generator.mapWidth = activeGen.mapWidth;
                source.Generator.mapHeight = activeGen.mapLength;
                source.Generator.GenerateMap(activeLevelSeed + 10 + noiseIndex);

                float[,] expandedMap = new float[activeGen.mapWidth + activeGen.bufferLength, activeGen.mapLength + activeGen.bufferLength];
                BufferGen.GenMapWithBuffer(source.Generator.noiseMap, expandedMap, activeGen.bufferLength);
                availableNoiseMaps.Add(source.RequiredNoiseType, expandedMap);
                noiseIndex++;
            }
        }

        // 5. Spawn Objects
        masterSpawnGrid = new Dictionary<Vector2Int, List<PlacedObject>>();

        if (profile.lighthouseConfig != null)
            SpawnUniqueLighthouse(chunkSize - 1, profile);

        for (int i = 0; i < profile.spawnConfigs.Count; i++)
        {
            SpawnConfig config = profile.spawnConfigs[i];
            float[,] mapToHandOver = (config.RequiredNoiseMap != NoiseType.None && availableNoiseMaps.ContainsKey(config.RequiredNoiseMap))
                ? availableNoiseMaps[config.RequiredNoiseMap] : null;

            UniversalSpawner.GenerateObjectData(
                config, globalHeightMap, expandedRoadMask, mapToHandOver,
                activeGen.meshHeightMultiplier, chunkSize - 1, activeLevelSeed + i, ref masterSpawnGrid
            );
        }
        Debug.Log("[WorldDataManager] Generation Complete!");
    }

    private void SpawnUniqueLighthouse(int chunkSize, WorldLevelProfile profile)
    {
        Vector2Int peakCoord = profile.generator.mainPeak;
        float height = globalHeightMap[peakCoord.x, peakCoord.y] * profile.generator.meshHeightMultiplier;

        Quaternion rotation = Quaternion.Euler(profile.lighthouseConfig.BaseRotation);
        Vector3 scale = Vector3.one * profile.lighthouseConfig.MaxScale;
        Vector3 finalPos = new Vector3(peakCoord.x, height, peakCoord.y) + (rotation * Vector3.Scale(profile.lighthouseConfig.PositionOffset, scale));

        Vector2Int chunkCoord = new Vector2Int(Mathf.FloorToInt((float)peakCoord.x / chunkSize), Mathf.FloorToInt((float)peakCoord.y / chunkSize));

        PlacedObject landmark = new PlacedObject { Prefab = profile.lighthouseConfig.Prefab, Position = finalPos, Rotation = rotation, Scale = scale };

        if (!masterSpawnGrid.ContainsKey(chunkCoord)) masterSpawnGrid.Add(chunkCoord, new List<PlacedObject>());
        masterSpawnGrid[chunkCoord].Add(landmark);
    }

    // RenderController calls this to ask "What should I spawn in this chunk?"
    public List<PlacedObject> GetObjectsForChunk(Vector2Int coord)
    {
        if (masterSpawnGrid != null && masterSpawnGrid.TryGetValue(coord, out List<PlacedObject> objects))
        {
            return objects;
        }
        return null;
    }


    private void LoadSeed()
    {
        var saveService = SaveLoadService.Instance;
        if (saveService == null || saveService.CurrentWorldSave == null)
        {
            Debug.LogError("[WorldDataManager] SaveLoadService or WorldSave is missing!");
            return;
        }

        SeedData seedData = saveService.CurrentWorldSave.seedData;
        seed1 = GetDeterministicHashCode(seedData.seed1);
        seed2 = GetDeterministicHashCode(seedData.seed2);
        seed3 = GetDeterministicHashCode(seedData.seed3);
    }

    private int GetDeterministicHashCode(string str)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;
            for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1 || str[i + 1] == '\0') break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }
            return hash1 + (hash2 * 1566083941);
        }
    }

    public Dictionary<Vector2Int, List<PlacedObject>> GetMasterGrid()
    {
        return masterSpawnGrid;
    }
}