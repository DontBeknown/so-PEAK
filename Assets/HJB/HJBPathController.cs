using UnityEngine;

public class HJBPathController : MonoBehaviour
{
    public HJBPathSolver solver;
    public HJBBacktracker backtracker;
    public HJBPathVisualizer visualizer;

    public Vector2Int start;
    public Vector2Int goal;

    public CostSurfaceBuilder cost;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Solving...");

            cost.Build();          // 🔥 สร้าง cost หลัง terrain ready
            solver.startPos = start;
            solver.Solve(goal);

            Debug.Log($"Start: {start}, Goal: {goal}");
            Debug.Log($"Map size: {solver.T.GetLength(0)} x {solver.T.GetLength(1)}");
            Debug.Log("T goal: " + solver.T[goal.x, goal.y]);
            Debug.Log("T start: " + solver.T[start.x, start.y]);

            var path =
                backtracker.BuildPath(start, goal);

            Debug.Log("Path length: " + path.Count);

            if (path.Count == 0)
            {
                Debug.LogError("Path NOT found!");
                return;
            }

            Debug.Log("Last node: " + path[path.Count - 1]);

            visualizer.DrawPathWorld(path);
        }
    }
}