using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PreviewCategory
{
    public string categoryName = "New Category";
    public Color gizmoColor = Color.white;
    [Tooltip("Type a part of the prefab name (e.g., 'tree', 'rock', 'camp')")]
    public List<string> nameKeywords = new List<string>();
}

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

    [Header("Object Categories")]
    public Color defaultColor = Color.white;
    public List<PreviewCategory> objectCategories = new List<PreviewCategory>();

    private Mesh previewMesh;

    private void OnValidate()
    {
        GeneratePreviewMesh();
    }

    [ContextMenu("Force Generate World Data")]
    public void ForceGenerateWorld()
    {
        if (dataManager != null)
        {
            dataManager.GenerateWorldData(41);
            GeneratePreviewMesh();
            Debug.Log("[Previewer] World Data manually generated! Gizmos should now appear.");
        }
        else
        {
            Debug.LogWarning("Assign the Data Manager first!");
        }
    }

    public void GeneratePreviewMesh()
    {
        if (dataManager == null || dataManager.globalHeightMap == null) return;

        float[,] map = dataManager.globalHeightMap;
        float heightMult = dataManager.activeGen.meshHeightMultiplier;
        int width = map.GetLength(0);
        int length = map.GetLength(1);

        int xSize = (width - 1) / previewStep;
        int zSize = (length - 1) / previewStep;

        Vector3[] vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        int[] triangles = new int[xSize * zSize * 6];

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

        if (showTerrainPreview && previewMesh != null)
        {
            Gizmos.color = terrainPreviewColor;
            Gizmos.DrawMesh(previewMesh);
        }

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
                Color colorToDraw = defaultColor;

                if (obj.Prefab != null)
                {
                    string pName = obj.Prefab.name.ToLower();

                    // Look through every category you made in the Inspector
                    foreach (PreviewCategory category in objectCategories)
                    {
                        foreach (string keyword in category.nameKeywords)
                        {
                            // If the prefab name contains the word (e.g., "tree")
                            if (!string.IsNullOrEmpty(keyword) && pName.Contains(keyword.ToLower()))
                            {
                                colorToDraw = category.gizmoColor;
                                break;
                            }
                        }
                        if (colorToDraw != defaultColor) break;
                    }
                }

                Gizmos.color = colorToDraw;

                float drawSize = obj.BoundingRadius > 0.1f ? obj.BoundingRadius : 3f;

                if (drawSize < 1f)
                    Gizmos.DrawCube(obj.Position, Vector3.one * drawSize);
                else
                    Gizmos.DrawSphere(obj.Position, drawSize);
            }
        }
    }
}