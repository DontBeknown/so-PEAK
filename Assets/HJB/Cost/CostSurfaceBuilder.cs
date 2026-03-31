using UnityEngine;

public class CostSurfaceBuilder : MonoBehaviour
{
    public HJBMeshDataProvider terrain;

    public float[,] baseSpeed;
    public float[,] baseCost;

    public float minSpeed = 0.05f;
    public float baseSpeedFlat = 5f;

    public void Build()
    {
        int w = terrain.width;
        int h = terrain.height;

        baseSpeed = new float[w, h];
        baseCost = new float[w, h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float slope =
                    terrain.slopeMap[x, y];

                float v =
                    baseSpeedFlat *
                    Mathf.Exp(-3.5f *
                    Mathf.Abs(slope + 0.05f));

                v = Mathf.Max(v, minSpeed);

                baseSpeed[x, y] = v;
                baseCost[x, y] = 1f / v;
            }
    }
}
