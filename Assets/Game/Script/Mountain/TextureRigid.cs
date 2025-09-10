using Unity.Mathematics;
using UnityEngine;

public class TextureRigid : MonoBehaviour
{
    public int width = 256;
    public int height = 256;
    public float scale = 20f;

    public Renderer displaySimplex;
    public Renderer displayRidge;

    void Start()
    {
        Texture2D simplexTex = GenerateSimplexNoiseTexture();
        Texture2D ridgeTex = GenerateRidgeNoiseTexture();

        displaySimplex.material.mainTexture = simplexTex;
        displayRidge.material.mainTexture = ridgeTex;
    }

    Texture2D GenerateSimplexNoiseTexture()
    {
        Texture2D tex = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float2 pos = new float2(x / scale, y / scale);
                float n = noise.snoise(pos); // Simplex Noise
                float value = (n + 1f) / 2f; // normalize 0-1
                tex.SetPixel(x, y, new Color(value, value, value));
            }
        }
        tex.Apply();
        return tex;
    }

    Texture2D GenerateRidgeNoiseTexture()
    {
        Texture2D tex = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float2 pos = new float2(x / scale, y / scale);
                float n = noise.snoise(pos); // Simplex Noise
                float ridge = 1f - Mathf.Abs(n); // Ridge Noise
                tex.SetPixel(x, y, new Color(ridge, ridge, ridge));
            }
        }
        tex.Apply();
        return tex;
    }
}
