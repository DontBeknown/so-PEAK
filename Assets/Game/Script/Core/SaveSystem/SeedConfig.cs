using UnityEngine;

[CreateAssetMenu(fileName = "SeedConfig", menuName = "Game/Config/Seed Config")]
public class SeedConfig : ScriptableObject
{
    [Header("Seed Part Lengths")]
    [Tooltip("Number of digits in Seed Part 1")]
    [Range(4, 12)]
    public int seed1DigitCount = 8;
    
    [Tooltip("Number of digits in Seed Part 2")]
    [Range(4, 12)]
    public int seed2DigitCount = 8;
    
    [Tooltip("Number of digits in Seed Part 3")]
    [Range(4, 12)]
    public int seed3DigitCount = 8;
    
    [Header("Generation Settings")]
    [Tooltip("Use current time as seed if not specified")]
    public bool useTimeAsDefaultSeed = true;
    
    [Tooltip("Maximum seed value (Total digits)")]
    public int maxSeedLength = 24;
    
    // Calculated total length
    public int TotalDigitCount => seed1DigitCount + seed2DigitCount + seed3DigitCount;
    
    // Validation
    private void OnValidate()
    {
        if (TotalDigitCount > maxSeedLength)
        {
            Debug.LogWarning($"Total seed length ({TotalDigitCount}) exceeds maximum ({maxSeedLength})");
        }
    }
}
