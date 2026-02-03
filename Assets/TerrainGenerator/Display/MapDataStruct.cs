using UnityEngine;

// This file has no class around it, so these are global!
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[,] colourMap;

    public MapData(float[,] heightMap, Color[,] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}