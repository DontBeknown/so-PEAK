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
}
