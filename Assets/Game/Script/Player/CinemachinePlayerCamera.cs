using UnityEngine;
using Unity.Cinemachine;
using Game.UI;
using DG.Tweening;

public class CinemachinePlayerCamera : MonoBehaviour, ICameraInputController
{
    private CinemachineCamera[] cinemachineCameras;
    private CinemachineBasicMultiChannelPerlin[] _perlinComponents;
    private Tween _shakeTween;
    private float _currentShakeAmplitude;

    private void Start()
    {
        SetCursorLock(true);
        CacheCinemachineCameras();
    }

    private void CacheCinemachineCameras()
    {
        cinemachineCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
    }

    public CinemachineCamera[] GetCinemachineCameras(bool refresh = false)
    {
        if (refresh || cinemachineCameras == null || cinemachineCameras.Length == 0)
        {
            CacheCinemachineCameras();
        }

        return cinemachineCameras;
    }

    /// <summary>
    /// Transitions camera shake to a target amplitude over a specified duration.
    /// Useful for environmental effects like tornados or earthquakes.
    /// </summary>
    public void TransitionShake(float targetAmplitude, float duration)
    {
        ResolvePerlinComponents(forceRefresh: true);
        if (_perlinComponents == null || _perlinComponents.Length == 0)
        {
            return;
        }

        _shakeTween?.Kill();

        float target = Mathf.Max(0f, targetAmplitude);
        if (duration <= 0f)
        {
            _currentShakeAmplitude = target;
            SetAllShakeAmplitudes(target);
            return;
        }

        _shakeTween = DOTween.To(
            () => _currentShakeAmplitude,
            value =>
            {
                _currentShakeAmplitude = value;
                SetAllShakeAmplitudes(value);
            },
            target,
            duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    /// <summary>
    /// Immediately stops camera shake and resets to zero amplitude.
    /// </summary>
    public void StopAndReset()
    {
        _shakeTween?.Kill();
        _shakeTween = null;
        _currentShakeAmplitude = 0f;
        SetAllShakeAmplitudes(0f);
    }

    private void ResolvePerlinComponents(bool forceRefresh)
    {
        if (forceRefresh || _perlinComponents == null)
        {
            CinemachineCamera[] cameras = GetCinemachineCameras(forceRefresh);
            if (cameras == null || cameras.Length == 0)
            {
                _perlinComponents = null;
                return;
            }

            System.Collections.Generic.List<CinemachineBasicMultiChannelPerlin> found = new System.Collections.Generic.List<CinemachineBasicMultiChannelPerlin>();
            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineCamera camera = cameras[i];
                if (camera == null)
                {
                    continue;
                }

                CinemachineBasicMultiChannelPerlin perlin = camera.GetComponent<CinemachineBasicMultiChannelPerlin>();
                if (perlin != null)
                {
                    found.Add(perlin);
                }
            }

            _perlinComponents = found.Count > 0 ? found.ToArray() : null;
        }
    }

    private void SetAllShakeAmplitudes(float amplitude)
    {
        if (_perlinComponents == null)
        {
            return;
        }

        for (int i = 0; i < _perlinComponents.Length; i++)
        {
            CinemachineBasicMultiChannelPerlin perlin = _perlinComponents[i];
            if (perlin != null)
            {
                perlin.AmplitudeGain = amplitude;
            }
        }
    }

    /// <summary>
    /// Updates all Cinemachine cameras to target the specified player transform.
    /// Call this after spawning a new player at runtime.
    /// </summary>
    /// <param name="playerTransform">The root transform of the player</param>
    /// <param name="cameraAimTarget">Optional specific target for LookAt (e.g., head). If null, uses playerTransform.</param>
    public void UpdateCameraTargets(Transform playerTransform, Transform cameraAimTarget = null)
    {
        if (playerTransform == null)
        {
            Debug.LogError("[CinemachinePlayerCamera] Cannot update camera targets - playerTransform is null!");
            return;
        }
        
        // Refresh camera cache
        CacheCinemachineCameras();
        
        Transform lookAtTarget = cameraAimTarget != null ? cameraAimTarget : playerTransform;
        
        if (cinemachineCameras != null)
        {
            foreach (var cam in cinemachineCameras)
            {
                if (cam != null)
                {
                    cam.Follow = playerTransform;
                    cam.LookAt = lookAtTarget;
                    //Debug.Log($"[CinemachinePlayerCamera] Updated camera '{cam.name}' - Follow: {playerTransform.name}, LookAt: {lookAtTarget.name}");
                }
            }
        }
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
