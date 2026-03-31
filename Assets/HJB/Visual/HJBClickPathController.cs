using System.Collections.Generic;
using UnityEngine;

public class HJBClickPathController : MonoBehaviour
{
    public Camera cam;

    public HJBMeshDataProvider provider;
    public HJBPathSolver solver;
    public HJBBacktracker backtracker;
    public HJBPathVisualizer visualizer;

    [Header("Markers")]
    public GameObject startMarkerPrefab;
    public GameObject goalMarkerPrefab;

    GameObject startMarker;
    GameObject goalMarker;

    Vector2Int? start = null;
    Vector2Int? goal = null;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetStart();
            TrySolvePath();
        }

        if (Input.GetMouseButtonDown(1))
        {
            SetGoal();
            TrySolvePath();
        }
    }

    void TrySolvePath()
    {
        if (start != null && goal != null)
        {
            Debug.Log("Solving path from ClickController...");
            solver.cost.Build(); // Make sure cost surface is built
            solver.startPos = start.Value;
            solver.Solve(goal.Value);
            var path = backtracker.BuildPath(start.Value, goal.Value);
        }
    }

    void SetStart()
    {
        Vector3 world;

        if (!RaycastTerrain(out world))
            return;

        Vector2Int g = provider.WorldToGrid(world);

        start = g;

        SpawnMarker(ref startMarker, startMarkerPrefab, world);

    }

    void SetGoal()
    {
        Vector3 world;

        if (!RaycastTerrain(out world))
            return;

        Vector2Int g = provider.WorldToGrid(world);

        goal = g;

        SpawnMarker(ref goalMarker, goalMarkerPrefab, world);

    }

    bool RaycastTerrain(out Vector3 world)
    {
        world = Vector3.zero;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            world = hit.point;
            return true;
        }

        return false;
    }

    void SpawnMarker(ref GameObject marker,
                     GameObject prefab,
                     Vector3 pos)
    {
        pos.y += 1f;

        if (marker == null)
        {
            marker = Instantiate(prefab, pos, Quaternion.identity);
        }
        else
        {
            marker.transform.position = pos;
        }
    }
}