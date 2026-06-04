using System.Collections.Generic;
using UnityEngine;

namespace HJB.Pathfind
{
    public class AStarPathSolver : MonoBehaviour
    {
        public HJBMeshDataProvider terrain;
        public CostSurfaceBuilder cost;

        public float step = 2f;
        public float fatigueRateTime = 0.12f;
        public float fatigueRateElev = 0.0005f;
        public float fatigueLimit = 1.0f;
        public float maxWalkableSlope = 0.8f;

        [Header("Risk Penalty")]
        public float c_risk = 0.03f;
        public float lambda = 1.0f; // Default penalty for fatigue

        public float heuristicMultiplier = 1f;

        Vector2[] directions;

        void Awake()
        {
            EnsureDirections();
        }

        void EnsureDirections()
        {
            if (directions != null)
            {
                return;
            }

            directions = new Vector2[16];
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        public List<Vector3> Solve(Vector2Int start, Vector2Int goal)
        {
            EnsureDirections();

            int w = terrain.width;
            int h = terrain.height;

            float[,] gCost = new float[w, h];
            float[,] fatigueMap = new float[w, h];
            Vector2Int[,] parent = new Vector2Int[w, h];
            bool[,] closedSet = new bool[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    gCost[x, y] = float.MaxValue;
                    fatigueMap[x, y] = 0f;
                    parent[x, y] = new Vector2Int(-1, -1);
                }
            }

            gCost[start.x, start.y] = 0f;
            fatigueMap[start.x, start.y] = 0f;

            // Simple Priority Queue
            List<Vector2Int> openSet = new List<Vector2Int> { start };
            Dictionary<Vector2Int, float> fCostMap = new Dictionary<Vector2Int, float>();
            fCostMap[start] = GetHeuristic(start, goal);

            int maxNodes = w * h;
            int evaluated = 0;

            while (openSet.Count > 0 && evaluated < maxNodes)
            {
                evaluated++;

                // Pop node with lowest fCost
                int lowestIndex = 0;
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fCostMap.GetValueOrDefault(openSet[i], float.MaxValue) < fCostMap.GetValueOrDefault(openSet[lowestIndex], float.MaxValue))
                    {
                        lowestIndex = i;
                    }
                }

                Vector2Int current = openSet[lowestIndex];
                openSet.RemoveAt(lowestIndex);

                if (Vector2Int.Distance(current, goal) <= step)
                {
                    parent[goal.x, goal.y] = current;
                    UnityEngine.Debug.Log($"[AStar] Path found! Evaluated {evaluated} nodes. Fatigue at goal: {fatigueMap[current.x, current.y]}");
                    return BuildPath(start, goal, parent);
                }

                closedSet[current.x, current.y] = true;

                float currentSlope = terrain.slopeMap[current.x, current.y];
                if (Mathf.Abs(currentSlope) > maxWalkableSlope) continue;

                foreach (Vector2 dir in directions)
                {
                    Vector2Int neighbor = current + Vector2Int.RoundToInt(dir * step);

                    if (neighbor.x < 0 || neighbor.x >= w || neighbor.y < 0 || neighbor.y >= h) continue;
                    if (closedSet[neighbor.x, neighbor.y]) continue;

                    float slope2 = terrain.slopeMap[neighbor.x, neighbor.y];
                    if (Mathf.Abs(slope2) > maxWalkableSlope) continue;

                    float speed2 = cost.baseSpeed[neighbor.x, neighbor.y];
                    float travelTime = step / speed2;

                    float currentFatigue = fatigueMap[current.x, current.y];
                    float newFatigue = currentFatigue + (fatigueRateTime * travelTime) + (fatigueRateElev * Mathf.Abs(slope2));

                    if (newFatigue > fatigueLimit)
                    {
                        travelTime += (newFatigue - fatigueLimit) * 5.0f; // Same punishment as HJB
                    }

                    // Cost Calculation
                    float risk_term = 0f;
                    if (terrain.worldDataManager != null && terrain.worldDataManager.expandedRiskMap != null)
                    {
                        if (neighbor.x < terrain.worldDataManager.expandedRiskMap.GetLength(0) &&
                            neighbor.y < terrain.worldDataManager.expandedRiskMap.GetLength(1))
                        {
                            risk_term = terrain.worldDataManager.expandedRiskMap[neighbor.x, neighbor.y] * c_risk;
                        }
                    }

                    float penalty_term = lambda * newFatigue;
                    float transitionCost = cost.baseCost[neighbor.x, neighbor.y] * travelTime + risk_term + penalty_term;

                    float tentativeGCost = gCost[current.x, current.y] + transitionCost;

                    if (tentativeGCost < gCost[neighbor.x, neighbor.y])
                    {
                        parent[neighbor.x, neighbor.y] = current;
                        gCost[neighbor.x, neighbor.y] = tentativeGCost;
                        fatigueMap[neighbor.x, neighbor.y] = newFatigue;

                        fCostMap[neighbor] = tentativeGCost + GetHeuristic(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            UnityEngine.Debug.LogWarning("[AStar] No path found.");
            return new List<Vector3>();
        }

        private float GetHeuristic(Vector2Int a, Vector2Int b)
        {
            return Vector2.Distance(a, b) * heuristicMultiplier; // Euclidean distance
        }

        private List<Vector3> BuildPath(Vector2Int start, Vector2Int goal, Vector2Int[,] parent)
        {
            List<Vector3> path = new List<Vector3>();
            Vector2Int current = goal;

            // Optional: Include goal explicitly if it doesn't align exactly with step logic
            path.Add(terrain.GridToWorld(goal.x, goal.y));

            int safety = 0;
            while (current != start && safety < 100000)
            {
                safety++;
                current = parent[current.x, current.y];
                if (current.x == -1 || current.y == -1) break;

                path.Add(terrain.GridToWorld(current.x, current.y));
            }

            path.Reverse(); // Start to Goal
            return path;
        }
    }
}
