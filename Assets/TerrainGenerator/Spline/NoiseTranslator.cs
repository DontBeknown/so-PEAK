using System.Numerics;
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

    //private void OnEnable() // Called when script is loaded/enabled
    //{
    //    if (mainSpline == null)
    //        InitMainSpline();
    //    MapDisplay display = GetComponent<MapDisplay>();
    //    if (display != null && display.textureRender != null)
    //    {
    //        display.textureRender.gameObject.SetActive(true);
    //    }
    //}

    //void OnDisable()
    //{
    //    MapDisplay display = GetComponent<MapDisplay>();
    //    if (display != null && display.textureRender != null)
    //    {
    //        display.textureRender.gameObject.SetActive(false);
    //    }
    //}




}
