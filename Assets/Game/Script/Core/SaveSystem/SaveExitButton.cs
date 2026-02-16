using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveExitButton : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";
    private void Start()
    {
        // Auto-bind to button on this GameObject
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(SaveAndExitToMenu);
        }
    }
    
    private void SaveAndExitToMenu()
    {
        var saveService = SaveLoadService.Instance;
        if (saveService != null)
        {
            saveService.PerformAutoSave();
        }
        
        SceneManager.LoadScene(menuSceneName);
    }
}