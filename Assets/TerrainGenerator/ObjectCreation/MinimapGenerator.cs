using UnityEngine;
using System.IO;

public class MinimapGenerator
{
    public static Texture2D GenerateTopographicMinimap(float[,] depthMap, float[,] roadMask, float maxHeightMeters, bool saveToDisk = false, string folderName = "SavedMaps", string fileName = "TopographicMap.png")
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        Texture2D minimapTex = new Texture2D(width, length, TextureFormat.RGBA32, false);
        minimapTex.filterMode = FilterMode.Bilinear;

        Color paperColor = new Color(0.95f, 0.95f, 0.93f);
        Color edgeColor = new Color(0.1f, 0.1f, 0.1f, 0.7f); // Lowered the alpha slightly
        Color roadColor = new Color(0.85f, 0.8f, 0.7f); // Lighter sand color

        // LOWER SENSITIVITY: This stops the cliffs from turning completely black
        float sobelSensitivity = 1.2f;

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float roadValue = roadMask[x, z];

                // 1. Paper Background
                Color pixelColor = paperColor;

                // 2. Sobel Shading (Calculates the ENTIRE terrain, roads included)
                float edgeMagnitude = CalculateSobelEdge(depthMap, x, z);
                float edgeAlpha = Mathf.Clamp01(edgeMagnitude * sobelSensitivity);

                pixelColor = Color.Lerp(pixelColor, edgeColor, edgeAlpha);

                // 3. Stamp the road flatly on top (NO EXTRA OUTLINES)
                if (roadValue < 0.25f)
                {
                    pixelColor = roadColor;
                }

                minimapTex.SetPixel(x, z, pixelColor);
            }
        }

        minimapTex.Apply();
        if (saveToDisk) SaveTextureAsPNG(minimapTex, folderName, fileName);
        return minimapTex;
    }

    private static float CalculateSobelEdge(float[,] depthMap, int x, int z)
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        // Prevent checking outside the array boundaries
        if (x == 0 || x == width - 1 || z == 0 || z == length - 1)
            return 0f;

        // We look at the 3x3 grid around our current pixel [x, z]
        float tl = depthMap[x - 1, z - 1]; // Top Left
        float tc = depthMap[x, z - 1]; // Top Center
        float tr = depthMap[x + 1, z - 1]; // Top Right
        float ml = depthMap[x - 1, z]; // Mid Left
        float mr = depthMap[x + 1, z]; // Mid Right
        float bl = depthMap[x - 1, z + 1]; // Bottom Left
        float bc = depthMap[x, z + 1]; // Bottom Center
        float br = depthMap[x + 1, z + 1]; // Bottom Right

        // 1. Horizontal Edge Detection (Zucconi's Horizontal Kernel)
        float edgeX = (tl * 1f) + (tc * 2f) + (tr * 1f)
                    + (bl * -1f) + (bc * -2f) + (br * -1f);

        // 2. Vertical Edge Detection (Zucconi's Vertical Kernel)
        float edgeZ = (tl * 1f) + (ml * 2f) + (bl * 1f)
                    + (tr * -1f) + (mr * -2f) + (br * -1f);

        // 3. Pythagoras Theorem to combine them (Magnitude)
        return Mathf.Sqrt((edgeX * edgeX) + (edgeZ * edgeZ));
    }

    public static void SaveTextureAsPNG(Texture2D texture, string folderName, string fileName)
    {
        string folderPath = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string finalPath = Path.Combine(folderPath, fileName);
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(finalPath, bytes);

        Debug.Log($"<color=yellow>[MinimapGenerator]</color> Saved Map to: {finalPath}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}