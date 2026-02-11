using System.Collections.Generic;
using UnityEngine;

public class TreePreviewer : MonoBehaviour
{
    [Header("Debug / Editor Settings")]
    public bool autoPreviewTrees = true;
    public GameObject treePrefab;
    public Transform treePreviewParent;
    public int maxTreeLimit = 10000;

    // This function now takes ALL the data it needs as parameters
    public void GenerateDebugTrees(
        float[,] treeNoiseMap,
        float[,] heightMap,
        float[,] roadMask,
        float heightMultiplier,
        int chunkSize
    )
    {
        // 0. Safety Check
        if (!autoPreviewTrees || treePrefab == null || heightMap == null) return;

        // --- ADD THESE DEBUG LOGS HERE ---
        if (treeNoiseMap != null)
            Debug.Log($"[DIMENSION CHECK] treeNoiseMap: {treeNoiseMap.GetLength(0)} x {treeNoiseMap.GetLength(1)}");
        else
            Debug.Log("[DIMENSION CHECK] treeNoiseMap is NULL!");

        Debug.Log($"[DIMENSION CHECK] heightMap: {heightMap.GetLength(0)} x {heightMap.GetLength(1)}");

        if (roadMask != null)
            Debug.Log($"[DIMENSION CHECK] roadMask: {roadMask.GetLength(0)} x {roadMask.GetLength(1)}");
        else
            Debug.Log("[DIMENSION CHECK] roadMask is NULL!");
        // ---------------------------------

        // 1. Clear Old Preview Trees
        ClearDebugTrees();

        // 2. Generate Data 
        // We assume 2.0f spacing for debug view
        var allTreeData = TreePlanter.GenerateTreeData(
            treeNoiseMap,
            heightMap,
            roadMask,
            2.0f,
            12.0f,
            heightMultiplier,
            chunkSize,
            2.0f
        );

        // 3. Create Container if missing
        if (treePreviewParent == null)
        {
            GameObject p = new GameObject("PREVIEW_TREES");
            p.transform.parent = this.transform; // Keep hierarchy clean
            treePreviewParent = p.transform;
        }

        // 4. Spawn Loop
        int treeCount = 0;
        foreach (var chunk in allTreeData.Values)
        {
            foreach (var treeData in chunk)
            {
                if (treeCount >= maxTreeLimit)
                {
                    Debug.LogWarning($"Hit Tree Preview Limit ({maxTreeLimit}). Stopping.");
                    return;
                }

                GameObject t = Instantiate(treePrefab, treeData.position, treeData.rotation, treePreviewParent);
                t.transform.localScale = treeData.scale;
                treeCount++;
            }
        }

        Debug.Log($"Spawned {treeCount} debug trees.");
    }

    public void ClearDebugTrees()
    {
        if (treePreviewParent == null) return;

        // "DestroyImmediate" is required when running in Edit Mode
        while (treePreviewParent.childCount > 0)
        {
            DestroyImmediate(treePreviewParent.GetChild(0).gameObject);
        }
    }
}