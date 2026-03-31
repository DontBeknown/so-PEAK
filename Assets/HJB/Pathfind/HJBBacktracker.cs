using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HJBBacktracker : MonoBehaviour
{
    public HJBPathSolver solver;
    public HJBMeshDataProvider terrain;
    public HJBPathVisualizer visualizer;
    public List<Vector3> BuildPath(
    Vector2Int start,
    Vector2Int goal)
    {
        List<Vector3> path = new();

        Vector2Int p = start;
        path.Add(GridToWorld(p.x, p.y));

        Debug.Log($"[Backtracker] Starting at: {start}, Goal is: {goal}");
        Debug.Log($"[Backtracker] Initial Start T_Value: {solver.T[p.x, p.y]}");
        Debug.Log($"[Backtracker] Initial Goal T_Value: {solver.T[goal.x, goal.y]}");

        int safety = 0; // ป้องกัน infinite loop

        while (p != goal && safety < 10000)
        {
            safety++;

            Vector2Int best = p;
            float bestVal =
                solver.T[p.x, p.y];

            foreach (var dir in
                DirectionUtility.Directions16)
            {
                // ?? แก้ไข: ก่อนหน้านี้ + dir ธรรมดา ซึ่งมันขยับแค่ 1 ช่อง
                // ใน Solver เราใช้ระยะก้าว (step) ในการคำนวณ T 
                // ดังนั้นตอนแกะรอยกลับ (Backtrack) จะต้องคูณระยะ step ให้ตรงกันด้วย
                Vector2Int p2 =
                    p + Vector2Int.RoundToInt(dir * solver.step);

                if (!Inside(p2)) continue;

                float v =
                    solver.T[p2.x, p2.y];

                if (v < bestVal)
                {
                    bestVal = v;
                    best = p2;
                }
            }

            if (best == p) 
            {
                Debug.LogWarning($"[Backtracker] Stuck at {p}! No lower T_Value found around it. Current T: {bestVal}");
                break;
            }

            p = best;

            path.Add(GridToWorld(
                p.x, p.y));
            
            // เช็คว่าเข้าใกล้ Goal ในระยะก้าว (step) หรือยัง 
            // ถ้าใช่ ให้กระโดดเข้า Goal เลย เพื่อป้องกันการเด้งไปมา
            if (Vector2Int.Distance(p, goal) <= solver.step)
            {
                p = goal;
                path.Add(GridToWorld(p.x, p.y));
                break;
            }
        }
        visualizer.DrawPathWorld(path);
        Debug.Log($"[Backtracker] Finished! Path length: {path.Count}");
        return path;
    }

    bool Inside(Vector2Int p)
    {
        return p.x >= 0 && p.y >= 0 &&
               p.x < solver.T.GetLength(0) &&
               p.y < solver.T.GetLength(1);
    }

    public Vector3 GridToWorld(int x, int y)
    {
        return terrain.GridToWorld(x, y);
    }
}
