using UnityEngine;

// 1. Define the Enum right here, above the class, so your whole game knows what a NoiseType is!
public enum NoiseType { None, TreeNoise, PuddleNoise, OreNoise }
public enum RoadSpawnRule { AvoidRoads, AllowEverywhere, OnlyOnRoads }

[CreateAssetMenu(fileName = "New Spawn Config", menuName = "World Generation/Spawn Config")]
public class SpawnConfig : ScriptableObject
{
    [Header("Basic Setup")]
    public string ObjectName;
    public GameObject Prefab;

    [Header("Rendering Type")]
    [Tooltip("Check this ONLY if using Unity's built-in Terrain system")]
    public bool IsTerrainTree = false;
    public int TreePrototypeIndex = 0;

    [Header("Dimensions")]
    public float BaseRadius = 1.0f;
    public float BaseHeight = 1.0f;
    public float MinScale = 0.8f;
    public float MaxScale = 1.2f;

    [Header("Placement Rules")]
    public Vector3 BaseRotation = Vector3.zero; // NEW: Tells the spawner if the prefab needs flipping
    public Vector3 PositionOffset = Vector3.zero; // NEW: Full XYZ control
    // 2. Add the new Dropdown variable here!
    public RoadSpawnRule RoadRule = RoadSpawnRule.AvoidRoads; // Defaults to avoiding roads
    public float MinSpacing = 5f;
    public float Density = 1.0f;
    public float MaxSlopeDiff = 0.5f;

    [Header("Optional Noise Mapping")]
    // 2. We replaced the old 'UseNoiseMap' boolean with your awesome new Enum dropdown!
    public NoiseType RequiredNoiseMap = NoiseType.None;
    public float NoiseThreshold = 0.5f;
}