using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

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
        [SerializeField] private Button deleteButton;

        [Header("Visual States")]
        [SerializeField] private GameObject normalModeVisuals;
        [SerializeField] private GameObject deleteModeVisuals;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color deleteColor = new Color(1f, 0.5f, 0.5f, 1f);
        
        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;

        private SaveMetadata worldMetadata;
        private Action<SaveMetadata> onWorldSelected;
        private Action<SaveMetadata> onWorldDeleted;
        private bool isDeleteMode = false;

        public void Initialize(SaveMetadata metadata, Action<SaveMetadata> onSelect, Action<SaveMetadata> onDelete)
        {
            worldMetadata = metadata;
            onWorldSelected = onSelect;
            onWorldDeleted = onDelete;

            UpdateUI();

            // Setup button listeners
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(OnDeleteClicked);
            }
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(OnDeleteClicked);
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
                lastPlayedText.text = $"Last Played: {worldMetadata.lastPlayedDate:yyyy-MM-dd HH:mm}";
            }

            if (playTimeText != null)
            {
                if (worldMetadata.totalPlayTime > 0)
                {
                    int hours = (int)(worldMetadata.totalPlayTime / 3600f);
                    int minutes = (int)((worldMetadata.totalPlayTime % 3600f) / 60f);
                    playTimeText.text = $"Play Time: {hours}h {minutes}m";
                    playTimeText.gameObject.SetActive(true);
                }
                else
                {
                    playTimeText.gameObject.SetActive(false);
                }
            }

            // Hide info panel by default
            if (worldInfoPanel != null)
            {
                worldInfoPanel.SetActive(false);
            }
        }

        public void SetDeleteMode(bool enabled)
        {
            isDeleteMode = enabled;

            // Show/hide appropriate visuals
            if (normalModeVisuals != null)
                normalModeVisuals.SetActive(!isDeleteMode);

            if (deleteModeVisuals != null)
                deleteModeVisuals.SetActive(isDeleteMode);

            // Update background color
            if (backgroundImage != null)
            {
                backgroundImage.color = isDeleteMode ? deleteColor : normalColor;
            }

            // Update button interactability
            if (selectButton != null)
            {
                selectButton.interactable = !isDeleteMode;
            }

            deleteButton.gameObject.SetActive(isDeleteMode);
        }

        private void OnSelectClicked()
        {
            if (isDeleteMode)
            {
                Debug.LogWarning("Cannot select world in delete mode");
                return;
            }

            if (enableDebug) Debug.Log($"World slot clicked: {worldMetadata.worldName}");
            onWorldSelected?.Invoke(worldMetadata);
        }

        private void OnDeleteClicked()
        {
            if (!isDeleteMode)
            {
                Debug.LogWarning("Delete button should only be active in delete mode");
                return;
            }

            if (enableDebug) Debug.Log($"Delete button clicked for: {worldMetadata.worldName}");
            
            // Optional: Add confirmation dialog here
            onWorldDeleted?.Invoke(worldMetadata);
        }

        // Mouse hover effects - implements IPointerEnterHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null && !isDeleteMode)
            {
                backgroundImage.color = hoverColor;
            }

            // Show world info on hover
            if (worldInfoPanel != null)
            {
                worldInfoPanel.SetActive(true);
            }
        }

        // Mouse hover effects - implements IPointerExitHandler
        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null && !isDeleteMode)
            {
                backgroundImage.color = normalColor;
            }

            // Hide world info when not hovering
            if (worldInfoPanel != null)
            {
                worldInfoPanel.SetActive(false);
            }
        }
    }
}
