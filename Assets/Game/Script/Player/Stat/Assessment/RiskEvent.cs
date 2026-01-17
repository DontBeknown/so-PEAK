using System;
using UnityEngine;

/// <summary>
/// Represents a risky situation the player encountered or avoided
/// </summary>
[Serializable]
public class RiskEvent
{
    public RiskType riskType;
    public Vector3 location;
    public float timestamp;
    public bool wasEncountered;     // true if player didn't avoid it
    public float severity;          // 0-1 scale
}
