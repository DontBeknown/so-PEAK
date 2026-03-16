using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Player;
using Game.Player.Services;
using Game.Core.DI;
using Game.Core.Events;
using Game.UI;

/// <summary>
/// UI presenter for displaying player statistics.
/// SRP: Only responsible for UI presentation.
/// </summary>
public class PlayerStatsTrackerUI : MonoBehaviour, IUIPanel
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStatsTrackerService trackerService;
    [SerializeField] private CinemachinePlayerCamera playerCamera;
    [SerializeField] private PlayerControllerRefactored playerController;
    [SerializeField] private SimpleStatsHUD simpleStatsHUD;
    [SerializeField] private AssessmentReportUI assessmentReportUI;
    
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    
    [Header("Tab System")]
    [SerializeField] private GameObject statTrackingTab;
    [SerializeField] private GameObject assessmentTab;
    [SerializeField] private Button statTrackingTabButton;
    [SerializeField] private Button assessmentTabButton;
    [SerializeField] private Button nextLevelButton;
    
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
    private bool _progressNextLevelMode = false;
    private IEventBus _eventBus;
    
    // IUIPanel implementation
    public bool IsActive => panel != null && panel.activeSelf;
    public string PanelName => "PlayerStatsTracker";
    public bool BlocksInput => true;
    public bool UnlocksCursor => true;
    
    private void Awake()
    {
        if (trackerService == null)
        {
            trackerService = ServiceContainer.Instance.TryGet<PlayerStatsTrackerService>();
        }
        
        if (graphRenderer == null)
        {
            graphRenderer = GetComponentInChildren<StatGraphRenderer>();
        }

        if (playerCamera == null)
        {
            playerCamera = ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
        }
        
        if (playerController == null)
        {
            playerController = ServiceContainer.Instance.TryGet<PlayerControllerRefactored>();
        }

        if(simpleStatsHUD == null)
        {
            simpleStatsHUD = ServiceContainer.Instance.TryGet<SimpleStatsHUD>();
        }
        
        _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        
        SetupButtons();
        
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        _eventBus?.Subscribe<AssessmentUIOpenedEvent>(OnAssessmentUIOpened);
    }
    
    private void OnDisable()
    {
        _eventBus?.Unsubscribe<AssessmentUIOpenedEvent>(OnAssessmentUIOpened);
    }
    
    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonPress);
        
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelButtonPressed);
        
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
        
        if (assessmentReportUI != null){
            assessmentReportUI.GenerateAndDisplayAssessment();
        } 
        
        if (panel != null)
        {
            panel.SetActive(true);
            SwitchToTab(true); // Default to stat tracking tab
            ApplyButtonMode();
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
        
        if (assessmentTab != null)
            assessmentTab.SetActive(!showStatTracking);
        
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
        
        // Reset progression mode flag when hiding
        _progressNextLevelMode = false;
        
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

    private void OnCloseButtonPress(){
        var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
        if (uiService != null)
        {
            //Debug.Log("[PlayerStatsTrackerUI] Closing panel via UIServiceProvider");
            uiService.ClosePanel(PanelName);
        }
    }
    
    private void OnAssessmentUIOpened(AssessmentUIOpenedEvent evt)
    {
        if (evt.PanelName != PanelName) return;
        
        // Store the progression mode for this terminal
        _progressNextLevelMode = evt.ProgressNextLevelOnUse;
        ApplyButtonMode();
    }
    
    private void ApplyButtonMode()
    {
        // Show/hide buttons based on progression mode
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(_progressNextLevelMode);
        
        if (closeButton != null)
            closeButton.gameObject.SetActive(!_progressNextLevelMode);
    }
    
    private void OnNextLevelButtonPressed()
    {
        var saveService = SaveLoadService.Instance;
        if (saveService == null)
        {
            Debug.LogWarning("[PlayerStatsTrackerUI] SaveLoadService not found");
            return;
        }
        
        int currentLevel = saveService.GetCurrentLevel();
        
        if (currentLevel == 3)
        {
            // Level 3 reached: show ending screen instead of progressing
            var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
            if (uiService != null)
            {
                uiService.OpenPanel("EndingScreen");
            }
        }
        else
        {
            // Progression: increment level and close panel
            saveService.ProgressToNextLevel();
            saveService.PerformAutoSave();
            
            var uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
            if (uiService != null)
            {
                uiService.ClosePanel(PanelName);
            }
        }
    }
}