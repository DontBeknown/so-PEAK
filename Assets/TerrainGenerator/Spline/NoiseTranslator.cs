
using System;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.Linq;
using static UnityEditor.PlayerSettings.SplashScreen;

public class NoiseTranslator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, Mesh };
    public DrawMode drawMode;


    [Header("Noise Sources")]
    public MapGenerator ContinentalNoise;
    public MapGenerator ErosionNoise;
    public MapGenerator WeirdnessNoise;
    public MapGenerator RoadNoise;

    [Header("Map Size")]
    public int mapWidth = 1000;
    public int mapLength = 1000;

    [Header("Falloff Mask Settings")]
    public bool useFalloffMask = true;
    public Vector2 peakCenter = new Vector2(0.5f, 0.5f); // normalized center (0–1)
    public float falloffPower = 2.5f; // controls slope shape

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public AnimationCurve roadHeightCurve;


    [Range(1, 6)]
    public int levelOfDetail;
    [Range(1, 5)]
    public int mapIteration;

    [HideInInspector] public Spline mainSpline;
    [HideInInspector] public float[,] depthMap;
    [HideInInspector] public float[,] tempHeight;

    //for collect max height index and value
    private float maxHeight = 0f;
    List<Vector2Int> peakPoints = new List<Vector2Int>();

    //init Spline
    public void InitMainSpline()
    { 
        mainSpline = new Spline();
        SplineMap.InitContinentalnessSpline(mainSpline);

    }


    //do the depth noise algorithm
    public void DepthNoise()
    {
        depthMap = new float[mapWidth, mapLength]; 

        for (int i = 0; i < mapIteration; i++)
        {
            //Translate
            TranslateHeight(i+1);

            //Correct
            FixHeight();

            //Merge
            float mergeAlpha = (i == 0) ? 1f : 0.7f;
            MergeTempToMain(mergeAlpha);
        }
        //Curve to 0-1
        CurveHeight();
        //Do the roads
        ErodedMountain();

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

    private void TranslateHeight(int round)
    {
        if (mainSpline == null)
            InitMainSpline();

        
        //this also random new noise
        ContinentalNoise.GenerateMap();
        ErosionNoise.GenerateMap();
        WeirdnessNoise.GenerateMap();


        tempHeight = new float[mapWidth, mapLength];
        float[,] continentalness = ContinentalNoise.noiseMap;
        float[,] erosion = ErosionNoise.noiseMap;
        float[,] weirdness = WeirdnessNoise.noiseMap;

        //change falloff poer from each round
        falloffPower = round;

        //parallel for faster
        System.Threading.Tasks.Parallel.For(0, mapLength, z =>
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float c = continentalness[x, z];
                float e = erosion[x, z];
                float w = weirdness[x, z];
                float r = -3.0f * (Mathf.Abs(Mathf.Abs(w) - 0.6666667f) - 0.33333334f); //Peaks and Valley (Ridges)
                float[] np_param = { c, e, r };
                double off = SplineMap.GetSpline(mainSpline, np_param) + 0.015f;
                float d = 1.0f - 83.0f / 160.0f + (float)off; // simplified, y=0 always

                if (useFalloffMask)
                {
                    // Compute normalized distance from center
                    float nx = (float)x / mapWidth;
                    float nz = (float)z / mapLength;

                    // radial distance (0 = center, 1 = far edge)
                    float dx = nx - peakCenter.x;
                    float dz = nz - peakCenter.y;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz) / 0.7071f; // normalize to 0–1 range

                    // mask curve
                    float mask = Mathf.Clamp01(1f - Mathf.Pow(dist, falloffPower));

                    d *= mask; // apply falloff mask
                }

                if (!float.IsNaN(d) && !float.IsInfinity(d))
                    d = Mathf.Clamp(d, 0f, 1f);
                else
                    d = -1f;
                tempHeight[x, z] = d;
            }
        });
    }

    //in case NAN we try to fix like a flex tape
    private void FixHeight()
    {
        
        for (int z = 0; z < mapLength; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float d = tempHeight[x, z];
                if (d == -1f)
                {
                    if (x > 0 && tempHeight[x - 1, z] != -1f) // try left
                        tempHeight[x, z] = tempHeight[x - 1, z];
                    else if (x < mapWidth - 1 && tempHeight[x + 1, z] != -1f) // try right
                        tempHeight[x, z] = tempHeight[x + 1, z];
                    else if (z > 0 && tempHeight[x, z - 1] != -1f) // try up
                        tempHeight[x, z] = tempHeight[x, z - 1];
                    else if (z < mapLength - 1 && tempHeight[x, z + 1] != -1f) // try down
                        tempHeight[x, z] = tempHeight[x, z + 1];
                    else
                        tempHeight[x, z] = 0f; // fallback
                }


                




            }
        }

    }

    // Merges a temporary map into main height map
    private void MergeTempToMain(float alpha = 1f)
    {
        float flatThreshold = 0.22f;

        System.Threading.Tasks.Parallel.For(0, mapLength, z =>
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float oldH = depthMap[x, z];
                float newH = tempHeight[x, z];

                // keep flat areas flat
                if (oldH <= flatThreshold && newH <= flatThreshold)
                {
                    // optional: keep exact old height
                    depthMap[x, z] = oldH;
                }
                else
                {
                    // stack height normally
                    depthMap[x, z] = oldH * (1 - alpha) + newH * alpha;
                }
            }
        });
    }

    //Curve Height via Curve anim
    //will compare with roads later
    private void CurveHeight()
    {
        //maxHeight = 0f;
        for (int z = 0; z < mapWidth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                depthMap[x,z]= meshHeightCurve.Evaluate(depthMap[x, z]);
                // Track max value
                if (depthMap[x, z] > maxHeight)
                {
                    maxHeight = depthMap[x, z];
                    peakPoints.Clear();
                    peakPoints.Add(new Vector2Int(x, z));
                }
                else if (Mathf.Approximately(depthMap[x, z], maxHeight))
                {
                    peakPoints.Add(new Vector2Int(x, z));
                }

            }

        }

    }

    private void ErodedMountain()
    {
  
        RoadNoise.GenerateMap();
        float[,] roadRidge = RoadNoise.noiseMap;

        float maxDist = 0f;

        Vector2Int peakCoord = GetPeakCoordinate();

        for (int z = 0; z < mapWidth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (roadRidge[x, z] < 0.25f)
                {
                    float d = Vector2.Distance(new Vector2(x, z), peakCoord);
                    if (d > maxDist) maxDist = d;
                }
            }
        }


        if (maxDist < 0.0001f) maxDist = 0.0001f;


        for (int z = 0; z < mapWidth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (roadRidge[x, z] < 0.25f)
                {
                    float d = Vector2.Distance(new Vector2(x, z), peakCoord);
                    float t = 1f - (d / maxDist);  
                    t = Mathf.Clamp01(t);


                    float height = maxHeight * t;
                    depthMap[x, z] = roadHeightCurve.Evaluate(height);
                }

            }
        }

    }

    // Returns the “central” coordinate of the maximum height in a 2D map
    private Vector2Int GetPeakCoordinate()
    {
        
        // 2. Pick median X and Z for central peak
        if (peakPoints.Count == 0)
        {
            return new Vector2Int(0, 0); // fallback if no peak
        }

        var sortedX = peakPoints.Select(p => p.x).OrderBy(v => v).ToList();
        var sortedZ = peakPoints.Select(p => p.y).OrderBy(v => v).ToList();

        int medianX = sortedX[sortedX.Count / 2];
        int medianZ = sortedZ[sortedZ.Count / 2];

        return new Vector2Int(medianX, medianZ);
    }



}
