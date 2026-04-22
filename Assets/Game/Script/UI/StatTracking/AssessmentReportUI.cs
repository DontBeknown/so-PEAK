using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Player.Stat.Assessment;
using Game.Core.DI;

/// <summary>
/// UI presenter for displaying assessment reports
/// Shows detailed breakdown of player performance
/// </summary>
public class AssessmentReportUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private LearningAssessmentService assessmentService;
    
    [Header("Overall Score")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI rankProgressText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private Image rankIconImage;
    
    [Header("Category Scores")]
    [SerializeField] private TextMeshProUGUI efficiencyScoreText;
    [SerializeField] private Slider efficiencySlider;
    [SerializeField] private TextMeshProUGUI safetyScoreText;
    [SerializeField] private Slider safetySlider;
    [SerializeField] private TextMeshProUGUI planningScoreText;
    [SerializeField] private Slider planningSlider;
    
    [Header("Detailed Breakdowns")]
    [SerializeField] private TextMeshProUGUI efficiencyDetailsText;
    [SerializeField] private TextMeshProUGUI safetyDetailsText;
    [SerializeField] private TextMeshProUGUI planningDetailsText;
    
    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    
    [Header("Generate Button")]
    [SerializeField] private Button generateAssessmentButton;
    
    [Header("Rank Icons (Optional)")]
    [SerializeField] private Sprite lostWandererIcon;
    [SerializeField] private Sprite survivorIcon;
    [SerializeField] private Sprite skilledPlannerIcon;
    [SerializeField] private Sprite alpineMasterIcon;
    
    private AssessmentScore currentScore;
    
    private void Awake()
    {
        if (assessmentService == null)
        {
            assessmentService = ServiceContainer.Instance.TryGet<LearningAssessmentService>();
        }
        
        if (generateAssessmentButton != null)
        {
            generateAssessmentButton.onClick.AddListener(GenerateAndDisplayAssessment);
        }
    }
    
    private void OnEnable()
    {
        if (assessmentService != null)
        {
            assessmentService.OnAssessmentComplete += DisplayAssessment;
        }
    }
    
    private void OnDisable()
    {
        if (assessmentService != null)
        {
            assessmentService.OnAssessmentComplete -= DisplayAssessment;
        }
    }
    
    /// <summary>
    /// Generates new assessment and displays it
    /// </summary>
    public void GenerateAndDisplayAssessment()
    {
        if (assessmentService != null)
        {
            currentScore = assessmentService.GenerateAssessment();
            if (currentScore != null)
            {
                DisplayAssessment(currentScore);
            }
        }
        else
        {
            Debug.LogError("[AssessmentReportUI] LearningAssessmentService not found!");
        }
    }
    
    /// <summary>
    /// Displays the assessment score on the UI
    /// </summary>
    public void DisplayAssessment(AssessmentScore score)
    {
        if (score == null)
        {
            Debug.LogError("[AssessmentReportUI] Cannot display null assessment score!");
            return;
        }
        
        currentScore = score;
        
        // Display rank
        if (rankText != null)
        {
            rankText.text = GetRankName(score.rank);
        }

        // Display progress toward next rank
        if (rankProgressText != null)
        {
            rankProgressText.text = GetRankProgressText(score.totalScore, score.rank);
        }
        
        // Display total score
        if (totalScoreText != null)
        {
            totalScoreText.text = $"{score.totalScore:F1}/100";
        }
        
        // Set rank icon
        if (rankIconImage != null)
        {
            rankIconImage.sprite = GetRankIcon(score.rank);
        }
        
        // Display category scores
        DisplayCategoryScore(efficiencyScoreText, efficiencySlider, score.efficiencyScore, "Efficiency");
        DisplayCategoryScore(safetyScoreText, safetySlider, score.safetyScore, "Safety");
        DisplayCategoryScore(planningScoreText, planningSlider, score.planningScore, "Planning");
        
        // Display detailed breakdowns
        DisplayEfficiencyDetails(score.efficiencyDetails, score);
        DisplaySafetyDetails(score.safetyDetails);
        DisplayPlanningDetails(score.planningDetails, score);
        
        // Display combined feedback
        DisplayFeedback(score);
    }
    
    /// <summary>
    /// Displays a single category score
    /// </summary>
    private void DisplayCategoryScore(TextMeshProUGUI text, Slider slider, float score, string label)
    {
        if (text != null)
        {
            text.text = $"{label}: {score:F1}/100";
        }
        
        if (slider != null)
        {
            slider.value = score / 100f; // Normalize to 0-1
        }
    }
    
    /// <summary>
    /// Displays efficiency breakdown details
    /// </summary>
    private void DisplayEfficiencyDetails(EfficiencyBreakdown details, AssessmentScore score)
    {
        if (efficiencyDetailsText == null || details == null)
            return;

        var raw = score.rawMetrics;
        var opt = score.optimalMetrics;

        string text = "<b>Resource Efficiency:</b>\n";

        if (raw != null && opt != null)
        {
            text += $"  Stamina: {details.staminaEfficiency:F1}%  ({raw.totalStaminaUsed:F0} used / {opt.expectedStamina:F0} optimal)\n";
            text += $"  Food: {details.foodEfficiency:F1}%  ({raw.totalFoodItemsConsumed} used / {opt.expectedFoodItems} optimal)\n";
            text += $"  Water: {details.waterEfficiency:F1}%  ({raw.totalWaterItemsConsumed} used / {opt.expectedWaterItems} optimal)\n";
        }
        else
        {
            text += $"  Stamina: {details.staminaEfficiency:F1}%\n";
            text += $"  Food: {details.foodEfficiency:F1}%\n";
            text += $"  Water: {details.waterEfficiency:F1}%\n";
        }

        text += $"  Overall Usage: {details.resourceUsageRatio:F2}x optimal\n";
        text += $"<i>{details.feedback}</i>";

        efficiencyDetailsText.text = text;
    }
    
    /// <summary>
    /// Displays safety breakdown details
    /// </summary>
    private void DisplaySafetyDetails(SafetyBreakdown details)
    {
        if (safetyDetailsText == null || details == null)
            return;
        
        string text = "<b>Safety Performance:</b>\n";
        text += $"  Risks Avoided: {details.risksAvoided}\n";
        text += $"  Risks Encountered: {details.risksEncountered}\n";
        text += $"  Avoidance Rate: {details.avoidanceRate:F1}%\n";
        text += $"  Deaths: {details.deathCount} (-{details.deathPenaltyScore:F1})\n";
        text += $"<i>{details.feedback}</i>";
        
        safetyDetailsText.text = text;
    }
    
    /// <summary>
    /// Displays planning breakdown details
    /// </summary>
    private void DisplayPlanningDetails(PlanningBreakdown details, AssessmentScore score)
    {
        if (planningDetailsText == null || details == null)
            return;

        var raw = score.rawMetrics;
        var opt = score.optimalMetrics;

        string text = "<b>Route Planning:</b>\n";

        if (raw != null && opt != null)
        {
            text += $"  Distance: {raw.totalDistance:F0}m  (optimal {opt.optimalDistance:F0}m, {details.pathDeviation:F1}% off)\n";
            text += $"  Time: {FormatTime(raw.totalTime)}  (optimal {FormatTime(opt.optimalTime)}, {100f - details.timeEfficiency:F1}% off)\n";
        }
        else
        {
            text += $"  Path Deviation: {details.pathDeviation:F1}%\n";
            text += $"  Time Efficiency: {details.timeEfficiency:F1}%\n";
        }

        text += $"  Route Optimality: {details.routeOptimality:F1}/100\n";

        if (score.planningUsedFallbackPath)
            text += "<i>Note: No reference path available — planning score is estimated only.</i>\n";

        text += $"<i>{details.feedback}</i>";

        planningDetailsText.text = text;
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return m > 0 ? $"{m}m {s:D2}s" : $"{s}s";
    }
    
    /// <summary>
    /// Displays actionable improvement tips per category.
    /// </summary>
    private void DisplayFeedback(AssessmentScore score)
    {
        if (feedbackText == null)
            return;

        string feedback = "<b>How to Improve:</b>\n\n";
        feedback += $"<b>Efficiency:</b>\n{LearningAssessmentService.GetEfficiencyTip(score.efficiencyScore)}\n\n";
        feedback += $"<b>Safety:</b>\n{LearningAssessmentService.GetSafetyTip(score.safetyScore)}\n\n";
        feedback += $"<b>Planning:</b>\n{LearningAssessmentService.GetPlanningTip(score.planningScore)}";

        feedbackText.text = feedback;
    }
    
    
    /// <summary>
    /// Gets display name for performance rank
    /// </summary>
    private string GetRankName(PerformanceRank rank)
    {
        return rank switch
        {
            PerformanceRank.AlpineMaster => "Alpine Master",
            PerformanceRank.SkilledPlanner => "Skilled Planner",
            PerformanceRank.Survivor => "Survivor",
            PerformanceRank.LostWanderer => "Lost Wanderer",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// Returns a short progress hint toward the next rank, or a congratulation at max rank.
    /// </summary>
    private string GetRankProgressText(float totalScore, PerformanceRank rank)
    {
        return rank switch
        {
            PerformanceRank.AlpineMaster => "Peak performance — you've reached the top rank!",
            PerformanceRank.SkilledPlanner => $"Need {Mathf.Ceil(90f - totalScore):F0} more points for Alpine Master (90+)",
            PerformanceRank.Survivor      => $"Need {Mathf.Ceil(70f - totalScore):F0} more points for Skilled Planner (70+)",
            PerformanceRank.LostWanderer  => $"Need {Mathf.Ceil(50f - totalScore):F0} more points for Survivor (50+)",
            _                             => string.Empty
        };
    }

    /// <summary>
    /// Gets icon sprite for performance rank
    /// </summary>
    private Sprite GetRankIcon(PerformanceRank rank)
    {
        return rank switch
        {
            PerformanceRank.AlpineMaster => alpineMasterIcon,
            PerformanceRank.SkilledPlanner => skilledPlannerIcon,
            PerformanceRank.Survivor => survivorIcon,
            PerformanceRank.LostWanderer => lostWandererIcon,
            _ => null
        };
    }
}
