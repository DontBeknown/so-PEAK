using System.Collections.Generic;
using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;

namespace Game.Dialog
{
    public class DialogManager : MonoBehaviour, IDialogManager
    {
        private IEventBus _eventBus;

        private readonly HashSet<string> _triggeredDialogs = new HashSet<string>();
        private DialogData _currentDialog;
        private int _currentLineIndex;
        private bool _isPaused;

        public bool IsActive => _currentDialog != null;
        public bool IsPaused => _isPaused;
        public DialogData CurrentDialog => _currentDialog;
        public int CurrentLineIndex => _currentLineIndex;

        /// <summary>Called by GameServiceBootstrapper after registration.</summary>
        public void Initialize(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public bool HasTriggered(string dialogId)
        {
            return !string.IsNullOrWhiteSpace(dialogId) && _triggeredDialogs.Contains(dialogId);
        }

        public void StartDialog(DialogData data, bool isReplay = false)
        {
            if (data == null || data.lines == null || data.lines.Count == 0)
                return;

            if (!isReplay && HasTriggered(data.dialogId))
                return;

            _currentDialog = data;
            _currentLineIndex = 0;
            _isPaused = false;

            if (!string.IsNullOrWhiteSpace(data.dialogId))
            {
                _triggeredDialogs.Add(data.dialogId);
            }

            _eventBus?.Publish(new DialogStartedEvent(data, isReplay));
            PublishCurrentLine();
        }

        public void AdvanceLine()
        {
            if (!IsActive || _isPaused)
                return;

            _currentLineIndex++;
            if (_currentDialog == null || _currentDialog.lines == null || _currentLineIndex >= _currentDialog.lines.Count)
            {
                EndDialog();
                return;
            }

            PublishCurrentLine();
        }

        public void PauseDialog()
        {
            if (!IsActive || _isPaused)
                return;

            _isPaused = true;
            _eventBus?.Publish(new DialogPausedEvent(true));
        }

        public void ResumeDialog()
        {
            if (!IsActive || !_isPaused)
                return;

            _isPaused = false;
            _eventBus?.Publish(new DialogPausedEvent(false));
            PublishCurrentLine();
        }

        public void EndDialog()
        {
            if (_currentDialog == null)
                return;

            var id = _currentDialog.dialogId;
            _currentDialog = null;
            _currentLineIndex = 0;
            _isPaused = false;

            _eventBus?.Publish(new DialogEndedEvent(id));
        }

        public void LoadState(List<string> triggeredIds)
        {
            _triggeredDialogs.Clear();
            if (triggeredIds == null)
                return;

            foreach (var id in triggeredIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _triggeredDialogs.Add(id);
                }
            }
        }

        public List<string> GetTriggeredIds()
        {
            return new List<string>(_triggeredDialogs);
        }

        private void PublishCurrentLine()
        {
            if (_currentDialog == null || _currentDialog.lines == null)
                return;

            if (_currentLineIndex < 0 || _currentLineIndex >= _currentDialog.lines.Count)
                return;

            var line = _currentDialog.lines[_currentLineIndex];
            _eventBus?.Publish(new DialogLineChangedEvent(_currentDialog, _currentLineIndex, line));
        }
    }
}
