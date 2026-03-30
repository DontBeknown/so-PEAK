
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


    public void TerrainDrawing(int seed)
    {
        //first gen mountain
        DepthNoise(seed);
        ////find peaks coordinates here
        //FindPeakPoints(depthMap, peakPoints);
        //then carve a road
        ErodedMountain(seed);
        //smooth test
        depthMap = SmoothHeightMap(depthMap, 3);
        //then buffer zone
        GenerateBufferArea();
        
        // Flatten the ground AND get the offset spawn coordinate
        mainPeak = CarveLighthouseFoundation(completeMap, mainPeak, completeMap[mainPeak.x, mainPeak.y]);

        //then color map
        //colorMap = ColorMapping();

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
                            display.DrawMesh(PerlinTerrainMeshGenerator.GenerateTerrainMesh(
                                completeMap, meshHeightMultiplier, levelOfDetail));

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

        RoadCarver.CarveRoad(depthMap, roadRidge, peakPointsArray, maxHeight, roadHeightCurve, seed,out mainPeak);


        
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

   


    // would delete Later
    private Color[,] ColorMapping()
    { 
        //init color map size would be like complete heightmap
        int totalWidth = completeMap.GetLength(0);
        int totalLength = completeMap.GetLength(1); 
        Color[,] colorMap = new Color[totalWidth, totalLength];

        //then for loop filling color logic
        Parallel.For(0, totalLength, z =>
        {
            for (int x = 0; x < totalWidth; x++)
            {
                //get ref coord from old version of map
                Vector2Int refCoord = BufferGen.GetReferenceCoordinate(x, z, roadRidge, bufferLength/2);

                //current height that already computed
                float heightHere = completeMap[x, z];

                //then get the ref road mask value from the old
                float roadValue = roadRidge[refCoord.x, refCoord.y];


                Color finalColor;

                float heightLeft = (x > 0) ? completeMap[x - 1, z] : heightHere;
                float heightRight = (x < totalWidth - 1) ? completeMap[x + 1, z] : heightHere;
                float heightUp = (z > 0) ? completeMap[x, z - 1] : heightHere;
                float heightDown = (z < totalLength - 1) ? completeMap[x, z + 1] : heightHere;

                // Divide by 2 because we are measuring across 2 grid cells (Left to Right)
                float dX = (heightRight - heightLeft) / 2f;
                float dZ = (heightDown - heightUp) / 2f;

                dX *= meshHeightMultiplier;
                dZ *= meshHeightMultiplier;

                // 2. Convert to Angle
                // If your terrain heights are scaled up compared to your X/Z grid, adjust this.
                // Usually, it's 1.0f if 1 unit of height == 1 unit of grid width.
                float gridSpacing = 1.0f;
                Vector3 surfaceNormal = new Vector3(-dX, gridSpacing, -dZ).normalized;

                // Get the angle between straight up (0 degrees) and our slope normal
                float slopeAngle = Vector3.Angle(Vector3.up, surfaceNormal);




                if (roadValue < 0.25f)
                {
                    finalColor = roadColor;
                }
                else
                {
                    // Define your transition zone
                    float minRockAngle = 25f; // Anything below this is 100% fieldColor
                    float maxRockAngle = 45f; // Anything above this is 100% sideRockColor

                    // InverseLerp outputs a float between 0.0 and 1.0. 
                    // E.g., if the angle is 35, it outputs 0.5 (50% rock, 50% grass)
                    float rockBlend = Mathf.InverseLerp(minRockAngle, maxRockAngle, slopeAngle);

                    // Smoothly mix the two colors based on the blend percentage
                    finalColor = Color.Lerp(fieldColor, sideRockColor, rockBlend);
                }

                colorMap[x, z] = finalColor;



            }
                
        });

        return colorMap;

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
