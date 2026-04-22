
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
public class NoiseTranslator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, Mesh };
    public DrawMode drawMode;


    [Header("Noise Sources")]
    public MapGenerator ContinentalNoise;
    public MapGenerator ErosionNoise_1;
    public MapGenerator ErosionNoise_2;
    public MapGenerator ErosionNoise_3;
    public MapGenerator WeirdnessNoise;
    public MapGenerator RoadNoise;
    public MapGenerator TreeNoise;
    public MapGenerator RiskNoise;

    [Header("Puddle Generator")]
    public PondGenerator pondGenerator;


    [Header("Map Size")]
    public int mapWidth = 1000;
    public int mapLength = 1000;
    public int bufferLength = 100;

    [Header("Falloff Mask Settings")]
    public Vector2[] peakCenterArray = new Vector2[3]
    {
        new Vector2(0.2f, 0.2f),
        new Vector2(0.2f, 0.8f),
        new Vector2(0.8f, 0.8f)
    };
    public float falloffPower = 1.5f; 

    public float meshHeightMultiplier;
    public float mountainRadiusMeters;
    public AnimationCurve meshHeightCurve;
    public AnimationCurve roadHeightCurve;


    [Range(1, 6)]
    public int levelOfDetail;
    [Range(1, 5)]
    public int mapIteration;
    [Range(1, 6)]
    public int smoothMapPasses = 1;



    [Header("Terrain Colors")]
    [SerializeField] public Color roadColor = new Color(0.70f, 0.55f, 0.35f);
    [SerializeField] public Color sideRockColor = new Color(0.35f, 0.20f, 0.10f);
    [SerializeField] public Color fieldColor = new Color(0.20f, 0.70f, 0.20f);

    [HideInInspector] public Spline mainSpline;
    [HideInInspector] public Color[,] colorMap;
    [HideInInspector] public float[,] depthMap;
    [HideInInspector] public float[,] completeMap;
    [HideInInspector] public float[,] tempHeight;
    [HideInInspector] public float[,] treeNoiseMap;
    [HideInInspector] public float[,] roadRidge;
    [HideInInspector] public bool[,] waterMask;
    [HideInInspector] public float[,] riskMap;
    [HideInInspector] public Vector2Int spawnCoord;



    //for collect max height index and value
    private float maxHeight = 0f;
    List<List<Vector2Int>> peakPointsArray = new List<List<Vector2Int>>();
    [HideInInspector] public Vector2Int mainPeak;




    //init Spline
    public void InitMainSpline()
    { 
        mainSpline = new Spline();
        SplineMap.InitContinentalnessSpline(mainSpline);

    }


    //do the depth noise algorithm
    public void DepthNoise(int seed)
    {
        depthMap = new float[mapWidth, mapLength];

        if (mainSpline == null)
            InitMainSpline();


        //this also random new noise
        ContinentalNoise.GenerateMap(seed);
        ErosionNoise_1.GenerateMap(seed);
        ErosionNoise_2.GenerateMap(seed+1);
        ErosionNoise_3.GenerateMap(seed+2);
        WeirdnessNoise.GenerateMap(seed);

        float[,] continentalness = ContinentalNoise.noiseMap;
        float[,] erosion_1 = ErosionNoise_1.noiseMap;
        float[,] erosion_2 = ErosionNoise_2.noiseMap;
        float[,] erosion_3 = ErosionNoise_3.noiseMap;
        float[,] weirdness = WeirdnessNoise.noiseMap;

        float[][,] erosionArray = new float[3][,];
        erosionArray[0] = ErosionNoise_1.noiseMap;
        erosionArray[1] = ErosionNoise_2.noiseMap;
        erosionArray[2] = ErosionNoise_3.noiseMap;



        DefaultMountainGen.MultipleMountainTerarainGen(mainSpline, mapIteration, depthMap, continentalness, erosionArray, weirdness,
            meshHeightCurve, peakCenterArray, falloffPower, mountainRadiusMeters, peakPointsArray, seed);
    

        
    }


    public void TerrainDrawingForPath(int seed)
    {
        DepthNoise(seed);
        ErodedMountain(seed);
        depthMap = SmoothHeightMap(depthMap, smoothMapPasses);
        GenerateBufferArea();
        mainPeak = CarveLighthouseFoundation(completeMap, mainPeak, completeMap[mainPeak.x, mainPeak.y]);
    }

    public void TerrainDrawing(int seed)
    {
        //first gen mountain
        DepthNoise(seed);
        //then carve a road
        ErodedMountain(seed);
        //smooth test
        depthMap = SmoothHeightMap(depthMap, smoothMapPasses);

        int halfBuffer = bufferLength / 2;
        waterMask = new bool[mapWidth, mapLength];
        //dig some ponds
        pondGenerator.MassSpawnPonds(depthMap, roadRidge, waterMask, seed, meshHeightMultiplier, halfBuffer);
        //then buffer zone
        GenerateBufferArea();
        
        // Flatten the ground AND get the offset spawn coordinate
        mainPeak = CarveLighthouseFoundation(completeMap, mainPeak, completeMap[mainPeak.x, mainPeak.y]);

        RiskNoise.GenerateMap(seed);
        riskMap = RiskNoise.noiseMap;

        TreeNoise.GenerateMap(seed);
        treeNoiseMap = TreeNoise.noiseMap;



        ////////////////////DEBUG WILL DELETE THIS LATER ////////////////////////////////
        // This block only exists inside the Unity Editor
        #if UNITY_EDITOR
                // Only draw the full "Preview" map if the game is NOT playing
                // This prevents the big debug mesh from overlapping your chunks
                if (!Application.isPlaying)
                {
                    MapDisplay display = GetComponent<MapDisplay>();
                    if (display != null)
                    {
                        if (drawMode == DrawMode.NoiseMap)
                        {
                            display.DrawNoiseMap(completeMap, true);
                        }
                        else if (drawMode == DrawMode.Mesh)
                        {
                                display.DrawMesh(
                                PerlinTerrainMeshGenerator.GenerateTerrainMesh(completeMap, meshHeightMultiplier, levelOfDetail),
                                this // <--- Here is the missing argument!
                                );

                }

                    }
                }
        #endif


    }



    private void Reset() // Called when component is added or Reset button pressed
    {
        InitMainSpline();
    }

    


    



    private void ErodedMountain(int seed)
    {
        //Generate the road mask
        RoadNoise.GenerateMap(seed);
        roadRidge = RoadNoise.noiseMap;

        RoadCarver.CarveRoad(depthMap, roadRidge, peakPointsArray, meshHeightMultiplier, roadHeightCurve, seed ,out mainPeak, out spawnCoord);

        DetailPreservingEmbankments(depthMap, roadRidge, meshHeightMultiplier, 50);

    }

    private void GenerateBufferArea()
    {
        completeMap = new float[mapWidth + bufferLength, mapLength + bufferLength];
        //then send new buffer for it to filled
        BufferGen.GenMapWithBuffer(depthMap, completeMap, bufferLength);

        int offset = bufferLength / 2;
        mainPeak = new Vector2Int(mainPeak.x + offset, mainPeak.y + offset);


    }

    private float[,] SmoothHeightMap(float[,] map, int passes = 1)
    {
        int width = map.GetLength(0);
        int length = map.GetLength(1);

        // We need a temporary array so we don't blur using already-blurred pixels
        float[,] smoothedMap = new float[width, length];

        for (int p = 0; p < passes; p++)
        {
            // Copy the array to process it safely
            Array.Copy(map, smoothedMap, map.Length);

            Parallel.For(1, length - 1, z =>
            {
                for (int x = 1; x < width - 1; x++)
                {
                    // A simple 3x3 Box Blur
                    float averageHeight = (
                        map[x - 1, z - 1] + map[x, z - 1] + map[x + 1, z - 1] +
                        map[x - 1, z] + map[x, z] + map[x + 1, z] +
                        map[x - 1, z + 1] + map[x, z + 1] + map[x + 1, z + 1]
                    ) / 9f;

                    smoothedMap[x, z] = averageHeight;
                }
            });

            // Put the smoothed data back into the main map for the next pass
            Array.Copy(smoothedMap, map, smoothedMap.Length);
        }

        return smoothedMap;
    }

    private void DetailPreservingEmbankments(float[,] depthMap, float[,] roadMask, float maxHeightMeters, int searchRadius = 15)
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        // Convert your physical 3-meter rules into the 0.0 to 1.0 map scale
        float thresholdNorm = 3.0f / maxHeightMeters;
        float dummySpreadNorm = 8.0f / maxHeightMeters; // The "dummy 3" multiplier

        // We need a temp map so we don't read modified values while we are currently calculating
        float[,] tempMap = new float[width, length];
        Array.Copy(depthMap, tempMap, depthMap.Length);

        // Parallel processing to handle the heavy double-loops quickly
        Parallel.For(0, length, z =>
        {
            for (int x = 0; x < width; x++)
            {
                // Skip if this pixel is the road itself
                if (roadMask[x, z] < 0.25f) continue;

                float currentH = tempMap[x, z];

                // --- STEP 3 (Shifted First): Find the nearest road point ---
                float nearestRoadH = -1f;
                float minDistSqr = float.MaxValue;

                // Create a local bounding box to search for roads
                int minRX = Mathf.Max(0, x - searchRadius);
                int maxRX = Mathf.Min(width - 1, x + searchRadius);
                int minRZ = Mathf.Max(0, z - searchRadius);
                int maxRZ = Mathf.Min(length - 1, z + searchRadius);

                for (int rz = minRZ; rz <= maxRZ; rz++)
                {
                    for (int rx = minRX; rx <= maxRX; rx++)
                    {
                        if (roadMask[rx, rz] < 0.25f) // If it is a road
                        {
                            // Use Squared Distance (much faster than Mathf.Sqrt)
                            float distSqr = (rx - x) * (rx - x) + (rz - z) * (rz - z);
                            if (distSqr < minDistSqr)
                            {
                                minDistSqr = distSqr;
                                nearestRoadH = tempMap[rx, rz];
                            }
                        }
                    }
                }

                // If no road was found within the search radius, skip this pixel
                if (nearestRoadH < 0f) continue;

                // --- STEP 0: Check if point is lower than road by 3 meters ---
                if (nearestRoadH - currentH < thresholdNorm) continue;

                // --- STEP 1: Find Min/Max of local NON-ROAD area ---
                float localMin = float.MaxValue;
                float localMax = float.MinValue;

                // Look at a tight 5x5 neighborhood to figure out local details
                int detailRadius = 2;
                int minDX = Mathf.Max(0, x - detailRadius);
                int maxDX = Mathf.Min(width - 1, x + detailRadius);
                int minDZ = Mathf.Max(0, z - detailRadius);
                int maxDZ = Mathf.Min(length - 1, z + detailRadius);

                for (int dz = minDZ; dz <= maxDZ; dz++)
                {
                    for (int dx = minDX; dx <= maxDX; dx++)
                    {
                        if (roadMask[dx, dz] >= 0.25f) // Only look at non-road terrain
                        {
                            float h = tempMap[dx, dz];
                            if (h < localMin) localMin = h;
                            if (h > localMax) localMax = h;
                        }
                    }
                }

                // --- STEP 2: Calculate ratio (t) between 0.0 and 1.0 ---
                float t = 0f;
                if (localMax > localMin) // Prevent divide by zero if terrain is perfectly flat
                {
                    t = (currentH - localMin) / (localMax - localMin);
                }

                // --- STEP 4: Recalculate and push terrain up ---
                // Base is the road height minus the 3m threshold, then we add back the local detail
                float newHeight = nearestRoadH - thresholdNorm + (dummySpreadNorm * t);

                // Write the new elevated height back into the real map
                depthMap[x, z] = newHeight;
            }
        });
    }

    public static Vector2Int CarveLighthouseFoundation(float[,] completeMap, Vector2Int shiftedPeak, float peakHeight)
    {
        int foundationRadius = 10;
        float offsetMagnitude = foundationRadius * 0.7f; 
        int mapWidth = completeMap.GetLength(0);
        int mapLength = completeMap.GetLength(1);

        // Hardcoded direction: North-East (1, 1)
        Vector2 offsetDir = new Vector2(1, 1).normalized * offsetMagnitude;
        Vector2Int lighthouseSpawnPos = new Vector2Int(
            shiftedPeak.x + Mathf.RoundToInt(offsetDir.x),
            shiftedPeak.y + Mathf.RoundToInt(offsetDir.y)
        );

        for (int dz = -foundationRadius; dz <= foundationRadius; dz++)
        {
            for (int dx = -foundationRadius; dx <= foundationRadius; dx++)
            {
                int xx = shiftedPeak.x + dx;
                int zz = shiftedPeak.y + dz;

                if (xx < 0 || zz < 0 || xx >= mapWidth || zz >= mapLength) continue;

                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist <= foundationRadius)
                {
                    completeMap[xx, zz] = peakHeight;
                }
            }
        }

        // Return the new coordinate so your main script can update 'mainPeak'
        return lighthouseSpawnPos;
    }

}
