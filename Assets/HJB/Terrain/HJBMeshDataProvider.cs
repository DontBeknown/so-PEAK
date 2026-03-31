using System.Collections;
using UnityEngine;

public class HJBMeshDataProvider : MonoBehaviour
{
    public RenderController renderController;
    public WorldDataManager worldDataManager;

    public float[,] heightMap;
    public float[,] slopeMap;

    public float heightMultiplier = 10f;

    public int width;
    public int height;

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
        slopeMap =
            new float[width, height];

        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                float hL = heightMap[x - 1, y] * heightMultiplier;
                float hR = heightMap[x + 1, y] * heightMultiplier;
                float hD = heightMap[x, y - 1] * heightMultiplier;
                float hU = heightMap[x, y + 1] * heightMultiplier;
    
                float dx = (hR - hL) * 0.5f;
                float dy = (hU - hD) * 0.5f;

                slopeMap[x, y] =
                    Mathf.Sqrt(dx * dx + dy * dy);
            }
    }
    public Vector3 GridToWorld(int x, int y)
    {
        float h = heightMap[x, y] * heightMultiplier;

        float chunkSize = 41f; // ต้องตรงกับ RenderController
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
        Gizmos.DrawSphere(
            GridToWorld(0, 0), 3f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(
            GridToWorld(width / 2, height / 2), 3f);

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
