using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Game.Sound;

namespace Game.UI
{
    /// <summary>
    /// Sound settings panel for the Main Menu.
    /// Implements IUIPanel — register with UIServiceProvider if present,
    /// or open/close directly via SoundSettingsPanel.Toggle().
    /// </summary>
    public class SoundSettingsPanel : MonoBehaviour, IUIPanel
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] private float animDuration = 0.2f;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        [Header("Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider ambientSlider;
        [SerializeField] private Slider uiSlider;

        [Header("Value Labels")]
        [SerializeField] private TMP_Text masterValueLabel;
        [SerializeField] private TMP_Text musicValueLabel;
        [SerializeField] private TMP_Text sfxValueLabel;
        [SerializeField] private TMP_Text ambientValueLabel;
        [SerializeField] private TMP_Text uiValueLabel;

        // ─── IUIPanel ───────────────────────────────────────────────────────
        public string PanelName  => "SoundSettings";
        public bool BlocksInput  => false;
        public bool UnlocksCursor => true;
        public bool IsActive     => panelRoot != null && panelRoot.activeSelf;

        private SoundSettingsManager _manager;

        // ────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            // Wire slider callbacks
            if (masterSlider  != null) masterSlider .onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider   != null) musicSlider  .onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider     != null) sfxSlider    .onValueChanged.AddListener(OnSFXChanged);
            if (ambientSlider != null) ambientSlider.onValueChanged.AddListener(OnAmbientChanged);
            if (uiSlider      != null) uiSlider     .onValueChanged.AddListener(OnUIChanged);

            // Start hidden
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Start()
        {
            _manager = SoundSettingsManager.Instance != null
                ? SoundSettingsManager.Instance
                : FindFirstObjectByType<SoundSettingsManager>();

            if (_manager == null)
                Debug.LogWarning("[SoundSettingsPanel] SoundSettingsManager not found in scene.");
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);

            if (masterSlider  != null) masterSlider .onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider   != null) musicSlider  .onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider     != null) sfxSlider    .onValueChanged.RemoveListener(OnSFXChanged);
            if (ambientSlider != null) ambientSlider.onValueChanged.RemoveListener(OnAmbientChanged);
            if (uiSlider      != null) uiSlider     .onValueChanged.RemoveListener(OnUIChanged);
        }

        // ─── IUIPanel ───────────────────────────────────────────────────────

        public void Show()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            SyncSlidersFromManager();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            }
        }

        public void Hide()
        {
            if (panelRoot == null) return;

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, animDuration).SetEase(Ease.InQuad).SetUpdate(true)
                    .OnComplete(() => panelRoot.SetActive(false));
            }
            else
            {
                panelRoot.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsActive) Hide();
            else Show();
        }

        // ─── Slider callbacks ────────────────────────────────────────────────

        private void OnMasterChanged(float value)
        {
            _manager?.SetMaster(value);
            SetLabel(masterValueLabel, value);
        }

        private void OnMusicChanged(float value)
        {
            _manager?.SetMusic(value);
            SetLabel(musicValueLabel, value);
        }

        private void OnSFXChanged(float value)
        {
            _manager?.SetSFX(value);
            SetLabel(sfxValueLabel, value);
        }

        private void OnAmbientChanged(float value)
        {
            _manager?.SetAmbient(value);
            SetLabel(ambientValueLabel, value);
        }

        private void OnUIChanged(float value)
        {
            _manager?.SetUI(value);
            SetLabel(uiValueLabel, value);
        }

        private static void SetLabel(TMP_Text label, float value)
        {
            if (label != null)
                label.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        // ─── Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates sliders with current values from the manager without
        /// triggering the onValueChanged callbacks.
        /// </summary>
        private void SyncSlidersFromManager()
        {
            if (_manager == null) return;

            SetSliderSilent(masterSlider,  _manager.MasterVolume);
            SetSliderSilent(musicSlider,   _manager.MusicVolume);
            SetSliderSilent(sfxSlider,     _manager.SfxVolume);
            SetSliderSilent(ambientSlider, _manager.AmbientVolume);
            SetSliderSilent(uiSlider,      _manager.UIVolume);

            SetLabel(masterValueLabel,  _manager.MasterVolume);
            SetLabel(musicValueLabel,   _manager.MusicVolume);
            SetLabel(sfxValueLabel,     _manager.SfxVolume);
            SetLabel(ambientValueLabel, _manager.AmbientVolume);
            SetLabel(uiValueLabel,      _manager.UIVolume);
        }

        private void SetSliderSilent(Slider slider, float value)
        {
            if (slider == null) return;
            slider.onValueChanged.RemoveAllListeners();
            slider.value = value;
            slider.onValueChanged.AddListener(GetListenerFor(slider));
        }

        private UnityEngine.Events.UnityAction<float> GetListenerFor(Slider slider)
        {
            if (slider == masterSlider)  return OnMasterChanged;
            if (slider == musicSlider)   return OnMusicChanged;
            if (slider == sfxSlider)     return OnSFXChanged;
            if (slider == ambientSlider) return OnAmbientChanged;
            if (slider == uiSlider)      return OnUIChanged;
            return null;
        }
    }
}
