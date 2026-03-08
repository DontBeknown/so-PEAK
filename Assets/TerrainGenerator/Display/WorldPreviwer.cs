using System.Collections.Generic;
using UnityEngine;

public class WorldPreviewer : MonoBehaviour
{
    [Header("Link")]
    public WorldDataManager dataManager;

    [Header("Preview Settings")]
    public bool showPreview = true;
    public Color treeColor = Color.green;
    public Color campfireColor = Color.red;
    public Color defaultColor = Color.white;

    // Unity automatically calls this in the Editor to draw debug shapes!
    private void OnDrawGizmos()
    {
        if (!showPreview || dataManager == null) return;

        // Grab the grid data
        var grid = dataManager.GetMasterGrid();
        if (grid == null) return;

        // Loop through every single chunk in the entire world
        foreach (var chunk in grid)
        {
            // Loop through every object in that chunk
            foreach (PlacedObject obj in chunk.Value)
            {
                // 1. Pick a color based on the object's name
                if (obj.Prefab != null)
                {
                    if (obj.Prefab.name.Contains("Tree")) Gizmos.color = treeColor;
                    else if (obj.Prefab.name.Contains("Camp")) Gizmos.color = campfireColor;
                    else Gizmos.color = defaultColor;
                }

                // 2. Draw a lightweight "Hologram" sphere at the exact position and size!
                Gizmos.DrawSphere(obj.Position, obj.BoundingRadius);
            }
        }
    }
}