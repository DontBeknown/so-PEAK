using UnityEngine;

public class HJBPathSolver : MonoBehaviour
{
    public HJBMeshDataProvider terrain;
    public CostSurfaceBuilder cost;

    public float[,] T;
    public float[,] fatigue;

    public float step = 15f;
    public float tolerance = 1e-3f;
    public int maxIter = 20000;

    public float fatigueRateTime = 0.12f;
    public float fatigueRateElev = 0.0005f;
    public float fatigueLimit = 1.0f;

    const float INF = 1e15f;
    int w, h;

    public Vector2Int startPos;

    // 16 Directions (M=16)
    Vector2[] directions;

    void Awake()
    {
        directions = new Vector2[16];
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    public void Solve(Vector2Int goal)
    {
        w = terrain.width;
        h = terrain.height;

        T = new float[w, h];
        fatigue = new float[w, h];

        // Initialize to infinity
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                T[x, y] = INF;
                fatigue[x, y] = 0f;
            }
        }

        // Goal conditions
        T[goal.x, goal.y] = 0f;
        fatigue[goal.x, goal.y] = 0f;

        // Main Loop (Gauss-Seidel Sweep)
        for (int iter = 0; iter < maxIter; iter++)
        {
            float maxDiff = 0f;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (x == goal.x && y == goal.y) continue;

                    float oldT = T[x, y];

                    UpdateCell(x, y);

                    float diff = Mathf.Abs(T[x, y] - oldT);
                    if (diff > maxDiff)
                    {
                        maxDiff = diff;
                    }
                }
            }

            if (maxDiff < tolerance)
            {
                Debug.Log($"Converged at iteration {iter}");
                break;
            }
        }
    }

    void UpdateCell(int x, int y)
    {
        float bestT = T[x, y];
        float bestFatigue = fatigue[x, y];

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 dir = directions[i];

            float x2 = x + dir.x * step;
            float y2 = y + dir.y * step;

            // Bounds check
            if (x2 < 0 || x2 >= w - 1 || y2 < 0 || y2 >= h - 1) continue;

            // Nearest integer to sample grid values (Simple Interpolation)
            int ix = Mathf.RoundToInt(x2);
            int iy = Mathf.RoundToInt(y2);

            float T2 = T[ix, iy];
            if (T2 >= INF) continue;

            float slope2 = terrain.slopeMap[ix, iy];
            float speed2 = cost.baseSpeed[ix, iy];
            float travelTime = step / speed2;

            // Fatigue Increase (fatigue_rate_time * time_step + fatigue_rate_elev * abs(slope))
            float fLocal = fatigue[ix, iy] + (fatigueRateTime * travelTime) + (fatigueRateElev * Mathf.Abs(slope2));

            // Soft punishment if tired
            if (fLocal > fatigueLimit)
            {
                travelTime += (fLocal - fatigueLimit) * 5.0f;
            }

            // LocalCost = base_cost
            float cLocal = cost.baseCost[ix, iy];

            // Semi-Lagrangian candidate
            float candidate = cLocal * travelTime + T2;

            if (candidate < bestT)
            {
                bestT = candidate;
                bestFatigue = fLocal;
            }
        }

        T[x, y] = bestT;
        fatigue[x, y] = bestFatigue;
    }
}