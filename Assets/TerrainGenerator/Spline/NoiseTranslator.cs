
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
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
    public int bufferLength = 100;

    [Header("Falloff Mask Settings")]
    public bool useFalloffMask = true;
    public Vector2 peakCenter = new Vector2(0.5f, 0.5f); // normalized center (0�1)
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
    [HideInInspector] public float[,] completeMap;
    [HideInInspector] public float[,] tempHeight;


    //for collect max height index and value
    private float maxHeight = 0f;
    List<Vector2Int> peakPoints = new List<Vector2Int>();

    //collect mapNoise Bcs im lazy
    private float[,] roadRidge;

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

        if (mainSpline == null)
            InitMainSpline();


        //this also random new noise
        ContinentalNoise.GenerateMap();
        ErosionNoise.GenerateMap();
        WeirdnessNoise.GenerateMap();

        float[,] continentalness = ContinentalNoise.noiseMap;
        float[,] erosion = ErosionNoise.noiseMap;
        float[,] weirdness = WeirdnessNoise.noiseMap;


        DefaultMountainGen.MountainTerarainGen(mainSpline, mapIteration, depthMap, continentalness, erosion, weirdness,
            meshHeightCurve, peakCenter);
    

        
    }


    public void TerrainDrawing()
    {
        //first gen mountain
        DepthNoise();
        //find peaks coordinates here
        FindPeakPoints(depthMap, peakPoints);
        //then carve a road
        ErodedMountain();
        //then buffer zone
        GenerateBufferArea();
        //then color map
        Color[,] colorMap = ColorMapping();

        MapDisplay display = GetComponent<MapDisplay>();
        Debug.Log("DrawMode: " + drawMode);
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawNoiseMap(completeMap, true);
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(PerlinTerrainMeshGenerator.GenerateTerrainMesh(completeMap, colorMap, meshHeightMultiplier, levelOfDetail));
        }

    }



    private void Reset() // Called when component is added or Reset button pressed
    {
        InitMainSpline();
    }

    


    private void FindPeakPoints(float[,] depthMap, List<Vector2Int> peakPoints)
    {
        maxHeight = 0f;
        peakPoints.Clear();

        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
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
        //Generate the road mask
        RoadNoise.GenerateMap();
        roadRidge = RoadNoise.noiseMap;

        RoadCarver.CarveRoad(depthMap, roadRidge, peakPoints, maxHeight, roadHeightCurve);
        
    }

    private void GenerateBufferArea()
    {
        completeMap = new float[mapWidth + bufferLength, mapLength + bufferLength];
        //then send new buffer for it to filled
        BufferGen.GenMapWithBuffer(depthMap, completeMap, bufferLength);


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
                    finalColor = new Color(0.70f, 0.55f, 0.35f); // light brown
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
                        finalColor = new Color(0.35f, 0.20f, 0.10f); // dark brown rock
                    }
                    else
                    {
                        finalColor = new Color(0.2f, 0.7f, 0.2f); // green grass
                    }
                }

                colorMap[x, z] = finalColor;



            }
                
        });

        return colorMap;

    }




}
