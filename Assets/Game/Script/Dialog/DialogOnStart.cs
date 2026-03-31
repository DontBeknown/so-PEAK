using UnityEngine;
using Game.Dialog;

namespace Game.Script.Dialog
{
    public class DialogOnStart : MonoBehaviour
    {
        [SerializeField] private DialogData dialogData;
        [SerializeField] private bool replayIfTriggered = false;
        [SerializeField] private DialogManager _dialogManager;

        private void Awake()
        {
            // Auto-find DialogManager if not assigned in inspector
            if (_dialogManager == null)
            {
                _dialogManager = FindFirstObjectByType<DialogManager>();
                if (_dialogManager == null)
                {
                    Debug.LogError("DialogOnStart: No DialogManager found in the scene.");
                }
            }
        }

        private void Start()
        {
            // Check if SaveLoadService exists and level is 1
            var saveLoadService = SaveLoadService.Instance;
            if (saveLoadService != null && _dialogManager != null && dialogData != null)
            {
                int currentLevel = saveLoadService.GetCurrentLevel();
                if (currentLevel == 1)
                {
                    _dialogManager.StartDialog(dialogData, replayIfTriggered);
                    //Debug.Log($"DialogOnStart: Started dialog '{dialogData.dialogId}' on start. Replay if triggered: {replayIfTriggered}");
                }
            }
            else if (dialogData == null)
            {
                Debug.LogWarning("DialogOnStart: No DialogData assigned.");
            }
        }
    }
}
