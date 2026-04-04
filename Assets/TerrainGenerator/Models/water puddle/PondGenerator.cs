using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct PuddleSubEllipse
{
    public Vector2 offset;
    public Vector2 radii;
    public float rotationAngle;
}

public class PondGenerator : MonoBehaviour
{
    // --- SCRIPT CONFIGURATION ---
    public int targetPondCount = 20;
    public int maxSpawnAttempts = 3000;
    public float minScale = 3.0f;
    public float maxScale = 6.0f;
    public float minDepth = 0.5f;
    public float maxDepth = 1.5f;

    [Tooltip("How much height variance is allowed under the pond. 3.0 allows for gentle slopes.")]
    public float maxAllowedSlope = 3.0f;

    [Header("Spawn Restrictions")]
    [Tooltip("If false, ponds will avoid roads. If true, they can spawn right over them.")]
    public bool allowSpawningOnRoads = false;
    [Tooltip("If true, ponds will not spawn if they overlap with an existing pond.")]
    public bool preventPondOverlap = true; // --- NEW FEATURE ---

    [Header("Water Settings")]
    public Material waterMaterial;
    [Range(0.1f, 1.0f)]
    public float waterFillLevel = 0.8f;

    [Header("Debugging")]
    public bool spawnDebugMarkers = true;

    private List<List<PuddleSubEllipse>> pondDatabase;

    // --- YOUR EXTRACTED BLENDER DATA ---
    private List<PuddleSubEllipse> myFirstPuddle = new List<PuddleSubEllipse> {
        new PuddleSubEllipse { offset = new Vector2(-0.44121f, -0.6373f), radii = new Vector2(1.890f, 1.159f), rotationAngle = -16.505f },
        new PuddleSubEllipse { offset = new Vector2(0.68633f, 0.5556f), radii = new Vector2(1.995f, 1.422f), rotationAngle = -28.338f },
        new PuddleSubEllipse { offset = new Vector2(0.17975f, 0f), radii = new Vector2(1.683f, 1.032f), rotationAngle = -16.505f }
    };

    private List<PuddleSubEllipse> mySecondPuddle = new List<PuddleSubEllipse> {
        new PuddleSubEllipse { offset = new Vector2(-0.16341f, 0.040853f), radii = new Vector2(1.938f, 0.840f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(-0.32682f, 0.65364f), radii = new Vector2(1.407f, 1.407f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(0.926f, 0.463f), radii = new Vector2(1.108f, 1.108f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(-1.2392f, 0.16341f), radii = new Vector2(0.910f, 0.910f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(1.4162f, 0.068088f), radii = new Vector2(0.910f, 0.910f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(1.5524f, -0.6945f), radii = new Vector2(0.659f, 0.659f), rotationAngle = 0f }
    };

    private List<PuddleSubEllipse> myThirdPuddle = new List<PuddleSubEllipse> {
        new PuddleSubEllipse { offset = new Vector2(-0.20426f, 0.50385f), radii = new Vector2(1.588f, 1.588f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(-1.1711f, -0.28597f), radii = new Vector2(0.864f, 1.057f), rotationAngle = 23.064f },
        new PuddleSubEllipse { offset = new Vector2(0.83067f, -0.013618f), radii = new Vector2(1.607f, 1.267f), rotationAngle = -13.941f }
    };

    private List<PuddleSubEllipse> myFourthPuddle = new List<PuddleSubEllipse> {
        new PuddleSubEllipse { offset = new Vector2(-0.42487f, 0.5556f), radii = new Vector2(1.692f, 1.692f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(-0.76803f, -0.78437f), radii = new Vector2(1.203f, 0.928f), rotationAngle = -17.769f },
        new PuddleSubEllipse { offset = new Vector2(0.76803f, 0.9151f), radii = new Vector2(1.566f, 1.354f), rotationAngle = 0f },
        new PuddleSubEllipse { offset = new Vector2(-0.065364f, 0.8334f), radii = new Vector2(1.540f, 1.540f), rotationAngle = 0f }
    };

    void Awake()
    {
        pondDatabase = new List<List<PuddleSubEllipse>> {
            myFirstPuddle, mySecondPuddle, myThirdPuddle, myFourthPuddle
        };
    }

    public void MassSpawnPonds(float[,] heightmap, float[,] roadMask, bool[,] waterMask, int globalSeed, float terrainMaxHeight, int bufferOffset)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Pond_Water_Skirted" || child.name == "Pond Debug Marker")
            {
                Destroy(child.gameObject);
            }
        }

        Random.InitState(globalSeed);
        int gridWidth = heightmap.GetLength(0);
        int gridHeight = heightmap.GetLength(1);
        int maxSafeRadius = Mathf.CeilToInt(3f * maxScale) + 4;

        int pondsSuccessfullySpawned = 0;
        int attempts = 0;

        while (pondsSuccessfullySpawned < targetPondCount && attempts < maxSpawnAttempts)
        {
            attempts++;
            int randomX = Random.Range(maxSafeRadius, gridWidth - maxSafeRadius);
            int randomZ = Random.Range(maxSafeRadius, gridHeight - maxSafeRadius);
            int randomShapeIndex = Random.Range(0, pondDatabase.Count);
            List<PuddleSubEllipse> selectedShape = pondDatabase[randomShapeIndex];

            if (TryGeneratePond(heightmap, roadMask, waterMask, randomX, randomZ, selectedShape, Random.Range(0, 999999), terrainMaxHeight, bufferOffset))
            {
                pondsSuccessfullySpawned++;
            }
        }

        if (attempts >= maxSpawnAttempts) Debug.LogWarning($"[PondGenerator] Hit limit ({maxSpawnAttempts}). Spawned {pondsSuccessfullySpawned}/{targetPondCount}.");
        else Debug.Log($"[PondGenerator] Successfully spawned {pondsSuccessfullySpawned} ponds in {attempts} attempts.");
    }

    private bool TryGeneratePond(float[,] heightmap, float[,] roadMask, bool[,] waterMask, int centerX, int centerZ, List<PuddleSubEllipse> shapeData, int seed, float terrainMaxHeight, int bufferOffset)
    {
        Random.InitState(seed);
        float currentScale = Random.Range(minScale, maxScale);
        float currentDepth = Random.Range(minDepth, maxDepth);
        float normalizedDepth = currentDepth / terrainMaxHeight;
        int searchRadius = Mathf.CeilToInt(3f * currentScale) + 2;

        int minX = Mathf.Max(0, centerX - searchRadius);
        int maxX = Mathf.Min(heightmap.GetLength(0) - 1, centerX + searchRadius);
        int minZ = Mathf.Max(0, centerZ - searchRadius);
        int maxZ = Mathf.Min(heightmap.GetLength(1) - 1, centerZ + searchRadius);

        // --- 1. OVERLAP & ROAD CHECK ---
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // Reject if on road
                if (!allowSpawningOnRoads && roadMask[x, z] < 0.25f) return false;

                // --- THE FIX: Reject if another pond is already here ---
                if (preventPondOverlap && waterMask[x, z]) return false;
            }
        }

        // 2. HEIGHT & SLOPE CHECK
        float minH = 1f; float maxH = 0f;
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float h = heightmap[x, z];
                if (h < minH) minH = h;
                if (h > maxH) maxH = h;
            }
        }

        if ((maxH - minH) * terrainMaxHeight > maxAllowedSlope) return false;
        if (minH * terrainMaxHeight <= 2.0f) return false;

        // 3. DIG THE HOLE
        float targetHeight = minH - normalizedDepth;
        int arraySize = (searchRadius * 2) + 1;
        bool[,] localHole = new bool[arraySize, arraySize];

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 localPoint = new Vector2(x - centerX, z - centerZ);
                if (IsPointInsideScaledPuddle(localPoint, shapeData, currentScale))
                {
                    heightmap[x, z] = targetHeight;
                    localHole[x - centerX + searchRadius, z - centerZ + searchRadius] = true;
                }
            }
        }

        // 4. SMART SKIRT & MASK DILATION
        float normalizedWaterH = Mathf.Lerp(targetHeight, minH, waterFillLevel);
        float waterSurfaceY = normalizedWaterH * terrainMaxHeight;
        bool[,] meshSkirt = new bool[arraySize, arraySize];
        int skirtPadding = 2;

        for (int z = 0; z < arraySize; z++)
        {
            for (int x = 0; x < arraySize; x++)
            {
                if (localHole[x, z])
                {
                    for (int sz = -skirtPadding; sz <= skirtPadding; sz++)
                    {
                        for (int sx = -skirtPadding; sx <= skirtPadding; sx++)
                        {
                            int nx = x + sx; int nz = z + sz;
                            if (nx >= 0 && nx < arraySize && nz >= 0 && nz < arraySize)
                            {
                                int worldX = minX + nx; int worldZ = minZ + nz;
                                if (localHole[nx, nz] || heightmap[worldX, worldZ] > normalizedWaterH)
                                {
                                    meshSkirt[nx, nz] = true;
                                    waterMask[worldX, worldZ] = true; // Mark mask for both Trees AND other Ponds
                                }
                            }
                        }
                    }
                }
            }
        }

        if (waterMaterial != null) SpawnSkirtedWaterMesh(centerX + bufferOffset, waterSurfaceY, centerZ + bufferOffset, meshSkirt, searchRadius);
        if (spawnDebugMarkers) SpawnMarker(centerX + bufferOffset, waterSurfaceY, centerZ + bufferOffset);

        return true;
    }

    private void SpawnSkirtedWaterMesh(float worldCenterX, float worldY, float worldCenterZ, bool[,] skirtMask, int searchRadius)
    {
        GameObject waterObj = new GameObject("Pond_Water_Skirted");
        waterObj.transform.parent = this.transform;
        waterObj.transform.position = new Vector3(worldCenterX, worldY, worldCenterZ);

        MeshFilter mf = waterObj.AddComponent<MeshFilter>();
        MeshRenderer mr = waterObj.AddComponent<MeshRenderer>();
        if (waterMaterial != null) mr.material = waterMaterial;

        int gridSize = searchRadius * 2;
        Vector3[] vertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        List<int> triangles = new List<int>();

        for (int z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                int i = z * (gridSize + 1) + x;
                float localX = x - searchRadius; float localZ = z - searchRadius;
                vertices[i] = new Vector3(localX, 0, localZ);
                uvs[i] = new Vector2(localX / 10f, localZ / 10f);
            }
        }

        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (skirtMask[x, z])
                {
                    int v0 = z * (gridSize + 1) + x;
                    int v1 = (z + 1) * (gridSize + 1) + x;
                    int v2 = (z + 1) * (gridSize + 1) + x + 1;
                    int v3 = z * (gridSize + 1) + x + 1;
                    triangles.Add(v0); triangles.Add(v1); triangles.Add(v2);
                    triangles.Add(v0); triangles.Add(v2); triangles.Add(v3);
                }
            }
        }

        Mesh mesh = new Mesh { vertices = vertices, triangles = triangles.ToArray(), uv = uvs };
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    private bool IsPointInsideScaledPuddle(Vector2 localPoint, List<PuddleSubEllipse> shapeData, float scale)
    {
        foreach (var ellipse in shapeData)
        {
            float translatedX = localPoint.x - (ellipse.offset.x * scale);
            float translatedY = localPoint.y - (ellipse.offset.y * scale);
            float radAngle = -ellipse.rotationAngle * Mathf.Deg2Rad;
            float rotatedX = translatedX * Mathf.Cos(radAngle) - translatedY * Mathf.Sin(radAngle);
            float rotatedY = translatedX * Mathf.Sin(radAngle) + translatedY * Mathf.Cos(radAngle);
            if (Mathf.Pow(rotatedX / (ellipse.radii.x * scale), 2) + Mathf.Pow(rotatedY / (ellipse.radii.y * scale), 2) <= 1f) return true;
        }
        return false;
    }

    private void SpawnMarker(float worldX, float worldY, float worldZ)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.transform.position = new Vector3(worldX, worldY, worldZ);
        marker.transform.localScale = new Vector3(1f, 10f, 1f);
        marker.name = "Pond Debug Marker";
        Destroy(marker.GetComponent<Collider>());
        marker.GetComponent<Renderer>().material.color = Color.red;
    }
}