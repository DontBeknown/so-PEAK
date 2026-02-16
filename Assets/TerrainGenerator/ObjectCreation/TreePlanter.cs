using System.Collections.Generic;
using UnityEngine;

public static class TreePlanter
{
    public static Dictionary<Vector2Int, List<TreeInstance>> GenerateTreeData(
        float[,] noiseMap,
        float[,] depthMap,
        float[,] roadMask,
        float minTargetHeight,
        float maxTargetHeight,
        float terrainHeightMultiplier,
        int chunkSize,
        float minSpacing,
        // --- NEW HITBOX SETTINGS ---
        float basePrefabHeight = 4.56f,
        float treeBaseRadius = 0.257f,  // How "wide" the tree roots are
        float maxSlopeDiff = 0.25f     // Max allowed height difference across the base
    )
    {
        Dictionary<Vector2Int, List<TreeInstance>> chunkedTrees = new Dictionary<Vector2Int, List<TreeInstance>>();

        int width = noiseMap.GetLength(0);
        int length = noiseMap.GetLength(1);

        int step = Mathf.Max(1, Mathf.CeilToInt(minSpacing));

        // --- TRACKERS ---
        int totalChecks = 0;
        int skippedRoad = 0;
        int skippedNoise = 0;
        int skippedHeight = 0;
        int skippedSteep = 0; // NEW TRACKER
        int treesPlanted = 0;

        float highestTreePlanted = 0f;

        for (int z = 0; z < length; z += step)
        {
            for (int x = 0; x < width; x += step)
            {
                totalChecks++;

                // A. Bounds
                if (x >= width || z >= length) continue;

                // B. Road Mask
                if (roadMask[x, z] < 0.25f)
                {
                    skippedRoad++;
                    continue;
                }

                // C. Noise Density
                if (noiseMap[x, z] > 1.0f)
                {
                    skippedNoise++;
                    continue;
                }

                // D. Base Height / Underwater Check
                float baseH = depthMap[x, z];
                if (baseH <= 0.01f)
                {
                    skippedHeight++;
                    continue;
                }

                // --- 1. CALCULATE JITTER FIRST ---
                float jitterX = Random.Range(-minSpacing * 0.4f, minSpacing * 0.4f);
                float jitterZ = Random.Range(-minSpacing * 0.4f, minSpacing * 0.4f);
                float posX = x + jitterX;
                float posZ = z + jitterZ;

                // --- 2. CALCULATE SCALE EARLY ---
                // We moved this UP from the bottom so we can use it for the physics check!
                float chosenHeight = Random.Range(minTargetHeight, maxTargetHeight);
                float scaleY = chosenHeight / basePrefabHeight;

                // Randomize width slightly so trees aren't clones
                float scaleXZ = scaleY * Random.Range(0.85f, 1.15f);

                // --- 3. CALCULATE PHYSICAL PROPERTIES ---
                // A bigger tree has wider roots
                float scaledRadius = treeBaseRadius * scaleXZ;

                // A bigger tree has deeper roots, so it can tolerate a steeper slope!
                // This is the Magic Line:
                float scaledTolerance = maxSlopeDiff * scaleY;

                // --- 4. CHECK STABILITY (Using Scaled Values) ---
                // We pass 'scaledRadius' and 'scaledTolerance' instead of the raw values
                if (!IsGroundFlatEnough(posX, posZ, scaledRadius, scaledTolerance, depthMap, terrainHeightMultiplier))
                {
                    skippedSteep++;
                    continue; // Skip this tree, it would float or clip!
                }

                // --- 5. PLANT THE TREE ---
                // If we survive the check, we calculate the final Y position
                float exactHeight = GetHeightAt(posX, posZ, depthMap);
                float posY = exactHeight * terrainHeightMultiplier;
                Vector3 finalPos = new Vector3(posX, posY, posZ);

                // Update Highest Actual Tree
                if (finalPos.y > highestTreePlanted) highestTreePlanted = finalPos.y;

                Quaternion rot = Quaternion.Euler(180, Random.Range(0, 360), 0);
                Vector3 finalScale = new Vector3(scaleXZ, scaleY, scaleXZ);

                // Add to correct Chunk
                Vector2Int chunkCoord = new Vector2Int(
                    Mathf.FloorToInt(posX / chunkSize),
                    Mathf.FloorToInt(posZ / chunkSize)
                );

                if (!chunkedTrees.ContainsKey(chunkCoord))
                {
                    chunkedTrees.Add(chunkCoord, new List<TreeInstance>());
                }

                chunkedTrees[chunkCoord].Add(new TreeInstance
                {
                    position = finalPos,
                    rotation = rot,
                    scale = finalScale
                });

                treesPlanted++;
            }
        }

        // --- PRINT THE REPORTS ---
        Debug.Log($"<color=cyan><b>Tree Planter Report:</b></color>\n" +
                  $"Total Points Checked: {totalChecks}\n" +
                  $"Skipped (Road): {skippedRoad}\n" +
                  $"Skipped (Noise): {skippedNoise}\n" +
                  $"Skipped (Water): {skippedHeight}\n" +
                  $"Skipped (Too Steep): {skippedSteep}\n" + // LOGGED HERE
                  $"<b>Final Trees Planted: {treesPlanted}</b>");

        return chunkedTrees;
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
}