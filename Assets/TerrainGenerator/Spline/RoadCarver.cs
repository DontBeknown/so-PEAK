using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RoadCarver
{
    //Need to gen road ridge somewhere else
    //also use height map from generate mountain steps

    private class RoadCurveProfile
    {
        private float p0, p1, p2; // Start, Control(Middle), End heights

        public void GenerateWalkableCurve(float startHeight, float endHeight, float tierLength)
        {
            p0 = startHeight;
            p2 = endHeight;

            // 1. Calculate the 'Linear' middle point (straight line)
            float midX = tierLength / 2f;
            float linearMidY = (startHeight + endHeight) / 2f;

            // 2. Randomize the Middle Anchor (The "Photoshop Curve" bend)
            // We pull the middle point up or down to create a curve.
            // Range: +/- 15 meters deviation from the straight line
            float deviation = UnityEngine.Random.Range(-0.15f, 0.15f);
            float targetMidY = linearMidY + deviation;

            // 3. THE WALKABLE GUARANTEE (Slope Check)
            // Max Slope = 0.75 (Rise / Run)
            // Run is half the tier length (e.g., 50m)
            float maxRise = 0.75f * midX;

            // Clamp the middle point so it's reachable from Start and End without being too steep
            float minSafeY = Mathf.Max(p0 - maxRise, p2 - maxRise);
            float maxSafeY = Mathf.Min(p0 + maxRise, p2 + maxRise);

            p1 = Mathf.Clamp(targetMidY, minSafeY, maxSafeY);
        }

        // Quadratic Bezier: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        public float Evaluate(float t)
        {
            float u = 1 - t;
            return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
        }
    }





    public static void CarveRoad(float[,] depthMap, float[,] roadRidge, List<List<Vector2Int>> allMountainPeakPoints, float maxHeight, AnimationCurve roadHeightCurve)
    {
        Vector2Int[] repPeaks = new Vector2Int[allMountainPeakPoints.Count];
        float[] peakHeights = new float[allMountainPeakPoints.Count];

        int tallestMountainIndex = 0;
        float maxH = -1f;

        //HARDCODED RingWidth
        int ringWidth = 100;

        int mapWidth = depthMap.GetLength(0);
        int mapLength = depthMap.GetLength(1);

        // 2. Identify the peak of each mountain and find the global tallest
        for (int i = 0; i < allMountainPeakPoints.Count; i++)
        {
            // Get the average/center point for THIS specific mountain's peak cluster
            repPeaks[i] = GetPeakCoordinate(allMountainPeakPoints[i]);

            // Access depthMap directly to find how high this peak actually is
            peakHeights[i] = depthMap[repPeaks[i].x, repPeaks[i].y];

            // Track which one is the absolute king of the hill
            if (peakHeights[i] > maxH)
            {
                maxH = peakHeights[i];
                tallestMountainIndex = i;
            }
        }

        Vector2Int mainPeak = repPeaks[tallestMountainIndex];
        Vector2Int closestRoad = GetClosestRoadPoint(mainPeak, roadRidge);
        List<Vector2Int> line = GetLine(mainPeak, closestRoad);
        CarveRoad(line, roadRidge);


        /////////////// We will use Dartboard Here //////////////////
        // C. Generate Heightmaps for ALL Mountains (The Dartboards)
        // We create a full-size float[,] for each mountain's projected road heights
        List<float[,]> allRoadHeightMaps = new List<float[,]>();

        for (int i = 0; i < repPeaks.Length; i++)
        {
            // 1. Calculate dynamic tiers for this specific peak
            float maxDist = GetMaxDistanceToCorner(repPeaks[i], mapWidth, mapLength);
            int tierCount = Mathf.CeilToInt(maxDist / ringWidth) + 1;

            // 2. Init Dartboard logic
            RoadCurveProfile[,] dartboard = InitializeDartboard(peakHeights[i], tierCount, ringWidth);

            // 3. Generate the height map for this mountain
            float[,] mountainRoadMap = GenerateHeightMapFromDartboard(dartboard, repPeaks[i], mapWidth, mapLength, ringWidth);
            allRoadHeightMaps.Add(mountainRoadMap);
        }

        // D. Combine and Apply
        // We loop through the map ONCE. If we find a road pixel (from step B),
        // we calculate the max height from our generated maps (Step C) and apply it.
        ApplyCombinedHeights(depthMap, roadRidge, allRoadHeightMaps);


    }

    private static float[,] GenerateHeightMapFromDartboard(RoadCurveProfile[,] dartboard, Vector2Int peak, int width, int length, float ringWidth)
    {
        float[,] map = new float[width, length];
        int tierLimit = dartboard.GetLength(1);

        System.Threading.Tasks.Parallel.For(0, length, z =>
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - peak.x;
                float dz = z - peak.y;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                int tier = Mathf.FloorToInt(dist / ringWidth);

                if (tier < tierLimit)
                {
                    // 1. Calculate Angle (0 to 360)
                    float angle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // 2. Find the two sectors we are between
                    // Since 360 / 8 = 45 degrees per sector
                    float sectorFloat = angle / 45f;
                    int sectorA = Mathf.FloorToInt(sectorFloat) % 8;
                    int sectorB = (sectorA + 1) % 8;

                    // 3. Find the 't' weight (how far are we from sectorA toward sectorB?)
                    float t_sector = sectorFloat - Mathf.Floor(sectorFloat);

                    // 4. Evaluate both sectors at this distance
                    float distT = (dist % ringWidth) / ringWidth;
                    float heightA = dartboard[sectorA, tier].Evaluate(distT);
                    float heightB = dartboard[sectorB, tier].Evaluate(distT);

                    // 5. BLEND! No more tears.
                    map[x, z] = Mathf.Lerp(heightA, heightB, t_sector);
                }
                else { map[x, z] = 0f; }
            }
        });

        return map;
    }

    // --- 4. COMBINE AND APPLY ---
    private static void ApplyCombinedHeights(float[,] depthMap, float[,] roadRidge, List<float[,]> allRoadMaps)
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);
        int mapCount = allRoadMaps.Count;

        // Loop through the mask
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // If this pixel is part of the road (carved earlier)
                if (roadRidge[x, z] < 0.25f)
                {
                    float maxRoadHeight = 0f;

                    // Find the highest road value among all 3 mountains
                    for (int i = 0; i < mapCount; i++)
                    {
                        float h = allRoadMaps[i][x, z];
                        if (h > maxRoadHeight) maxRoadHeight = h;
                    }

                    // Apply to the actual terrain
                    depthMap[x, z] = maxRoadHeight;
                }
            }
        }
    }

    // --- 5. INITIALIZE DARTBOARD ---
    private static RoadCurveProfile[,] InitializeDartboard(float peakHeight, int tierCount, float ringWidth)
    {
        RoadCurveProfile[,] sectors = new RoadCurveProfile[8, tierCount];

        for (int s = 0; s < 8; s++)
        {
            float currentStartH = peakHeight;
            for (int t = 0; t < tierCount; t++)
            {
                float drop = UnityEngine.Random.Range(0.12f, 0.18f);
                float nextEndH = Mathf.Max(0, currentStartH - drop);

                sectors[s, t] = new RoadCurveProfile();
                sectors[s, t].GenerateWalkableCurve(currentStartH, nextEndH, ringWidth);

                currentStartH = nextEndH;
            }
        }
        return sectors;
    }



    /////////////////////// HELPERS ///////////////////////////////

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

    private static float GetMaxDistanceToCorner(Vector2Int p, int w, int l)
    {
        float d1 = Vector2.Distance(p, new Vector2(0, 0));
        float d2 = Vector2.Distance(p, new Vector2(w, 0));
        float d3 = Vector2.Distance(p, new Vector2(0, l));
        float d4 = Vector2.Distance(p, new Vector2(w, l));
        return Mathf.Max(d1, d2, d3, d4);
    }
}
