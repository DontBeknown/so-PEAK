using System;

/// <summary>
/// Defines all types of stat modifications that equipment can provide.
/// Based on the equipment system diagram.
/// </summary>
public enum StatModifierType
{
    // Walk Speed Modifiers
    UniversalWalkSpeed,      // Affects all walk speed
    NormalWalkSpeed,         // Affects normal terrain walk speed
    WalkSpeedSlope,          // Affects walk speed on slopes
    
    // Climb Speed Modifiers
    ClimbSpeed,              // Affects climbing speed
    
    // Stamina Reduction Modifiers (reduce stamina drain)
    UniversalStaminaReduce,  // Reduces all stamina drain
    WalkStaminaReduce,       // Reduces stamina drain while walking
    ClimbStaminaReduce,      // Reduces stamina drain while climbing
    PenaltyFatigueReduce,    // Reduces stamina penalty from fatigue
    
    // Fatigue Modifiers
    UniversalFatigueReduce,  // Reduces all fatigue accumulation
    SlopeFatigueReduce,      // Reduces fatigue on slopes
    FatigueGainWhenRest      // Increases fatigue recovery when resting
}
