using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Player.Stat.Assessment;

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
            assessmentService = FindFirstObjectByType<LearningAssessmentService>();
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
        
        // Display rank with emoji
        if (rankText != null)
        {
            string rankEmoji = GetRankEmoji(score.rank);
            string rankName = GetRankName(score.rank);
            rankText.text = $"{rankEmoji} {rankName}";
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
        DisplayEfficiencyDetails(score.efficiencyDetails);
        DisplaySafetyDetails(score.safetyDetails);
        DisplayPlanningDetails(score.planningDetails);
        
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
    private void DisplayEfficiencyDetails(EfficiencyBreakdown details)
    {
        if (efficiencyDetailsText == null || details == null)
            return;
        
        string text = "<b>Resource Efficiency:</b>\n";
        text += $"  Stamina: {details.staminaEfficiency:F1}%\n";
        text += $"  Food: {details.foodEfficiency:F1}%\n";
        text += $"  Water: {details.waterEfficiency:F1}%\n";
        text += $"  Usage Ratio: {details.resourceUsageRatio:F2}x\n";
        text += $"\n<i>{details.feedback}</i>";
        
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
        text += $"\n<i>{details.feedback}</i>";
        
        safetyDetailsText.text = text;
    }
    
    /// <summary>
    /// Displays planning breakdown details
    /// </summary>
    private void DisplayPlanningDetails(PlanningBreakdown details)
    {
        if (planningDetailsText == null || details == null)
            return;
        
        string text = "<b>Route Planning:</b>\n";
        text += $"  Path Deviation: {details.pathDeviation:F1}%\n";
        text += $"  Time Efficiency: {details.timeEfficiency:F1}%\n";
        text += $"  Route Optimality: {details.routeOptimality:F1}/100\n";
        text += $"\n<i>{details.feedback}</i>";
        
        planningDetailsText.text = text;
    }
    
    /// <summary>
    /// Displays combined feedback summary
    /// </summary>
    private void DisplayFeedback(AssessmentScore score)
    {
        if (feedbackText == null)
            return;
        
        string feedback = "<b>Performance Summary:</b>\n\n";
        feedback += $"<b>Efficiency ({score.efficiencyScore:F1}/100):</b>\n";
        feedback += $"{score.efficiencyDetails.feedback}\n\n";
        feedback += $"<b>Safety ({score.safetyScore:F1}/100):</b>\n";
        feedback += $"{score.safetyDetails.feedback}\n\n";
        feedback += $"<b>Planning ({score.planningScore:F1}/100):</b>\n";
        feedback += $"{score.planningDetails.feedback}";
        
        feedbackText.text = feedback;
    }
    
    /// <summary>
    /// Gets emoji for performance rank
    /// </summary>
    private string GetRankEmoji(PerformanceRank rank)
    {
        return rank switch
        {
            PerformanceRank.AlpineMaster => "🏔️",
            PerformanceRank.SkilledPlanner => "🧭",
            PerformanceRank.Survivor => "⚙️",
            PerformanceRank.LostWanderer => "🪶",
            _ => "❓"
        };
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
