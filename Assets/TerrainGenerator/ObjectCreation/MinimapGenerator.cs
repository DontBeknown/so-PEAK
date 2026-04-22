using UnityEngine;
using System.IO;

public class MinimapGenerator
{
    public static Sprite GenerateTopographicMinimap(float[,] depthMap, float[,] roadMask, float maxHeightMeters, bool saveToDisk = false, string folderName = "SavedMaps", string fileName = "TopographicMap.png")
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        // --- 1. THE DOUBLE-STEP QUANTIZATION ---
        float minorInterval = 5f;  // Normal lines every 25m
        float majorInterval = 25f; // Bold lines every 100m (Every 4th line)

        float minorSteps = maxHeightMeters / minorInterval;
        float majorSteps = maxHeightMeters / majorInterval;

        float[,] minorSteppedMap = new float[width, length];
        float[,] majorSteppedMap = new float[width, length];

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Create two separate blocky maps!
                minorSteppedMap[x, z] = Mathf.Floor(depthMap[x, z] * minorSteps) / minorSteps;
                majorSteppedMap[x, z] = Mathf.Floor(depthMap[x, z] * majorSteps) / majorSteps;
            }
        }

        Texture2D minimapTex = new Texture2D(width, length, TextureFormat.RGBA32, false);
        minimapTex.filterMode = FilterMode.Bilinear;

        // --- MAP COLORS ---
        Color shadingColor = new Color(0.0f, 0.0f, 0.0f, 0.4f);
        Color minorContourColor = new Color(0.0f, 0.0f, 0.0f, 0.3f); // Faint thin lines
        Color majorContourColor = new Color(0.0f, 0.0f, 0.0f, 0.9f); // Dark bold lines
        Color roadOutlineColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        float softShadingSensitivity = 1.0f;

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float smoothHeight = depthMap[x, z];
                float roadValue = roadMask[x, z];

                // LAYER A: BASE HEATMAP
                Color pixelColor = GetElevationColor(smoothHeight);

                // LAYER B: SOFT 3D SHADING
                float softEdge = CalculateSobelEdge(depthMap, x, z);
                pixelColor = Color.Lerp(pixelColor, shadingColor, Mathf.Clamp01(softEdge * softShadingSensitivity));

                // LAYER C: TOPOGRAPHIC CONTOURS (Now drawing EVERYWHERE)
                // We removed the 'if (roadValue >= 0.25f)' so it draws on the roads too!
                float majorEdge = CalculateSobelEdge(majorSteppedMap, x, z);
                float minorEdge = CalculateSobelEdge(minorSteppedMap, x, z);

                // Because the map is quantized (flat stairs), flat areas have exactly 0.0 slope.
                // ANY slope > 0 is a contour line. We use a tiny threshold (0.01f) to catch them all.
                if (majorEdge > 0.01f)
                {
                    // FIXED: We must Lerp to blend the transparent ink OVER the heatmap!
                    pixelColor = Color.Lerp(pixelColor, majorContourColor, majorContourColor.a);
                }
                else if (minorEdge > 0.01f)
                {
                    // FIXED: Lerp the faint ink over the heatmap.
                    pixelColor = Color.Lerp(pixelColor, minorContourColor, minorContourColor.a);
                }

                minimapTex.SetPixel(x, z, pixelColor);
            }
        }

        minimapTex.Apply();
        if (saveToDisk) SaveTextureAsPNG(minimapTex, folderName, fileName);

        Sprite minimapSprite = Sprite.Create(
            minimapTex,                                                  // The texture we just painted
            new Rect(0.0f, 0.0f, minimapTex.width, minimapTex.height),   // Use the whole image
            new Vector2(0.5f, 0.5f),                                     // Set pivot to the dead center
            100.0f                                                       // Standard Pixels Per Unit
        );

        return minimapSprite;
    }

    private static Color GetElevationColor(float height)
    {
        Color c0 = new Color(0.05f, 0.25f, 0.45f); // Blue
        Color c1 = new Color(0.10f, 0.80f, 0.40f); // Green
        Color c2 = new Color(0.90f, 0.90f, 0.20f); // Yellow
        Color c3 = new Color(0.95f, 0.50f, 0.10f); // Orange
        Color c4 = new Color(0.80f, 0.15f, 0.15f); // Red

        if (height < 0.25f) return Color.Lerp(c0, c1, height / 0.25f);
        if (height < 0.50f) return Color.Lerp(c1, c2, (height - 0.25f) / 0.25f);
        if (height < 0.75f) return Color.Lerp(c2, c3, (height - 0.50f) / 0.25f);
        return Color.Lerp(c3, c4, (height - 0.75f) / 0.25f);
    }

    private static float CalculateSobelEdge(float[,] depthMap, int x, int z)
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        if (x == 0 || x == width - 1 || z == 0 || z == length - 1) return 0f;

        float tl = depthMap[x - 1, z - 1];
        float tc = depthMap[x, z - 1];
        float tr = depthMap[x + 1, z - 1];
        float ml = depthMap[x - 1, z];
        float mr = depthMap[x + 1, z];
        float bl = depthMap[x - 1, z + 1];
        float bc = depthMap[x, z + 1];
        float br = depthMap[x + 1, z + 1];

        float edgeX = (tl * 1f) + (tc * 2f) + (tr * 1f) + (bl * -1f) + (bc * -2f) + (br * -1f);
        float edgeZ = (tl * 1f) + (ml * 2f) + (bl * 1f) + (tr * -1f) + (mr * -2f) + (br * -1f);

        return Mathf.Sqrt((edgeX * edgeX) + (edgeZ * edgeZ));
    }

    public static Sprite GenerateRoadMinimap(float[,] roadMask, bool saveToDisk = false, string folderName = "SavedMaps", string fileName = "RoadMap.png")
    {
        int width = roadMask.GetLength(0);
        int length = roadMask.GetLength(1);

        Texture2D minimapTex = new Texture2D(width, length, TextureFormat.RGBA32, false);
        minimapTex.filterMode = FilterMode.Bilinear;

        // --- ROAD MAP COLORS ---
        Color nonRoadColor = new Color(0.35f, 0.25f, 0.15f); // Dark, muddy brown for the wilderness
        Color roadColor = new Color(0.80f, 0.65f, 0.45f);    // Light, dusty brown for the cleared trail

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // In your mask, 0 is usually the road, and 1 is the grass.
                float roadValue = roadMask[x, z];

                // Lerp smoothly blends the colors. 
                // If roadValue is 0, it paints the light roadColor.
                // If roadValue is 1, it paints the dark nonRoadColor.
                Color pixelColor = Color.Lerp(roadColor, nonRoadColor, roadValue);

                minimapTex.SetPixel(x, z, pixelColor);
            }
        }

        minimapTex.Apply();

        // Save to AppData so the UI can load it!
        if (saveToDisk) SaveTextureAsPNG(minimapTex, folderName, fileName);

        Sprite minimapSprite = Sprite.Create(
            minimapTex,
            new Rect(0.0f, 0.0f, minimapTex.width, minimapTex.height),
            new Vector2(0.5f, 0.5f),
            100.0f
        );

        return minimapSprite;
    }

    public static string SaveTextureAsPNG(Texture2D texture, string folderName, string fileName)
    {
        // Always save to AppData so it works in the built game
        string rootPath = Application.persistentDataPath;

        string folderPath = Path.Combine(rootPath, folderName);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string finalPath = Path.Combine(folderPath, fileName);

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(finalPath, bytes);

        Debug.Log($"<color=yellow>[MinimapGenerator]</color> Saved Map to: {finalPath}");

        // Return the exact hard drive path so the UI knows where to load it from!
        return finalPath;
    }
}