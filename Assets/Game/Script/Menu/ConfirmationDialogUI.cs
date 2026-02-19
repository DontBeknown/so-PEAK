using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Game.Menu
{
    /// <summary>
    /// Confirmation dialog UI for critical actions (e.g., world deletion)
    /// Shows modal dialog with title, message, and confirm/cancel buttons
    /// </summary>
    public class ConfirmationDialogUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private GameObject backdropPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private TextMeshProUGUI cancelButtonText;

        [Header("Animation")]
        [SerializeField] private MenuPanelAnimator panelAnimator;

        [Header("Button Colors")]
        [SerializeField] private Color confirmButtonColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red for destructive actions
        [SerializeField] private Color cancelButtonColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Gray

        private Action onConfirmCallback;
        private Action onCancelCallback;

        private void Awake()
        {
            // Ensure dialog is hidden on start
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }
            if (backdropPanel != null)
            {
                backdropPanel.SetActive(false);
            }

            // Setup button listeners
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            // Apply button colors
            ApplyButtonColors();
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            }
        }

        /// <summary>
        /// Show the confirmation dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <param name="onConfirm">Callback when confirm is clicked</param>
        /// <param name="onCancel">Callback when cancel is clicked (optional)</param>
        /// <param name="confirmText">Custom confirm button text (default: "Confirm")</param>
        /// <param name="cancelText">Custom cancel button text (default: "Cancel")</param>
        public void Show(string title, string message, Action onConfirm, Action onCancel = null, 
                        string confirmText = "Confirm", string cancelText = "Cancel")
        {
            onConfirmCallback = onConfirm;
            onCancelCallback = onCancel;

            // Set text
            if (titleText != null)
            {
                titleText.text = title;
            }
            if (messageText != null)
            {
                messageText.text = message;
            }
            if (confirmButtonText != null)
            {
                confirmButtonText.text = confirmText;
            }
            if (cancelButtonText != null)
            {
                cancelButtonText.text = cancelText;
            }

            // Show backdrop first
            if (backdropPanel != null)
            {
                backdropPanel.SetActive(true);
            }

            // Show dialog with animation
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }

            if (panelAnimator != null)
            {
                panelAnimator.PlayShowAnimation();
            }
        }

        /// <summary>
        /// Hide the confirmation dialog
        /// </summary>
        public void Hide()
        {
            if (panelAnimator != null)
            {
                panelAnimator.PlayHideAnimation(() =>
                {
                    if (dialogPanel != null)
                    {
                        dialogPanel.SetActive(false);
                    }
                    if (backdropPanel != null)
                    {
                        backdropPanel.SetActive(false);
                    }
                });
            }
            else
            {
                if (dialogPanel != null)
                {
                    dialogPanel.SetActive(false);
                }
                if (backdropPanel != null)
                {
                    backdropPanel.SetActive(false);
                }
            }
        }

        private void OnConfirmClicked()
        {
            Hide();
            onConfirmCallback?.Invoke();
        }

        private void OnCancelClicked()
        {
            Hide();
            onCancelCallback?.Invoke();
        }

        private void ApplyButtonColors()
        {
            if (confirmButton != null)
            {
                var colors = confirmButton.colors;
                colors.normalColor = confirmButtonColor;
                colors.highlightedColor = confirmButtonColor * 1.2f;
                colors.pressedColor = confirmButtonColor * 0.8f;
                confirmButton.colors = colors;
            }

            if (cancelButton != null)
            {
                var colors = cancelButton.colors;
                colors.normalColor = cancelButtonColor;
                colors.highlightedColor = cancelButtonColor * 1.2f;
                colors.pressedColor = cancelButtonColor * 0.8f;
                cancelButton.colors = colors;
            }
        }
    }
}
