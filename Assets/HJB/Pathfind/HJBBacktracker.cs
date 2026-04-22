using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HJBBacktracker : MonoBehaviour
{
    public HJBPathSolver solver;
    public HJBMeshDataProvider terrain;
    public HJBPathVisualizer visualizer;
    [Header("Debug")]
    public bool enableDebugLog;
    public List<Vector3> BuildPath(
    Vector2Int start,
    Vector2Int goal)
    {
        List<Vector3> path = new();

        Vector2Int p = start;
        path.Add(GridToWorld(p.x, p.y));

        if (enableDebugLog) Debug.Log($"[Backtracker] Starting at: {start}, Goal is: {goal}");
        if (enableDebugLog) Debug.Log($"[Backtracker] Initial Start T_Value: {solver.T[p.x, p.y]}");
        if (enableDebugLog) Debug.Log($"[Backtracker] Initial Goal T_Value: {solver.T[goal.x, goal.y]}");

        int safety = 0; // ��ͧ�ѹ infinite loop

        while (p != goal && safety < 10000)
        {
            safety++;

            Vector2Int best = p;
            float bestVal =
                solver.T[p.x, p.y];

            foreach (var dir in
                DirectionUtility.Directions16)
            {
                // ?? ���: ��͹˹�ҹ�� + dir ������ ����ѹ��Ѻ�� 1 ��ͧ
                // � Solver ��������С��� (step) 㹡�äӹǳ T 
                // �ѧ��鹵͹����¡�Ѻ (Backtrack) �е�ͧ�ٳ���� step ���ç�ѹ����
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
            
            // ����������� Goal ����С��� (step) �����ѧ 
            // ����� �����ⴴ��� Goal ��� ���ͻ�ͧ�ѹ��������
            if (Vector2Int.Distance(p, goal) <= solver.step)
            {
                p = goal;
                path.Add(GridToWorld(p.x, p.y));
                break;
            }
        }
        
        if (enableDebugLog) Debug.Log($"[Backtracker] Finished! Path length: {path.Count}");
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
