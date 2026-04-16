using NUnit.Framework.Internal;
using System.Collections;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Renderer textureRender;

    [Header("Shader Setup")]
    public Material baseTerrainMaterial; // Drag your Shader Graph material here

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

    public void DrawMesh(MeshData meshData, NoiseTranslator generatorSettings)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        Material editorMaterial = new Material(baseTerrainMaterial);

        // Your existing color setup
        editorMaterial.SetColor("_Field_Color", generatorSettings.fieldColor);
        editorMaterial.SetColor("_Side_Rock_Color", generatorSettings.sideRockColor);
        editorMaterial.SetColor("_Road_Color", generatorSettings.roadColor);

        // --- ADD THIS LINE ---
        // This passes a pure black image to the shader, meaning "There are 0 roads here."
        editorMaterial.SetTexture("_Road_Mask", Texture2D.blackTexture);

        meshRenderer.sharedMaterial = editorMaterial;
    }

}