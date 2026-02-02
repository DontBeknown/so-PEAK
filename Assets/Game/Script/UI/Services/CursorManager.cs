using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Manages cursor visibility and lock state
    /// Single Responsibility: Cursor control only
    /// </summary>
    public class CursorManager : ICursorManager
    {
        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;
        public bool IsCursorVisible => Cursor.visible;
        
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        public void ShowCursor()
        {
            Cursor.visible = true;
        }
        
        public void HideCursor()
        {
            Cursor.visible = false;
        }
    }
}
