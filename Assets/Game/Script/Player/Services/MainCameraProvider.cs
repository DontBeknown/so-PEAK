using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player.Services
{
    /// <summary>
    /// Default implementation of ICameraProvider using Camera.main.
    /// Can be replaced with custom camera systems for testing or different gameplay modes.
    /// </summary>
    public class MainCameraProvider : ICameraProvider
    {
        private Transform _cameraTransform;

        public Transform CameraTransform
        {
            get
            {
                if (_cameraTransform == null)
                {
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        _cameraTransform = mainCamera.transform;
                    }
                    else
                    {
                        Debug.LogWarning("MainCameraProvider: No main camera found!");
                    }
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
            Vector3 moveDir = Quaternion.FromToRotation(CameraTransform.up, Vector3.up) *
                             CameraTransform.TransformDirection(new Vector3(normalizedInput.x, 0f, normalizedInput.y));

            return moveDir;
        }

        /// <summary>
        /// Allows manual camera assignment for testing or special scenarios
        /// </summary>
        public void SetCamera(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }
    }
}
