using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RoadCarver
{
    //Need to gen road ridge somewhere else
    //also use height map from generate mountain steps

    private class RoadCurveProfile
    {
        private float p0, p1, p2;

        public void GenerateWalkableCurve(float startHeight, float endHeight, float tierLength, float stageMaxHeight)
        {
            p0 = startHeight;
            p2 = endHeight;

            float midX = tierLength / 2f;
            float linearMidY = (startHeight + endHeight) / 2f;

            // --- 1. BENDING IN METERS ---
            float maxBendMeters = 7.0f;
            float deviationNormalized = UnityEngine.Random.Range(-maxBendMeters, maxBendMeters) / stageMaxHeight;
            float targetMidY = linearMidY + deviationNormalized;

            // --- 2. SLOPE CHECK IN METERS ---
            float maxRiseMeters = 0.60f * midX;
            float maxRiseNormalized = maxRiseMeters / stageMaxHeight;

            // --- 3. THE FAILSAFE CLAMP ---
            // Calculate the highest and lowest points reachable from BOTH ends
            float highestAllowedFromP0 = p0 + maxRiseNormalized;
            float lowestAllowedFromP0 = p0 - maxRiseNormalized;

            float highestAllowedFromP2 = p2 + maxRiseNormalized;
            float lowestAllowedFromP2 = p2 - maxRiseNormalized;

            // The safe zone is where both ends overlap
            float maxSafeY = Mathf.Min(highestAllowedFromP0, highestAllowedFromP2);
            float minSafeY = Mathf.Max(lowestAllowedFromP0, lowestAllowedFromP2);

            // If the drop is physically too steep for the max grade, min will exceed max. 
            // Fallback to the linear middle to prevent breaking the curve.
            if (minSafeY > maxSafeY)
            {
                p1 = linearMidY;
            }
            else
            {
                p1 = Mathf.Clamp(targetMidY, minSafeY, maxSafeY);
            }
        }

        public float Evaluate(float t)
        {
            float u = 1 - t;
            return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
        }
    }

    public static void CarveRoad(float[,] depthMap, float[,] roadRidge, List<List<Vector2Int>> allMountainPeakPoints, float maxHeight, AnimationCurve roadHeightCurve, int seed, out Vector2Int OutPeak, out Vector2Int OutSpawn)
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

        // --- DEBUG LOGS ---
        Debug.Log($"<color=cyan>[RoadCarver]</color> <b>Peak Identification:</b> " +
                  $"Tallest Mountain Index: {tallestMountainIndex} | " +
                  $"Coordinate: {mainPeak} | " +
                  $"Raw Height: {maxH}");

        OutPeak = mainPeak;

        Vector2Int closestRoad = GetClosestRoadPoint(mainPeak, roadRidge);
        List<Vector2Int> line = GetLine(mainPeak, closestRoad);
        CarveRoad(line, roadRidge);

        // --- GUARANTEE CONNECTIVITY (Retained from the newer logic) ---
        OutSpawn = EnsureConnectivity(roadRidge, mainPeak);

        /////////////// We will use Dartboard Here //////////////////
        // C. Generate Heightmaps for ALL Mountains (The Dartboards)
        List<float[,]> allRoadHeightMaps = new List<float[,]>();

        for (int i = 0; i < repPeaks.Length; i++)
        {
            float maxDist = GetMaxDistanceToCorner(repPeaks[i], mapWidth, mapLength);
            int tierCount = Mathf.CeilToInt(maxDist / ringWidth) + 1;

            RoadCurveProfile[,] dartboard = InitializeDartboard(peakHeights[i], tierCount, ringWidth, seed, maxHeight);

            // --- THE FIX: Define the flat "Landing Pad" radius for the lighthouse ---
            // If this is the main peak, make a 25-meter flat area. Otherwise, just 5 meters.
            float plateauRadius = (i == tallestMountainIndex) ? 25f : 5f;

            // Pass the peak height AND the plateau radius into the generator!
            float[,] mountainRoadMap = GenerateHeightMapFromDartboard(dartboard, repPeaks[i], mapWidth, mapLength, ringWidth, peakHeights[i], plateauRadius);

            allRoadHeightMaps.Add(mountainRoadMap);
        }

        // D. Combine and Apply
        // We loop through the map ONCE. If we find a road pixel (from step B),
        // we calculate the max height from our generated maps (Step C) and apply it.
        ApplyCombinedHeights(depthMap, roadRidge, allRoadHeightMaps);
    }

    private static float[,] GenerateHeightMapFromDartboard(RoadCurveProfile[,] dartboard, Vector2Int peak, int width, int length, float ringWidth, float peakHeight, float plateauRadius)
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

                // --- THE FIX: Create the flat plateau ---
                if (dist <= plateauRadius)
                {
                    // Perfectly flat under the lighthouse!
                    map[x, z] = peakHeight;
                    continue; // Skip the slope math entirely for these pixels
                }

                // Push the start of the mathematical slope OUTWARD to the edge of the plateau
                float effectiveDist = dist - plateauRadius;
                int tier = Mathf.FloorToInt(effectiveDist / ringWidth);

                if (tier < tierLimit)
                {
                    // 1. Calculate Angle (0 to 360)
                    float angle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // 2. Find the two sectors we are between
                    float sectorFloat = angle / 45f;
                    int sectorA = Mathf.FloorToInt(sectorFloat) % 8;
                    int sectorB = (sectorA + 1) % 8;

                    // 3. Find the 't' weight
                    float t_sector = sectorFloat - Mathf.Floor(sectorFloat);

                    // 4. Evaluate both sectors using the EFFECTIVE distance
                    float distT = (effectiveDist % ringWidth) / ringWidth;
                    float heightA = dartboard[sectorA, tier].Evaluate(distT);
                    float heightB = dartboard[sectorB, tier].Evaluate(distT);

                    // 5. BLEND!
                    map[x, z] = Mathf.Lerp(heightA, heightB, t_sector);
                }
                else
                {
                    map[x, z] = 0f;
                }
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
    private static RoadCurveProfile[,] InitializeDartboard(float peakHeight, int tierCount, float ringWidth, int seed, float maxHeight)
    {
        UnityEngine.Random.InitState(seed);
        RoadCurveProfile[,] sectors = new RoadCurveProfile[8, tierCount];

        // 1. Calculate the absolute maximum drop allowed by your slope rules.
        // Max Slope = 0.60. Total run per tier = ringWidth (100).
        float maxDropMetersPerTier = 0.60f * ringWidth;
        float maxDropNormalized = maxDropMetersPerTier / maxHeight;

        for (int s = 0; s < 8; s++)
        {
            float currentStartH = peakHeight;
            for (int t = 0; t < tierCount; t++)
            {
                // 2. Drop by a safe, normalized amount (e.g., 50% to 90% of the maximum allowed slope)
                // This guarantees the drop uses the 0-to-1 multiplier logic but respects physical reality.
                float safeDropNormalized = maxDropNormalized * UnityEngine.Random.Range(0.5f, 0.9f);

                float nextEndH = Mathf.Max(0, currentStartH - safeDropNormalized);

                sectors[s, t] = new RoadCurveProfile();
                sectors[s, t].GenerateWalkableCurve(currentStartH, nextEndH, ringWidth, maxHeight);

                currentStartH = nextEndH;
            }
        }
        return sectors;
    }

    /////////////////////// CONNECTIVITY ALGORITHM ///////////////////////////////

    public static Vector2Int EnsureConnectivity(float[,] roadRidge, Vector2Int mainPeak)
    {
        int mapWidth = roadRidge.GetLength(0);
        int mapLength = roadRidge.GetLength(1);

        // 1. Calculate Spawn Target near (0.8, 0.2)
        Vector2Int spawnTarget = new Vector2Int(Mathf.FloorToInt(mapWidth * 0.95f), Mathf.FloorToInt(mapLength * 0.05f));
        Vector2Int spawnPoint = GetClosestRoadPoint(spawnTarget, roadRidge);
        spawnPoint = CenterSpawnOnRoad(spawnPoint, roadRidge, 12, 3);

        int maxBridges = 25; // Safety fallback

        for (int i = 0; i < maxBridges; i++)
        {
            bool reachedPeak = false;
            bool[,] visited = new bool[mapWidth, mapLength];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            List<Vector2Int> currentIsland = new List<Vector2Int>();

            // 2. Start Flood Fill (BFS) from the Spawn Point
            queue.Enqueue(spawnPoint);
            visited[spawnPoint.x, spawnPoint.y] = true;

            int[] dx = { 0, 0, -1, 1, -1, 1, -1, 1 };
            int[] dz = { 1, -1, 0, 0, 1, 1, -1, -1 };

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                currentIsland.Add(curr);

                // If close enough to main peak, we are fully connected
                if (Vector2.Distance(curr, mainPeak) <= 15f)
                {
                    reachedPeak = true;
                    break;
                }

                for (int d = 0; d < 8; d++)
                {
                    int nx = curr.x + dx[d];
                    int nz = curr.y + dz[d];

                    if (nx >= 0 && nx < mapWidth && nz >= 0 && nz < mapLength)
                    {
                        if (!visited[nx, nz] && roadRidge[nx, nz] < 0.25f)
                        {
                            visited[nx, nz] = true;
                            queue.Enqueue(new Vector2Int(nx, nz));
                        }
                    }
                }
            }

            if (reachedPeak)
            {
                Debug.Log($"<color=green>[RoadCarver]</color> Connectivity Ensured! Bridges built: {i}");
                break;
            }

            // 3. We didn't reach the peak. Find the point in our island closest to the peak.
            Vector2Int bestEdgePoint = currentIsland[0];
            float minDistToPeak = float.MaxValue;

            foreach (Vector2Int p in currentIsland)
            {
                float distToPeak = Vector2.Distance(p, mainPeak);
                if (distToPeak < minDistToPeak)
                {
                    minDistToPeak = distToPeak;
                    bestEdgePoint = p;
                }
            }

            // 4. Find the closest UNVISITED road using fast Squared Distance
            Vector2Int targetUnvisitedRoad = bestEdgePoint;
            float minSqrDistToUnvisited = float.MaxValue;

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapLength; z++)
                {
                    if (roadRidge[x, z] < 0.25f && !visited[x, z])
                    {
                        float deltaX = x - bestEdgePoint.x;
                        float deltaZ = z - bestEdgePoint.y;
                        float sqrDist = (deltaX * deltaX) + (deltaZ * deltaZ); // Optimized math!

                        if (sqrDist < minSqrDistToUnvisited)
                        {
                            minSqrDistToUnvisited = sqrDist;
                            targetUnvisitedRoad = new Vector2Int(x, z);
                        }
                    }
                }
            }

            // 5. Carve bridge to unvisited road
            List<Vector2Int> bridgeLine = GetLine(bestEdgePoint, targetUnvisitedRoad);
            CarveRoad(bridgeLine, roadRidge);
        }

        return spawnPoint;
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

    // OLD 2D Road Point Finder
    private static Vector2Int GetClosestRoadPoint(Vector2Int peak, float[,] roadMask)
    {
        float bestDist = float.MaxValue;
        Vector2Int best = peak;
        int mapWidth = roadMask.GetLength(0);
        int mapLength = roadMask.GetLength(1);

        for (int z = 0; z < mapLength; z++)
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

    private static Vector2Int CenterSpawnOnRoad(Vector2Int initialPoint, float[,] roadMask, int radius, int iterations)
    {
        Vector2Int currentPoint = initialPoint;
        int mapWidth = roadMask.GetLength(0);
        int mapLength = roadMask.GetLength(1);

        for (int i = 0; i < iterations; i++)
        {
            long sumX = 0;
            long sumZ = 0;
            int roadPixelCount = 0;

            // Create a local bounding box based on our radius
            int minX = Mathf.Max(0, currentPoint.x - radius);
            int maxX = Mathf.Min(mapWidth - 1, currentPoint.x + radius);
            int minZ = Mathf.Max(0, currentPoint.y - radius);
            int maxZ = Mathf.Min(mapLength - 1, currentPoint.y + radius);

            // Scan the local neighborhood
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (roadMask[x, z] < 0.25f) // If it is a road pixel
                    {
                        sumX += x;
                        sumZ += z;
                        roadPixelCount++;
                    }
                }
            }

            // Move the current point to the average center of all nearby road pixels
            if (roadPixelCount > 0)
            {
                currentPoint = new Vector2Int((int)(sumX / roadPixelCount), (int)(sumZ / roadPixelCount));
            }
            else
            {
                break; // Failsafe in case something goes wrong
            }
        }
        return currentPoint;
    }

    private static void CarveRoad(List<Vector2Int> line, float[,] roadMask)
    {
        int radius = 10;            // road width
        int mapWidth = roadMask.GetLength(0);
        int mapLength = roadMask.GetLength(1);

        foreach (var p in line)
        {
            for (int dz = -radius; dz <= radius; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int xx = p.x + dx;
                    int zz = p.y + dz;

                    if (xx < 0 || zz < 0 || xx >= mapWidth || zz >= mapLength)
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