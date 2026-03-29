using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using DG.Tweening;
using Game.Sound;

namespace Game.Menu
{
    /// <summary>
    /// Individual world slot UI component - represents a single world in the list
    /// </summary>
    public class WorldSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI worldNameText;
        [SerializeField] private GameObject worldInfoPanel;
        [SerializeField] private TextMeshProUGUI seedText;
        [SerializeField] private TextMeshProUGUI lastPlayedText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private Button selectButton;

        [Header("Visual States")]
        [SerializeField] private GameObject highlightBorder;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.4f, 0.7f, 1f, 1f);
        [SerializeField] private Color selectedHoverColor = new Color(0.5f, 0.8f, 1f, 1f);
        
        [Header("Hover Scale")]
        [SerializeField] private float hoverScale    = 1.05f;
        [SerializeField] private float hoverDuration = 0.25f;
        [SerializeField] private Ease  hoverEase     = Ease.OutBack;

        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;

        private Vector3 _originalScale;
        private Tweener _scaleTween;
        private SaveMetadata worldMetadata;
        private Action<SaveMetadata> onWorldSelected;
        private bool isSelected = false;
        private SoundService soundService;

        public bool IsSelected => isSelected;
        public SaveMetadata WorldMetadata => worldMetadata;

        public void Initialize(SaveMetadata metadata, Action<SaveMetadata> onSelect,SoundService sound)
        {
            worldMetadata = metadata;
            onWorldSelected = onSelect;
            soundService = sound;

            UpdateUI();

            // Setup button listeners
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectClicked);
            }
        }

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            // Clean up listeners
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectClicked);
            }
        }

        private void UpdateUI()
        {
            if (worldMetadata == null)
                return;

            // Update world name
            if (worldNameText != null)
            {
                worldNameText.text = worldMetadata.worldName;
            }

            // Update world info (seed, last played, play time)
            if (seedText != null)
            {
                // Display seed in 3-part format (seed1-seed2-seed3)
                string seedDisplay = $"{worldMetadata.seed1}-{worldMetadata.seed2}-{worldMetadata.seed3}";
                seedText.text = $"Seed: {seedDisplay}";
            }

            if (lastPlayedText != null)
            {
                lastPlayedText.text = $"{worldMetadata.lastPlayedDate:dd/MM/yyyy}";
            }

            if (playTimeText != null)
            {
                if (worldMetadata.totalPlayTime > 0)
                {
                    int hours = (int)(worldMetadata.totalPlayTime / 3600f);
                    int minutes = (int)((worldMetadata.totalPlayTime % 3600f) / 60f);
                    playTimeText.text = $"{hours}h {minutes}m";
                    playTimeText.gameObject.SetActive(true);
                }
                else
                {
                    playTimeText.gameObject.SetActive(false);
                }
            }

            // Always show info panel (changed from hover-only behavior)
            if (worldInfoPanel != null)
            {
                worldInfoPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Mark this slot as selected
        /// </summary>
        public void Select()
        {
            isSelected = true;
            UpdateBackgroundColor();

            if (enableDebug) Debug.Log($"World slot selected: {worldMetadata.worldName}");
        }

        /// <summary>
        /// Mark this slot as deselected
        /// </summary>
        public void Deselect()
        {
            isSelected = false;
            UpdateBackgroundColor();

            if (enableDebug) Debug.Log($"World slot deselected: {worldMetadata.worldName}");
        }

        private void UpdateBackgroundColor()
        {
            highlightBorder.SetActive(isSelected);
            if (backgroundImage == null)
                return;

            if (isSelected)
            {
                backgroundImage.color = selectedColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }

        private void OnSelectClicked()
        {
            if (enableDebug) Debug.Log($"World slot clicked: {worldMetadata.worldName}");
            soundService.PlayUISound("ui_click", volumeScale: 0.3f);
            onWorldSelected?.Invoke(worldMetadata);
        }

        // Mouse hover effects - implements IPointerEnterHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? selectedHoverColor : hoverColor;
            }

            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale * hoverScale, hoverDuration)
                .SetEase(hoverEase)
                .SetLink(gameObject);

            soundService.PlayUISound("ui_hover", volumeScale: 0.3f);
        }

        // Mouse hover effects - implements IPointerExitHandler
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? selectedColor : normalColor;
            }

            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale, hoverDuration)
                .SetEase(hoverEase)
                .SetLink(gameObject);
        }
    }
}
