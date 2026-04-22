using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player;
using Game.UI;

public class DeathScreenUI : MonoBehaviour, IUIPanel
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text tipsText;

    [Header("Buttons")]
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Save / Scene")]
    [SerializeField] private WorldPersistenceManager worldPersistenceManager;
    [SerializeField] private string gameplaySceneName = "TerrainGenDemo";
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Player")]
    [SerializeField] private CinemachinePlayerCamera playerCamera;
    [SerializeField] private PlayerControllerRefactored playerController;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 1.5f;

    private IEventBus _eventBus;



    // IUIPanel
    public string PanelName => "DeathScreen";
    public bool BlocksInput => true;
    public bool UnlocksCursor => true;
    public bool IsActive => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (respawnButton != null)
            respawnButton.onClick.AddListener(OnRespawnClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();

        if (playerController == null)
            playerController = ServiceContainer.Instance.TryGet<PlayerControllerRefactored>();

        _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        _eventBus?.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
    }

    private void OnDestroy()
    {
        _eventBus?.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
    }

    private void OnPlayerDeath(PlayerDeathEvent evt)
    {
        ShowDeath(evt.Cause);
    }

    // --- IUIPanel ---

    public void Show()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InQuad);
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.DOKill();
    }

    public void Toggle()
    {
        if (IsActive) Hide();
        else Show();
    }

    // --- Public API ---

    public void ShowDeath(DeathCause cause)
    {
        if (titleText != null)
            titleText.text = "YOU DIED";

        if (detailText != null)
            detailText.text = GetDetailMessage(cause);

        if (tipsText != null)
            tipsText.text = GetTipMessage(cause);

        playerCamera?.SetCursorLock(false);
        playerController?.SetInputBlocked(true);

        Show();
    }

    // --- Private ---

    private string GetTipMessage(DeathCause cause)
    {
        return cause switch
        {
            DeathCause.Starvation   => "Watch your hunger bar and eat regularly to survive.",
            DeathCause.Dehydration  => "Keep your water flask filled. Dehydration happens faster than you think.",
            DeathCause.Damage       => "Avoid high falls and dangerous terrain. Recover before moving on.",
            DeathCause.LandslideRock=> "Listen for falling rocks and watch steep slopes. Move out of the impact path immediately.",
            DeathCause.Falling      => "Plan your route ahead and stay clear of cliff edges. Look for safer slopes instead of jumping.",
            DeathCause.Tornado      => "Avoid tornado zones. If caught inside, run away from the center line as quickly as possible.",
            _                       => "Prepare yourself before exploring the unknown."
        };
    }

    private string GetDetailMessage(DeathCause cause)
    {
        return cause switch
        {
            DeathCause.Starvation   => "You starved to death. Your body gave out without food.",
            DeathCause.Dehydration  => "You died from dehydration. Your body needed water to survive.",
            DeathCause.Damage       => "You died from your injuries.",
            DeathCause.LandslideRock=> "You were fatally struck by a landslide rock.",
            DeathCause.Falling      => "You fell from a height and died on impact.",
            DeathCause.Tornado      => "You were caught near the tornado core and killed by violent spinning force and impact.",
            _                       => "Your life ended before you could react."
        };
    }

    private void OnRespawnClicked()
    {
        var saveService = SaveLoadService.Instance;
        if (saveService == null || saveService.CurrentWorldSave == null)
        {
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        saveService.SaveDeathCountOnly();

        string worldGuid = saveService.CurrentWorldSave.worldGuid;
        WorldSaveData data = saveService.LoadWorld(worldGuid);

        if (data != null && worldPersistenceManager != null)
            worldPersistenceManager.PrepareLoadWorld(data);

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnMainMenuClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}
