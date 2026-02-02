using UnityEngine;
using Unity.Cinemachine;
using Game.UI;

public class CinemachinePlayerCamera : MonoBehaviour, ICameraInputController
{
    private CinemachineCamera[] cinemachineCameras;
    //private bool originalCameraEnabled = true;

    private void Start()
    {
        SetCursorLock(true);
        CacheCinemachineCameras();
    }

    private void CacheCinemachineCameras()
    {
        cinemachineCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
    }

    public void SetCursorLock(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            EnableCameraInput(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EnableCameraInput(false);
        }
        
    }

    public void EnableCameraInput(bool enable)
    {
        // Refresh cache in case cameras were added/changed
        if (cinemachineCameras == null || cinemachineCameras.Length == 0)
        {
            CacheCinemachineCameras();
        }
        
        int controllersFound = 0;
        
        // Enable or disable input for all Cinemachine cameras
        if (cinemachineCameras != null)
        {
            foreach (var cam in cinemachineCameras)
            {
                if (cam != null)
                {
                    // Disable the camera's input by setting enabled state of input components
                    var inputControllers = cam.GetComponents<CinemachineInputAxisController>();
                    foreach (var controller in inputControllers)
                    {
                        if (controller != null)
                        {
                            controller.enabled = enable;
                            controllersFound++;
                        }
                    }
                }
            }
        }
           
    }

    public bool IsCursorLocked() => Cursor.lockState == CursorLockMode.Locked;

}
