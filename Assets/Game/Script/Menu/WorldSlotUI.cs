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

        private WorldData worldData;
        private Action<WorldData> onWorldSelected;
        private Action<WorldData> onWorldDeleted;
        private bool isDeleteMode = false;

        public void Initialize(WorldData data, Action<WorldData> onSelect, Action<WorldData> onDelete)
        {
            worldData = data;
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
            if (worldData == null)
                return;

            // Update world name
            if (worldNameText != null)
            {
                worldNameText.text = worldData.WorldName;
            }

            // Update world info (seed, last played, play time)
            if (seedText != null)
            {
                seedText.text = $"Seed: {worldData.Seed}";
            }

            if (lastPlayedText != null)
            {
                lastPlayedText.text = $"Last Played: {worldData.LastPlayed}";
            }

            if (playTimeText != null)
            {
                if (worldData.PlayTimeMinutes > 0)
                {
                    int hours = worldData.PlayTimeMinutes / 60;
                    int minutes = worldData.PlayTimeMinutes % 60;
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

            //Debug.Log($"World slot clicked: {worldData.WorldName}");
            onWorldSelected?.Invoke(worldData);
        }

        private void OnDeleteClicked()
        {
            if (!isDeleteMode)
            {
                Debug.LogWarning("Delete button should only be active in delete mode");
                return;
            }

            Debug.Log($"Delete button clicked for: {worldData.WorldName}");
            
            // Optional: Add confirmation dialog here
            onWorldDeleted?.Invoke(worldData);
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
