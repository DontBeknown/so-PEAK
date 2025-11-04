using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour
{

    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool usePowerMode = false;
    public bool autoUpdate;

    public float[,] noiseMap;

    public void GenerateMap()
    {
        noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        //Continentalness case: pow2 everything
        if (usePowerMode)
        {


            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    float value = (noiseMap[x, y] + 1f) / 2f;
                    value = Mathf.Pow(value, 0.45f);
                    noiseMap[x, y] = value * 2f - 1f;
                }
            }
        }

        MapDisplay display = GetComponent<MapDisplay>();
        display.DrawNoiseMap(noiseMap, false);
    }

    void OnDisable()
    {
        MapDisplay display = GetComponent<MapDisplay>();
        if (display != null && display.textureRender != null)
        {
            display.textureRender.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        MapDisplay display = GetComponent<MapDisplay>();
        if (display != null && display.textureRender != null)
        {
            display.textureRender.gameObject.SetActive(true);
        }
    }


    void OnValidate()
    {
        if (mapWidth < 1)
        {
            mapWidth = 1;
        }
        if (mapHeight < 1)
        {
            mapHeight = 1;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
    }

}