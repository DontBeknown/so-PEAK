using System.Collections.Generic;

namespace Game.Dialog
{
    public interface IDialogManager
    {
        bool IsActive { get; }
        bool IsPaused { get; }
        bool HasTriggered(string dialogId);
        DialogData CurrentDialog { get; }
        int CurrentLineIndex { get; }
        void StartDialog(DialogData data, bool isReplay = false);
        void AdvanceLine();
        void PauseDialog();
        void ResumeDialog();
        void EndDialog();
        void LoadState(List<string> triggeredIds);
        List<string> GetTriggeredIds();
    }
}
