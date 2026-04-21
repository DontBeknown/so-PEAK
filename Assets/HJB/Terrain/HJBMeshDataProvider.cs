using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelTerrainSnapshot
{
    public float[,]   heightMap;
    public float[,]   slopeMap;
    public float      heightMultiplier;
    public int        width;
    public int        height;
    public Vector2Int pathGoal;   // mainPeak (buffered)
    public Vector2Int pathStart;  // spawnCoord + bufferLength/2, clamped
}

public class HJBMeshDataProvider : MonoBehaviour
{
    public RenderController renderController;
    public WorldDataManager worldDataManager;

    public float[,] heightMap;
    public float[,] slopeMap;

    public float heightMultiplier = 10f;

    public int width;
    public int height;

    public Dictionary<WorldLevel, LevelTerrainSnapshot> levelSnapshots = new Dictionary<WorldLevel, LevelTerrainSnapshot>();

    IEnumerator Start()
    {
        // Wait until WorldDataManager finishes generating the terrain
        yield return new WaitUntil(
            () => worldDataManager != null && worldDataManager.globalHeightMap != null);

        heightMap = worldDataManager.globalHeightMap;

        // Fetch the correct dynamic height multiplier from the noise generator
        if (worldDataManager.activeGen != null)
        {
            heightMultiplier = worldDataManager.activeGen.meshHeightMultiplier;
        }

        width = heightMap.GetLength(0);
        height = heightMap.GetLength(1);

        ComputeSlope();
    }

    void ComputeSlope()
    {
        slopeMap = new float[width, height];

        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                float hL = heightMap[x - 1, y] * heightMultiplier;
                float hR = heightMap[x + 1, y] * heightMultiplier;
                float hD = heightMap[x, y - 1] * heightMultiplier;
                float hU = heightMap[x, y + 1] * heightMultiplier;

                float dx = (hR - hL) * 0.5f;
                float dy = (hU - hD) * 0.5f;

                slopeMap[x, y] = Mathf.Sqrt(dx * dx + dy * dy);
            }
    }

    private readonly object _snapshotLock = new object();

    private static float[,] ComputeSlopeFor(float[,] hMap, float mult, int w, int h)
    {
        var slope = new float[w, h];
        System.Threading.Tasks.Parallel.For(1, w - 1, x =>
        {
            for (int y = 1; y < h - 1; y++)
            {
                float hL = hMap[x - 1, y] * mult;
                float hR = hMap[x + 1, y] * mult;
                float hD = hMap[x, y - 1] * mult;
                float hU = hMap[x, y + 1] * mult;
                float dx = (hR - hL) * 0.5f;
                float dy = (hU - hD) * 0.5f;
                slope[x, y] = Mathf.Sqrt(dx * dx + dy * dy);
            }
        });
        return slope;
    }

    private void BuildSnapshotOffThread(WorldLevel level)
    {
        WorldLevelProfile profile = worldDataManager.levelProfiles.Find(p =>
            p.levelName.Equals(level.ToString(), System.StringComparison.OrdinalIgnoreCase));
        if (profile?.generator == null)
        {
            Debug.LogError($"[HJBMeshDataProvider] Missing profile/generator for {level}, skipping snapshot.");
            return;
        }

        Debug.Log($"[HJBMeshDataProvider] Building snapshot for {level}...");

        if (profile.generator.completeMap == null)
            profile.generator.TerrainDrawingForPath(worldDataManager.GetSeedForLevel(level));

        int w      = profile.generator.completeMap.GetLength(0);
        int h      = profile.generator.completeMap.GetLength(1);
        float mult = profile.generator.meshHeightMultiplier;
        int bufLen = profile.generator.bufferLength / 2;

        var snapshot = new LevelTerrainSnapshot
        {
            heightMap        = profile.generator.completeMap,
            slopeMap         = ComputeSlopeFor(profile.generator.completeMap, mult, w, h),
            heightMultiplier = mult,
            width            = w,
            height           = h,
            pathGoal         = profile.generator.mainPeak,
            pathStart        = new Vector2Int(
                Mathf.Clamp(profile.generator.spawnCoord.x + bufLen, 0, w - 1),
                Mathf.Clamp(profile.generator.spawnCoord.y + bufLen, 0, h - 1)
            )
        };

        lock (_snapshotLock)
        {
            levelSnapshots[level] = snapshot;
        }
    }

    public void PreloadSnapshotForLevel(WorldLevel level)
    {
        if (levelSnapshots.ContainsKey(level))
        {
            Debug.Log($"[HJBMeshDataProvider] Snapshot already exists for {level}, skipping.");
            return;
        }

        if (level == worldDataManager.currentLevel)
        {
            int buf = worldDataManager.activeGen.bufferLength / 2;
            levelSnapshots[level] = new LevelTerrainSnapshot
            {
                heightMap        = worldDataManager.globalHeightMap,
                slopeMap         = this.slopeMap,
                heightMultiplier = worldDataManager.activeGen.meshHeightMultiplier,
                width            = this.width,
                height           = this.height,
                pathGoal         = worldDataManager.activeGen.mainPeak,
                pathStart        = new Vector2Int(
                    Mathf.Clamp(worldDataManager.activeGen.spawnCoord.x + buf, 0, this.width  - 1),
                    Mathf.Clamp(worldDataManager.activeGen.spawnCoord.y + buf, 0, this.height - 1)
                )
            };
            Debug.Log($"[HJBMeshDataProvider] Snapshot ready for {level} (current level).");
            return;
        }

        BuildSnapshotOffThread(level);
    }

    public void PreloadLevelSnapshots()
    {
        foreach (WorldLevel level in System.Enum.GetValues(typeof(WorldLevel)))
            PreloadSnapshotForLevel(level);
        Debug.Log("[HJBMeshDataProvider] All level snapshots preloaded.");
    }

    public void ApplySnapshot(WorldLevel level)
    {
        if (!levelSnapshots.TryGetValue(level, out var s))
        {
            Debug.LogError($"[HJBMeshDataProvider] Cannot apply snapshot for {level} — not found.");
            return;
        }
        heightMap        = s.heightMap;
        slopeMap         = s.slopeMap;
        heightMultiplier = s.heightMultiplier;
        width            = s.width;
        height           = s.height;
    }

    public void RestoreCurrentLevelData()
    {
        if (worldDataManager.globalHeightMap == null) return;
        heightMap = worldDataManager.globalHeightMap;
        if (worldDataManager.activeGen != null)
            heightMultiplier = worldDataManager.activeGen.meshHeightMultiplier;
        width  = heightMap.GetLength(0);
        height = heightMap.GetLength(1);
        ComputeSlope();
    }

    public Vector3 GridToWorld(int x, int y)
    {
        float h = heightMap[x, y] * heightMultiplier;

        //float chunkSize = 41f; // ถูกต้องกับ RenderController
        float worldX = x;
        float worldZ = y;

        return new Vector3(worldX, h, worldZ);
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        int x = Mathf.RoundToInt(world.x);
        int y = Mathf.RoundToInt(world.z);

        return new Vector2Int(x, y);
    }

    void OnDrawGizmos()
    {
        if (heightMap == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GridToWorld(0, 0), 3f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GridToWorld(width / 2, height / 2), 3f);

        for (int x = 0; x < width; x += 10)
        {
            for (int y = 0; y < height; y += 10)
            {
                Vector3 p = GridToWorld(x, y);
                Gizmos.DrawSphere(p, 0.3f);
            }
        }
    }
}
