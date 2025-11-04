
using UnityEditor.AssetImporters;
using UnityEngine;
using static UnityEditor.PlayerSettings.SplashScreen;

public class NoiseTranslator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, Mesh };
    public DrawMode drawMode;


    [Header("Noise Sources")]
    public MapGenerator noiseMap1;
    public MapGenerator noiseMap2;
    public MapGenerator noiseMap3;

    [Header("Map Size")]
    public int mapWidth = 1000;
    public int mapHeight = 1000;

    [Header("Falloff Mask Settings")]
    public bool useFalloffMask = true;
    public Vector2 peakCenter = new Vector2(0.5f, 0.5f); // normalized center (0–1)
    public float falloffPower = 2.5f; // controls slope shape

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    [Range(0, 6)]
    public int levelOfDetail;

    [HideInInspector] public Spline mainSpline;
    [HideInInspector] public float[,] depthMap;
    //init Spline
    public void InitMainSpline()
    { 
        mainSpline = new Spline();
        SplineMap.InitContinentalnessSpline(mainSpline);

    }


    //do the depth noise algorithm
    public void DepthNoise()
    {
        if (mainSpline == null)
            InitMainSpline();


        if (noiseMap1.noiseMap == null)
            noiseMap1.GenerateMap();
        if (noiseMap2.noiseMap == null)
            noiseMap2.GenerateMap();
        if (noiseMap3.noiseMap == null)
            noiseMap3.GenerateMap();

        depthMap = new float[mapWidth, mapHeight];
        float[,] continentalness = noiseMap1.noiseMap;
        float[,] erosion = noiseMap2.noiseMap;
        float[,] weirdness = noiseMap3.noiseMap;


        //parallel for faster
        System.Threading.Tasks.Parallel.For(0, mapHeight, z =>
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float c = continentalness[x, z];
                float e = erosion[x, z];
                float w = weirdness[x, z];
                float r = -3.0f * (Mathf.Abs(Mathf.Abs(w) - 0.6666667f) - 0.33333334f); //Peaks and Valley (Ridges)
                float[] np_param = {c, e, r};
                double off = SplineMap.GetSpline(mainSpline, np_param) + 0.015f;
                float d = 1.0f - 83.0f / 160.0f + (float)off; // simplified, y=0 always

                if (useFalloffMask)
                {
                    // Compute normalized distance from center
                    float nx = (float)x / mapWidth;
                    float nz = (float)z / mapHeight;

                    // radial distance (0 = center, 1 = far edge)
                    float dx = nx - peakCenter.x;
                    float dz = nz - peakCenter.y;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz) / 0.7071f; // normalize to 0–1 range

                    // mask curve
                    float mask = Mathf.Clamp01(1f - Mathf.Pow(dist, falloffPower));

                    d *= mask; // apply falloff mask
                }

                if (float.IsNaN(d) || float.IsInfinity(d))
                    d = 0f;
                else
                    d = Mathf.Clamp(d, 0f, 1f);

                depthMap[x, z] = d;
            }
        });

        MapDisplay display = GetComponent<MapDisplay>();
        Debug.Log("DrawMode: " + drawMode);
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawNoiseMap(depthMap, true);
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(PerlinTerrainMeshGenerator.GenerateTerrainMesh(depthMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail));
        }
    }

    private void Reset() // Called when component is added or Reset button pressed
    {
        InitMainSpline();
    }


}
