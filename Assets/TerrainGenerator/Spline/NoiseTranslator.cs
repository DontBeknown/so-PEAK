
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

    [Header("Visualizers")]
    public TreePreviewer treePreviewer; // Assign this in Inspector!

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
        ErosionNoise_1.GenerateMap();
        ErosionNoise_2.GenerateMap();
        ErosionNoise_3.GenerateMap();
        WeirdnessNoise.GenerateMap();

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
            meshHeightCurve, peakCenterArray, falloffPower, mountainRadiusMeters, peakPointsArray);
    

        
    }


    public void TerrainDrawing()
    {
        //first gen mountain
        DepthNoise();
        ////find peaks coordinates here
        //FindPeakPoints(depthMap, peakPoints);
        //then carve a road
        ErodedMountain();
        //then buffer zone
        GenerateBufferArea();
        //then color map
        colorMap = ColorMapping();

        TreeNoise.GenerateMap();
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

                            ////////////////TREE TEST//////////
                            // B. Plant the Debug Trees
                            // Only happens when you explicitly ask for the Mesh view
                            if (treePreviewer != null)
                            {
                                // 1. Setup the new 1100x1100 arrays
                                float[,] expandedTreeNoise = new float[mapWidth+bufferLength, mapLength+bufferLength];
                                float[,] expandedRoadMask = new float[mapWidth + bufferLength, mapLength + bufferLength];

                                // 2. Expand them!
                                // Trees fade to 0 (No trees at the edge)
                                BufferGen.GenMapWithBuffer(treeNoiseMap, expandedTreeNoise, bufferLength);

                                // Roads default to 1 (No roads at the edge)
                                BufferGen.GenRoadMaskWithBuffer(roadRidge, expandedRoadMask, bufferLength);

                                treePreviewer.GenerateDebugTrees(
                                            expandedTreeNoise,
                                            completeMap,
                                            expandedRoadMask,
                                            meshHeightMultiplier,
                                            200
                                        );



                            }



                        }

                    }
                }
        #endif


    }



    private void Reset() // Called when component is added or Reset button pressed
    {
        InitMainSpline();
    }

    


    



    private void ErodedMountain()
    {
        //Generate the road mask
        RoadNoise.GenerateMap();
        roadRidge = RoadNoise.noiseMap;

        RoadCarver.CarveRoad(depthMap, roadRidge, peakPointsArray, maxHeight, roadHeightCurve);
        
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




}
