using UnityEngine;

public class HJBPathController : MonoBehaviour
{
    public HJBPathSolver solver;
    public HJBBacktracker backtracker;
    public HJBPathVisualizer visualizer;

    public Vector2Int start;
    public Vector2Int goal;

    public CostSurfaceBuilder cost;
}