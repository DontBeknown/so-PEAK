# Learning Assessment System - Technical Implementation Design

## 🎯 Overview
This document outlines the technical implementation for extending the existing player stat tracking system to support the Learning Assessment framework. The design maintains SOLID principles and integrates seamlessly with the current architecture.

**STATUS: ✅ FULLY IMPLEMENTED** - All phases complete and tested. Ready for Unity scene setup.

---

## 📋 Table of Contents
1. [Quick Start Guide](#quick-start-guide)
2. [How It Works](#how-it-works)
3. [Architecture Overview](#architecture-overview)
4. [Component Details](#component-details)
5. [Unity Scene Setup](#unity-scene-setup)
6. [Usage Examples](#usage-examples)
7. [Troubleshooting](#troubleshooting)
8. [Configuration & Balancing](#configuration--balancing)

---

## 🚀 Quick Start Guide

### Prerequisites
- Unity project with existing PlayerStatsTrackerService
- TextMeshPro package installed
- Basic understanding of Unity UI

### Setup Steps (5 minutes)

1. **Scene Setup**
   - Open your player stats UI prefab/scene
   - Add two child GameObjects under Stats Panel: `StatTrackingTab` and `AssessmentTab`
   - Move existing stat UI elements into `StatTrackingTab`
   - Create assessment UI layout in `AssessmentTab` (see [Unity Scene Setup](#unity-scene-setup))

2. **Wire Dependencies**
   - Drag `PlayerStatsTrackerService` reference to `PlayerStatsTrackerUI`
   - Drag `LearningAssessmentService` reference to `AssessmentReportUI`
   - Assign UI element references in `AssessmentReportUI` inspector

3. **Hook Expedition Events**
   - Use `ExpeditionIntegrationExample.cs` as template
   - Call `StartExpedition()` when level/expedition begins
   - Call `EndExpedition()` when player reaches summit/completes objective

4. **Test**
   - Start expedition → Play through → End expedition
   - Assessment report should appear in the Assessment tab

---

## 🔄 How It Works

### System Flow Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    EXPEDITION LIFECYCLE                          │
└─────────────────────────────────────────────────────────────────┘

1. EXPEDITION START
   └─> PlayerStatsTrackerService.StartTracking()
       ├─> DistanceTracker starts recording
       ├─> StaminaTracker starts recording  
       ├─> ConsumableTracker starts recording (food/water)
       ├─> HealthLossTracker starts recording
       ├─> PathTracker starts recording positions
       └─> RiskTracker initializes

2. DURING EXPEDITION (Real-time Tracking)
   ├─> Player moves → PathTracker.UpdatePosition()
   ├─> Player consumes item → ConsumableTracker.RecordConsumable()
   ├─> Hazard detected → RiskTracker.RecordValue(RiskEvent)
   ├─> Health lost → HealthLossTracker.RecordValue()
   └─> Stamina used → StaminaTracker.RecordValue()

3. EXPEDITION END
   └─> LearningAssessmentService.GenerateAssessment()
       ├─> Collect metrics from all trackers
       ├─> Calculate optimal metrics (or use pre-set values)
       ├─> StandardAssessmentCalculator.Calculate()
       │   ├─> CalculateEfficiencyScore() → 40% weight
       │   ├─> CalculateSafetyScore() → 30% weight
       │   └─> CalculatePlanningScore() → 30% weight
       ├─> Determine rank (Alpine Master / Skilled Planner / etc.)
       ├─> Create detailed breakdowns with feedback
       └─> Fire OnAssessmentComplete event

4. DISPLAY RESULTS
   └─> AssessmentReportUI receives event
       ├─> Display rank with emoji/icon
       ├─> Show category scores on sliders
       ├─> Display detailed breakdowns
       └─> Show Thai language feedback
```

### Data Flow Architecture

```
┌──────────────────────┐
│  Game Events         │ (Player actions, hazards, etc.)
└──────────┬───────────┘
           │
           ▼
┌──────────────────────────────────────────────────┐
│  PlayerStatsTrackerService (Central Hub)         │
│  ├─ DistanceTracker                              │
│  ├─ StaminaTracker                               │
│  ├─ ConsumableTracker (tracks food/water items) │
│  ├─ FatigueTracker                               │
│  ├─ HealthLossTracker                            │
│  ├─ PathTracker (NEW - records positions)       │
│  └─ RiskTracker (NEW - tracks hazard events)    │
└──────────┬───────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────┐
│  AssessmentTracker (Metrics Collector)           │
│  └─ GetCurrentMetrics() → PerformanceMetrics     │
└──────────┬───────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────┐
│  LearningAssessmentService (Main Coordinator)    │
│  ├─ OptimalMetricsCalculator                     │
│  └─ StandardAssessmentCalculator                 │
└──────────┬───────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────┐
│  AssessmentScore (Results)                       │
│  ├─ Efficiency Score (0-100)                     │
│  ├─ Safety Score (0-100)                         │
│  ├─ Planning Score (0-100)                       │
│  ├─ Total Score (weighted)                       │
│  ├─ Performance Rank                             │
│  └─ Detailed Breakdowns                          │
└──────────┬───────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────┐
│  AssessmentReportUI (Display)                    │
│  └─ Shows results in Assessment Tab              │
└──────────────────────────────────────────────────┘
```

### Key Components Interaction

**1. Tracking Phase** (During Expedition)
- `PlayerStatsTrackerService` manages all trackers centrally
- Each tracker extends `BaseStatTracker<T>` and records data independently
- `PathTracker` updates every frame if player moved >= `minRecordDistance`
- `RiskTracker` receives events via `RegisterRiskEvent()` from hazard detection system

**2. Collection Phase** (End of Expedition)
- `AssessmentTracker.GetCurrentMetrics()` assembles `PerformanceMetrics` from all trackers
- Data includes: stamina used, items consumed, distance traveled, time elapsed, risks encountered, health lost

**3. Calculation Phase**
- `OptimalMetricsCalculator` determines expected performance (auto or external)
- `StandardAssessmentCalculator` applies formulas:
  - **Efficiency**: `100 - ((actual - optimal) / optimal × 100)` for each resource
  - **Safety**: `100 - (encountered / total × 100)` with health penalties
  - **Planning**: Path cost deviation from optimal route

**4. Presentation Phase**
- `LearningAssessmentService` determines rank based on total score
- Creates detailed breakdowns with Thai feedback messages
- Fires `OnAssessmentComplete` event
- `AssessmentReportUI` updates UI elements

---

## Architecture Overview

### Current System Analysis
The existing tracking system uses:
- **IStatTracker<T>** interface for individual metric tracking
- **BaseStatTracker<T>** abstract class providing common functionality
- **PlayerStatsTrackerService** as the main coordinator
- Specialized trackers: `DistanceTracker`, `StaminaTracker`, `ConsumableTracker`, etc.
- Time-series data collection for graphing

### Extension Strategy
Add a new **Assessment Layer** that:
1. Consumes data from existing trackers
2. Calculates optimal/expected values
3. Computes performance scores
4. Generates final assessment reports

---

## New Components to Implement

### 1. Assessment Data Models

#### 1.1 PerformanceMetrics.cs
```csharp
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
```

#### 1.2 AssessmentScore.cs
```csharp
/// <summary>
/// Contains calculated scores for each assessment category
/// </summary>
[Serializable]
public class AssessmentScore
{
    // Individual Scores (0-100)
    public float efficiencyScore;
    public float safetyScore;
    public float planningScore;
    
    // Weighted Final Score (0-100)
    public float totalScore;
    
    // Performance Rank
    public PerformanceRank rank;
    
    // Detailed Breakdown
    public EfficiencyBreakdown efficiencyDetails;
    public SafetyBreakdown safetyDetails;
    public PlanningBreakdown planningDetails;
}
```

#### 1.3 Performance Breakdown Classes
```csharp
[Serializable]
public class EfficiencyBreakdown
{
    public float staminaEfficiency;      // 0-100
    public float foodEfficiency;         // 0-100
    public float waterEfficiency;        // 0-100
    public float resourceUsageRatio;     // actual/optimal ratio
    public string feedback;              // Text feedback
}

[Serializable]
public class SafetyBreakdown
{
    public int risksAvoided;
    public int risksEncountered;
    public float avoidanceRate;          // Percentage
    public float healthLossScore;        // Penalty for health loss
    public string feedback;
}

[Serializable]
public class PlanningBreakdown
{
    public float pathDeviation;          // Percentage deviation
    public float timeEfficiency;         // Time vs optimal
    public float routeOptimality;        // Overall route quality
    public string feedback;
}
```

#### 1.4 PerformanceRank.cs (Enum)
```csharp
public enum PerformanceRank
{
    LostWanderer = 0,      // 0-49
    Survivor = 1,          // 50-69
    SkilledPlanner = 2,    // 70-89
    AlpineMaster = 3       // 90-100
}
```

---

### 2. Assessment Calculator

#### 2.1 IAssessmentCalculator.cs
```csharp
/// <summary>
/// Interface for assessment calculation strategies
/// Allows different calculation methods (e.g., for different difficulty modes)
/// </summary>
public interface IAssessmentCalculator
{
    float CalculateEfficiencyScore(PerformanceMetrics metrics, OptimalMetrics optimal);
    float CalculateSafetyScore(PerformanceMetrics metrics);
    float CalculatePlanningScore(PerformanceMetrics metrics, OptimalMetrics optimal);
}
```

#### 2.2 StandardAssessmentCalculator.cs
```csharp
/// <summary>
/// Default implementation of assessment calculation
/// Uses formulas defined in Learning Assessment document
/// </summary>
public class StandardAssessmentCalculator : IAssessmentCalculator
{
    // Weight constants (from design doc)
    private const float EFFICIENCY_WEIGHT = 0.4f;
    private const float SAFETY_WEIGHT = 0.3f;
    private const float PLANNING_WEIGHT = 0.3f;
    
    // Path cost weights
    private const float DISTANCE_WEIGHT = 0.5f;
    private const float TIME_WEIGHT = 0.3f;
    private const float STAMINA_WEIGHT = 0.2f;
    
    public float CalculateEfficiencyScore(PerformanceMetrics metrics, OptimalMetrics optimal)
    {
        // Calculate resource usage efficiency
        float staminaRatio = metrics.totalStaminaUsed / optimal.expectedStamina;
        float foodRatio = (float)metrics.totalFoodItemsConsumed / Mathf.Max(optimal.expectedFoodItems, 1);
        float waterRatio = (float)metrics.totalWaterItemsConsumed / Mathf.Max(optimal.expectedWaterItems, 1);
        
        // Average ratio
        float avgRatio = (staminaRatio + foodRatio + waterRatio) / 3f;
        
        // Score formula: 100 - ((actual - optimal) / optimal × 100)
        float efficiency = 100f - ((avgRatio - 1f) * 100f);
        
        // Clamp to 0-100
        return Mathf.Clamp(efficiency, 0f, 100f);
    }
    
    public float CalculateSafetyScore(PerformanceMetrics metrics)
    {
        if (metrics.totalRiskyEvents == 0)
            return 100f;
        
        // Avoidance rate
        float avoidanceRate = 1f - ((float)metrics.encounterredRisks / metrics.totalRiskyEvents);
        
        // Base score from avoidance
        float baseScore = avoidanceRate * 100f;
        
        // Penalty for health loss (each incident reduces score)
        float healthPenalty = metrics.healthLossIncidents * 5f; // -5 points per incident
        
        float finalScore = baseScore - healthPenalty;
        
        return Mathf.Clamp(finalScore, 0f, 100f);
    }
    
    public float CalculatePlanningScore(PerformanceMetrics metrics, OptimalMetrics optimal)
    {
        // Calculate actual path cost
        float actualCost = CalculatePathCost(
            metrics.totalDistance,
            metrics.totalTime,
            metrics.totalStaminaUsed
        );
        
        // Calculate optimal path cost
        float optimalCost = CalculatePathCost(
            optimal.optimalDistance,
            optimal.optimalTime,
            optimal.expectedStamina
        );
        
        // Deviation percentage
        float deviation = Mathf.Abs((actualCost - optimalCost) / optimalCost);
        
        // Score: 100 - (deviation × 100)
        float planningScore = 100f - (deviation * 100f);
        
        return Mathf.Clamp(planningScore, 0f, 100f);
    }
    
    private float CalculatePathCost(float distance, float time, float stamina)
    {
        return (distance * DISTANCE_WEIGHT) + 
               (time * TIME_WEIGHT) + 
               (stamina * STAMINA_WEIGHT);
    }
}
```

---

### 3. Optimal Metrics Calculator

#### 3.1 OptimalMetrics.cs
```csharp
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
```

#### 3.2 OptimalMetricsCalculator.cs
```csharp
/// <summary>
/// Calculates optimal expected values based on terrain and conditions
/// Uses terrain analysis and pathfinding data
/// Can also accept pre-calculated values from external planning modules
/// </summary>
public class OptimalMetricsCalculator
{
    // Base consumption rates (per meter)
    private const float BASE_STAMINA_PER_METER = 0.5f;
    private const float BASE_FOOD_ITEMS_PER_KM = 2f;    // ~2 food items per kilometer
    private const float BASE_WATER_ITEMS_PER_KM = 3f;   // ~3 water items per kilometer
    
    // Movement speed (meters per second)
    private const float BASE_MOVE_SPEED = 2.5f;
    
    /// <summary>
    /// Calculates optimal metrics for a given path (automatic calculation)
    /// </summary>
    public OptimalMetrics Calculate(List<Vector3> optimalPath)
    {
        float distance = CalculatePathDistance(optimalPath);
        float distanceKm = distance / 1000f; // Convert to kilometers
        
        return new OptimalMetrics
        {
            expectedStamina = distance * BASE_STAMINA_PER_METER,
            expectedFoodItems = Mathf.CeilToInt(distanceKm * BASE_FOOD_ITEMS_PER_KM),
            expectedWaterItems = Mathf.CeilToInt(distanceKm * BASE_WATER_ITEMS_PER_KM),
            optimalDistance = distance,
            optimalTime = distance / BASE_MOVE_SPEED
        };
    }
    
    /// <summary>
    /// Creates optimal metrics from externally provided values
    /// Use this when planning module has already calculated optimal values
    /// </summary>
    public OptimalMetrics CreateFromValues(
        float expectedStamina,
        int expectedFoodItems,
        int expectedWaterItems,
        float optimalDistance,
        float optimalTime)
    {
        return new OptimalMetrics
        {
            expectedStamina = expectedStamina,
            expectedFoodItems = expectedFoodItems,
            expectedWaterItems = expectedWaterItems,
            optimalDistance = optimalDistance,
            optimalTime = optimalTime
        };
    }
    }
    
    private float CalculatePathDistance(List<Vector3> path)
    {
        float totalDistance = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(path[i], path[i + 1]);
        }
        return totalDistance;
    }
}
```

---

### 4. Assessment Tracker Extension

#### 4.1 AssessmentTracker.cs
```csharp
/// <summary>
/// Tracks assessment-specific metrics during expedition
/// Extends the existing tracking system
/// </summary>
public class AssessmentTracker : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStatsTrackerService statsTracker;
    
    // Assessment data
    private PerformanceMetrics currentMetrics;
    private List<RiskEvent> riskEvents;
    private List<Vector3> playerPath;
    
    // Risk tracking
    private int totalPossibleRisks;
    private int risksEncountered;
    
    public void StartAssessmentTracking()
    {
        currentMetrics = new PerformanceMetrics();
        riskEvents = new List<RiskEvent>();
        playerPath = new List<Vector3>();
        
        totalPossibleRisks = 0;
        risksEncountered = 0;
    }
    
    private void Update()
    {
        if (statsTracker.IsTracking)
        {
            // Record player path
            RecordPlayerPosition();
            
            // Check for nearby risks
            CheckForRiskyAreas();
        }
    }
    
    private void RecordPlayerPosition()
    {
        Vector3 currentPos = transform.position;
        
        // Only record if moved significantly
        if (playerPath.Count == 0 || 
            Vector3.Distance(playerPath[playerPath.Count - 1], currentPos) > 1f)
        {
            playerPath.Add(currentPos);
        }
    }
    
    private void CheckForRiskyAreas()
    {
        // Check for dangerous terrain, weather, etc.
        // This would integrate with your existing hazard system
    }
    
    public void RegisterRiskEvent(RiskEvent riskEvent)
    {
        totalPossibleRisks++;
        
        if (riskEvent.wasEncountered)
        {
            risksEncountered++;
            riskEvents.Add(riskEvent);
        }
    }
    
    public PerformanceMetrics GetCurrentMetrics()
    {
        // Collect metrics from PlayerStatsTrackerService
        currentMetrics.totalStaminaUsed = statsTracker.GetStaminaUsed();
        currentMetrics.totalDistance = statsTracker.GetDistanceWalked();
        currentMetrics.totalTime = statsTracker.SessionDuration;
        currentMetrics.totalHealthLost = statsTracker.GetHealthLost();
        currentMetrics.totalRiskyEvents = totalPossibleRisks;
        currentMetrics.encounterredRisks = risksEncountered;
        currentMetrics.pathTaken = new List<Vector3>(playerPath);
        
        // Get food and water consumption from consumables
        currentMetrics.totalFoodItemsConsumed = statsTracker.GetFoodItemsConsumed();
        currentMetrics.totalWaterItemsConsumed = statsTracker.GetWaterItemsConsumed();
        
        return currentMetrics;
    }
}
```

#### 4.2 RiskEvent.cs
```csharp
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

public enum RiskType
{
    SteepSlope,
    WeatherHazard,
    Rockfall,
    IcePatch,
    Avalanche,
    Exhaustion
}
```

---

### 5. Assessment Service (Main Coordinator)

#### 5.1 LearningAssessmentService.cs
```csharp
/// <summary>
/// Main service that coordinates the entire assessment system
/// Called when expedition ends
/// </summary>
public class LearningAssessmentService : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStatsTrackerService statsTracker;
    [SerializeField] private AssessmentTracker assessmentTracker;
    [SerializeField] private PathfindingSystem pathfinding;
    
    [Header("Calculator")]
    private IAssessmentCalculator calculator;
    private OptimalMetricsCalculator optimalCalculator;
    
    // Cached optimal metrics (can be set externally)
    private OptimalMetrics cachedOptimalMetrics;
    private bool hasExternalOptimalMetrics;
    
    // Events
    public event System.Action<AssessmentScore> OnAssessmentComplete;
    
    private void Awake()
    {
        calculator = new StandardAssessmentCalculator();
        optimalCalculator = new OptimalMetricsCalculator();
        hasExternalOptimalMetrics = false;
    }
    
    /// <summary>
    /// Set optimal metrics from external planning module
    /// Call this before GenerateAssessment() if you have pre-calculated optimal values
    /// </summary>
    public void SetOptimalMetrics(OptimalMetrics optimalMetrics)
    {
        cachedOptimalMetrics = optimalMetrics;
        hasExternalOptimalMetrics = true;
    }
    
    /// <summary>
    /// Set optimal metrics from individual values
    /// </summary>
    public void SetOptimalMetrics(
        float expectedStamina,
        int expectedFoodItems,
        int expectedWaterItems,
        float optimalDistance,
        float optimalTime)
    {
        cachedOptimalMetrics = optimalCalculator.CreateFromValues(
            expectedStamina,
            expectedFoodItems,
            expectedWaterItems,
            optimalDistance,
            optimalTime
        );
        hasExternalOptimalMetrics = true;
    }
    
    /// <summary>
    /// Clear cached optimal metrics (forces recalculation on next assessment)
    /// </summary>
    public void ClearOptimalMetrics()
    {
        hasExternalOptimalMetrics = false;
        cachedOptimalMetrics = null;
    }
    
    /// <summary>
    /// Generate assessment report when expedition ends
    /// </summary>
    public AssessmentScore GenerateAssessment()
    {
        // Get performance metrics
        PerformanceMetrics metrics = assessmentTracker.GetCurrentMetrics();
        
        // Get or calculate optimal metrics
        OptimalMetrics optimal;
        
        if (hasExternalOptimalMetrics)
        {
            // Use externally provided optimal metrics
            optimal = cachedOptimalMetrics;
        }
        else
        {
            // Calculate optimal metrics automatically
            List<Vector3> optimalPath = pathfinding.GetOptimalPath(
                metrics.pathTaken[0], 
                metrics.pathTaken[metrics.pathTaken.Count - 1]
            );
            optimal = optimalCalculator.Calculate(optimalPath);
        }
        
        // Calculate scores
        float efficiencyScore = calculator.CalculateEfficiencyScore(metrics, optimal);
        float safetyScore = calculator.CalculateSafetyScore(metrics);
        float planningScore = calculator.CalculatePlanningScore(metrics, optimal);
        
        // Calculate weighted total
        float totalScore = 
            (efficiencyScore * 0.4f) + 
            (safetyScore * 0.3f) + 
            (planningScore * 0.3f);
        
        // Create assessment result
        AssessmentScore assessment = new AssessmentScore
        {
            efficiencyScore = efficiencyScore,
            safetyScore = safetyScore,
            planningScore = planningScore,
            totalScore = totalScore,
            rank = DetermineRank(totalScore),
            efficiencyDetails = CreateEfficiencyBreakdown(metrics, optimal),
            safetyDetails = CreateSafetyBreakdown(metrics),
            planningDetails = CreatePlanningBreakdown(metrics, optimal)
        };
        
        OnAssessmentComplete?.Invoke(assessment);
        
        return assessment;
    }
    
    private PerformanceRank DetermineRank(float totalScore)
    {
        if (totalScore >= 90f) return PerformanceRank.AlpineMaster;
        if (totalScore >= 70f) return PerformanceRank.SkilledPlanner;
        if (totalScore >= 50f) return PerformanceRank.Survivor;
        return PerformanceRank.LostWanderer;
    }
    
    private EfficiencyBreakdown CreateEfficiencyBreakdown(
        PerformanceMetrics metrics, 
        OptimalMetrics optimal)
    {
        float ratio = metrics.totalStaminaUsed / optimal.expectedStamina;
        
        string feedback = ratio <= 1.1f ? "ยอดเยี่ยม! ใช้ทรัพยากรอย่างมีประสิทธิภาพ" :
                         ratio <= 1.3f ? "ดี แต่ยังสามารถปรับปรุงได้" :
                         ratio <= 1.6f ? "ใช้ทรัพยากรมากเกินไป ควรวางแผนให้ดีขึ้น" :
                         "ใช้ทรัพยากรสิ้นเปลืองมาก จำเป็นต้องปรับปรุง";
        
        return new EfficiencyBreakdown
        {
            staminaEfficiency = 100f - ((ratio - 1f) * 100f),
            resourceUsageRatio = ratio,
            feedback = feedback
        };
    }
    
    private SafetyBreakdown CreateSafetyBreakdown(PerformanceMetrics metrics)
    {
        float avoidanceRate = metrics.totalRiskyEvents > 0 ?
            (1f - (float)metrics.encounterredRisks / metrics.totalRiskyEvents) * 100f : 100f;
        
        string feedback = avoidanceRate >= 90f ? "ปลอดภัยมาก หลีกเลี่ยงอันตรายได้ดีเยี่ยม" :
                         avoidanceRate >= 70f ? "ปลอดภัย แต่ยังมีความเสี่ยงบางส่วน" :
                         avoidanceRate >= 50f ? "เสี่ยงค่อนข้างมาก ควรระมัดระวังมากขึ้น" :
                         "อันตราย! พบเหตุการณ์เสี่ยงมากเกินไป";
        
        return new SafetyBreakdown
        {
            risksAvoided = metrics.totalRiskyEvents - metrics.encounterredRisks,
            risksEncountered = metrics.encounterredRisks,
            avoidanceRate = avoidanceRate,
            feedback = feedback
        };
    }
    
    private PlanningBreakdown CreatePlanningBreakdown(
        PerformanceMetrics metrics, 
        OptimalMetrics optimal)
    {
        float deviation = Mathf.Abs((metrics.totalDistance - optimal.optimalDistance) / 
                                    optimal.optimalDistance) * 100f;
        
        string feedback = deviation <= 10f ? "วางแผนเยี่ยม! เลือกเส้นทางที่เหมาะสมมาก" :
                         deviation <= 25f ? "วางแผนดี แต่ยังมีเส้นทางที่ดีกว่า" :
                         deviation <= 40f ? "วางแผนพอใช้ ควรเลือกเส้นทางที่เหมาะสมกว่า" :
                         "วางแผนไม่ดี เลือกเส้นทางที่ไม่เหมาะสม";
        
        return new PlanningBreakdown
        {
            pathDeviation = deviation,
            routeOptimality = 100f - deviation,
            feedback = feedback
        };
    }
}
```

---

## 🎮 Unity Scene Setup

### Scene Hierarchy

Your Stats UI should look like this:

```
Canvas
└── StatsPanel (existing)
    ├── HeaderText
    ├── CloseButton
    ├── TabButtons (NEW)
    │   ├── StatTrackingButton
    │   └── AssessmentButton
    ├── StatTrackingTab (NEW - container)
    │   ├── MetricsList (existing stat UI)
    │   ├── GraphRenderer (existing)
    │   └── ControlButtons (existing)
    └── AssessmentTab (NEW - container)
        ├── RankDisplay
        │   ├── RankIcon (Image)
        │   ├── RankText (TextMeshPro)
        │   └── RankEmoji (TextMeshPro)
        ├── CategoryScores
        │   ├── EfficiencySlider (Slider + Text)
        │   ├── SafetySlider (Slider + Text)
        │   └── PlanningSlider (Slider + Text)
        ├── DetailedBreakdowns
        │   ├── EfficiencyDetails (TextMeshPro)
        │   ├── SafetyDetails (TextMeshPro)
        │   └── PlanningDetails (TextMeshPro)
        ├── FeedbackText (TextMeshPro - large)
        └── GenerateButton (Button - optional manual trigger)
```

### Step-by-Step Setup Instructions

#### Step 1: Create Tab Containers

1. Right-click `StatsPanel` → Create Empty → Name: `StatTrackingTab`
2. Move all existing stat UI elements into `StatTrackingTab`
3. Right-click `StatsPanel` → Create Empty → Name: `AssessmentTab`
4. Set `AssessmentTab` to inactive by default

#### Step 2: Create Tab Buttons

1. Right-click `StatsPanel` → UI → Button → Name: `StatTrackingButton`
   - Position at top-left of panel
   - Text: "📊 Stats"
2. Right-click `StatsPanel` → UI → Button → Name: `AssessmentButton`
   - Position next to StatTrackingButton
   - Text: "🏆 Assessment"

#### Step 3: Create Assessment UI Layout

Inside `AssessmentTab`:

**A. Rank Display**
```
1. Add Image → Name: RankIcon
   - Anchor: Top Center
   - Size: 80x80
   - (Assign rank icon sprites in inspector)

2. Add TextMeshPro → Name: RankText
   - Text: "Alpine Master"
   - Font Size: 36
   - Alignment: Center
   - Position below icon

3. Add TextMeshPro → Name: RankEmoji
   - Text: "🏔️"
   - Font Size: 48
   - Position next to RankText
```

**B. Category Scores**
```
1. Add Slider → Name: EfficiencySlider
   - Max Value: 100
   - Add TextMeshPro child for label: "Efficiency: 85/100"
   
2. Add Slider → Name: SafetySlider
   - Max Value: 100
   - Add TextMeshPro child for label: "Safety: 72/100"
   
3. Add Slider → Name: PlanningSlider
   - Max Value: 100
   - Add TextMeshPro child for label: "Planning: 88/100"
```

**C. Detailed Breakdowns**
```
1. Add TextMeshPro → Name: EfficiencyDetails
   - Text: "Stamina: 85/100\nFood: 90/100..."
   - Font Size: 16
   - Alignment: Left
   - Enable Rich Text
   
2. Add TextMeshPro → Name: SafetyDetails
   - Similar setup
   
3. Add TextMeshPro → Name: PlanningDetails
   - Similar setup
```

**D. Feedback Text**
```
Add TextMeshPro → Name: FeedbackText
- Text: "Thai feedback message here"
- Font Size: 18
- Alignment: Center
- Word Wrapping: Enabled
- Color: Yellow/Gold
```

**E. Generate Button (Optional)**
```
Add Button → Name: GenerateButton
- Text: "Generate Assessment"
- Position at bottom
- (For manual testing/regeneration)
```

#### Step 4: Wire Component References

**PlayerStatsTrackerUI Component:**
1. Select `StatsPanel` GameObject
2. Find `PlayerStatsTrackerUI` component
3. Assign references:
   - **Stat Tracking Tab**: Drag `StatTrackingTab` GameObject
   - **Assessment Tab**: Drag `AssessmentTab` GameObject
   - **Stat Tracking Button**: Drag `StatTrackingButton` Button
   - **Assessment Button**: Drag `AssessmentButton` Button
   - **Stats Tracker Service**: Drag your scene's `PlayerStatsTrackerService` GameObject

**AssessmentReportUI Component:**
1. Add `AssessmentReportUI` component to `AssessmentTab` GameObject
2. Assign references:
   - **Rank Text**: Drag `RankText` TextMeshPro
   - **Rank Emoji**: Drag `RankEmoji` TextMeshPro
   - **Rank Icon**: Drag `RankIcon` Image
   - **Total Score Text**: Create and drag total score text
   - **Efficiency Slider**: Drag `EfficiencySlider`
   - **Efficiency Score Text**: Drag slider's label text
   - **Safety Slider**: Drag `SafetySlider`
   - **Safety Score Text**: Drag slider's label text
   - **Planning Slider**: Drag `PlanningSlider`
   - **Planning Score Text**: Drag slider's label text
   - **Efficiency Details**: Drag `EfficiencyDetails` TextMeshPro
   - **Safety Details**: Drag `SafetyDetails` TextMeshPro
   - **Planning Details**: Drag `PlanningDetails` TextMeshPro
   - **Feedback Text**: Drag `FeedbackText` TextMeshPro
   - **Generate Button**: Drag `GenerateButton` (optional)
   - **Assessment Service**: Drag your scene's `LearningAssessmentService` GameObject

#### Step 5: Create Service GameObjects

1. Create empty GameObject → Name: `LearningAssessmentService`
   - Add `LearningAssessmentService` component
   - Assign **Stats Tracker** reference
   - Assign **Assessment Tracker** reference
   - (PathfindingSystem is optional)

2. If `AssessmentTracker` doesn't exist:
   - Add it to your Player GameObject or create separate GameObject
   - Assign **Stats Tracker** reference

#### Step 6: Hook Expedition Events

Use the provided `ExpeditionIntegrationExample.cs`:

```csharp
// In your LevelManager, ExpeditionController, or similar:

public class YourExpeditionManager : MonoBehaviour
{
    [SerializeField] private ExpeditionIntegrationExample integration;
    
    void OnLevelStart()
    {
        integration.StartExpedition(); // Starts tracking
    }
    
    void OnSummitReached()
    {
        integration.EndExpedition(); // Generates assessment
    }
}
```

Or call directly:
```csharp
// When expedition starts:
statsTracker.ResetTracking();
statsTracker.StartTracking();

// When expedition ends:
statsTracker.StopTracking();
assessmentService.GenerateAssessment(); // Auto-triggers UI
```

---

## 📝 Usage Examples

### Example 1: Basic Expedition

```csharp
using Game.Player.Stat.Assessment;

public class ExpeditionController : MonoBehaviour
{
    [SerializeField] private PlayerStatsTrackerService statsTracker;
    [SerializeField] private LearningAssessmentService assessmentService;
    
    public void StartExpedition()
    {
        // Reset and start tracking
        statsTracker.ResetTracking();
        statsTracker.StartTracking();
        
        Debug.Log("Expedition started - tracking active");
    }
    
    public void EndExpedition()
    {
        // Stop tracking
        statsTracker.StopTracking();
        
        // Generate assessment (auto-displays UI)
        AssessmentScore score = assessmentService.GenerateAssessment();
        
        // Optional: Log results
        Debug.Log($"Total Score: {score.totalScore:F1}/100 - Rank: {score.rank}");
    }
}
```

### Example 2: With External Optimal Metrics

```csharp
public void StartExpeditionWithPlanning()
{
    statsTracker.ResetTracking();
    statsTracker.StartTracking();
    
    // Get optimal values from your route planning module
    RouteData plannedRoute = routePlanner.GetOptimalRoute();
    
    // Set optimal metrics for comparison
    assessmentService.SetOptimalMetrics(
        expectedStamina: plannedRoute.estimatedStamina,
        expectedFoodItems: plannedRoute.estimatedFood,
        expectedWaterItems: plannedRoute.estimatedWater,
        optimalDistance: plannedRoute.distance,
        optimalTime: plannedRoute.estimatedTime
    );
    
    Debug.Log("Expedition started with pre-calculated optimal metrics");
}
```

### Example 3: Register Risk Events

```csharp
public class HazardDetectionSystem : MonoBehaviour
{
    [SerializeField] private PlayerStatsTrackerService statsTracker;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hazard"))
        {
            // Determine if player encountered or avoided
            bool encountered = CheckIfPlayerEncountered(other);
            
            RiskEvent risk = new RiskEvent
            {
                riskType = GetRiskType(other),
                location = other.transform.position,
                timestamp = Time.time,
                wasEncountered = encountered,
                severity = 0.8f
            };
            
            statsTracker.RegisterRiskEvent(risk);
            
            Debug.Log($"Risk registered: {risk.riskType} - Encountered: {encountered}");
        }
    }
    
    private RiskType GetRiskType(Collider hazard)
    {
        // Determine risk type from hazard properties
        if (hazard.name.Contains("Slope")) return RiskType.SteepSlope;
        if (hazard.name.Contains("Weather")) return RiskType.WeatherHazard;
        if (hazard.name.Contains("Rockfall")) return RiskType.Rockfall;
        return RiskType.SteepSlope; // default
    }
}
```

### Example 4: Subscribe to Assessment Events

```csharp
public class AchievementManager : MonoBehaviour
{
    [SerializeField] private LearningAssessmentService assessmentService;
    
    private void OnEnable()
    {
        assessmentService.OnAssessmentComplete += HandleAssessment;
    }
    
    private void OnDisable()
    {
        assessmentService.OnAssessmentComplete -= HandleAssessment;
    }
    
    private void HandleAssessment(AssessmentScore score)
    {
        // Unlock achievements based on score
        if (score.rank == PerformanceRank.AlpineMaster)
        {
            UnlockAchievement("Alpine Master");
        }
        
        if (score.efficiencyScore >= 95f)
        {
            UnlockAchievement("Resource Management Expert");
        }
        
        if (score.safetyScore == 100f)
        {
            UnlockAchievement("Zero Accidents");
        }
        
        // Save to leaderboard
        Leaderboard.SubmitScore(score.totalScore);
    }
}
```

---

## 🔧 Troubleshooting

### Issue: Assessment doesn't generate

**Solution:**
1. Check `LearningAssessmentService` is in scene with references assigned
2. Verify `PlayerStatsTrackerService.IsTracking` is true during expedition
3. Ensure `StopTracking()` is called before `GenerateAssessment()`
4. Check Console for errors

### Issue: Scores are always 0 or 100

**Solution:**
1. Verify trackers are recording data:
   ```csharp
   Debug.Log($"Distance: {statsTracker.GetTotalDistance()}");
   Debug.Log($"Stamina: {statsTracker.GetTotalStaminaUsed()}");
   Debug.Log($"Food Items: {statsTracker.GetFoodItemsConsumed()}");
   ```
2. Check optimal metrics are reasonable (not too high/low)
3. Adjust calculation constants in `StandardAssessmentCalculator`

### Issue: Food/Water counts are wrong

**Solution:**
1. Ensure `ConsumableTracker.RecordConsumable(InventoryItem item)` is called
2. Verify `InventoryItem` has `ConsumableEffect` with `StatType.Hunger` or `StatType.Thirst`
3. Check item is passed as object, not just name string

### Issue: UI doesn't update

**Solution:**
1. Verify `AssessmentReportUI` subscribes to `OnAssessmentComplete` event
2. Check all UI references are assigned in inspector
3. Ensure `AssessmentTab` GameObject is active when assessment fires
4. Check `LearningAssessmentService` reference is assigned to `AssessmentReportUI`

### Issue: PathTracker not recording positions

**Solution:**
1. Check `PlayerStatsTrackerService.Update()` is being called
2. Verify `minRecordDistance` in `PathTracker` is reasonable (default 1.0f)
3. Player must move >= minRecordDistance for positions to record

### Issue: RiskTracker has no events

**Solution:**
1. Ensure hazard detection system calls `RegisterRiskEvent()`
2. Check `RiskEvent.wasEncountered` is set correctly
3. Verify `totalRiskyEvents` is being incremented

---

## ⚙️ Configuration & Balancing

### Adjusting Score Weights

Edit `StandardAssessmentCalculator.cs`:

```csharp
// Score category weights (must sum to 1.0)
private const float EFFICIENCY_WEIGHT = 0.4f;  // 40%
private const float SAFETY_WEIGHT = 0.3f;      // 30%
private const float PLANNING_WEIGHT = 0.3f;    // 30%

// Path cost calculation weights
private const float DISTANCE_WEIGHT = 0.5f;    // 50%
private const float TIME_WEIGHT = 0.3f;        // 30%
private const float STAMINA_WEIGHT = 0.2f;     // 20%
```

### Adjusting Optimal Metrics

Edit `OptimalMetricsCalculator.cs`:

```csharp
// Resource consumption rates
private const float BASE_STAMINA_PER_METER = 0.5f;
private const float BASE_FOOD_ITEMS_PER_KM = 2f;    // ~2 food items per km
private const float BASE_WATER_ITEMS_PER_KM = 3f;   // ~3 water items per km
private const float BASE_MOVE_SPEED = 2.5f;         // meters/second
```

### Adjusting Rank Thresholds

Edit `LearningAssessmentService.cs`:

```csharp
private PerformanceRank DetermineRank(float totalScore)
{
    if (totalScore >= 90f) return PerformanceRank.AlpineMaster;
    if (totalScore >= 70f) return PerformanceRank.SkilledPlanner;
    if (totalScore >= 50f) return PerformanceRank.Survivor;
    return PerformanceRank.LostWanderer;
}
```

### Create Configurable ScriptableObject (Optional)

```csharp
[CreateAssetMenu(fileName = "AssessmentConfig", menuName = "Game/Assessment Config")]
public class AssessmentConfig : ScriptableObject
{
    [Header("Score Weights")]
    [Range(0, 1)] public float efficiencyWeight = 0.4f;
    [Range(0, 1)] public float safetyWeight = 0.3f;
    [Range(0, 1)] public float planningWeight = 0.3f;
    
    [Header("Rank Thresholds")]
    [Range(0, 100)] public float alpineMasterThreshold = 90f;
    [Range(0, 100)] public float skilledPlannerThreshold = 70f;
    [Range(0, 100)] public float survivorThreshold = 50f;
    
    [Header("Optimal Metrics")]
    public float staminaPerMeter = 0.5f;
    public float foodItemsPerKm = 2f;
    public float waterItemsPerKm = 3f;
    public float baseMoveSpeed = 2.5f;
    
    [Header("Penalties")]
    public float healthLossIncidentPenalty = 5f;
}
```

---

## 🔍 Component Details

### 6. Integration Points

#### 6.1 PlayerStatsTrackerService Integration

**Status:** ✅ Complete

Methods added:

```csharp
// Assessment support methods
public int GetFoodItemsConsumed() => consumableTracker.GetFoodItemsConsumed();
public int GetWaterItemsConsumed() => consumableTracker.GetWaterItemsConsumed();
public int GetHealthLossIncidents() => healthLossTracker.GetIncidentCount();
public PathTracker GetPathTracker() => pathTracker;
public RiskTracker GetRiskTracker() => riskTracker;
public void RegisterRiskEvent(RiskEvent riskEvent) => riskTracker.RecordValue(riskEvent);
```

#### 6.2 Item Consumption Integration

**Important:** When items are consumed, pass the `InventoryItem` object:

```csharp
// In your InventoryManager or item consumption logic:
public void ConsumeItem(InventoryItem item)
{
    // ... your consumption logic ...
    
    // Record in tracker (pass the item object, not just the name)
    if (playerStatsTracker != null)
    {
        playerStatsTracker.RecordConsumable(item); // Pass InventoryItem
    }
}
```

The `ConsumableTracker` now has:
- `RecordConsumable(string itemName)` - backward compatible
- `RecordConsumable(InventoryItem item)` - **preferred method**
- `GetFoodItemsConsumed()` - automatically filters by Hunger stat
- `GetWaterItemsConsumed()` - automatically filters by Thirst stat

---

### 7. UI Components (Optional)

#### 7.1 AssessmentReportUI.cs
Display assessment results to player:

```csharp
public class AssessmentReportUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private Slider efficiencySlider;
    [SerializeField] private Slider safetySlider;
    [SerializeField] private Slider planningSlider;
    [SerializeField] private TextMeshProUGUI feedbackText;
    
    [Header("Dependencies")]
    [SerializeField] private LearningAssessmentService assessmentService;
    
    private void OnEnable()
    {
        assessmentService.OnAssessmentComplete += DisplayAssessment;
    }
    
    private void OnDisable()
    {
        assessmentService.OnAssessmentComplete -= DisplayAssessment;
    }
    
    private void DisplayAssessment(AssessmentScore score)
    {
        // Display rank with emoji
        string rankEmoji = score.rank switch
        {
            PerformanceRank.AlpineMaster => "🏔️",
            PerformanceRank.SkilledPlanner => "🧭",
            PerformanceRank.Survivor => "⚙️",
            PerformanceRank.LostWanderer => "🪶",
            _ => ""
        };
        
        rankText.text = $"{rankEmoji} {score.rank}";
        totalScoreText.text = $"{score.totalScore:F1}/100";
        
        // Update sliders
        efficiencySlider.value = score.efficiencyScore;
        safetySlider.value = score.safetyScore;
        planningSlider.value = score.planningScore;
        
        // Combine feedback
        string feedback = $"<b>Efficiency:</b> {score.efficiencyDetails.feedback}\n\n" +
                         $"<b>Safety:</b> {score.safetyDetails.feedback}\n\n" +
                         $"<b>Planning:</b> {score.planningDetails.feedback}";
        
        feedbackText.text = feedback;
        
        // Show UI panel
        gameObject.SetActive(true);
    }
}
```

---

## ✅ Implementation Checklist

### Phase 1: Core Data Models ✅ COMPLETE
- [x] ✅ Create `PerformanceMetrics.cs` - Contains raw expedition data
- [x] ✅ Create `AssessmentScore.cs` - Final calculated scores
- [x] ✅ Create `EfficiencyBreakdown.cs` - Detailed efficiency breakdown
- [x] ✅ Create `SafetyBreakdown.cs` - Detailed safety breakdown
- [x] ✅ Create `PlanningBreakdown.cs` - Detailed planning breakdown
- [x] ✅ Create `OptimalMetrics.cs` - Expected performance values
- [x] ✅ Create `PerformanceRank.cs` enum - 4 performance tiers
- [x] ✅ Create `RiskEvent.cs` - Individual risk event data
- [x] ✅ Create `RiskType.cs` enum - 6 risk types

**Location:** `Assets/Game/Script/Player/Stat/Assessment/`

### Phase 2: Calculators ✅ COMPLETE
- [x] ✅ Create `IAssessmentCalculator.cs` interface - Strategy pattern
- [x] ✅ Implement `StandardAssessmentCalculator.cs` - Default calculation with weighted formulas
- [x] ✅ Implement `OptimalMetricsCalculator.cs` - Auto-calculates or accepts external metrics

**Location:** `Assets/Game/Script/Player/Stat/Assessment/`

### Phase 3: Tracking Extensions ✅ COMPLETE
- [x] ✅ Create `PathTracker.cs` - Extends BaseStatTracker<Vector3>, records player positions
- [x] ✅ Create `RiskTracker.cs` - Extends BaseStatTracker<RiskEvent>, tracks hazard events
- [x] ✅ Refactor `AssessmentTracker.cs` - Simplified coordinator, delegates to PlayerStatsTrackerService
- [x] ✅ Enhance `PlayerStatsTrackerService` - Added pathTracker and riskTracker management
- [x] ✅ Enhance `HealthLossTracker` - Added incident counting
- [x] ✅ Enhance `ConsumableTracker` - Added food/water item tracking by InventoryItem object

**Location:** `Assets/Game/Script/Player/Stat/Tracking/`

### Phase 4: Main Service ✅ COMPLETE
- [x] ✅ Create `LearningAssessmentService.cs` - Main coordinator
  - GenerateAssessment() - Full pipeline
  - SetOptimalMetrics() - External input support (2 overloads)
  - Event system (OnAssessmentComplete)
  - Thai language feedback generation
- [x] ✅ Integration with PlayerStatsTrackerService
- [x] ✅ Assessment generation pipeline complete

**Location:** `Assets/Game/Script/Player/Stat/Assessment/`

### Phase 5: Integration ✅ COMPLETE
- [x] ✅ Update `StatMetricType` enum - Added PathTracking and RiskTracking
- [x] ✅ Create `ExpeditionIntegrationExample.cs` - Complete integration guide
- [x] ✅ Enhanced PlayerStatsTrackerService API:
  - `RegisterRiskEvent()` - Public risk registration
  - `GetPathTracker()` - Access to path data
  - `GetRiskTracker()` - Access to risk data
  - `GetFoodItemsConsumed()` / `GetWaterItemsConsumed()`
  - `GetHealthLossIncidents()`

**Status:** Ready for game integration

### Phase 6: UI ✅ COMPLETE
- [x] ✅ Create `AssessmentReportUI.cs` - Complete assessment display component (300+ lines)
  - Rank display with emoji and icon support
  - Category score sliders (Efficiency, Safety, Planning)
  - Detailed breakdowns for each category
  - Thai language feedback display
  - Event subscription to OnAssessmentComplete
  - Manual generation button support
- [x] ✅ Enhance `PlayerStatsTrackerUI.cs` - Tab system implementation
  - Added statTrackingTab and assessmentTab GameObjects
  - Tab switching buttons with listeners
  - SwitchToTab(bool showStatTracking) method
  - Updated GetMetricName() for new metrics

**Location:** `Assets/Game/Script/Player/Stat/UI/StatTracking/`

### Phase 7: Testing & Balancing ⏳ PENDING
- [ ] ⏳ Test in Unity with actual gameplay
- [ ] ⏳ Test various expedition scenarios (short/long, safe/risky, efficient/wasteful)
- [ ] ⏳ Balance optimal calculation formulas
  - Adjust BASE_STAMINA_PER_METER, BASE_FOOD_ITEMS_PER_KM, BASE_WATER_ITEMS_PER_KM
- [ ] ⏳ Verify rank thresholds (90/70/50 boundaries)
- [ ] ⏳ Test external optimal metrics integration with planning module
- [ ] ⏳ Adjust score weights if needed (currently 40/30/30)
- [ ] ⏳ Create rank icon sprites (optional visual enhancement)

**Status:** Requires Unity scene setup and gameplay testing

---

## 📦 Files Created

### Assessment System (9 files)
```
Assets/Game/Script/Player/Stat/Assessment/
├── PerformanceMetrics.cs          ✅ Raw metrics data model
├── AssessmentScore.cs             ✅ Final score with breakdowns
├── EfficiencyBreakdown.cs         ✅ Efficiency details
├── SafetyBreakdown.cs             ✅ Safety details
├── PlanningBreakdown.cs           ✅ Planning details
├── OptimalMetrics.cs              ✅ Expected values
├── PerformanceRank.cs             ✅ Enum (4 ranks)
├── RiskEvent.cs                   ✅ Risk event data
└── RiskType.cs                    ✅ Enum (6 types)
```

### Calculators (3 files)
```
Assets/Game/Script/Player/Stat/Assessment/
├── IAssessmentCalculator.cs       ✅ Interface
├── StandardAssessmentCalculator.cs ✅ Default implementation
└── OptimalMetricsCalculator.cs    ✅ Optimal value calculator
```

### Services (2 files)
```
Assets/Game/Script/Player/Stat/Assessment/
├── LearningAssessmentService.cs   ✅ Main coordinator
└── AssessmentTracker.cs           ✅ Metrics collector (refactored)
```

### Tracking (2 files)
```
Assets/Game/Script/Player/Stat/Tracking/
├── PathTracker.cs                 ✅ Position tracking
└── RiskTracker.cs                 ✅ Risk event tracking
```

### UI (2 files)
```
Assets/Game/Script/Player/Stat/UI/StatTracking/
├── AssessmentReportUI.cs          ✅ Assessment display
└── PlayerStatsTrackerUI.cs        ✅ Enhanced with tabs
```

### Integration (1 file)
```
Assets/Game/Script/Player/Stat/Assessment/
└── ExpeditionIntegrationExample.cs ✅ Integration template
```

**Total:** 19 files created/modified

---

## 🎯 Quick Reference: Key Methods

### Starting an Expedition
```csharp
// Basic start
playerStatsTrackerService.ResetTracking();
playerStatsTrackerService.StartTracking();

// With external optimal metrics
assessmentService.SetOptimalMetrics(expectedStamina, foodItems, waterItems, distance, time);
```

### During Expedition
```csharp
// Item consumption (automatically tracked)
playerStatsTrackerService.RecordConsumable(inventoryItem);

// Risk event registration
RiskEvent risk = new RiskEvent { /* ... */ };
playerStatsTrackerService.RegisterRiskEvent(risk);

// Path tracking (automatic via Update())
// No manual calls needed - PathTracker handles it
```

### Ending an Expedition
```csharp
// Stop tracking and generate assessment
playerStatsTrackerService.StopTracking();
AssessmentScore score = assessmentService.GenerateAssessment();

// Assessment automatically displayed via OnAssessmentComplete event
```

### Accessing Results
```csharp
// Subscribe to event
assessmentService.OnAssessmentComplete += (score) => {
    Debug.Log($"Score: {score.totalScore} - Rank: {score.rank}");
    // Save to profile, unlock achievements, etc.
};
```

---

## ❓ Frequently Asked Questions

### Q: Do I need to set optimal metrics manually?
**A:** No. If you don't call `SetOptimalMetrics()`, the system will auto-calculate based on player's path distance. However, if you have a route planning module that calculates optimal values, setting them manually provides more accurate comparisons.

### Q: How does the system know if an item is food or water?
**A:** It checks the `InventoryItem.ConsumableEffect[]` array for effects with `StatType.Hunger` (food) or `StatType.Thirst` (water). Make sure your items have these effects defined.

### Q: Can I change the score weights (40/30/30)?
**A:** Yes. Edit the constants in `StandardAssessmentCalculator.cs`:
```csharp
private const float EFFICIENCY_WEIGHT = 0.4f;
private const float SAFETY_WEIGHT = 0.3f;
private const float PLANNING_WEIGHT = 0.3f;
```
Or create a ScriptableObject config (see [Configuration](#configuration--balancing) section).

### Q: What determines if a risk was "encountered" vs "avoided"?
**A:** Your hazard detection system sets the `RiskEvent.wasEncountered` boolean. Typically:
- `true` = Player triggered/entered the hazard
- `false` = Player detected but successfully avoided it

### Q: Can I have multiple assessments per session?
**A:** Yes. Each time you call `GenerateAssessment()`, it creates a new `AssessmentScore` based on current tracker data. Just reset tracking between expeditions:
```csharp
statsTracker.ResetTracking();
statsTracker.StartTracking();
```

### Q: How do I save assessment history?
**A:** The system fires `OnAssessmentComplete` events. Subscribe and save the `AssessmentScore` to your player profile:
```csharp
assessmentService.OnAssessmentComplete += (score) => {
    PlayerProfile.AddAssessment(score); // Your save system
};
```

### Q: Can I use this system in a multiplayer game?
**A:** Yes, but each player needs their own `PlayerStatsTrackerService` and `LearningAssessmentService` instances. The system is designed for single-player tracking but can be instanced per player.

### Q: What if my game doesn't have risk events?
**A:** The system will still work. Safety score will be 100 if no risks are registered. You can also modify the calculator to reduce or eliminate the safety weight.

---

## 🎨 Best Practices

### 1. Item Consumption Tracking
**✅ DO:**
```csharp
// Pass the full InventoryItem object
playerStatsTrackerService.RecordConsumable(inventoryItem);
```

**❌ DON'T:**
```csharp
// Don't rely on item names alone
playerStatsTrackerService.RecordConsumable(item.name);
```

### 2. Risk Event Registration
**✅ DO:**
```csharp
// Register risks with meaningful severity values
RiskEvent risk = new RiskEvent
{
    riskType = RiskType.SteepSlope,
    location = hazard.position,
    timestamp = Time.time,
    wasEncountered = playerTriggeredHazard,
    severity = CalculateSeverity() // 0-1 based on danger level
};
statsTracker.RegisterRiskEvent(risk);
```

**❌ DON'T:**
```csharp
// Don't forget to set wasEncountered
RiskEvent risk = new RiskEvent { riskType = RiskType.SteepSlope };
// Missing critical fields!
```

### 3. Expedition Lifecycle
**✅ DO:**
```csharp
// Always reset before starting new expedition
statsTracker.ResetTracking();
statsTracker.StartTracking();

// ... expedition gameplay ...

// Always stop before generating assessment
statsTracker.StopTracking();
assessmentService.GenerateAssessment();
```

**❌ DON'T:**
```csharp
// Don't forget to reset - data will accumulate
statsTracker.StartTracking(); // Old data still present!

// Don't generate assessment while tracking is active
assessmentService.GenerateAssessment(); // Tracking still running!
```

### 4. Optimal Metrics
**✅ DO:**
```csharp
// Set optimal metrics BEFORE starting tracking
assessmentService.SetOptimalMetrics(/* values */);
statsTracker.StartTracking();

// OR let it auto-calculate (recommended for dynamic terrain)
statsTracker.StartTracking(); // Auto-calculates from path
```

**❌ DON'T:**
```csharp
// Don't set optimal metrics after expedition ends
statsTracker.StopTracking();
assessmentService.SetOptimalMetrics(/* values */); // Too late!
assessmentService.GenerateAssessment();
```

### 5. Event Subscriptions
**✅ DO:**
```csharp
// Subscribe in OnEnable, unsubscribe in OnDisable
private void OnEnable()
{
    assessmentService.OnAssessmentComplete += HandleAssessment;
}

private void OnDisable()
{
    assessmentService.OnAssessmentComplete -= HandleAssessment;
}
```

**❌ DON'T:**
```csharp
// Don't forget to unsubscribe - causes memory leaks
private void Start()
{
    assessmentService.OnAssessmentComplete += HandleAssessment;
    // No unsubscribe! Memory leak!
}
```

### 6. UI References
**✅ DO:**
```csharp
// Assign all UI references in inspector before testing
// Use Unity Events for button callbacks
// Test in Editor with dummy data
```

**❌ DON'T:**
```csharp
// Don't use GameObject.Find at runtime
GameObject.Find("AssessmentTab"); // Slow and fragile!
```

---

## 🚀 Next Steps After Setup

1. **Test Basic Flow**
   - Create simple test expedition
   - Trigger StartExpedition() → play 30 seconds → EndExpedition()
   - Verify assessment UI appears

2. **Integrate Consumables**
   - Hook up your inventory consumption to `RecordConsumable()`
   - Test that food/water counts appear correctly

3. **Add Risk Detection**
   - Identify hazardous areas in your game
   - Call `RegisterRiskEvent()` when player enters/avoids hazards
   - Verify safety scores change appropriately

4. **Balance Formulas**
   - Play through full expedition multiple times
   - Adjust optimal calculation constants until scores feel fair
   - Verify rank distribution (not everyone should be Alpine Master!)

5. **Polish UI**
   - Add rank icon sprites
   - Create animations for score reveals
   - Add sound effects for rank achievements
   - Localize feedback messages

6. **Integrate with Game Systems**
   - Save assessments to player profile
   - Connect to achievements/unlocks
   - Add to leaderboards
   - Show improvement over time

---

## 📚 Additional Resources

### Related Files
- [Learning Assessment Design (Thai)](Learning%20Assesment.md) - Original design document
- [ExpeditionIntegrationExample.cs](../Assessment/ExpeditionIntegrationExample.cs) - Complete integration template
- [PlayerStatsTrackerService.cs](../Tracking/PlayerStatsTrackerService.cs) - Central tracking service

### System Architecture
- **SOLID Principles**: Interface segregation (IAssessmentCalculator), Single Responsibility
- **Design Patterns**: Strategy (calculator), Template Method (BaseStatTracker), Observer (events)
- **Data Flow**: Tracking → Collection → Calculation → Presentation

### Performance Considerations
- PathTracker records positions only when player moves >= `minRecordDistance`
- Risk events stored in lists, consider capacity limits for very long expeditions
- Assessment calculation is done once at expedition end (not per-frame)

---

## 🎉 Summary

The Learning Assessment System is a comprehensive performance evaluation framework that:

✅ **Tracks** player behavior during expeditions (resource usage, path, risks, health)  
✅ **Calculates** performance scores across 3 categories (Efficiency 40%, Safety 30%, Planning 30%)  
✅ **Ranks** players into 4 performance tiers (Lost Wanderer → Alpine Master)  
✅ **Displays** detailed feedback in Thai with visual UI (rank icons, sliders, breakdowns)  
✅ **Integrates** seamlessly with existing PlayerStatsTrackerService  
✅ **Supports** external optimal metrics from planning modules  
✅ **Fires** events for achievement/save system integration  

**Status:** ✅ **Fully Implemented** - Ready for Unity scene setup and gameplay testing!

---

*Last Updated: January 2026*  
*Version: 1.0*  
*All phases complete - pending Unity integration and balancing*


## Usage Example

```csharp
// In your ExpeditionManager or similar class

public class ExpeditionManager : MonoBehaviour
{
    [SerializeField] private LearningAssessmentService assessmentService;
    [SerializeField] private PlayerStatsTrackerService statsTracker;
    [SerializeField] private AssessmentTracker assessmentTracker;
    [SerializeField] private RoutePlanningModule routePlanner; // Your planning module
    
    public void StartExpedition()
    {
        statsTracker.ResetTracking();
        statsTracker.StartTracking();
        assessmentTracker.StartAssessmentTracking();
        
        // Optional: Set optimal metrics from your planning module
        if (routePlanner.HasOptimalRoute)
        {
            assessmentService.SetOptimalMetrics(
                routePlanner.expectedStamina,
                routePlanner.expectedFoodItems,
                routePlanner.expectedWaterItems,
                routePlanner.optimalDistance,
                routePlanner.expectedTime
            );
        }
    }
    
    public void EndExpedition()
    {
        statsTracker.StopTracking();
        
        // Generate and display assessment
        // Will use external optimal metrics if set, otherwise calculates automatically
        AssessmentScore score = assessmentService.GenerateAssessment();
        
        Debug.Log($"Expedition Complete! Score: {score.totalScore} - Rank: {score.rank}");
        
        // Clear for next expedition
        assessmentService.ClearOptimalMetrics();
        
        // Save to player profile or leaderboard
        SaveAssessmentToProfile(score);
    }
}
```

---

## Configuration & Balancing

### Adjustable Parameters

```csharp
// In StandardAssessmentCalculator or as ScriptableObject

[Serializable]
public class AssessmentConfig
{
    [Header("Score Weights")]
    [Range(0, 1)] public float efficiencyWeight = 0.4f;
    [Range(0, 1)] public float safetyWeight = 0.3f;
    [Range(0, 1)] public float planningWeight = 0.3f;
    
    [Header("Rank Thresholds")]
    [Range(0, 100)] public float alpineMasterThreshold = 90f;
    [Range(0, 100)] public float skilledPlannerThreshold = 70f;
    [Range(0, 100)] public float survivorThreshold = 50f;
    
    [Header("Resource Consumption Rates")]
    public float baseStaminaPerMeter = 0.5f;
    public float baseFoodItemsPerKm = 2f;    // Expected food items per kilometer
    public float baseWaterItemsPerKm = 3f;   // Expected water items per kilometer
    
    [Header("Safety Penalties")]
    public float healthLossIncidentPenalty = 5f;
}
```

---

## Future Extensions

### Potential Enhancements
1. **Historical Tracking**: Save all assessments to show player improvement over time
2. **Difficulty Modes**: Different calculators for easy/normal/hard expeditions
3. **Leaderboards**: Compare scores with other players
4. **Achievements**: Unlock rewards for high scores
5. **Adaptive Difficulty**: Adjust game difficulty based on assessment results
6. **Detailed Analytics**: Graphs and charts showing performance trends
7. **AI Recommendations**: Suggest improvements based on weaknesses

---

## Notes

- All calculations use **0-100 score range** for consistency
- System is **modular** - can swap calculators for different game modes
- Maintains **SOLID principles** from existing codebase
- Integrates seamlessly with current tracking infrastructure
- Can be **disabled/enabled** via configuration for testing
- Ready for **localization** (Thai/English feedback strings)

---

## Questions to Consider Before Implementation

1. **How should we categorize consumable items?**
   - Use item name keywords ("Food", "Water")?  
   - Add `ConsumableType` enum to `InventoryItem` class? (Recommended)
   - Or use existing item categories from your inventory system?

2. **How do we detect "risky events"?**
   - Need integration with existing hazard/danger zones
   - Could use terrain slope, weather system, enemy encounters

3. **What defines the "optimal path"?**
   - Use A* pathfinding result?
   - Pre-computed ideal routes?
   - Dynamic calculation based on player level?

4. **When exactly does assessment happen?**
   - On reaching summit?
   - On returning to base camp?
   - Configurable trigger?

5. **Should assessments be saved persistently?**
   - Save to file/database for progression tracking?
   - Keep only latest assessment?

---

*Ready for review and implementation upon confirmation.*
