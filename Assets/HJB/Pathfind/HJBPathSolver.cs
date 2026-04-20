using UnityEngine;

public class HJBPathSolver : MonoBehaviour
{
    public HJBMeshDataProvider terrain;
    public CostSurfaceBuilder cost;

    public float[,] T;
    public float[,] fatigue;

    public float step = 15f;
    public float tolerance = 0.5f;   // relaxed: T values are in hundreds of seconds, 0.5s precision is sufficient
    public int maxIter = 50;         // FSM converges in a handful of quad-sweeps

    public float fatigueRateTime = 0.12f;
    public float fatigueRateElev = 0.0005f;
    public float fatigueLimit = 1.0f;

    public float maxWalkableSlope = 0.8f;

    const float INF = 1e15f;
    int w, h;

    public Vector2Int startPos;

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

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                T[x, y] = INF;
                fatigue[x, y] = 0f;
            }

        T[goal.x, goal.y] = 0f;
        fatigue[goal.x, goal.y] = 0f;

        // Fast Sweeping Method: 4 alternating directional sweeps per iteration.
        // Each sweep propagates information optimally in one quadrant direction,
        // so convergence typically occurs in 2-8 iterations instead of thousands.
        for (int iter = 0; iter < maxIter; iter++)
        {
            float maxDiff = 0f;
            maxDiff = Mathf.Max(maxDiff, Sweep(goal, 0,     w,  1, 0,     h,  1));
            maxDiff = Mathf.Max(maxDiff, Sweep(goal, 0,     w,  1, h - 1, -1, -1));
            maxDiff = Mathf.Max(maxDiff, Sweep(goal, w - 1, -1, -1, 0,    h,  1));
            maxDiff = Mathf.Max(maxDiff, Sweep(goal, w - 1, -1, -1, h - 1, -1, -1));

            if (maxDiff < tolerance)
            {
                UnityEngine.Debug.Log($"[HJB] Converged at iteration {iter + 1} (maxDiff={maxDiff:F4})");
                return;
            }
        }

        UnityEngine.Debug.LogWarning($"[HJB] Reached maxIter={maxIter} without full convergence.");
    }

    float Sweep(Vector2Int goal, int xStart, int xEnd, int xStep, int yStart, int yEnd, int yStep)
    {
        float maxDiff = 0f;
        for (int x = xStart; x != xEnd; x += xStep)
        {
            for (int y = yStart; y != yEnd; y += yStep)
            {
                if (x == goal.x && y == goal.y) continue;

                float oldT = T[x, y];
                UpdateCell(x, y);

                float diff = oldT - T[x, y]; // T only decreases; diff > 0 when a cell improves
                if (diff > maxDiff) maxDiff = diff;
            }
        }
        return maxDiff;
    }

    void UpdateCell(int x, int y)
    {
        // Cache current-cell slope once — avoids 16 redundant array reads per cell
        float slopeCurrent = terrain.slopeMap[x, y];
        if (Mathf.Abs(slopeCurrent) > maxWalkableSlope) return;

        float bestT = T[x, y];
        float bestFatigue = fatigue[x, y];

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 dir = directions[i];

            float x2 = x + dir.x * step;
            float y2 = y + dir.y * step;

            if (x2 < 0f || x2 >= w - 1 || y2 < 0f || y2 >= h - 1) continue;

            int ix = Mathf.RoundToInt(x2);
            int iy = Mathf.RoundToInt(y2);

            float T2 = T[ix, iy];
            if (T2 >= INF) continue;

            float slope2 = terrain.slopeMap[ix, iy];
            if (Mathf.Abs(slope2) > maxWalkableSlope) continue;

            float speed2 = cost.baseSpeed[ix, iy];
            float travelTime = step / speed2;

            float fLocal = fatigue[ix, iy] + (fatigueRateTime * travelTime) + (fatigueRateElev * Mathf.Abs(slope2));

            if (fLocal > fatigueLimit)
                travelTime += (fLocal - fatigueLimit) * 5.0f;

            float cLocal = cost.baseCost[ix, iy];
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
