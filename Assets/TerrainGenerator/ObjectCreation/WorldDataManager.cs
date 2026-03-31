using System.Collections.Generic;
using System.Threading.Tasks;
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


    [Header("Stage Specific Spawning")]
    public List<SpawnConfig> spawnConfigs;
    public SpawnConfig lighthouseConfig;
    public List<NoiseSource> resourceNoises;
}
public enum WorldLevel
{
    Forest = 1,  // Match friend's default level 1
    Desert = 2,  // Match level 2
    Tundra = 3   // Match level 3
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


    //ridge expanded
    [HideInInspector] public float[,] expandedRoadRidge;
    [HideInInspector] public Texture2D roadRidgeTexture;
    [HideInInspector] public Color fieldColor, roadColor, sideRockColor;
    [HideInInspector] public int activeLevelSeed, seed1, seed2, seed3;
    public Dictionary<Vector2Int, List<PlacedObject>> masterSpawnGrid;

    public void GenerateWorldData(int chunkSize)
    {
        Debug.Log($"[WorldDataManager] Generating {currentLevel} Data...");

        LoadSeed();

        string worldGuid = SaveLoadService.Instance?.CurrentWorldSave?.worldGuid;
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
        //globalColorMap = activeGen.colorMap;

        expandedRoadRidge = new float[activeGen.mapWidth + activeGen.bufferLength, activeGen.mapLength + activeGen.bufferLength];
        BufferGen.GenRoadMaskWithBuffer(activeGen.roadRidge, expandedRoadRidge, activeGen.bufferLength);

        // get color and prepare ridge for render shader
        roadColor = activeGen.roadColor;
        sideRockColor = activeGen.sideRockColor;
        fieldColor = activeGen.fieldColor;
        roadRidgeTexture =  GenerateRoadMaskTexture(expandedRoadRidge);

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
            SpawnUniqueLighthouse(chunkSize - 1, profile, worldGuid);

        for (int i = 0; i < profile.spawnConfigs.Count; i++)
        {
            SpawnConfig config = profile.spawnConfigs[i];

            if (config == null) continue;

            float[,] mapToHandOver = null;

            // 2. Check if we actually have noise maps to look through
            if (availableNoiseMaps != null && config.RequiredNoiseMap != NoiseType.None)
            {
                if (availableNoiseMaps.ContainsKey(config.RequiredNoiseMap))
                {
                    mapToHandOver = availableNoiseMaps[config.RequiredNoiseMap];
                }
            }
            UniversalSpawner.GenerateObjectData(
                config, globalHeightMap, expandedRoadRidge, mapToHandOver,
                activeGen.meshHeightMultiplier, chunkSize - 1, activeLevelSeed + i,
                worldGuid, (int)currentLevel, i,
                ref masterSpawnGrid
            );
        }
        Debug.Log("[WorldDataManager] Generation Complete!");
    }

    private void SpawnUniqueLighthouse(int chunkSize, WorldLevelProfile profile, string worldGuid)
    {
        Vector2Int peakCoord = profile.generator.mainPeak;
        float height = globalHeightMap[peakCoord.x, peakCoord.y] * profile.generator.meshHeightMultiplier;

        Quaternion rotation = Quaternion.Euler(profile.lighthouseConfig.BaseRotation);
        Vector3 scale = Vector3.one * profile.lighthouseConfig.MaxScale;
        Vector3 finalPos = new Vector3(peakCoord.x, height, peakCoord.y) + (rotation * Vector3.Scale(profile.lighthouseConfig.PositionOffset, scale));

        Vector2Int chunkCoord = new Vector2Int(Mathf.FloorToInt((float)peakCoord.x / chunkSize), Mathf.FloorToInt((float)peakCoord.y / chunkSize));
        string safeWorldGuid = string.IsNullOrEmpty(worldGuid) ? "unknown-world" : worldGuid;
        string lighthouseSpawnId = $"{safeWorldGuid}|L{(int)currentLevel}|Lighthouse|X:{peakCoord.x}|Z:{peakCoord.y}";

        PlacedObject landmark = new PlacedObject
        {
            Prefab = profile.lighthouseConfig.Prefab,
            Position = finalPos,
            Rotation = rotation,
            Scale = scale,
            SpawnId = lighthouseSpawnId
        };

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

    private Texture2D GenerateRoadMaskTexture(float[,] roadRidgeArray)
    {


        int width = roadRidgeArray.GetLength(0);
        int length = roadRidgeArray.GetLength(1);

        // Create the blank texture
        Texture2D roadMask = new Texture2D(width, length);

        roadMask.filterMode = FilterMode.Bilinear; // This forces Unity to blur and smooth the pixels!
        roadMask.wrapMode = TextureWrapMode.Clamp; // Prevents the road from glitching at the edges of the map

        // Using a 1D array is required for the fast SetPixels() method
        Color[] pixels = new Color[width * length];

        // We can calculate the colors in parallel for speed
        Parallel.For(0, length, z =>
        {
            for (int x = 0; x < width; x++)
            {
                float roadValue = roadRidgeArray[x, z];

                // If it's below 0.25, paint it White (1). Otherwise, Black (0).
                float maskIntensity = Mathf.InverseLerp(0.35f, 0.20f, roadValue);

                // Map the 2D [x, z] coordinates to the 1D array index
                pixels[z * width + x] = new Color(maskIntensity, maskIntensity, maskIntensity);
            }
        });

        // Apply the pixel array to the texture (Must happen on the main thread)
        roadMask.SetPixels(pixels);
        roadMask.Apply();

        return roadMask;
    }



    private void LoadSeed()
    {
        var saveService = SaveLoadService.Instance;
        if (saveService == null || saveService.CurrentWorldSave == null)
        {
            Debug.LogError("[WorldDataManager] SaveLoadService or WorldSave is missing!");
            return;
        }

        ////////////////Load level would uncomment if test completed
        int savedLevelInt = saveService.GetCurrentLevel();
        currentLevel = (WorldLevel)savedLevelInt;


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