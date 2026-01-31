using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.Interaction.UI
{
    /// <summary>
    /// Billboard UI marker that floats above interactable objects.
    /// Changes color based on whether it's the nearest/selected interactable.
    /// Always faces the camera for clear visibility.
    /// </summary>
    public class InteractableUIMarker : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image markerImage;
        [SerializeField] private Canvas canvas;
        
        [Header("Colors")]
        [SerializeField] private Color inRangeColor = new Color(1f, 1f, 0f, 0.7f); // Yellow
        [SerializeField] private Color selectedColor = new Color(0f, 1f, 0f, 1f); // Green
        [SerializeField] private Color depletedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grey
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeSpeed = 0.3f;
        [SerializeField] private float scaleNormal = 1f;
        [SerializeField] private float scaleSelected = 1.3f;
        [SerializeField] private float scaleDuration = 0.2f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;
        
        [Header("Float Animation")]
        [SerializeField] private bool enableFloating = true;
        [SerializeField] private float floatAmplitude = 0.2f;
        [SerializeField] private float floatSpeed = 2f;
        
        [Header("Billboard Settings")]
        [SerializeField] private bool billboardToCamera = true;
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
        
        private Camera mainCamera;
        private Transform targetTransform;
        private bool isVisible = false;
        private bool isSelected = false;
        private Vector3 initialLocalPosition;
        private Tweener scaleTween;
        private Tweener fadeTween;
        private MarkerState currentState = MarkerState.Hidden;

        private enum MarkerState
        {
            Hidden,
            InRange,
            Selected,
            Depleted
        }

        private void Awake()
        {
            // Auto-setup canvas
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = gameObject.AddComponent<Canvas>();
                }
            }
            
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Auto-find marker image
            if (markerImage == null)
            {
                markerImage = GetComponentInChildren<Image>();
            }
            
            // Store initial position for floating animation
            initialLocalPosition = transform.localPosition;
            
            // Start hidden
            if (markerImage != null)
            {
                markerImage.color = new Color(inRangeColor.r, inRangeColor.g, inRangeColor.b, 0f);
            }
            
            isVisible = false;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[InteractableUIMarker] Main camera not found! Billboard effect will not work.");
            }
        }

        private void LateUpdate()
        {
            // Billboard effect - always face camera
            if (billboardToCamera && mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
            
            // Follow target with offset
            if (targetTransform != null)
            {
                transform.position = targetTransform.position + offset;
            }
            
            // Floating animation
            if (enableFloating && isVisible)
            {
                float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                transform.localPosition = initialLocalPosition + Vector3.up * floatOffset;
            }
        }

        /// <summary>
        /// Initialize the marker with a target transform
        /// </summary>
        public void Initialize(Transform target)
        {
            targetTransform = target;
            transform.position = target.position + offset;
            initialLocalPosition = transform.localPosition;
        }

        /// <summary>
        /// Show the marker in "in range" state (yellow)
        /// </summary>
        public void ShowInRange()
        {
            if (currentState == MarkerState.InRange) return;
            
            currentState = MarkerState.InRange;
            isVisible = true;
            isSelected = false;
            
            AnimateToState(inRangeColor, scaleNormal);
        }

        /// <summary>
        /// Show the marker in "selected" state (green, larger)
        /// </summary>
        public void ShowSelected()
        {
            if (currentState == MarkerState.Selected) return;
            
            currentState = MarkerState.Selected;
            isVisible = true;
            isSelected = true;
            
            AnimateToState(selectedColor, scaleSelected);
        }

        /// <summary>
        /// Show the marker in "depleted" state (grey, dimmed)
        /// </summary>
        public void ShowDepleted()
        {
            if (currentState == MarkerState.Depleted) return;
            
            currentState = MarkerState.Depleted;
            isVisible = true;
            isSelected = false;
            
            AnimateToState(depletedColor, scaleNormal * 0.8f);
        }

        /// <summary>
        /// Hide the marker completely
        /// </summary>
        public void Hide()
        {
            if (currentState == MarkerState.Hidden && !isVisible) return;
            
            currentState = MarkerState.Hidden;
            isVisible = false;
            isSelected = false;
            
            // Kill existing tweens
            scaleTween?.Kill();
            fadeTween?.Kill();
            
            // Fade out
            if (markerImage != null)
            {
                fadeTween = markerImage.DOFade(0f, fadeSpeed)
                    .SetEase(Ease.OutQuad);
            }
        }

        private void AnimateToState(Color targetColor, float targetScale)
        {
            if (markerImage == null) return;
            
            // Kill existing tweens
            scaleTween?.Kill();
            fadeTween?.Kill();
            
            // Fade to target color
            fadeTween = markerImage.DOColor(targetColor, fadeSpeed)
                .SetEase(Ease.OutQuad);
            
            // Scale animation
            scaleTween = transform.DOScale(Vector3.one * targetScale, scaleDuration)
                .SetEase(scaleEase);
        }

        /// <summary>
        /// Set marker offset from target
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        /// Enable or disable floating animation
        /// </summary>
        public void SetFloating(bool enabled)
        {
            enableFloating = enabled;
            if (!enabled)
            {
                transform.localPosition = initialLocalPosition;
            }
        }

        private void OnDestroy()
        {
            // Cleanup tweens
            scaleTween?.Kill();
            fadeTween?.Kill();
        }

        #region Static Factory Method

        /// <summary>
        /// Creates a UI marker for an interactable object
        /// </summary>
        public static InteractableUIMarker CreateMarker(Transform target, Sprite markerSprite = null)
        {
            // Create canvas object
            GameObject markerObj = new GameObject("InteractableMarker");
            markerObj.transform.SetParent(target);
            markerObj.transform.localPosition = Vector3.up * 2f;
            
            // Add marker component
            InteractableUIMarker marker = markerObj.AddComponent<InteractableUIMarker>();
            
            // Setup canvas
            Canvas canvas = markerObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            
            // Create image
            GameObject imageObj = new GameObject("MarkerImage");
            imageObj.transform.SetParent(markerObj.transform);
            imageObj.transform.localPosition = Vector3.zero;
            imageObj.transform.localRotation = Quaternion.identity;
            imageObj.transform.localScale = Vector3.one;
            
            Image image = imageObj.AddComponent<Image>();
            image.sprite = markerSprite;
            image.raycastTarget = false;
            
            RectTransform rect = imageObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);
            
            marker.markerImage = image;
            marker.canvas = canvas;
            marker.Initialize(target);
            
            return marker;
        }

        #endregion
    }
}
