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
        float minSpacing)
    {
        Dictionary<Vector2Int, List<TreeInstance>> chunkedTrees = new Dictionary<Vector2Int, List<TreeInstance>>();

        int width = noiseMap.GetLength(0);
        int length = noiseMap.GetLength(1);

        // TIP: To reduce clustering, pass a higher 'minSpacing' value into this function!
        int step = Mathf.Max(1, Mathf.CeilToInt(minSpacing));

        // --- TRACKERS ---
        int totalChecks = 0;
        int skippedRoad = 0;
        int skippedNoise = 0;
        int skippedHeight = 0;
        int treesPlanted = 0;

        float highestTreePlanted = 0f;
        float maxSpawnableRawDepth = 0f; // Tracks the highest point a tree is ACTUALLY allowed to spawn

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

                // D. Base Height/Water Check (Using integer grid to decide if it's valid)
                float baseH = depthMap[x, z];
                if (baseH <= 0.01f)
                {
                    skippedHeight++;
                    continue;
                }

                // --- APPLY JITTER ---
                // Move the tree slightly off the perfect grid
                float jitterX = Random.Range(-minSpacing * 0.4f, minSpacing * 0.4f);
                float jitterZ = Random.Range(-minSpacing * 0.4f, minSpacing * 0.4f);
                float posX = x + jitterX;
                float posZ = z + jitterZ;

                // --- GET PRECISE SLOPED HEIGHT ---
                // Calculate the exact height on the slope at this new floating-point coordinate
                float exactHeight = GetHeightAt(posX, posZ, depthMap);
                float posY = exactHeight * terrainHeightMultiplier;

                // Update max height for VALID spawn points only
                if (exactHeight > maxSpawnableRawDepth)
                {
                    maxSpawnableRawDepth = exactHeight;
                }

                Vector3 finalPos = new Vector3(posX, posY, posZ);

                // Update Highest Actual Tree
                if (finalPos.y > highestTreePlanted)
                {
                    highestTreePlanted = finalPos.y;
                }

                // ... [Rotation & Scale Logic] ...
                // ... [Rotation Logic] ...
                Quaternion rot = Quaternion.Euler(180, Random.Range(0, 360), 0);

                // --- NEW EXACT UNIT SCALING LOGIC ---
                // 1. Define the physical height of your original Tree Prefab (Check this in Unity/Blender)
                float basePrefabHeight = 5.0f;



                // 3. Roll the random target height and convert it to a Scale Multiplier
                float chosenHeight = Random.Range(minTargetHeight, maxTargetHeight);
                float scaleY = chosenHeight / basePrefabHeight;

                // 4. Make the width scale proportionally with the height (with a little random variance for fat/thin trees)
                float scaleXZ = scaleY * Random.Range(0.85f, 1.15f);

                Vector3 scale = new Vector3(scaleXZ, scaleY, scaleXZ);
                // ------------------------------------

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
                    scale = scale
                });

                treesPlanted++;
            }
        }

        // --- PRINT THE REPORTS ---
        Debug.Log($"<color=cyan><b>Tree Planter Report:</b></color>\n" +
                  $"Total Points Checked: {totalChecks}\n" +
                  $"Skipped (Road/Black Mask): {skippedRoad}\n" +
                  $"Skipped (Noise < 0.5): {skippedNoise}\n" +
                  $"Skipped (Underwater): {skippedHeight}\n" +
                  $"<b>Final Trees Planted: {treesPlanted}</b>");

        return chunkedTrees;
    }

    // NEW: Bilinear Interpolation. This ensures jittered trees sit perfectly flush on the sloped terrain.
    private static float GetHeightAt(float x, float z, float[,] map)
    {
        // Get the 4 integer corners around the tree
        int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, map.GetLength(0) - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(x), 0, map.GetLength(0) - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(z), 0, map.GetLength(1) - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt(z), 0, map.GetLength(1) - 1);

        // Find how far between the points we are (0.0 to 1.0)
        float tx = x - x0;
        float tz = z - z0;

        // Get the height of the 4 corners
        float h00 = map[x0, z0];
        float h10 = map[x1, z0];
        float h01 = map[x0, z1];
        float h11 = map[x1, z1];

        // Interpolate across X, then interpolate across Z
        float h0 = Mathf.Lerp(h00, h10, tx);
        float h1 = Mathf.Lerp(h01, h11, tx);

        return Mathf.Lerp(h0, h1, tz);
    }
}