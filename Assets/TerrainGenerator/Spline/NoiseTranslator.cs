
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
    [SerializeField] private Color roadColor = new Color(0.70f, 0.55f, 0.35f);
    [SerializeField] private Color sideRockColor = new Color(0.35f, 0.20f, 0.10f);
    [SerializeField] private Color fieldColor = new Color(0.20f, 0.70f, 0.20f);

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
        //then buffer zone
        GenerateBufferArea();
        // Flatten the ground AND get the offset spawn coordinate
        mainPeak = CarveLighthouseFoundation(completeMap, mainPeak, completeMap[mainPeak.x, mainPeak.y]);

        //then color map
        colorMap = ColorMapping();

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
                                completeMap, colorMap, meshHeightMultiplier, levelOfDetail));

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

                //Road check from the value we get 
                if (roadValue < 0.25f)
                {
                    finalColor = roadColor;
                }
                else
                {
                    // 2. Compute steepness (compare with right and down neighbors)
                    
                    float heightRight = (x < totalWidth - 1) ? completeMap[x + 1, z] : heightHere;
                    float heightDown = (z < totalLength - 1) ? completeMap[x, z + 1] : heightHere;

                    float steepness = Mathf.Max(
                        Mathf.Abs(heightHere - heightRight),
                        Mathf.Abs(heightHere - heightDown)
                    );

                    if (steepness > 0.15f)
                    {
                        finalColor = sideRockColor;
                    }
                    else
                    {
                        finalColor = fieldColor;
                    }
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
