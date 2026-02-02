using UnityEngine;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// Blocks player input when UI is open
    /// Single Responsibility: Input blocking only
    /// </summary>
    public class PlayerInputBlocker : IInputBlocker
    {
        private readonly PlayerControllerRefactored _playerController;
        private readonly ICameraInputController _cameraController;
        private bool _isBlocked = false;
        
        public bool IsInputBlocked => _isBlocked;
        
        public PlayerInputBlocker(PlayerControllerRefactored playerController, ICameraInputController cameraController = null)
        {
            _playerController = playerController;
            _cameraController = cameraController;
        }
        
        public void BlockInput()
        {
            if (_isBlocked) return;
            
            _isBlocked = true;
            _playerController?.SetInputBlocked(true);
            _cameraController?.EnableCameraInput(false);
        }
        
        public void UnblockInput()
        {
            if (!_isBlocked) return;
            
            _isBlocked = false;
            _playerController?.SetInputBlocked(false);
            _cameraController?.EnableCameraInput(true);
        }
    }
}
