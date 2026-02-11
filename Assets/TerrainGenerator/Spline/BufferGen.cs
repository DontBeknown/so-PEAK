using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public static class BufferGen
{
    //pass the old height map, blank height map
    public static void GenMapWithBuffer(float[,] oldHeightMap, float[,] newHeightMap, int bufferSize)
    {
        int newWidth = newHeightMap.GetLength(0);
        int newLength = newHeightMap.GetLength(1);
        int halfBuffer = bufferSize / 2;

        Parallel.For(0, newWidth, z =>
        {
            for (int x = 0; x < newLength; x++)
            {
                // 1. Get reference coordinate
                Vector2Int refCoord = GetReferenceCoordinate(x, z, oldHeightMap, halfBuffer);

                // 2. Get height
                float refHeight = oldHeightMap[refCoord.x, refCoord.y];

                // 3. Get Chebyshev distance
                int dist = GetChebyshevDistance(x, z, refCoord.x+halfBuffer, refCoord.y+halfBuffer);

                // 4. Calculate fade and final height
                newHeightMap[x, z] = CalculateFinalHeight(halfBuffer, dist, refHeight);
            }
        });

    }

    // Use this strictly for the Road Mask so the edges don't turn into roads!
    public static void GenRoadMaskWithBuffer(float[,] oldMask, float[,] newMask, int bufferSize)
    {
        int newWidth = newMask.GetLength(0);
        int newLength = newMask.GetLength(1);
        int halfBuffer = bufferSize / 2;
        int oldWidth = oldMask.GetLength(0);
        int oldLength = oldMask.GetLength(1);

        Parallel.For(0, newLength, z =>
        {
            for (int x = 0; x < newWidth; x++)
            {
                // Check if we are inside the ORIGINAL 1000x1000 area
                bool isInsideOriginal =
                    (x >= halfBuffer && x < newWidth - halfBuffer) &&
                    (z >= halfBuffer && z < newLength - halfBuffer);

                if (isInsideOriginal)
                {
                    // Copy the exact road data
                    newMask[x, z] = oldMask[x - halfBuffer, z - halfBuffer];
                }
                else
                {
                    // WE ARE IN THE BUFFER. 1.0f means "NO ROAD HERE"
                    newMask[x, z] = 1.0f;
                }
            }
        });
    }



    //first we get the relative coordinates
    public static Vector2Int GetReferenceCoordinate(int x, int z, float[,] oldHeightMap, int halfBuffer)
    {
        //min 0 max relative with old height map since we want reference point
        int width = oldHeightMap.GetLength(0);
        int length = oldHeightMap.GetLength(1);

        // find ref coordinate
        int refX = Mathf.Clamp(x - halfBuffer, 0, width - 1);
        int refZ = Mathf.Clamp(z - halfBuffer, 0, length - 1);

        return new Vector2Int(refX,refZ);
    }

    private static int GetChebyshevDistance(int X, int Z, int refX, int refZ)
    {
        int distX = math.abs(X - refX);
        int distZ = math.abs(Z - refZ);

        return math.max(distX, distZ);
    }

    private static float CalculateFinalHeight(int halfBuffer, int dist, float refHeight)
    {
        //Complement and interpolate and clamp
        int complement = (halfBuffer - dist);

        //normalized
        float fade = (float)complement / halfBuffer;

        //clamping for safety but still can multiply directly
        return math.clamp(refHeight * fade, 0.0f, 1.0f);

        


    }


}
