using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Game.Menu
{
    /// <summary>
    /// World creation menu - handles creating new worlds with name and seed
    /// </summary>
    public class WorldCreateUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField worldNameInput;
        [SerializeField] private TMP_InputField seedInput;
        [SerializeField] private Button createButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button randomSeedButton;

        [Header("Validation")]
        [SerializeField] private TextMeshProUGUI errorText;

        [Header("References")]
        [SerializeField] private WorldSelectionUI worldSelectionUI;

        private void Start()
        {
            // Setup button listeners
            createButton.onClick.AddListener(OnCreateClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
            
            if (randomSeedButton != null)
            {
                randomSeedButton.onClick.AddListener(OnRandomSeedClicked);
            }

            if (worldSelectionUI == null)
            {
                worldSelectionUI = FindFirstObjectByType<WorldSelectionUI>();
            }

            // Clear error text on start
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }

            // Set default random seed
            GenerateRandomSeed();
        }

        private void OnDestroy()
        {
            // Clean up listeners
            createButton.onClick.RemoveListener(OnCreateClicked);
            cancelButton.onClick.RemoveListener(OnCancelClicked);
            
            if (randomSeedButton != null)
            {
                randomSeedButton.onClick.RemoveListener(OnRandomSeedClicked);
            }
        }

        private void OnEnable()
        {
            // Reset fields when opening
            if (worldNameInput != null)
            {
                worldNameInput.text = "";
            }
            
            GenerateRandomSeed();

            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }

            // Focus on world name input
            if (worldNameInput != null)
            {
                worldNameInput.Select();
                worldNameInput.ActivateInputField();
            }
        }

        private void OnCreateClicked()
        {
            if (!ValidateInput())
            {
                return;
            }

            string worldName = worldNameInput.text.Trim();
            int seed = 0;

            // Parse seed
            if (string.IsNullOrEmpty(seedInput.text))
            {
                // Generate random seed if empty
                seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
            else if (!int.TryParse(seedInput.text, out seed))
            {
                // If not a valid number, use the string hash code as seed
                seed = seedInput.text.GetHashCode();
            }

            Debug.Log($"Creating world: {worldName} with seed: {seed}");

            // Create world data
            WorldData newWorld = new WorldData(
                worldName, 
                seed, 
                DateTime.Now.ToString("yyyy-MM-dd"), 
                0
            );

            // Add to world list
            if (worldSelectionUI != null)
            {
                worldSelectionUI.AddWorld(newWorld);
                worldSelectionUI.ShowWorldSelection();
            }

            gameObject.SetActive(false);
        }

        private void OnCancelClicked()
        {
            Debug.Log("Cancel world creation");
            
            if (worldSelectionUI != null)
            {
                worldSelectionUI.ShowWorldSelection();
            }
            
            gameObject.SetActive(false);
        }

        private void OnRandomSeedClicked()
        {
            GenerateRandomSeed();
        }

        private void GenerateRandomSeed()
        {
            if (seedInput != null)
            {
                int randomSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                seedInput.text = randomSeed.ToString();
            }
        }

        private bool ValidateInput()
        {
            // Check if world name is empty
            if (string.IsNullOrWhiteSpace(worldNameInput.text))
            {
                ShowError("Please enter a world name");
                return false;
            }

            // Check if world name is too long
            if (worldNameInput.text.Trim().Length > 50)
            {
                ShowError("World name is too long (max 50 characters)");
                return false;
            }

            // Additional validation can be added here
            // For example: check if world name already exists

            return true;
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
            Debug.LogWarning($"World creation error: {message}");
        }
    }
}
