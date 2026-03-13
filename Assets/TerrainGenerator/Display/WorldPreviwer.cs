using System.Collections.Generic;
using UnityEngine;

public class WorldPreviewer : MonoBehaviour
{
    [Header("Links")]
    public WorldDataManager dataManager;

    [Header("Preview Settings")]
    public bool showObjectPreview = true;
    public bool showTerrainPreview = true;

    [Header("Terrain Low-Res Settings")]
    [Range(2, 20)] public int previewStep = 5; // Higher = Lower Resolution
    public Color terrainPreviewColor = new Color(1, 1, 1, 0.3f); // Semi-transparent

    [Header("Object Colors")]
    public Color treeColor = Color.green;
    public Color campfireColor = Color.red;
    public Color defaultColor = Color.white;

    private void OnDrawGizmos()
    {
        if (dataManager == null) return;

        // 1. LOW-RES TERRAIN PREVIEW
        if (showTerrainPreview && dataManager.globalHeightMap != null)
        {
            DrawLowResTerrain();
        }

        // 2. OBJECT PREVIEW (Your existing logic)
        if (showObjectPreview)
        {
            DrawObjectGizmos();
        }
    }

    private void DrawLowResTerrain()
    {
        Gizmos.color = terrainPreviewColor;
        float[,] map = dataManager.globalHeightMap;
        float heightMult = dataManager.activeGen.meshHeightMultiplier;

        int width = map.GetLength(0);
        int length = map.GetLength(1);

        // We loop through the heightmap in "Steps" to keep it low-res and fast
        for (int y = 0; y < length - previewStep; y += previewStep)
        {
            for (int x = 0; x < width - previewStep; x += previewStep)
            {
                // Define 4 corners of a "Low Res" tile
                Vector3 p1 = new Vector3(x, map[x, y] * heightMult, y);
                Vector3 p2 = new Vector3(x + previewStep, map[x + previewStep, y] * heightMult, y);
                Vector3 p3 = new Vector3(x, map[x, y + previewStep] * heightMult, y + previewStep);
                Vector3 p4 = new Vector3(x + previewStep, map[x + previewStep, y + previewStep] * heightMult, y + previewStep);

                // Draw a wireframe or simple lines to see the terrain shape
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p1, p3);
            }
        }
    }

    private void DrawObjectGizmos()
    {
        var grid = dataManager.GetMasterGrid();
        if (grid == null) return;

        foreach (var chunk in grid)
        {
            foreach (PlacedObject obj in chunk.Value)
            {
                if (obj.Prefab != null)
                {
                    if (obj.Prefab.name.Contains("Tree")) Gizmos.color = treeColor;
                    else if (obj.Prefab.name.Contains("Camp")) Gizmos.color = campfireColor;
                    else Gizmos.color = defaultColor;
                }
                Gizmos.DrawSphere(obj.Position, obj.BoundingRadius);
            }
        }
    }
}