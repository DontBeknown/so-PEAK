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
    [Range(2, 20)] public int previewStep = 5;
    public Color terrainPreviewColor = new Color(1, 1, 1, 0.3f);

    [Header("Object Colors")]
    public Color treeColor = Color.green;
    public Color campfireColor = Color.red;
    public Color defaultColor = Color.white;

    private Mesh previewMesh;

    // Triggered whenever you change a value in the Inspector
    private void OnValidate()
    {
        GeneratePreviewMesh();
    }

    public void GeneratePreviewMesh()
    {
        if (dataManager == null || dataManager.globalHeightMap == null) return;

        float[,] map = dataManager.globalHeightMap;
        float heightMult = dataManager.activeGen.meshHeightMultiplier;
        int width = map.GetLength(0);
        int length = map.GetLength(1);

        // Calculate grid dimensions based on the step
        int xSize = (width - 1) / previewStep;
        int zSize = (length - 1) / previewStep;

        Vector3[] vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        int[] triangles = new int[xSize * zSize * 6];

        // 1. Generate Vertices
        int i = 0;
        for (int z = 0; z <= length - 1; z += previewStep)
        {
            for (int x = 0; x <= width - 1; x += previewStep)
            {
                if (i >= vertices.Length) break;
                vertices[i] = new Vector3(x, map[x, z] * heightMult, z);
                i++;
            }
        }

        // 2. Generate Triangles (Stitching the grid)
        int vert = 0;
        int tris = 0;
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        // 3. Update the Mesh Object
        if (previewMesh == null)
        {
            previewMesh = new Mesh();
            previewMesh.name = "TerrainPreview_Internal";
        }

        previewMesh.Clear();
        previewMesh.vertices = vertices;
        previewMesh.triangles = triangles;
        previewMesh.RecalculateNormals();
    }

    private void OnDrawGizmos()
    {
        if (dataManager == null) return;

        // Draw the Solid 3D Preview
        if (showTerrainPreview && previewMesh != null)
        {
            Gizmos.color = terrainPreviewColor;
            Gizmos.DrawMesh(previewMesh);
        }

        // Draw the Objects
        if (showObjectPreview)
        {
            DrawObjectGizmos();
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
                // Logic to color based on Prefab Name
                if (obj.Prefab != null)
                {
                    string pName = obj.Prefab.name.ToLower();
                    if (pName.Contains("tree")) Gizmos.color = treeColor;
                    else if (pName.Contains("camp")) Gizmos.color = campfireColor;
                    else Gizmos.color = defaultColor;
                }

                // Using Cubes for grass/small objects is faster than Spheres
                if (obj.BoundingRadius < 1f)
                    Gizmos.DrawCube(obj.Position, Vector3.one * obj.BoundingRadius);
                else
                    Gizmos.DrawSphere(obj.Position, obj.BoundingRadius);
            }
        }
    }
}