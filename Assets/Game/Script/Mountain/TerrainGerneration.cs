using UnityEngine;
using System.Collections;

public class TerrainGerneration : MonoBehaviour
{
    // Public variables to control the terrain generation parameters
    [Header("Terrain Dimensions")]
    [Tooltip("The width of the terrain height map.")]
    public int width = 513;
    [Tooltip("The length (depth) of the terrain height map.")]
    public int length = 513;
    [Tooltip("The overall height multiplier for the terrain.")]
    public float heightScale = 20.0f;

    [Header("Noise Parameters")]
    [Tooltip("A global scale for the noise. Smaller values result in larger features.")]
    public float noiseScale = 50.0f;
    [Tooltip("The number of layers of noise used to create detail.")]
    public int octaves = 8;
    [Tooltip("The amount by which the amplitude of each octave decreases.")]
    [Range(0, 1)]
    public float persistence = 0.5f;
    [Tooltip("The amount by which the frequency of each octave increases.")]
    public float lacunarity = 2.0f;
    [Tooltip("A parameter to control how much the gradient affects the noise. Higher values lead to more smoothing on steep slopes.")]
    [Range(0, 2)]
    public float gradientMultiplier = 0.5f;

    [Header("Erosion Parameters")]
    [Tooltip("The number of water droplets to simulate.")]
    public int erosionIterations = 50000;
    [Tooltip("The strength of the erosion. How much height a droplet removes.")]
    public float erosionStrength = 0.05f;
    [Tooltip("How much sediment a droplet can carry.")]
    public float sedimentCapacity = 0.05f;

    private Terrain terrain;

    // Use this to generate the terrain when the script starts
    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (terrain != null)
        {
            GenerateTerrain();
        }
        else
        {
            Debug.LogError("No Terrain component found! Please attach this script to a GameObject with a Terrain component.");
        }
    }

    /// <summary>
    /// Generates the terrain height map based on the parameters.
    /// This is where the core logic of the Gradient Trick is implemented.
    /// </summary>
    public void GenerateTerrain()
    {
        // Get the TerrainData object from the Terrain component
        TerrainData terrainData = terrain.terrainData;

        // Set up the terrain's dimensions
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, heightScale, length);

        // Create a 2D float array to store the height map values
        float[,] heights = new float[width, length];

        // Loop through each point on the terrain grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                // Calculate and set the height for the current point
                heights[x, y] = CalculateHeight(x, y);
            }
        }

        // Start erosion as a coroutine
        StartCoroutine(ErodeTerrainCoroutine(heights, (result) =>
        {
            terrainData.SetHeights(0, 0, result);
        }));
    }

    /// <summary>
    /// Calculates the height for a single point (x, y) using Perlin noise and the gradient trick.
    /// </summary>
    /// <param name="x">The x-coordinate of the point.</param>
    /// <param name="y">The y-coordinate of the point.</param>
    /// <returns>The normalized height value (between 0 and 1).</returns>
    float CalculateHeight(int x, int y)
    {
        float amplitude = 1.0f;
        float frequency = 1.0f;
        float noiseHeight = 0.0f;
        float maxAmplitude = 0.0f; // Keep track of total possible amplitude

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x / noiseScale) * frequency;
            float sampleY = (y / noiseScale) * frequency;
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

            float gradX = Mathf.Abs(Mathf.PerlinNoise((x + 1) / noiseScale * frequency, sampleY) -
                                    Mathf.PerlinNoise((x - 1) / noiseScale * frequency, sampleY));
            float gradY = Mathf.Abs(Mathf.PerlinNoise(sampleX, (y + 1) / noiseScale * frequency) -
                                    Mathf.PerlinNoise(sampleX, (y - 1) / noiseScale * frequency));
            float gradient = (gradX + gradY) / 2.0f;

            float adjustedAmplitude = amplitude * (1.0f - gradient * gradientMultiplier);
            noiseHeight += perlinValue * adjustedAmplitude;

            maxAmplitude += amplitude; // Accumulate maximum amplitude
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize height so it's always between 0 and 1
        float height = noiseHeight / maxAmplitude;

        // Plateau curve: makes high areas flatter
        height = Mathf.Pow(height, 0.8f); // raises lower ground
        if (height > 0.7f)
        {
            height = 0.7f + (height - 0.7f) * 0.3f; // squash values near the top
        }

        return height;
    }

    /// <summary>
    /// Simulates a simple hydraulic erosion process by tracing the path of water droplets.
    /// </summary>
    /// <param name="heights">The input height map array.</param>
    /// <returns>The height map after erosion has been applied.</returns>
    IEnumerator ErodeTerrainCoroutine(float[,] heights, System.Action<float[,]> onComplete)
    {
        float[,] erodedHeights = (float[,])heights.Clone();

        for (int i = 0; i < erosionIterations; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, length);
            float sediment = 0.0f;

            for (int j = 0; j < 100; j++) // steps per droplet
            {
                int lowestNeighborX = x;
                int lowestNeighborY = y;
                float lowestHeight = erodedHeights[x, y];

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < length)
                        {
                            if (erodedHeights[nx, ny] < lowestHeight)
                            {
                                lowestHeight = erodedHeights[nx, ny];
                                lowestNeighborX = nx;
                                lowestNeighborY = ny;
                            }
                        }
                    }
                }

                if (lowestNeighborX == x && lowestNeighborY == y)
                {
                    erodedHeights[x, y] += sediment;
                    break;
                }

                float heightDifference = erodedHeights[x, y] - lowestHeight;

                float erodedAmount = Mathf.Min(heightDifference, erosionStrength);
                erodedHeights[x, y] -= erodedAmount;
                sediment += erodedAmount;

                if (sediment > sedimentCapacity)
                {
                    float depositAmount = sediment - sedimentCapacity;
                    erodedHeights[x, y] += depositAmount;
                    sediment -= depositAmount;
                }

                x = lowestNeighborX;
                y = lowestNeighborY;
            }

            // Yield every 1000 iterations to prevent freezing
            if (i % 1000 == 0)
                yield return null;
        }

        // Return the modified heightmap via callback
        onComplete(erodedHeights);
    }
}
