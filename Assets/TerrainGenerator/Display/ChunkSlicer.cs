using UnityEngine;



    public static class MapSlicer
    {

        public static float[,] GetChunkData(Vector2 chunkCoord, int chunkSize, float[,] globalHeightMap)
        {

            int width = chunkSize;
            int height = chunkSize;

            // 1. Prepare the empty containers for our slice
            float[,] slicedHeights = new float[width, height];
            Color[,] slicedColors = new Color[width, height];

            // 2. Calculate where this chunk starts in the Big Map
            // We use (chunkSize - 1) if we want chunks to stitch together perfectly (share an edge)
            // Otherwise use (chunkSize) for distinct tiles.
            int startX = (int)chunkCoord.x * (chunkSize - 1);
            int startY = (int)chunkCoord.y * (chunkSize - 1);

            // 3. Loop through every pixel of the NEW chunk
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {

                    // Calculate the corresponding coordinate in the GLOBAL map
                    int globalX = startX + x;
                    int globalY = startY + y;

                    //Inverted fix
                    int invertedY = height - 1 - y;


                    // 4. BOUNDARY CHECK
                    // "If slice giving you nothing, then that height is 0"
                    bool isInsideMap = (globalX >= 0 && globalX < globalHeightMap.GetLength(0)) &&
                                       (globalY >= 0 && globalY < globalHeightMap.GetLength(1));

                    if (isInsideMap)
                    {
                        // FIX: Use invertedY here!
                        slicedHeights[x, invertedY] = globalHeightMap[globalX, globalY];
                }
                    else
                    {
                    // FIX: Use invertedY here too!
                        slicedHeights[x, invertedY] = 0f;
                    }
                }
            }

            return slicedHeights;
        }
    }


