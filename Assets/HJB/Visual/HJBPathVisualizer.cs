using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HJBPathVisualizer : MonoBehaviour
{
    public HJBMeshDataProvider provider;
    public HJBBacktracker backtracker;

    LineRenderer line;

    [Header("Style")]
    public float lineWidth = 2f;
    public Color lineColor = Color.red;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.material =
            new Material(
                Shader.Find("Sprites/Default"));

        line.startColor = lineColor;
        line.endColor = lineColor;

        line.positionCount = 0;
    }

    public void DrawPath(List<Vector2Int> gridPath)
    {
        if (gridPath == null ||
            gridPath.Count == 0)
            return;

        line.positionCount =
            gridPath.Count;

        for (int i = 0; i < gridPath.Count; i++)
        {
            Vector2Int g = gridPath[i];

            Vector3 world =
                provider.GridToWorld(
                    g.x, g.y);
            world.y += 1.0f;
            line.SetPosition(i, world);
        }
    }

    public void DrawPathWorld(List<Vector3> worldPath)
    {
        if (worldPath == null ||
            worldPath.Count == 0)
            return;

        line.positionCount =
            worldPath.Count;

        for (int i = 0; i < worldPath.Count; i++)
        {
            line.SetPosition(i, worldPath[i]);
        }
    }

    public void Clear()
    {
        line.positionCount = 0;
    }
}
