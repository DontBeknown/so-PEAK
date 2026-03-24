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
//Tree
public struct TreeInstance
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

public struct PlacedObject
{
    public GameObject Prefab;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public float BoundingRadius;
    public string SpawnId;

    // --- NEW: Pass the flags to the final object ---
    public bool IsTerrainTree;
    public int TreePrototypeIndex;
}