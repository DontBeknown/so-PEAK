using UnityEngine;
using System.Threading.Tasks;

public static class ProximityGenerator
{
    public static float[,] CreateWaterProximityMap(bool[,] waterMask, int radiusInMeters)
    {
        int width = waterMask.GetLength(0);
        int length = waterMask.GetLength(1);
        float[,] proximityMap = new float[width, length];

        Parallel.For(0, length, z =>
        {
            for (int x = 0; x < width; x++)
            {
                // PRE-INVERTED: 0.0 means "Touching Water"
                if (waterMask[x, z])
                {
                    proximityMap[x, z] = 0f;
                    continue;
                }

                float minDist = float.MaxValue;
                int minX = Mathf.Max(0, x - radiusInMeters);
                int maxX = Mathf.Min(width - 1, x + radiusInMeters);
                int minZ = Mathf.Max(0, z - radiusInMeters);
                int maxZ = Mathf.Min(length - 1, z + radiusInMeters);

                for (int sz = minZ; sz <= maxZ; sz++)
                {
                    for (int sx = minX; sx <= maxX; sx++)
                    {
                        if (waterMask[sx, sz])
                        {
                            float dist = Vector2.Distance(new Vector2(x, z), new Vector2(sx, sz));
                            if (dist < minDist) minDist = dist;
                        }
                    }
                }

                if (minDist <= radiusInMeters)
                {
                    // PRE-INVERTED: Fades from 0.0 (near water) up to 1.0 (far away)
                    proximityMap[x, z] = minDist / (float)radiusInMeters;
                }
                else
                {
                    // PRE-INVERTED: 1.0 means "Too far away from water"
                    proximityMap[x, z] = 1f;
                }
            }
        });

        return proximityMap;
    }
}