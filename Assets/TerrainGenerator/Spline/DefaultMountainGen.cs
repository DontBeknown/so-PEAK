using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DefaultMountainGen
{

    public static void MountainTerarainGen(Spline mainSpline,int mapIteration, float[,] depthMap, 
        float[,] continentalness, float[,] erosion, float[,] weirdness,AnimationCurve meshHeightCurve , Vector2 peakCenter)
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
                            i+1, mapWidth, mapLength, peakCenter);

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
        float[,] weirdness, int round, int mapWidth, int mapLength, Vector2 peakCenter)
    {
        //change falloff power from each round
        int falloffPower = round;


        //parallel for faster
        System.Threading.Tasks.Parallel.For(0, mapLength, z =>
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float c = continentalness[x, z];
                float e = erosion[x, z];
                float w = weirdness[x, z];
                float r = -3.0f * (Mathf.Abs(Mathf.Abs(w) - 0.6666667f) - 0.33333334f); //Peaks and Valley (Ridges)
                float[] np_param = { c, e, r };
                double off = SplineMap.GetSpline(mainSpline, np_param) + 0.015f;
                float d = 1.0f - 83.0f / 160.0f + (float)off; // simplified, y=0 always


                    // Compute normalized distance from center
                    float nx = (float)x / mapWidth;
                    float nz = (float)z / mapLength;

                    // radial distance (0 = center, 1 = far edge)
                    float dx = nx - peakCenter.x;
                    float dz = nz - peakCenter.y;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz) / 0.7071f; // normalize to 0?1 range

                    // mask curve
                    float mask = Mathf.Clamp01(1f - Mathf.Pow(dist, falloffPower));

                    d *= mask; // apply falloff mask


                if (!float.IsNaN(d) && !float.IsInfinity(d))
                    d = Mathf.Clamp(d, 0f, 1f);
                else
                    d = -1f;
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

          
}
