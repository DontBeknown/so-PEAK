using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveExitButton : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Scenes_Menu";
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

        var saveService = SaveLoadService.Instance;
        if (saveService != null)
        {
            saveService.PerformAutoSave();
        }
        
        SceneManager.LoadScene(menuSceneName);
    }
}