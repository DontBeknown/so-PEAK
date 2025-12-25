using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RoadCarver
{
    //Need to gen road ridge somewhere else
    //also use height map from generate mountain steps

    public static void CarveRoad(float[,] depthMap, float[,] roadRidge, List<Vector2Int> peakPoint, float maxHeight, AnimationCurve roadHeightCurve)
    {

        Vector2Int peak = GetPeakCoordinate(peakPoint);
        Vector2Int closestRoad = GetClosestRoadPoint(peak, roadRidge);
        List<Vector2Int> line = GetLine(peak, closestRoad);
        CarveRoad(line, roadRidge);
        ApplyErosionGradient(depthMap, roadRidge, peak, maxHeight, roadHeightCurve);
        

    }
    public static Vector2Int GetPeakCoordinate(List<Vector2Int> peakPoints)
    {
        // If the list you passed in is empty, return 0,0
        if (peakPoints == null || peakPoints.Count == 0)
            return new Vector2Int(0, 0);

        float avgX = (float)peakPoints.Average(p => p.x);
        float avgZ = (float)peakPoints.Average(p => p.y);

        var sorted = peakPoints
            .OrderBy(p => Vector2.Distance(
                new Vector2(p.x, p.y),
                new Vector2(avgX, avgZ)
            ))
            .ToList();

        return sorted[sorted.Count / 2];
    }

    private static Vector2Int GetClosestRoadPoint(Vector2Int peak, float[,] roadMask)
    {
        float bestDist = float.MaxValue;
        Vector2Int best = peak;
        int mapWidth = roadMask.GetLength(0);

        //MAYBE CAN THREADED?
        for (int z = 0; z < mapWidth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (roadMask[x, z] < 0.25f) // road area
                {
                    float d = Vector2.Distance(new Vector2(x, z), peak);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = new Vector2Int(x, z);
                    }
                }
            }
        }

        return best;
    }

    private static List<Vector2Int> GetLine(Vector2Int a, Vector2Int b)
    {
        List<Vector2Int> pts = new List<Vector2Int>();

        int x0 = a.x, y0 = a.y;
        int x1 = b.x, y1 = b.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            pts.Add(new Vector2Int(x0, y0));
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        return pts;
    }

    private static void CarveRoad(List<Vector2Int> line, float[,] roadMask)
    {
        int radius = 10;            // road width
        int mapWidth = roadMask.GetLength(0);

        foreach (var p in line)
        {
            for (int dz = -radius; dz <= radius; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int xx = p.x + dx;
                    int zz = p.y + dz;

                    if (xx < 0 || zz < 0 || xx >= mapWidth || zz >= mapWidth)
                        continue;

                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > radius) continue;

                    roadMask[xx, zz] = 0;
                }
        }
    }

    private static void ApplyErosionGradient(float[,] depthMap, float[,] roadRidge, Vector2Int peak, float maxHeight, AnimationCurve roadHeightCurve)
    {
        float maxDist = 0f;
        int mapWidth = depthMap.GetLength(0);

        //CAN BE OPTIMIZED BY THREAD
        for (int z = 0; z < mapWidth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (roadRidge[x, z] < 0.25f)
                {
                    float d = Vector2.Distance(new Vector2(x, z), peak);
                    if (d > maxDist) maxDist = d;
                }
            }
        }

        if (maxDist < 0.0001f) maxDist = 0.0001f;

        for (int z = 0; z < mapWidth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (roadRidge[x, z] >= 0.25f)
                    continue;

                float d = Vector2.Distance(new Vector2(x, z), peak);
                //got the distance
                float t = Mathf.Clamp01(1f - d / maxDist);
                //evaluate it in AnimCurve
                t = roadHeightCurve.Evaluate(t);
                //Overwrites
                depthMap[x, z] = maxHeight * t;
            }
        }
    }
}
