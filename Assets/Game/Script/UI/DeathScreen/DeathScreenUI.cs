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
            titleText.text = "YOU DEAD";

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
            DeathCause.Starvation   => "จับตาดูแถบความหิวและกินอาหารสม่ำเสมอเพื่อความอยู่รอด",
            DeathCause.Dehydration  => "เติมกระติกน้ำให้เต็มไว้เสมอ — การขาดน้ำเกิดขึ้นเร็วกว่าที่คุณคิด",
            DeathCause.Damage       => "หลีกเลี่ยงการตกจากที่สูงและภูมิประเทศอันตราย รักษาตัวเองก่อนออกเดินทางต่อ",
            DeathCause.Falling      => "วางแผนเส้นทางไว้ก่อน และระวังขอบผา มองหาเส้นทางลาดที่ปลอดภัยแทนการกระโดด",
            _                       => "เตรียมตัวให้พร้อมก่อนออกสำรวจดินแดนที่ไม่รู้จัก"
        };
    }

    private string GetDetailMessage(DeathCause cause)
    {
        return cause switch
        {
            DeathCause.Starvation   => "คุณอดอยากจนตาย ร่างกายของคุณทนไม่ไหวโดยไม่มีอาหาร",
            DeathCause.Dehydration  => "คุณตายเพราะขาดน้ำ ร่างกายของคุณต้องการน้ำเพื่อความอยู่รอด",
            DeathCause.Damage       => "คุณตายจากบาดแผลที่ได้รับ",
            DeathCause.Falling      => "คุณพลัดตกจากที่สูงและเสียชีวิตจากแรงกระแทก",
            _                       => "คุณจบชีวิตลงอย่างไม่ทันตั้งตัว"
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
