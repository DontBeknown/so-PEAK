using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Game.Interaction.UI
{
    /// <summary>
    /// World-space progress bar UI for gathering interactions.
    /// Shows gathering progress from 0-100% with item name, icon, and cancel hint.
    /// </summary>
    public class GatheringProgressUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI progressPercentText;
        [SerializeField] private TextMeshProUGUI hintText;
        
        [Header("Settings")]
        [SerializeField] private Color progressColor = new Color(0.3f, 0.8f, 0.3f); // Green
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
        [SerializeField] private float smoothSpeed = 10f;
        
        private Camera mainCamera;
        private Transform targetTransform;
        private float currentProgress = 0f;
        private Tweener fadeInTween;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            // Setup canvas
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = gameObject.AddComponent<Canvas>();
                }
            }
            
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200; // Above markers
            
            // Add canvas group for fading
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Start invisible
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[GatheringProgressUI] Main camera not found!");
            }
            
            // Setup colors
            if (progressBarFill != null)
                progressBarFill.color = progressColor;
            
            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;
            
            // Set hint text
            if (hintText != null)
                hintText.text = "(Hold E)";
        }

        private void LateUpdate()
        {
            // Always face camera (billboard)
            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
            
            // Follow target
            if (targetTransform != null)
            {
                transform.position = targetTransform.position + offset;
            }
        }

        /// <summary>
        /// Initialize and show the progress bar
        /// </summary>
        public void Show(Transform target, string itemName, Sprite itemIcon = null)
        {
            targetTransform = target;
            transform.position = target.position + offset;
            
            // Set item info
            if (itemNameText != null)
                itemNameText.text = $"Gathering {itemName}...";
            
            if (itemIconImage != null && itemIcon != null)
            {
                itemIconImage.sprite = itemIcon;
                itemIconImage.enabled = true;
            }
            else if (itemIconImage != null)
            {
                itemIconImage.enabled = false;
            }
            
            // Reset progress
            currentProgress = 0f;
            UpdateProgressBar(0f);
            
            // Show with fade in
            gameObject.SetActive(true);
            
            fadeInTween?.Kill();
            fadeInTween = canvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Update the progress bar fill amount (0-1)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            currentProgress = Mathf.Clamp01(progress);
            UpdateProgressBar(currentProgress);
        }

        /// <summary>
        /// Hide the progress bar
        /// </summary>
        public void Hide()
        {
            fadeInTween?.Kill();
            fadeInTween = canvasGroup.DOFade(0f, 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void UpdateProgressBar(float progress)
        {
            // Update fill amount
            if (progressBarFill != null)
            {
                // Smooth the progress
                float currentFillAmount = progressBarFill.fillAmount;
                progressBarFill.fillAmount = Mathf.Lerp(currentFillAmount, progress, Time.deltaTime * smoothSpeed);
            }
            
            // Update percentage text
            if (progressPercentText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                progressPercentText.text = $"{percentage}%";
            }
        }

        /// <summary>
        /// Set custom offset from target
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        private void OnDestroy()
        {
            fadeInTween?.Kill();
        }

        #region Static Factory Method

        /// <summary>
        /// Creates a gathering progress UI instance
        /// </summary>
        public static GatheringProgressUI CreateProgressBar(Transform target, GameObject prefab = null)
        {
            GameObject progressObj;
            
            if (prefab != null)
            {
                // Use provided prefab
                progressObj = Instantiate(prefab, target.position + Vector3.up * 2.5f, Quaternion.identity);
            }
            else
            {
                // Create basic version programmatically
                progressObj = CreateBasicProgressBar(target);
            }
            
            GatheringProgressUI progressUI = progressObj.GetComponent<GatheringProgressUI>();
            if (progressUI == null)
            {
                progressUI = progressObj.AddComponent<GatheringProgressUI>();
            }
            
            progressUI.targetTransform = target;
            
            return progressUI;
        }

        private static GameObject CreateBasicProgressBar(Transform target)
        {
            // Create main object
            GameObject mainObj = new GameObject("GatheringProgressBar");
            mainObj.transform.position = target.position + Vector3.up * 2.5f;
            
            // Add canvas
            Canvas canvas = mainObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;
            
            CanvasScaler scaler = mainObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            // Create background panel
            GameObject bgPanel = new GameObject("Background");
            bgPanel.transform.SetParent(mainObj.transform);
            bgPanel.transform.localPosition = Vector3.zero;
            bgPanel.transform.localRotation = Quaternion.identity;
            bgPanel.transform.localScale = Vector3.one;
            
            Image bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(300, 80);
            
            // Create progress bar background
            GameObject progressBg = new GameObject("ProgressBackground");
            progressBg.transform.SetParent(bgPanel.transform);
            progressBg.transform.localPosition = new Vector3(0, -10, 0);
            progressBg.transform.localRotation = Quaternion.identity;
            progressBg.transform.localScale = Vector3.one;
            
            Image progressBgImage = progressBg.AddComponent<Image>();
            progressBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            RectTransform progressBgRect = progressBg.GetComponent<RectTransform>();
            progressBgRect.sizeDelta = new Vector2(260, 20);
            
            // Create progress bar fill
            GameObject progressFill = new GameObject("ProgressFill");
            progressFill.transform.SetParent(progressBg.transform);
            progressFill.transform.localPosition = Vector3.zero;
            progressFill.transform.localRotation = Quaternion.identity;
            progressFill.transform.localScale = Vector3.one;
            
            Image fillImage = progressFill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.8f, 0.3f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            
            RectTransform fillRect = progressFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            // Create text label
            GameObject textObj = new GameObject("ItemNameText");
            textObj.transform.SetParent(bgPanel.transform);
            textObj.transform.localPosition = new Vector3(0, 20, 0);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Gathering...";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(280, 30);
            
            // Create hint text
            GameObject hintObj = new GameObject("HintText");
            hintObj.transform.SetParent(bgPanel.transform);
            hintObj.transform.localPosition = new Vector3(0, -30, 0);
            hintObj.transform.localRotation = Quaternion.identity;
            hintObj.transform.localScale = Vector3.one;
            
            TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
            hintText.text = "(Hold E)";
            hintText.fontSize = 14;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(280, 20);
            
            // Setup the component references
            GatheringProgressUI progressUI = mainObj.AddComponent<GatheringProgressUI>();
            progressUI.canvas = canvas;
            progressUI.progressBarFill = fillImage;
            progressUI.backgroundImage = bgImage;
            progressUI.itemNameText = text;
            progressUI.hintText = hintText;
            
            return mainObj;
        }

        #endregion
    }
}
