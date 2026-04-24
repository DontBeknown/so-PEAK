using UnityEngine;
using System.IO;

public class MinimapGenerator
{
    public static Sprite GenerateTopographicMinimap(float[,] depthMap, float[,] roadMask, float maxHeightMeters, bool saveToDisk = false, string folderName = "SavedMaps", string fileName = "TopographicMap.png")
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        float minorInterval = 5f;
        float majorInterval = 25f;

        float minorSteps = maxHeightMeters / minorInterval;
        float majorSteps = maxHeightMeters / majorInterval;

        float[,] minorSteppedMap = new float[width, length];
        float[,] majorSteppedMap = new float[width, length];

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                minorSteppedMap[x, z] = Mathf.Floor(depthMap[x, z] * minorSteps) / minorSteps;
                majorSteppedMap[x, z] = Mathf.Floor(depthMap[x, z] * majorSteps) / majorSteps;
            }
        }

        // --- FIXED: Use RGB24 to completely remove the Alpha channel ---
        Texture2D minimapTex = new Texture2D(width, length, TextureFormat.RGB24, false);
        minimapTex.filterMode = FilterMode.Bilinear;

        Color shadingColor = new Color(0.0f, 0.0f, 0.0f, 1.0f); // Solid black for shading
        Color minorContourColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        Color majorContourColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

        float softShadingSensitivity = 1.0f;

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float smoothHeight = depthMap[x, z];

                // LAYER A: BASE HEATMAP (Now returns Alpha 1.0)
                Color pixelColor = GetElevationColor(smoothHeight);

                // LAYER B: SOFT 3D SHADING
                float softEdge = CalculateSobelEdge(depthMap, x, z);
                // Blend with 0.4 intensity but keep result opaque
                pixelColor = Color.Lerp(pixelColor, Color.black, Mathf.Clamp01(softEdge * softShadingSensitivity) * 0.4f);

                // LAYER C: TOPOGRAPHIC CONTOURS
                float majorEdge = CalculateSobelEdge(majorSteppedMap, x, z);
                float minorEdge = CalculateSobelEdge(minorSteppedMap, x, z);

                if (majorEdge > 0.01f)
                {
                    pixelColor = Color.Lerp(pixelColor, Color.black, 0.9f); // 90% dark bold
                }
                else if (minorEdge > 0.01f)
                {
                    pixelColor = Color.Lerp(pixelColor, Color.black, 0.3f); // 30% faint thin
                }

                minimapTex.SetPixel(x, z, pixelColor);
            }
        }

        minimapTex.Apply();
        if (saveToDisk) SaveTextureAsPNG(minimapTex, folderName, fileName);

        Sprite minimapSprite = Sprite.Create(
            minimapTex,
            new Rect(0.0f, 0.0f, minimapTex.width, minimapTex.height),
            new Vector2(0.5f, 0.5f),
            100.0f
        );

        return minimapSprite;
    }

    private static Color GetElevationColor(float height)
    {
        // --- FIXED: All colors now have Alpha 1.0 ---
        Color c0 = new Color(0.05f, 0.25f, 0.45f, 1.0f);
        Color c1 = new Color(0.10f, 0.80f, 0.40f, 1.0f);
        Color c2 = new Color(0.90f, 0.90f, 0.20f, 1.0f);
        Color c3 = new Color(0.95f, 0.50f, 0.10f, 1.0f);
        Color c4 = new Color(0.80f, 0.15f, 0.15f, 1.0f);

        if (height < 0.25f) return Color.Lerp(c0, c1, height / 0.25f);
        if (height < 0.50f) return Color.Lerp(c1, c2, (height - 0.25f) / 0.25f);
        if (height < 0.75f) return Color.Lerp(c2, c3, (height - 0.50f) / 0.25f);
        return Color.Lerp(c3, c4, (height - 0.75f) / 0.25f);
    }

    // (CalculateSobelEdge, GenerateRoadMinimap, and SaveTextureAsPNG remain the same 
    // but ensure RGB24 is used in GenerateRoadMinimap as well!)

    public static Sprite GenerateRoadMinimap(float[,] roadMask, bool saveToDisk = false, string folderName = "SavedMaps", string fileName = "RoadMap.png")
    {
        int width = roadMask.GetLength(0);
        int length = roadMask.GetLength(1);

        Texture2D minimapTex = new Texture2D(width, length, TextureFormat.RGB24, false);
        minimapTex.filterMode = FilterMode.Bilinear;

        Color nonRoadColor = new Color(0.35f, 0.25f, 0.15f, 1.0f);
        Color roadColor = new Color(0.80f, 0.65f, 0.45f, 1.0f);

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float roadValue = roadMask[x, z];
                Color pixelColor = Color.Lerp(roadColor, nonRoadColor, roadValue);
                minimapTex.SetPixel(x, z, pixelColor);
            }
        }

        minimapTex.Apply();
        if (saveToDisk) SaveTextureAsPNG(minimapTex, folderName, fileName);

        return Sprite.Create(minimapTex, new Rect(0.0f, 0.0f, width, length), new Vector2(0.5f, 0.5f), 100.0f);
    }

    private static float CalculateSobelEdge(float[,] depthMap, int x, int z)
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);
        if (x == 0 || x == width - 1 || z == 0 || z == length - 1) return 0f;

        float tl = depthMap[x - 1, z - 1]; float tc = depthMap[x, z - 1]; float tr = depthMap[x + 1, z - 1];
        float ml = depthMap[x - 1, z]; float mr = depthMap[x + 1, z];
        float bl = depthMap[x - 1, z + 1]; float bc = depthMap[x, z + 1]; float br = depthMap[x + 1, z + 1];

        float edgeX = (tl * 1f) + (tc * 2f) + (tr * 1f) + (bl * -1f) + (bc * -2f) + (br * -1f);
        float edgeZ = (tl * 1f) + (ml * 2f) + (bl * 1f) + (tr * -1f) + (mr * -2f) + (br * -1f);
        return Mathf.Sqrt((edgeX * edgeX) + (edgeZ * edgeZ));
    }

    public static string SaveTextureAsPNG(Texture2D texture, string folderName, string fileName)
    {
        string rootPath = Application.persistentDataPath;
        string folderPath = Path.Combine(rootPath, folderName);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        string finalPath = Path.Combine(folderPath, fileName);
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(finalPath, bytes);
        return finalPath;
    }
}