using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveExitButton : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Scenes_Menu";
    [SerializeField] private string waitingRoomSceneName = "Scene_Debug_Gameplay";
    private Button _button;

    private void Start()
    {
        // Auto-bind to button on this GameObject
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(SaveAndExitToMenu);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(SaveAndExitToMenu);
        }
    }
    
    public void SaveAndExitToMenu()
    {
        // Ensure gameplay is resumed before scene transition.
        Time.timeScale = 1f;

        if (IsPlayerInWaitingRoom())
        {
            Debug.Log("[SaveExitButton] Skipping save while in waiting room.");
        }
        else
        {
            var saveService = SaveLoadService.Instance;
            if (saveService != null)
            {
                saveService.PerformAutoSave();
            }
            else
            {
                Debug.LogWarning("SaveLoadService instance not found. Unable to perform auto-save.");
            }
        }
        
        SaveLoadService.Instance?.MarkFirstSpawnComplete();
        SceneManager.LoadScene(menuSceneName);
    }

    private bool IsPlayerInWaitingRoom()
    {
        if (string.IsNullOrEmpty(waitingRoomSceneName))
        {
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.name == waitingRoomSceneName)
        {
            return true;
        }

        Scene waitingRoomScene = SceneManager.GetSceneByName(waitingRoomSceneName);
        return waitingRoomScene.IsValid() && waitingRoomScene.isLoaded;
    }
}