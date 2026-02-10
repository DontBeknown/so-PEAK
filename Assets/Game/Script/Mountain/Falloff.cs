using UnityEngine;
using Unity.Mathematics; 

[ExecuteAlways]

public class Falloff : MonoBehaviour
{
    [Header("Output Targets (assign Quad/Plane renderers)")]
    public Renderer islandRenderer;   
    public Renderer mountainRenderer; 

    [Header("Texture")]
    [Range(64, 1024)] public int texSize = 256;
    public FilterMode filterMode = FilterMode.Bilinear;
    public Gradient heightGradient; 

    [Header("Mesh Variables (match your formula)")]
    public float height = 50f;    

    [Header("Noise (Octaves)")]
    public float noiseScale = 2f;      
    [Range(1, 10)] public int octaves = 5;
    [Tooltip("??????????????????? octave: freq /= frequency; (??????? = ???????????????????)")]
    public float frequency = 2f;
    [Tooltip("?????????????????? octave: amplitude /= lacunarity; (??????? = ???????????)")]
    public float lacunarity = 2f;

    [Header("Ridge (sharp ridges)")]
    [Tooltip("????????????????? Ridge (0..~2). 1=????, ???????=?????????")]
    public float ridgeWeight = 1.0f;

    [Header("Falloff (Island shape)")]
    [Tooltip("?????????? (???????=???????????)")]
    public float falloffSteepness = 3f;
    [Tooltip("??????????? (???????=?????????????)")]
    public float falloffOffset = 2.2f;

    [Header("Water")]
    [Tooltip("???????? (????????????? height)")]
    public float waterLevel = 0f;

    Texture2D _texIsland, _texMountain;

    void OnEnable() 
    { 
        if (Application.isPlaying) Refresh(); 
    }
    
    void OnValidate() 
    { 
        if (!Application.isPlaying) Refresh(); 
    }

    void Refresh()
    {
        if (texSize < 2) texSize = 2;

        if (_texIsland == null || _texIsland.width != texSize)
        {
            _texIsland = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            _texIsland.wrapMode = TextureWrapMode.Clamp;
            _texIsland.filterMode = filterMode;
        }
        if (_texMountain == null || _texMountain.width != texSize)
        {
            _texMountain = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            _texMountain.wrapMode = TextureWrapMode.Clamp;
            _texMountain.filterMode = filterMode;
        }


        var pixelsIsland = new Color[texSize * texSize];
        var pixelsMountain = new Color[texSize * texSize];

        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                
                float2 pos = new float2(x, y);

                float s = OctavedSimplex(pos);
                float r = OctavedRidge(pos);
                float mix = (s + r) * 0.5f; 

                float fIsland = FalloffMap(pos, texSize, falloffSteepness, falloffOffset); // 0..1
                float fNone = 1f; 

                float hIsland = Mathf.Clamp(mix * fIsland * height, waterLevel, 1000f);
                float hMountain = Mathf.Clamp(mix * fNone * height, waterLevel, 1000f);

                float island01 = Mathf.InverseLerp(waterLevel, height, hIsland);
                float mountain01 = Mathf.InverseLerp(waterLevel, height, hMountain);

                Color cIsland = heightGradient.Evaluate(island01);
                Color cMountain = heightGradient.Evaluate(mountain01);

                int i = x + y * texSize;
                pixelsIsland[i] = cIsland;
                pixelsMountain[i] = cMountain;
            }

        _texIsland.SetPixels(pixelsIsland);
        _texIsland.Apply(false, false);

        _texMountain.SetPixels(pixelsMountain);
        _texMountain.Apply(false, false);

        if (islandRenderer) islandRenderer.sharedMaterial.mainTexture = _texIsland;
        if (mountainRenderer) mountainRenderer.sharedMaterial.mainTexture = _texMountain;
    }

    // ---------- Noise helpers ----------
    float OctavedSimplex(float2 pos)
    {
        float noiseVal = 0f;
        float amplitude = 1f;
        float freq = noiseScale;

        for (int o = 0; o < octaves; o++)
        {
            float v = (noise.snoise(pos / freq / texSize) + 1f) * 0.5f; // 0..1
            noiseVal += v * amplitude;

            freq /= Mathf.Max(0.0001f, frequency);
            amplitude /= Mathf.Max(0.0001f, lacunarity);
        }
        return Mathf.Clamp01(noiseVal);
    }

    float OctavedRidge(float2 pos)
    {
        float noiseVal = 0f;
        float amplitude = 1f;
        float freq = noiseScale;
        float weight = 1f;

        for (int o = 0; o < octaves; o++)
        {
            float baseN = noise.snoise(pos / freq / texSize); // -1..1
            float v = 1f - Mathf.Abs(baseN); 
            v *= v;                          
            v *= weight;
            weight = Mathf.Clamp01(v * ridgeWeight);

            noiseVal += v * amplitude;

            freq /= Mathf.Max(0.0001f, frequency);
            amplitude /= Mathf.Max(0.0001f, lacunarity);
        }
        return Mathf.Clamp01(noiseVal);
    }

    // ---------- Falloff ----------
    // ???????????????????? (??????????/???????? Max(|x|,|y|))
    static float FalloffMap(float2 pos, int size, float steepness, float offset)
    {
        float nx = (pos.x / (size - 1)) * 2f - 1f; // [-1,1]
        float ny = (pos.y / (size - 1)) * 2f - 1f; // [-1,1]
        float v = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny)); // 0 (????) ? 1 (???)

        float a = steepness;
        float b = offset;

        float num = Mathf.Pow(v, a);
        float denom = num + Mathf.Pow((b - b * v), a);
        float fall = 1f - (num / denom); // 0..1 (??????? ??????)
        return Mathf.Clamp01(fall);
    }
}
