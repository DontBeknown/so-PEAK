using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DefaultMountainGen
{
    //Multiple one
    public static void MultipleMountainTerarainGen(Spline mainSpline, int mapIteration, float[,] depthMap,
        float[,] continentalness, float[][,] erosionArray, float[,] weirdness, AnimationCurve meshHeightCurve, Vector2[] peakCenterArray, float fallOffPower, float mountainRadiusMeters, List<List<Vector2Int>> allMountainPeakPoints, int seed)
    {
        //initiate randomseed here
        UnityEngine.Random.InitState(seed);



        //HARDCODED peaks count
        int peaksCount = 3;

        //HARDCODED peaks height for random
        // We want to ensure each range is used exactly once
        List<Vector2> heightRanges = new List<Vector2> {
        new Vector2(0.4f, 0.59f),
        new Vector2(0.6f, 0.79f),
        new Vector2(0.8f, 1.0f)
        };

        // Simple Fisher-Yates shuffle to randomize which mountain gets which range
        for (int i = 0; i < heightRanges.Count; i++)
        {
            Vector2 temp = heightRanges[i];
            int randomIndex = UnityEngine.Random.Range(i, heightRanges.Count);
            heightRanges[i] = heightRanges[randomIndex];
            heightRanges[randomIndex] = temp;
        }



        //can pick length and width here
        int mapWidth = depthMap.GetLength(0);
        int mapLength = depthMap.GetLength(1);

        //init Array
        float[][,] tempDepthMap = new float[peaksCount][,];
        for (int i = 0; i < tempDepthMap.Length; i++)
        {
            tempDepthMap[i] = new float[mapWidth, mapLength];
        }


        //for loop three mountains
        for (int i = 0; i < peaksCount; i++)
        {
            // Pick the random height from the shuffled range
            float randomTargetHeight = UnityEngine.Random.Range(heightRanges[i].x, heightRanges[i].y);

            MountainTerarainGen(mainSpline, mapIteration, tempDepthMap[i], continentalness, erosionArray[i], weirdness,
            meshHeightCurve, peakCenterArray[i], fallOffPower, randomTargetHeight, mountainRadiusMeters);

            //////////////Find peaks here
            // 2. Find the peaks for THIS specific mountain while it's still isolated
            List<Vector2Int> currentPeakPoints = new List<Vector2Int>();
            FindPeakPoints(tempDepthMap[i], currentPeakPoints);
            allMountainPeakPoints.Add(currentPeakPoints);

        }

        //Then Compared and Evaluated Height here into depthMap[,]
        System.Threading.Tasks.Parallel.For(0, mapLength, z =>
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float maxH = 0f;
                for (int i = 0; i < peaksCount; i++)
                {
                    if (tempDepthMap[i][x, z] > maxH)
                    {
                        maxH = tempDepthMap[i][x, z];
                    }
                }
                depthMap[x, z] = maxH;
            }
        });

    }





    public static void MountainTerarainGen(Spline mainSpline,int mapIteration, float[,] depthMap, 
        float[,] continentalness, float[,] erosion, float[,] weirdness,AnimationCurve meshHeightCurve , Vector2 peakCenter, float fallOffPower, float targetPeakHeight,float mountainRadiusMeters)
    {
        //can pick length and width here
        int mapWidth = depthMap.GetLength(0);
        int mapLength = depthMap.GetLength(1); 

        //We gen Noise before here by calling the main thingy
        float [,] tempHeight = new float[mapWidth, mapLength];

        for (int i = 0; i < mapIteration; i++)
        {
            //Translate
            TranslateHeight(mainSpline, tempHeight, continentalness, erosion, weirdness,
                            i+1, mapWidth, mapLength, peakCenter, fallOffPower, targetPeakHeight, mountainRadiusMeters);

            //Correct
            FixHeight(tempHeight, mapLength, mapWidth);

            //Merge
            float mergeAlpha = (i == 0) ? 1f : 0.7f;
            MergeTempToMain(depthMap, tempHeight, mapWidth, mapLength, mergeAlpha);
        }

        //apply curve
        ApplyHeightCurve(depthMap, meshHeightCurve);





    }

    private static void TranslateHeight(Spline mainSpline, float[,] tempHeight, float[,] continentalness, float[,] erosion,
    float[,] weirdness, int round, int mapWidth, int mapLength, Vector2 peakCenter, float fallOffPower, float targetPeakHeight, float mountainRadiusMeters)
    {
        // Convert meters to a normalized value (0.0 to 1.0)
        // Assuming mapWidth is your world size in meters.

 

        float normalizedRadius = mountainRadiusMeters / mapWidth;

        System.Threading.Tasks.Parallel.For(0, mapLength, z =>
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // 1. Original Spline Noise
                float c = continentalness[x, z];
                float e = erosion[x, z];
                float r = -3.0f * (Mathf.Abs(Mathf.Abs(weirdness[x, z]) - 0.6666667f) - 0.33333334f);
                float[] np_param = { c, e, r };
                double off = SplineMap.GetSpline(mainSpline, np_param) + 0.015f;
                float d = 1.0f - 83.0f / 160.0f + (float)off;

                // 2. Normalized distance from peakCenter
                float dx = ((float)x / mapWidth) - peakCenter.x;
                float dz = ((float)z / mapLength) - peakCenter.y;
                float rawDist = Mathf.Sqrt(dx * dx + dz * dz);

                // 3. New Radius Logic
                // This makes 'dist' reach 1.0 exactly at your mountainRadiusMeters
                float dist = Mathf.Clamp01(rawDist / normalizedRadius);

                // 4. Mask Curve
                float mask = Mathf.Clamp01(1f - Mathf.Pow(dist, fallOffPower));

                // 5. THE "SOBER" PADDING
                float baseRoadHeight = 0.0f;
                float maxExceedAmount = 0.05f; // 10m buffer

                // This is the theoretical "Road Line"
                float padLevel = Mathf.Lerp(baseRoadHeight, targetPeakHeight, mask);
                float maxHeight = padLevel + maxExceedAmount;

                // Squash the mountain noise into the 10m buffer zone
                float containedNoise = Mathf.Lerp(padLevel, maxHeight, d);

                // Apply the contained noise based on mask strength
                float mountainHeight = Mathf.Lerp(d, containedNoise, mask);

                // THE CRITICAL FIX: Multiply by mask to bring the base back to 0
                d = mountainHeight * mask;

                // 6. Safety Checks
                if (float.IsNaN(d) || float.IsInfinity(d)) d = -1f;
                else d = Mathf.Clamp01(d);

                tempHeight[x, z] = d;
            }
        });
    }

    //in case NAN we try to fix like a flex tape
    private static void FixHeight(float[,] tempHeight, int mapLength, int mapWidth)
    {

        for (int z = 0; z < mapLength; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float d = tempHeight[x, z];
                if (d == -1f)
                {
                    if (x > 0 && tempHeight[x - 1, z] != -1f) // try left
                        tempHeight[x, z] = tempHeight[x - 1, z];
                    else if (x < mapWidth - 1 && tempHeight[x + 1, z] != -1f) // try right
                        tempHeight[x, z] = tempHeight[x + 1, z];
                    else if (z > 0 && tempHeight[x, z - 1] != -1f) // try up
                        tempHeight[x, z] = tempHeight[x, z - 1];
                    else if (z < mapLength - 1 && tempHeight[x, z + 1] != -1f) // try down
                        tempHeight[x, z] = tempHeight[x, z + 1];
                    else
                        tempHeight[x, z] = 0f; // fallback
                }







            }
        }

    }

    // Merges a temporary map into main height map
    private static void MergeTempToMain(float [,] depthMap, float[,] tempHeight,int mapWidth, int mapLength ,float alpha = 1f)
    {
        float flatThreshold = 0.22f;

        System.Threading.Tasks.Parallel.For(0, mapLength, z =>
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float oldH = depthMap[x, z];
                float newH = tempHeight[x, z];

                // keep flat areas flat
                if (oldH <= flatThreshold && newH <= flatThreshold)
                {
                    // optional: keep exact old height
                    depthMap[x, z] = oldH;
                }
                else
                {
                    // stack height normally
                    depthMap[x, z] = oldH * (1 - alpha) + newH * alpha;
                }
            }
        });
    }

    //Curve Height via Curve anim
    //will compare with roads later
    private static void ApplyHeightCurve(float[,] depthMap, AnimationCurve curve)
    {
        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        for (int z = 0; z < length; z++)
            for (int x = 0; x < width; x++)
                depthMap[x, z] = curve.Evaluate(depthMap[x, z]);
    }

    public static void FindPeakPoints(float[,] depthMap, List<Vector2Int> peakPoints)
    {
        float localMaxHeight = 0f;
        peakPoints.Clear();

        int width = depthMap.GetLength(0);
        int length = depthMap.GetLength(1);

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float currentH = depthMap[x, z];
                // Only care about heights that are actually part of the mountain
                if (currentH > localMaxHeight)
                {
                    localMaxHeight = currentH;
                    peakPoints.Clear();
                    peakPoints.Add(new Vector2Int(x, z));
                }
                else if (currentH > 0 && Mathf.Approximately(currentH, localMaxHeight))
                {
                    peakPoints.Add(new Vector2Int(x, z));
                }
            }
        }
    }

}
