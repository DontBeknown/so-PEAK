using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Player;
using Game.Player.Services;

/// <summary>
/// UI presenter for displaying player statistics.
/// SRP: Only responsible for UI presentation.
/// </summary>
public class PlayerStatsTrackerUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStatsTrackerService trackerService;
    [SerializeField] private CinemachinePlayerCamera playerCamera;
    [SerializeField] private PlayerControllerRefactored playerController;
    [SerializeField] private SimpleStatsHUD simpleStatsHUD;
    
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    
    [Header("Tab System")]
    [SerializeField] private GameObject statTrackingTab;
    [SerializeField] private GameObject assessmentTab;
    [SerializeField] private Button statTrackingTabButton;
    [SerializeField] private Button assessmentTabButton;
    
    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI sessionTimeText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI fatigueText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI consumablesText;
    [SerializeField] private TextMeshProUGUI consumableDetailsText;
    
    [Header("Graph")]
    [SerializeField] private StatGraphRenderer graphRenderer;
    [SerializeField] private TextMeshProUGUI currentMetricText;
    
    [Header("Buttons")]
    [SerializeField] private Button distanceButton;
    [SerializeField] private Button staminaButton;
    [SerializeField] private Button fatigueButton;
    [SerializeField] private Button healthButton;
    [SerializeField] private Button consumablesButton;
    [SerializeField] private Button resetButton;
    
    [Header("Settings")]
    [SerializeField] private float refreshRate = 0.5f; // Update UI every 0.5 seconds
    
    private StatMetricType currentGraphMetric = StatMetricType.Distance;
    private float timeSinceLastRefresh;
    
    private void Awake()
    {
        if (trackerService == null)
        {
            trackerService = FindFirstObjectByType<PlayerStatsTrackerService>();
        }
        
        if (graphRenderer == null)
        {
            graphRenderer = GetComponentInChildren<StatGraphRenderer>();
        }

        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<CinemachinePlayerCamera>();
        }
        
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerControllerRefactored>();
        }

        if(simpleStatsHUD == null)
        {
            simpleStatsHUD = FindFirstObjectByType<SimpleStatsHUD>();
        }
        
        SetupButtons();
        
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        
        if (statTrackingTabButton != null)
            statTrackingTabButton.onClick.AddListener(() => SwitchToTab(true));
        
        if (assessmentTabButton != null)
            assessmentTabButton.onClick.AddListener(() => SwitchToTab(false));
        
        if (distanceButton != null)
            distanceButton.onClick.AddListener(() => SwitchGraphMetric(StatMetricType.Distance));
        
        if (staminaButton != null)
            staminaButton.onClick.AddListener(() => SwitchGraphMetric(StatMetricType.Stamina));
        
        if (fatigueButton != null)
            fatigueButton.onClick.AddListener(() => SwitchGraphMetric(StatMetricType.Fatigue));
        
        if (healthButton != null)
            healthButton.onClick.AddListener(() => SwitchGraphMetric(StatMetricType.Health));
        
        if (consumablesButton != null)
            consumablesButton.onClick.AddListener(() => SwitchGraphMetric(StatMetricType.Consumables));
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetTracking);
    }
    
    /// <summary>
    /// Shows the statistics UI panel.
    /// Call this from a public function or button.
    /// </summary>
    public void Show()
    {
        playerCamera.SetCursorLock(false);
        
        // Stop player movement
        BlockPlayerInput(true);

        simpleStatsHUD.Hide();
        
        // Stop tracking when UI is opened
        if (trackerService != null)
        {
            trackerService.StopTracking();
        }
        
        if (panel != null)
        {
            panel.SetActive(true);
            SwitchToTab(true); // Default to stat tracking tab
            RefreshDisplay();
        }
    }
    
    /// <summary>
    /// Switches between stat tracking and assessment tabs
    /// </summary>
    /// <param name="showStatTracking">True for stat tracking, false for assessment</param>
    public void SwitchToTab(bool showStatTracking)
    {
        if (statTrackingTab != null)
            statTrackingTab.SetActive(showStatTracking);
        
        if (assessmentTab != null){
            assessmentTab.GetComponent<AssessmentReportUI>()?.GenerateAndDisplayAssessment();
            assessmentTab.SetActive(!showStatTracking);
        } 
        
        if (showStatTracking)
        {
            RefreshDisplay();
        }
    }
    
    /// <summary>
    /// Hides the statistics UI panel.
    /// </summary>
    public void Hide()
    {
        playerCamera.SetCursorLock(true);
        
        simpleStatsHUD.Show();
        
        // Resume player movement
        BlockPlayerInput(false);
        
        // Resume tracking when UI is closed
        if (trackerService != null)
        {
            trackerService.StartTracking();
        }
        
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Toggles the statistics UI panel visibility.
    /// </summary>
    public void Toggle()
    {
        
        if (panel != null)
        {
            if (panel.activeSelf)
                Hide();
            else
                Show();
        }
    }

    
    private void Update()
    {
        if (panel != null && panel.activeSelf)
        {
            timeSinceLastRefresh += Time.deltaTime;
            
            if (timeSinceLastRefresh >= refreshRate)
            {
                RefreshDisplay();
                timeSinceLastRefresh = 0f;
            }
        }
    }
    
    /// <summary>
    /// Refreshes all displayed statistics.
    /// </summary>
    public void RefreshDisplay()
    {
        if (trackerService == null) return;
        
        UpdateTextDisplays();
        UpdateGraph();
    }
    
    private void UpdateTextDisplays()
    {
        // Session time
        if (sessionTimeText != null)
        {
            sessionTimeText.text = $"Session Time: {FormatTime(trackerService.SessionDuration)}";
        }
        
        // Distance
        if (distanceText != null)
        {
            float distance = trackerService.GetDistanceWalked();
            distanceText.text = $"Distance Walked: {distance:F1} m";
        }
        
        // Stamina
        if (staminaText != null)
        {
            float stamina = trackerService.GetStaminaUsed();
            staminaText.text = $"Stamina Used: {stamina:F1}";
        }
        
        // Fatigue
        if (fatigueText != null)
        {
            float fatigue = trackerService.GetFatigueAccumulated();
            fatigueText.text = $"Fatigue: {fatigue:F1}";
        }
        
        // Health
        if (healthText != null)
        {
            float health = trackerService.GetHealthLost();
            healthText.text = $"Health Lost: {health:F1}";
        }
        
        // Consumables
        if (consumablesText != null)
        {
            int totalConsumables = trackerService.GetTotalConsumablesUsed();
            consumablesText.text = $"Consumables Used: {totalConsumables} items";
        }
        
        // Consumable details
        if (consumableDetailsText != null)
        {
            Dictionary<string, int> consumables = trackerService.GetConsumablesUsed();
            if (consumables.Count > 0)
            {
                string details = "Details:\n";
                foreach (var kvp in consumables)
                {
                    details += $"  • {kvp.Key}: {kvp.Value}x\n";
                }
                consumableDetailsText.text = details;
            }
            else
            {
                consumableDetailsText.text = "No consumables used yet.";
            }
        }
    }
    
    private void UpdateGraph()
    {
        if (graphRenderer == null) return;
        
        List<TimeSeriesDataPoint> data = trackerService.GetTimeSeriesData(currentGraphMetric);
        graphRenderer.RenderGraph(data, currentGraphMetric);
        
        // Update current metric label
        if (currentMetricText != null)
        {
            currentMetricText.text = GetMetricName(currentGraphMetric);
        }
    }
    
    /// <summary>
    /// Switches the displayed graph to a different metric.
    /// </summary>
    public void SwitchGraphMetric(StatMetricType metricType)
    {
        currentGraphMetric = metricType;
        UpdateGraph();
    }
    
    /// <summary>
    /// Resets all tracking data.
    /// </summary>
    private void ResetTracking()
    {
        if (trackerService != null)
        {
            trackerService.ResetTracking();
            RefreshDisplay();
        }
    }
    
    private string GetMetricName(StatMetricType metricType)
    {
        return metricType switch
        {
            StatMetricType.Distance => "Distance Over Time",
            StatMetricType.Stamina => "Stamina Used Over Time",
            StatMetricType.Fatigue => "Fatigue Over Time",
            StatMetricType.Health => "Health Lost Over Time",
            StatMetricType.Consumables => "Consumables Used Over Time",
            StatMetricType.PathTracking => "Path Positions Over Time",
            StatMetricType.RiskTracking => "Risk Events Over Time",
            _ => "Unknown Metric"
        };
    }
    
    private string FormatTime(float seconds)
    {
        int hours = Mathf.FloorToInt(seconds / 3600f);
        int minutes = Mathf.FloorToInt((seconds % 3600f) / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        
        if (hours > 0)
            return $"{hours}h {minutes}m {secs}s";
        else
            return $"{minutes}m {secs}s";
    }
    
    /// <summary>
    /// Blocks or unblocks player input
    /// </summary>
    private void BlockPlayerInput(bool block)
    {
        playerController?.SetInputBlocked(block);
    }
}
