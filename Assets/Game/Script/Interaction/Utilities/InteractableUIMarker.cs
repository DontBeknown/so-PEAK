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
        [SerializeField] private RectTransform markerRectTransform;
        [SerializeField] private Canvas parentCanvas; // Screen-space canvas to attach to
        [SerializeField] private bool autoFindCanvas = true;
        
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
        
        [Header("Positioning")]
        [SerializeField] private Vector2 screenOffset = new Vector2(0, 50f); // Pixel offset from target position
        
        private Camera mainCamera;
        private Transform targetTransform;
        private bool isVisible = false;
        //private bool isSelected = false;
        private Vector2 initialLocalPosition;
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
            // Auto-find parent canvas
            if (parentCanvas == null && autoFindCanvas)
            {
                parentCanvas = GetComponentInParent<Canvas>();
                
                // If no parent canvas, find one in scene
                if (parentCanvas == null)
                {
                    parentCanvas = FindFirstObjectByType<Canvas>();
                }
                
                if (parentCanvas == null)
                {
                    Debug.LogError("[InteractableUIMarker] No Canvas found! Create a screen-space canvas first.");
                    return;
                }
            }
            
            // Ensure we're parented to the canvas
            if (parentCanvas != null && transform.parent != parentCanvas.transform)
            {
                transform.SetParent(parentCanvas.transform, false);
            }
            
            // Auto-find marker image and rect transform
            if (markerImage == null)
            {
                markerImage = GetComponent<Image>();
                if (markerImage == null)
                {
                    markerImage = GetComponentInChildren<Image>();
                }
            }
            
            if (markerRectTransform == null)
            {
                markerRectTransform = GetComponent<RectTransform>();
            }
            
            // Store initial position for floating animation
            if (markerRectTransform != null)
            {
                initialLocalPosition = markerRectTransform.anchoredPosition;
            }
            
            // Start hidden
            if (markerImage != null)
            {
                markerImage.color = new Color(inRangeColor.r, inRangeColor.g, inRangeColor.b, 0f);
            }
            
            isVisible = false;
        }

        private void Start()
        {
            // Try to find camera - works with Cinemachine or regular camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindAnyObjectByType<Camera>();
                
                if (mainCamera == null)
                {
                    Debug.LogWarning("[InteractableUIMarker] No camera found! Marker positioning will not work.");
                }
            }
        }

        private void LateUpdate()
        {
            // Update screen position to follow target
            if (targetTransform != null && mainCamera != null && markerRectTransform != null)
            {
                // Convert world position to screen point
                Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetTransform.position);
                
                // Check if target is in front of camera
                if (screenPoint.z > 0)
                {
                    // Convert screen point to canvas position
                    Vector2 canvasPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentCanvas.transform as RectTransform,
                        screenPoint,
                        parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                        out canvasPos
                    );
                    
                    // Apply offset and floating animation
                    Vector2 finalPos = canvasPos + screenOffset;
                    
                    if (enableFloating && isVisible)
                    {
                        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                        finalPos.y += floatOffset;
                    }
                    
                    markerRectTransform.anchoredPosition = finalPos;
                    
                    // Show marker (target is visible)
                    if (markerImage != null && markerImage.color.a == 0 && isVisible)
                    {
                        markerImage.color = new Color(markerImage.color.r, markerImage.color.g, markerImage.color.b, currentState == MarkerState.InRange ? inRangeColor.a : selectedColor.a);
                    }
                }
                else
                {
                    // Target is behind camera - hide marker
                    if (markerImage != null && isVisible)
                    {
                        markerImage.color = new Color(markerImage.color.r, markerImage.color.g, markerImage.color.b, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Initialize the marker with a target transform
        /// </summary>
        public void Initialize(Transform target)
        {
            targetTransform = target;
            
            if (markerRectTransform != null)
            {
                initialLocalPosition = markerRectTransform.anchoredPosition;
            }
        }

        /// <summary>
        /// Show the marker in "in range" state (yellow)
        /// </summary>
        public void ShowInRange()
        {
            if (currentState == MarkerState.InRange) return;
            
            currentState = MarkerState.InRange;
            isVisible = true;
            //isSelected = false;
            
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
            //isSelected = true;
            
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
            //isSelected = false;
            
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
            //isSelected = false;
            
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
        
        /// <summary>
        /// Hide the marker instantly without fade animation
        /// </summary>
        public void HideInstant()
        {
            if (currentState == MarkerState.Hidden && !isVisible) return;
            
            currentState = MarkerState.Hidden;
            isVisible = false;
            //isSelected = false;
            
            // Kill existing tweens
            scaleTween?.Kill();
            fadeTween?.Kill();
            
            // Instantly hide
            if (markerImage != null)
            {
                markerImage.color = new Color(markerImage.color.r, markerImage.color.g, markerImage.color.b, 0);
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
        /// Set marker screen offset in pixels
        /// </summary>
        public void SetOffset(Vector2 newOffset)
        {
            screenOffset = newOffset;
        }

        /// <summary>
        /// Enable or disable floating animation
        /// </summary>
        public void SetFloating(bool enabled)
        {
            enableFloating = enabled;
            if (!enabled && markerRectTransform != null)
            {
                markerRectTransform.anchoredPosition = initialLocalPosition;
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
        /// Creates a UI marker for an interactable object on screen-space canvas
        /// </summary>
        public static InteractableUIMarker CreateMarker(Transform target, Sprite markerSprite = null, Canvas canvas = null, 
            Color? inRangeColor = null, Color? selectedColor = null, Color? depletedColor = null)
        {
            // Find canvas if not provided
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[InteractableUIMarker] No Canvas found in scene! Cannot create marker.");
                    return null;
                }
            }
            
            // Create marker object on canvas
            GameObject markerObj = new GameObject("InteractableMarker_" + target.name);
            markerObj.transform.SetParent(canvas.transform, false);
            
            // Add RectTransform and configure
            RectTransform rectTransform = markerObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(25, 25);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            // Add Image component
            Image image = markerObj.AddComponent<Image>();
            image.sprite = markerSprite;
            image.raycastTarget = false;
            image.color = new Color(1, 1, 0, 0); // Start invisible
            
            // Add marker component
            InteractableUIMarker marker = markerObj.AddComponent<InteractableUIMarker>();
            marker.markerImage = image;
            marker.markerRectTransform = rectTransform;
            marker.parentCanvas = canvas;
            
            // Apply custom colors if provided
            if (inRangeColor.HasValue)
                marker.inRangeColor = inRangeColor.Value;
            if (selectedColor.HasValue)
                marker.selectedColor = selectedColor.Value;
            if (depletedColor.HasValue)
                marker.depletedColor = depletedColor.Value;
            
            marker.Initialize(target);
            
            return marker;
        }

        #endregion
    }
}
