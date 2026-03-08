using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseSource
{
    public NoiseType RequiredNoiseType; // e.g., TreeNoise
    public MapGenerator Generator;      // The GameObject doing the math
}

public class WorldDataManager : MonoBehaviour
{
    [Header("Noise Generators")]
    public NoiseTranslator worldGen;

    // This replaces your old individual MapGenerators!
    public List<NoiseSource> resourceNoiseGenerators = new List<NoiseSource>();

    [Header("Unique Landmarks")]
    public SpawnConfig lighthouseConfig;


    [Header("Universal Spawning")]
    public List<SpawnConfig> worldSpawnConfigs = new List<SpawnConfig>();

    [Header("Global Data Outputs")]
    public float[,] globalHeightMap;
    public Color[,] globalColorMap;
    public Color fieldColor;

    // Seeds
    [HideInInspector] public int seed1, seed2, seed3;

    // The Master Memory Grid
    public Dictionary<Vector2Int, List<PlacedObject>> masterSpawnGrid;

    // RenderController will call this function to kick everything off
    public void GenerateWorldData(int chunkSize)
    {
        Debug.Log("[WorldDataManager] Generating World Data...");

        LoadSeed();

        if (worldGen != null)
        {
            // 1. Generate Base Terrain Math
            worldGen.TerrainDrawing(seed1);
            globalHeightMap = worldGen.completeMap;
            globalColorMap = worldGen.colorMap;

            // Base Road Mask
            float[,] expandedRoadMask = new float[worldGen.mapWidth + worldGen.bufferLength, worldGen.mapLength + worldGen.bufferLength];
            BufferGen.GenRoadMaskWithBuffer(worldGen.roadRidge, expandedRoadMask, worldGen.bufferLength);

            // --- NEW: GENERATE ALL NOISE MAPS FROM THE LIST ---
            Dictionary<NoiseType, float[,]> availableNoiseMaps = new Dictionary<NoiseType, float[,]>();

            // 1. Create a counter to keep track of which map we are generating
            int noiseIndex = 0;

            foreach (NoiseSource source in resourceNoiseGenerators)
            {
                if (source.Generator != null)
                {
        
                    // (Optional but recommended) Force the noise map size to match the world size!
                    source.Generator.mapWidth = worldGen.mapWidth;
                    source.Generator.mapHeight = worldGen.mapLength;

                    // 2. Use seed2, and add the index so every single map is unique!
                    source.Generator.GenerateMap(seed1 + 10 + noiseIndex);

                    float[,] expandedMap = new float[worldGen.mapWidth + worldGen.bufferLength, worldGen.mapLength + worldGen.bufferLength];
                    BufferGen.GenMapWithBuffer(source.Generator.noiseMap, expandedMap, worldGen.bufferLength);
                    availableNoiseMaps.Add(source.RequiredNoiseType, expandedMap);

                    // 3. Increase the counter so the next map gets a different seed!
                    noiseIndex++;
                }
            }

            // --- INITIALIZE THE MASTER GRID ---
            masterSpawnGrid = new Dictionary<Vector2Int, List<PlacedObject>>();

            // --- NEW: Priority Placement ---
            SpawnUniqueLighthouse(chunkSize - 1);

            // --- THE NEW, ULTRA-CLEAN CARD DEALER LOOP ---
            for (int i = 0; i < worldSpawnConfigs.Count; i++)
            {
                SpawnConfig config = worldSpawnConfigs[i];
                float[,] mapToHandOver = null;

                // If the config asks for a map, and we generated it in the step above, grab it!
                if (config.RequiredNoiseMap != NoiseType.None && availableNoiseMaps.ContainsKey(config.RequiredNoiseMap))
                {
                    mapToHandOver = availableNoiseMaps[config.RequiredNoiseMap];
                }

                UniversalSpawner.GenerateObjectData(
                    config,
                    globalHeightMap,
                    expandedRoadMask,
                    mapToHandOver, // Will automatically pass the correct map, or null!
                    worldGen.meshHeightMultiplier,
                    chunkSize - 1,
                    seed1 + i,
                    ref masterSpawnGrid
                );
            }

            Debug.Log("[WorldDataManager] World Data Generation Complete!");
        }
        else
        {
            Debug.LogError("[WorldDataManager] Missing NoiseTranslator script!");
        }
    }

    private void SpawnUniqueLighthouse(int chunkSize)
    {
        if (lighthouseConfig == null) return;

        // 1. Use the shifted/offset peak from NoiseTranslator
        Vector2Int peakCoord = worldGen.mainPeak;

        // 2. Sample the height from the final buffered map
        float height = globalHeightMap[peakCoord.x, peakCoord.y] * worldGen.meshHeightMultiplier;

        // 3. Apply the XYZ and Vertical Offsets from the Config
        Quaternion rotation = Quaternion.Euler(lighthouseConfig.BaseRotation);
        Vector3 scale = Vector3.one * lighthouseConfig.MaxScale;
        Vector3 scaledOffset = Vector3.Scale(lighthouseConfig.PositionOffset, scale);

        Vector3 finalPos = new Vector3(peakCoord.x, height, peakCoord.y) + (rotation * scaledOffset);

        // 4. Calculate which chunk this belongs to
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt((float)peakCoord.x / (chunkSize)),
            Mathf.FloorToInt((float)peakCoord.y / (chunkSize))
        );

        // --- DEBUG LOG BLOCK ---
        Debug.Log($"<color=cyan>[Lighthouse Debug]</color>\n" +
                  $"<b>Map Index:</b> ({peakCoord.x}, {peakCoord.y})\n" +
                  $"<b>World Pos:</b> {finalPos}\n" +
                  $"<b>Assigned Chunk:</b> {chunkCoord}\n" +
                  $"<b>Config Offset Applied:</b> {scaledOffset}");

        // 5. Manual Injection into the Master Grid
        PlacedObject lighthouse = new PlacedObject
        {
            Prefab = lighthouseConfig.Prefab,
            Position = finalPos,
            Rotation = rotation,
            Scale = scale,
            BoundingRadius = lighthouseConfig.BaseRadius * scale.x
        };

        if (!masterSpawnGrid.ContainsKey(chunkCoord))
            masterSpawnGrid.Add(chunkCoord, new List<PlacedObject>());

        masterSpawnGrid[chunkCoord].Add(lighthouse);
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