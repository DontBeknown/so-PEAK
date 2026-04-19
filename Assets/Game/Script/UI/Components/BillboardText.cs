using UnityEngine;
using Unity.Cinemachine;

namespace Game.UI.Components
{
    /// <summary>
    /// Makes a UI canvas always face the camera (billboard effect).
    /// Automatically rotates each frame to face the camera's position.
    /// Works with WorldSpace Canvas for floating text effects.
    /// </summary>
    public class BillboardText : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private Camera overrideCamera;
        [SerializeField] private bool flipFacing = true;

        [Header("Fade Settings")]
        [SerializeField] private bool enableDistanceFade = true;
        [SerializeField] private float fadeStartDistance = 50f;
        [SerializeField] private float fadeEndDistance = 100f;

        [Header("Scale Settings")]
        [SerializeField] private bool enableDistanceScale = true;
        [SerializeField] private float scaleStartDistance = 5f;
        [SerializeField] private float scaleEndDistance = 100f;
        [SerializeField] private float minScaleMultiplier = 1f;
        [SerializeField] private float maxScaleMultiplier = 2f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private CanvasGroup _canvasGroup;
        private Camera _currentCamera;
        private Vector3 _initialLocalScale;
        private bool _hasInitialScale;

        private void Awake()
        {
            CacheInitialScale();
        }

        private void OnEnable()
        {
            CacheInitialScale();
            transform.localScale = _initialLocalScale;

            // Get camera
            if (overrideCamera != null)
            {
                _currentCamera = overrideCamera;
            }
            else
            {
                _currentCamera = ResolveCameraFromCinemachine() ?? Camera.main;
            }

            // Try to get CanvasGroup for fade effect
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_currentCamera == null && debugLogs)
                Debug.LogWarning("[BillboardText] No camera found!", gameObject);
        }

        private void LateUpdate()
        {
            if (_currentCamera == null)
            {
                // Retry finding camera
                _currentCamera = ResolveCameraFromCinemachine() ?? Camera.main;
                if (_currentCamera == null)
                    return;
            }

            // Rotate to face camera
            RotateTowardCamera();

            // Update scale based on distance
            if (enableDistanceScale)
            {
                UpdateDistanceScale();
            }

            // Update fade based on distance
            if (enableDistanceFade && _canvasGroup != null)
            {
                UpdateDistanceFade();
            }
        }

        private void CacheInitialScale()
        {
            if (_hasInitialScale)
            {
                return;
            }

            _initialLocalScale = transform.localScale;
            _hasInitialScale = true;
        }

        private Camera ResolveCameraFromCinemachine()
        {
            var cinemachineCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            if (cinemachineCameras != null && cinemachineCameras.Length > 0)
            {
                var brains = FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None);
                if (brains != null)
                {
                    for (int i = 0; i < brains.Length; i++)
                    {
                        var brain = brains[i];
                        if (brain == null)
                        {
                            continue;
                        }

                        var brainCamera = brain.GetComponent<Camera>();
                        if (brainCamera != null)
                        {
                            return brainCamera;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Rotates the canvas to face the camera.
        /// </summary>
        private void RotateTowardCamera()
        {
            Vector3 directionToCamera = _currentCamera.transform.position - transform.position;

            if (directionToCamera.sqrMagnitude < 0.001f)
                return; // Too close to camera

            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            if (flipFacing)
            {
                // Some world-space canvases have reversed front faces.
                targetRotation *= Quaternion.Euler(0f, 180f, 0f);
            }
            transform.rotation = targetRotation;
        }

        /// <summary>
        /// Updates the canvas transparency based on distance from camera.
        /// </summary>
        private void UpdateDistanceFade()
        {
            float distance = Vector3.Distance(transform.position, _currentCamera.transform.position);

            if (debugLogs)
                Debug.Log($"[BillboardText] Distance to camera: {distance}", gameObject);

            if (distance < fadeStartDistance)
            {
                _canvasGroup.alpha = 1f;
            }
            else if (distance > fadeEndDistance)
            {
                _canvasGroup.alpha = 0f;
            }
            else
            {
                // Smooth fade between start and end distance
                float fadeRange = fadeEndDistance - fadeStartDistance;
                _canvasGroup.alpha = Mathf.Clamp01(1f - (distance - fadeStartDistance) / fadeRange);
            }
        }

        private void UpdateDistanceScale()
        {
            float distance = Vector3.Distance(transform.position, _currentCamera.transform.position);

            float scaleMultiplier;
            if (distance <= scaleStartDistance)
            {
                scaleMultiplier = minScaleMultiplier;
            }
            else if (distance >= scaleEndDistance)
            {
                scaleMultiplier = maxScaleMultiplier;
            }
            else
            {
                float range = Mathf.Max(0.0001f, scaleEndDistance - scaleStartDistance);
                float t = (distance - scaleStartDistance) / range;
                scaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);
            }

            transform.localScale = _initialLocalScale * scaleMultiplier;

            if (debugLogs)
                Debug.Log($"[BillboardText] Distance scale multiplier: {scaleMultiplier}", gameObject);
        }

        /// <summary>
        /// Gets the current camera being used for billboarding.
        /// </summary>
        public Camera GetCurrentCamera() => _currentCamera;

        /// <summary>
        /// Sets a custom camera to use for billboarding.
        /// </summary>
        public void SetOverrideCamera(Camera camera)
        {
            overrideCamera = camera;
            _currentCamera = camera;
        }
    }
}
