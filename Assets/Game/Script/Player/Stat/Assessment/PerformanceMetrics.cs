using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains raw metrics collected during expedition
/// </summary>
[Serializable]
public class PerformanceMetrics
{
    // Resource Usage
    public float totalStaminaUsed;
    public int totalFoodItemsConsumed;      // Number of food items eaten
    public int totalWaterItemsConsumed;     // Number of water/drink items consumed
    
    // Distance & Time
    public float totalDistance;
    public float totalTime;
    
    // Safety Events
    public int totalRiskyEvents;        // Total possible risky events
    public int encounterredRisks;       // Actual risks encountered
    public int healthLossIncidents;     // Times health was damaged
    public float totalHealthLost;
    
    // Path Planning
    public float actualPathCost;
    public float optimalPathCost;
    public List<Vector3> pathTaken;     // For analysis
    
    // Additional Context
    public float weatherSeverity;       // Weather impact factor (0-1)
}
