using System.Collections.Generic;
using UnityEngine;

public static class UniversalSpawner
{
    public static void GenerateObjectData(
        SpawnConfig config,
        float[,] depthMap,
        float[,] roadMask,
        float[,] noiseMap, // Can be null if config.UseNoiseMap is false
        float terrainHeightMultiplier,
        int chunkSize,
        int seed,
        string worldGuid,
        int levelId,
        int configIndex,
        ref Dictionary<Vector2Int, List<PlacedObject>> spatialGrid)
    {
        // Set the seed so we get the exact same random results every time
        Random.InitState(seed);

        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);
        int step = Mathf.Max(1, Mathf.CeilToInt(config.MinSpacing));

        for (int z = 0; z < length; z += step)
        {
            for (int x = 0; x < width; x += step)
            {
                // A. Bounds Check
                if (x >= width || z >= length) continue;

                // ---------------------------------------------------------
                // --- NEW: STRICT PROBABILITY CALCULATOR ---
                float finalSpawnChance = config.Density;

                // If this object REQUIRES a noise map...
                if (config.RequiredNoiseMap != NoiseType.None)
                {
                    // TRAP 1 FIX: Scream if the map is missing!
                    if (noiseMap == null)
                    {
                        Debug.LogError($"[UniversalSpawner] ERROR: {config.Prefab.name} requires {config.RequiredNoiseMap}, but it received a NULL map! Check your WorldDataManager setup.");
                        continue; // Skip spawning completely so it doesn't default to random garbage
                    }

                    // TRAP 2 REMINDER: If 'noiseMap' has a buffer, you must offset X and Z!
                    float noiseValue = 1f - noiseMap[x, z];// (Assuming your arrays are perfectly matched for now)

                    if (noiseValue < config.NoiseThreshold) continue;

                    // Multiply base density by the noise value for smooth transitions
                    finalSpawnChance *= noiseValue;
                }

                // B. The SINGLE Random Density Check
                if (Random.value > finalSpawnChance) continue;
                // ---------------------------------------------------------

                // C. Road Mask Check
                if (roadMask != null)
                {
                    // Check if this specific X/Z coordinate is a road
                    bool isOnRoad = roadMask[x, z] < 0.25f;

                    // Apply the rules from the Inspector!
                    if (config.RoadRule == RoadSpawnRule.AvoidRoads && isOnRoad) continue;
                    if (config.RoadRule == RoadSpawnRule.OnlyOnRoads && !isOnRoad) continue;
                    // If the rule is 'AllowEverywhere', it just ignores this check entirely!
                }

                // E. Underwater Check
                float baseH = depthMap[x, z];
                if (baseH <= 0.01f) continue;

                // --- 1. CALCULATE POSITION & SCALE ---
                float jitterX = Random.Range(-config.MinSpacing * 0.4f, config.MinSpacing * 0.4f);
                float jitterZ = Random.Range(-config.MinSpacing * 0.4f, config.MinSpacing * 0.4f);
                float posX = x + jitterX;
                float posZ = z + jitterZ;

                float scaleMult = Random.Range(config.MinScale, config.MaxScale);

                // Allow slightly varying width vs height
                float scaleX = scaleMult * Random.Range(0.9f, 1.1f);
                float scaleY = scaleMult;
                float scaleZ = scaleMult * Random.Range(0.9f, 1.1f);

                Vector3 finalScale = new Vector3(scaleX, scaleY, scaleZ);

                // Calculate real physical size for this specific object
                float scaledRadius = config.BaseRadius * Mathf.Max(scaleX, scaleZ);
                float scaledTolerance = config.MaxSlopeDiff * scaleY;

                // --- 2. CHECK SPATIAL OVERLAPS FIRST (Fastest) ---
                // We don't want to do heavy slope math if a house is already sitting here
                float exactHeight = GetHeightAt(posX, posZ, depthMap) * terrainHeightMultiplier;
                Vector3 testPos = new Vector3(posX, exactHeight, posZ);

                Vector2Int chunkCoord = new Vector2Int(
                    Mathf.FloorToInt(posX / chunkSize),
                    Mathf.FloorToInt(posZ / chunkSize)
                );

                if (!IsPositionClear(testPos, scaledRadius, chunkCoord, spatialGrid))
                {
                    continue; // Something is already here!
                }

                // --- 3. CHECK GROUND SLOPE (Slowest) ---
                if (!IsGroundFlatEnough(posX, posZ, scaledRadius, scaledTolerance, depthMap, terrainHeightMultiplier))
                {
                    continue; // Ground is too bumpy/steep
                }

                // --- 4. SUCCESS! SAVE TO GRID ---
                Quaternion rot = Quaternion.Euler(
                config.BaseRotation.x,
                config.BaseRotation.y + Random.Range(0, 360),
                config.BaseRotation.z
                );

                Vector3 scaledOffset = new Vector3(
                config.PositionOffset.x * finalScale.x,
                config.PositionOffset.y * finalScale.y,
                config.PositionOffset.z * finalScale.z
                );

                Vector3 finalPlacedPos = testPos + (rot * scaledOffset);
                string spawnId = BuildSpawnId(worldGuid, levelId, configIndex, config, x, z, finalPlacedPos.x, finalPlacedPos.z);

                PlacedObject newObj = new PlacedObject
                {
                    Prefab = config.Prefab,
                    Position = finalPlacedPos, // Use the new offset position!
                    Rotation = rot,
                    Scale = finalScale,
                    BoundingRadius = scaledRadius,
                    SpawnId = spawnId,
                    IsTerrainTree = config.IsTerrainTree,
                    TreePrototypeIndex = config.TreePrototypeIndex
                };

                // Add to the grid dictionary
                if (!spatialGrid.ContainsKey(chunkCoord))
                {
                    spatialGrid.Add(chunkCoord, new List<PlacedObject>());
                }
                spatialGrid[chunkCoord].Add(newObj);
            }
        }
    }

    // --- OVERLAP CHECKER ---
    private static bool IsPositionClear(Vector3 pos, float radius, Vector2Int chunkCoord, Dictionary<Vector2Int, List<PlacedObject>> grid)
    {
        // Check this chunk and the 8 surrounding chunks just in case it's on a border
        for (int cx = -1; cx <= 1; cx++)
        {
            for (int cz = -1; cz <= 1; cz++)
            {
                Vector2Int neighborChunk = new Vector2Int(chunkCoord.x + cx, chunkCoord.y + cz);

                if (grid.TryGetValue(neighborChunk, out List<PlacedObject> objectsInChunk))
                {
                    foreach (PlacedObject existing in objectsInChunk)
                    {
                        // 2D distance check (ignoring Y height) is usually safer for top-down spawning
                        float dx = pos.x - existing.Position.x;
                        float dz = pos.z - existing.Position.z;
                        float distSq = (dx * dx) + (dz * dz);

                        float combinedRadius = radius + existing.BoundingRadius;

                        // Compare squared distances to save CPU (Mathf.Sqrt is expensive)
                        if (distSq < (combinedRadius * combinedRadius))
                        {
                            return false; // OVERLAP!
                        }
                    }
                }
            }
        }
        return true; // Clear!
    }

    // --- NEW HELPER FUNCTION ---
    private static bool IsGroundFlatEnough(float centerX, float centerZ, float radius, float maxDiff, float[,] map, float heightMult)
    {
        int w = map.GetLength(0);
        int l = map.GetLength(1);

        // 1. Define the 4 cardinal points around the tree base (North, South, East, West)
        // We use Clamp to make sure we don't crash by checking outside the map array
        float northZ = Mathf.Clamp(centerZ + radius, 0, l - 1.1f);
        float southZ = Mathf.Clamp(centerZ - radius, 0, l - 1.1f);
        float eastX = Mathf.Clamp(centerX + radius, 0, w - 1.1f);
        float westX = Mathf.Clamp(centerX - radius, 0, w - 1.1f);

        // 2. Get the RAW Height (0.0 - 1.0) at these points
        float hCenter = GetHeightAt(centerX, centerZ, map);
        float hNorth = GetHeightAt(centerX, northZ, map);
        float hSouth = GetHeightAt(centerX, southZ, map);
        float hEast = GetHeightAt(eastX, centerZ, map);
        float hWest = GetHeightAt(westX, centerZ, map);

        // 3. Convert to WORLD Height (Multiply by your terrain amplitude, e.g., 50)
        float worldH_Center = hCenter * heightMult;
        float worldH_North = hNorth * heightMult;
        float worldH_South = hSouth * heightMult;
        float worldH_East = hEast * heightMult;
        float worldH_West = hWest * heightMult;

        // 4. Find the Highest and Lowest point under the tree
        float minH = Mathf.Min(worldH_Center, Mathf.Min(worldH_North, Mathf.Min(worldH_South, Mathf.Min(worldH_East, worldH_West))));
        float maxH = Mathf.Max(worldH_Center, Mathf.Max(worldH_North, Mathf.Max(worldH_South, Mathf.Max(worldH_East, worldH_West))));

        // 5. Check the difference
        // If the ground fluctuates more than 'maxDiff' units under the tree, reject it.
        return (maxH - minH) <= maxDiff;
    }

    private static float GetHeightAt(float x, float z, float[,] map)
    {
        // ... (Your existing Bilinear Interpolation code stays exactly the same) ...
        int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, map.GetLength(0) - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(x), 0, map.GetLength(0) - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(z), 0, map.GetLength(1) - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt(z), 0, map.GetLength(1) - 1);

        float tx = x - x0;
        float tz = z - z0;

        float h00 = map[x0, z0];
        float h10 = map[x1, z0];
        float h01 = map[x0, z1];
        float h11 = map[x1, z1];

        float h0 = Mathf.Lerp(h00, h10, tx);
        float h1 = Mathf.Lerp(h01, h11, tx);

        return Mathf.Lerp(h0, h1, tz);
    }

    private static string BuildSpawnId(
        string worldGuid,
        int levelId,
        int configIndex,
        SpawnConfig config,
        int sampleX,
        int sampleZ,
        float finalX,
        float finalZ)
    {
        string safeWorldGuid = string.IsNullOrEmpty(worldGuid) ? "unknown-world" : worldGuid;
        string prefabName = config.Prefab != null ? config.Prefab.name : "missing-prefab";
        int quantizedX = Mathf.RoundToInt(finalX * 100f);
        int quantizedZ = Mathf.RoundToInt(finalZ * 100f);

        return $"{safeWorldGuid}|L{levelId}|C{configIndex}|P:{prefabName}|SX:{sampleX}|SZ:{sampleZ}|QX:{quantizedX}|QZ:{quantizedZ}";
    }
}