using NUnit.Framework.Internal;
using System.Collections;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Renderer textureRender;

    public void DrawNoiseMap(float[,] noiseMap, bool isZeroOneRange)
    {
        if (textureRender == null) return;

        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = noiseMap[x, y];
                if (isZeroOneRange == false)
                {
                    value = (value + 1f) / 2f; // remap [-1,1] → [0,1]
                }
                    colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, value);
            }
        }
        texture.SetPixels(colourMap);
        texture.Apply();

        // Only create a new material if it doesn't exist yet
        if (textureRender.material == null || textureRender.material.name == "Default-Material")
        {
            textureRender.material = new Material(Shader.Find("Standard"));
        }

        //only use instace of material here, making it unsharable
        textureRender.material.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(width, 1, height);
    }

    public void DrawMesh(MeshData meshData)
    {
        if (meshFilter == null) return;

        // Create the mesh and store it in a variable
        Mesh mesh = meshData.CreateMesh();

        // Assign to MeshFilter
        meshFilter.mesh = mesh;

        // Try to get a MeshCollider on the same object
        MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();

        // If none exists, automatically add one
        if (meshCollider == null)
            meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();

        // Force Unity to refresh the collider
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

}