using System;

/// <summary>
/// Contains optimal/expected performance values for comparison
/// </summary>
[Serializable]
public class OptimalMetrics
{
    public float expectedStamina;
    public int expectedFoodItems;        // Expected number of food items needed
    public int expectedWaterItems;       // Expected number of water items needed
    public float optimalDistance;
    public float optimalTime;

    public float GetMinimumDistance(float tolerancePercent = 10f)
    {
        return optimalDistance * (1f - tolerancePercent / 100f);
    }

    public float GetMaximumDistance(float tolerancePercent = 10f)
    {
        return optimalDistance * (1f + tolerancePercent / 100f);
    }

    public float GetMinimumTime(float tolerancePercent = 10f)
    {
        return optimalTime * (1f - tolerancePercent / 100f);
    }

    public float GetMaximumTime(float tolerancePercent = 10f)
    {
        return optimalTime * (1f + tolerancePercent / 100f);
    }
}
