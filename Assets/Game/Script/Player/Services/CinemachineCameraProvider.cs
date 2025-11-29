using UnityEngine;
using Unity.Cinemachine;
using Game.Player.Interfaces;

namespace Game.Player.Services
{
    /// <summary>
    /// Camera provider that works with Cinemachine virtual cameras.
    /// Automatically finds and uses the active Cinemachine virtual camera or falls back to Camera.main.
    /// </summary>
    public class CinemachineCameraProvider : ICameraProvider
    {
        private Transform _cameraTransform;
        private Camera _mainCamera;
        private bool _cacheValid;

        public Transform CameraTransform
        {
            get
            {
                if (!_cacheValid || _cameraTransform == null)
                {
                    RefreshCamera();
                }
                return _cameraTransform;
            }
        }

        public Vector3 ForwardDirection
        {
            get
            {
                if (CameraTransform == null) return Vector3.forward;
                return Vector3.ProjectOnPlane(CameraTransform.forward, Vector3.up).normalized;
            }
        }

        public Vector3 RightDirection
        {
            get
            {
                if (CameraTransform == null) return Vector3.right;
                return Vector3.ProjectOnPlane(CameraTransform.right, Vector3.up).normalized;
            }
        }

        public Vector3 GetWorldDirection(Vector2 input)
        {
            if (CameraTransform == null) return Vector3.zero;

            // Normalize input if needed
            Vector2 normalizedInput = input.sqrMagnitude >= 1f ? input.normalized : input;

            // Convert screen-space input to world-space direction
            Vector3 forward = ForwardDirection;
            Vector3 right = RightDirection;

            return (forward * normalizedInput.y + right * normalizedInput.x).normalized;
        }

        private void RefreshCamera()
        {
            // Try to find main camera
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera != null)
            {
                _cameraTransform = _mainCamera.transform;
                _cacheValid = true;
            }
            else
            {
                // Fallback: try to find any camera with CinemachineBrain
                CinemachineBrain brain = Object.FindFirstObjectByType<CinemachineBrain>();
                if (brain != null)
                {
                    _mainCamera = brain.GetComponent<Camera>();
                    _cameraTransform = brain.transform;
                    _cacheValid = true;
                }
                else
                {
                    Debug.LogWarning("CinemachineCameraProvider: No camera with CinemachineBrain found!");
                }
            }
        }

        /// <summary>
        /// Allows manual camera assignment for testing or special scenarios
        /// </summary>
        public void SetCamera(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
            _cacheValid = true;
        }

        /// <summary>
        /// Invalidates the cache, forcing a camera refresh on next access
        /// </summary>
        public void InvalidateCache()
        {
            _cacheValid = false;
        }
    }
}
