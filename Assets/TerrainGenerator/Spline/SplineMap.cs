
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
//idea: seperate script for sending three variables


public enum SplineType
{
    Continentalness,
    Erosion,
    Ridges
}

public class SplineBase { }
//Spline Graph class
//also works as spline point for bigger spline, but it will be FixSpline eventually

public class Spline : SplineBase
{
    // 12 is the limit of points in spline
    //also spline types to determine value to use 
    //(continentalness - 0, erosion - 1, ridges - 2)
    public SplineType type;

    public List<float> x = new List<float>();
    public List<float> derivative = new List<float>();

    //can hold both types of spline
    public List<SplineBase> y = new List<SplineBase>();
}

public class FixSpline : SplineBase
{
    public float y;
}
public class SplineMap : MonoBehaviour
{

   

    public static void AddSplineVal(Spline sp, float x, SplineBase y, float derivative)
    {
        sp.x.Add(x);
        sp.y.Add(y);
        sp.derivative.Add(derivative);
    }

    public static SplineBase CreateFixSpline(float val)
    {
        FixSpline sp = new FixSpline();
        sp.y = val;  // your FixSpline field
        return sp;       // FixSpline inherits SplineBase, so it works in a Spline's list
    }

    //Cubic Hermite Interpolation
    public static float GetSpline(SplineBase sp, float[] arr)
    {
        //debug: whole null
        if (sp == null)
        {
            Debug.LogError("GetSpline(): sp is null");
            return 0f;
        }

        //debug: if null then check if it is fixspline. if it is then return only value
        //also if spline length 1 but is not a fix spline point is unacceptable
        if (sp is FixSpline fix)
            return fix.y;

        if (sp is not Spline spline)
        {
            Debug.LogError("GetSpline(): unknown spline types/bad spline");
            return 0f;
        }

        //debug check length
        if (spline.x.Count <= 0)
        {
            Debug.LogError("GetSpline(): bad parameters");
            return 0f;
        }

        //determine value to use, by type of spline
        float value = arr[(int)spline.type];
        int i;

        //shift to find a point where it is larger than val
        for (i = 0; i < spline.x.Count; i++)
            if (spline.x[i] >= value)
                break;
            
        //case overflow: outer lerp both sides
        // i-- bcs the last spline point is length-1
        if (i == 0 || i == spline.x.Count)
        {
            if (i != 0) 
            i--;

            //recursive get spline till got the value
            //then extrapolate y = y0 + m * (x - x0)
            float v = GetSpline(spline.y[i], arr);
            return v + spline.derivative[i] * (value - spline.x[i]);
        }

        //normal case: cubic hermite interpolate goes brr
        SplineBase sp1 = spline.y[i - 1];
        SplineBase sp2 = spline.y[i];

        float x0 = spline.x[i - 1];
        float x1 = spline.x[i];
        float t = (value - x0) / (x1 - x0);
        float m0 = spline.derivative[i - 1];
        float m1 = spline.derivative[i];
        float y0 = GetSpline(sp1, arr);
        float y1 = GetSpline(sp2, arr);

        //difference of y from raw slopes from both point vs from their true y
        float p = m0 * (x1 - x0) - (y1 - y0);
        float q = -m1 * (x1 - x0) + (y1 - y0);

        return Mathf.Lerp(y0, y1, t) + t * (1.0f - t) * Mathf.Lerp(p, q, t);
   
    }

    //Offset: very extra, might simplify later
    //may do parameter for experiment (-0.2222f )
    public static float GetOffsetValue(float weirdness, float continentalness)
    {
        //here are magic numbers
        float WeirdnessFloor = -0.2222f;
        float WeirdnessThreshold = -0.7f;

        //inverse lerp - strech till bottom threshold then manipulate it by powering 
        //very magic thing
        float t = Mathf.InverseLerp(-1.0f, WeirdnessThreshold, weirdness); 
        t = Mathf.Pow(t, 1.5f); 
        float baseVal = (weirdness + 1f) * 0.5f * continentalness; 
        float clamped = Mathf.Lerp(WeirdnessFloor, baseVal, t); 
        return Mathf.Max(clamped, 0f); 
    }

    //spline for peaks and valley (sp1-sp3)
    public static Spline LowerRidgesSpline(float f, bool bl)
    {
        Spline spline = new Spline();
        float magicNumber = 0.46082947f;

        float minHeight = GetOffsetValue(-1f, f);  // equivalent of getOffsetValue(-1.0F, f)
        float maxHeight = GetOffsetValue(1f, f);   // getOffsetValue(1.0F, f)

        float highMid = 1f - (1f - f) * 0.5f;        // scales f into [0.5, 1] range
        float lowMid = 0.5f * (1f - f);             // scales f into [0, 0.5] range
        highMid = lowMid / (magicNumber * highMid) - 1.17f;     // adjusts l using magic numbers

        if (highMid > -0.65f && highMid < 1f)
        {
            //offset value & slope here, it will be y value in spline
            //magic thing but cant help
            float p = GetOffsetValue(-0.75f, f);
            float q = (p - minHeight) * 4f; // slope lowerbound -> p
            float r = GetOffsetValue(highMid, f);
            float s = (maxHeight - r) / (1f - highMid); // slope maxheight to highmid
            lowMid = GetOffsetValue(-0.65f, f); //reuse var?

            //add value to spline from left to right, using offset we calculate
            AddSplineVal(spline, -1f, CreateFixSpline(minHeight), q);
            AddSplineVal(spline, -0.75f, CreateFixSpline(p), 0f);
            AddSplineVal(spline, -0.65f, CreateFixSpline(lowMid), 0f);
            AddSplineVal(spline, highMid - 0.01f, CreateFixSpline(r), 0f);
            AddSplineVal(spline, highMid, CreateFixSpline(r), s);
            AddSplineVal(spline, 1f, CreateFixSpline(maxHeight), s);
        }
        else
        {
            //div 0.5?
            float u = (maxHeight - minHeight) * 0.5f;

            // If "bl" (boolean flag) is true, clamp i to minimum 0.2
            if (bl)
            {
                AddSplineVal(spline, -1f, CreateFixSpline(minHeight > 0.2f ? minHeight : 0.2f), 0f);

                // Middle control point between i and k using linear interpolation
                AddSplineVal(spline, 0f, CreateFixSpline(Mathf.Lerp(minHeight, maxHeight, 0.5f)), u);
            }
            else
            {
                //add the start point normally
                AddSplineVal(spline, -1f, CreateFixSpline(minHeight), u);
            }
            //end control point
            AddSplineVal(spline, 1f, CreateFixSpline(maxHeight), u);
        }
        return spline;
    }

    //spline for peaks and valley (sp4-sp7)
    public static Spline UpperRidgesSpline(float y0, float y1, float y2, float y3, float y4, float minSlope)
    {
        Spline sp = new Spline();
        sp.type = SplineType.Ridges;

        // Compute control slopes
        float l = 0.5f * (y1 - y0);
        if (l < minSlope) l = minSlope;

        float m = 5.0f * (y2 - y1);

        // Add spline control points with their derivatives
        AddSplineVal(sp, -1f, CreateFixSpline(y0), l);
        //conditional slope: need to be less
        AddSplineVal(sp, -0.4f, CreateFixSpline(y1), l < m ? l : m);
        AddSplineVal(sp, 0f, CreateFixSpline(y2), m);
        AddSplineVal(sp, 0.4f, CreateFixSpline(y3), 2f * (y3 - y2));
        AddSplineVal(sp, 1f, CreateFixSpline(y4), 0.7f * (y4 - y3));

        return sp;
    }

    //spline for erosion
    //********Very Magic May Simplify Later***********
    static Spline ErosionSpline(float f, float g, float h, float i, float j, float k, bool bl)
    {
        // Generate smaller component splines (very magic)
        Spline sp1 = LowerRidgesSpline(Mathf.Lerp(i, 0.6f, 1.5f), bl);
        Spline sp2 = LowerRidgesSpline(Mathf.Lerp(i, 0.6f, 1.0f), bl);
        Spline sp3 = LowerRidgesSpline(i, bl);

        Spline sp4 = UpperRidgesSpline(f - 0.15f, i * 0.5f, i * 0.5f, i * 0.5f, i * 0.6f, 0.5f);
        Spline sp5 = UpperRidgesSpline(f, j * i, g * i, i * 0.5f, i * 0.6f, 0.5f);
        Spline sp6 = UpperRidgesSpline(f, j, j, g, h, 0.5f);
        Spline sp7 = UpperRidgesSpline(f, j, j, g, h, 0.5f);

        // Create intermediate ridge spline (sp8)
        Spline sp8 = new Spline();
        sp8.type = SplineType.Ridges;
        AddSplineVal(sp8, -1.0f, CreateFixSpline(f), 0.0f);
        AddSplineVal(sp8, -0.4f, sp6, 0.0f);
        AddSplineVal(sp8, 0.0f, CreateFixSpline(h + 0.07f), 0.0f);

        // Create final erosion spline (main return)
        Spline sp9 = UpperRidgesSpline(-0.02f, k, k, g, h, 0.0f);
        Spline sp = new Spline();
        sp.type = SplineType.Erosion;

        AddSplineVal(sp, -0.85f, sp1, 0.0f);
        AddSplineVal(sp, -0.7f, sp2, 0.0f);
        AddSplineVal(sp, -0.4f, sp3, 0.0f);
        AddSplineVal(sp, -0.35f, sp4, 0.0f);
        AddSplineVal(sp, -0.1f, sp5, 0.0f);
        AddSplineVal(sp, 0.2f, sp6, 0.0f);

        if (bl)
        {
            AddSplineVal(sp, 0.4f, sp7, 0.0f);
            AddSplineVal(sp, 0.45f, sp8, 0.0f);
            AddSplineVal(sp, 0.55f, sp8, 0.0f);
            AddSplineVal(sp, 0.58f, sp7, 0.0f);
        }

        AddSplineVal(sp, 0.7f, sp9, 0.0f);

        return sp;
    }

    //first spline (main) : continentalness 
    public static void InitContinentalnessSpline(Spline mainSpline)
    {

        //assign type
        mainSpline.type = SplineType.Continentalness;

        Spline sp1 = ErosionSpline(-0.15F, 0.00F, 0.0F, 0.1F, 0.00F, -0.03F, false);
        Spline sp2 = ErosionSpline(-0.10F, 0.03F, 0.1F, 0.1F, 0.01F, -0.03F, false);
        Spline sp3 = ErosionSpline(-0.10F, 0.03F, 0.1F, 0.7F, 0.01F, -0.03F, true);
        Spline sp4 = ErosionSpline(-0.05F, 0.03F, 0.1F, 1.0F, 0.01F, 0.01F, true);

        AddSplineVal(mainSpline, -1.10f, CreateFixSpline(0.044f), 0.0f);
        AddSplineVal(mainSpline, -1.02f, CreateFixSpline(-0.2222f), 0.0f);
        AddSplineVal(mainSpline, -0.51f, CreateFixSpline(-0.2222f), 0.0f);
        AddSplineVal(mainSpline, -0.44f, CreateFixSpline(-0.12f), 0.0f);
        AddSplineVal(mainSpline, -0.18f, CreateFixSpline(-0.12f), 0.0f);
        AddSplineVal(mainSpline, -0.16F, sp1, 0.0F);
        AddSplineVal(mainSpline, -0.15F, sp1, 0.0F);
        AddSplineVal(mainSpline, -0.10F, sp2, 0.0F);
        AddSplineVal(mainSpline, 0.25F, sp3, 0.0F);
        AddSplineVal(mainSpline, 1.00F, sp4, 0.0F);
    }

    //let other script init spline and do the calculation
    
}
