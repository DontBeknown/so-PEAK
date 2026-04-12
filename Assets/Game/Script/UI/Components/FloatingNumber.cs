using UnityEngine;
using TMPro;
using Game.Core.DI;

namespace Game.UI.Components
{
    /// <summary>
    /// Example component showing how to create floating numbers (damage, healing, etc.)
    /// that billboard toward the camera and fade out over time.
    /// 
    /// Usage:
    /// 1. Create a prefab with Canvas (WorldSpace) → TextMeshPro Text
    /// 2. Add BillboardText to the Canvas
    /// 3. Add FloatingNumber to the Canvas or as a parent
    /// 4. When spawning, call floatingNumber.Show(position, value, color)
    /// </summary>
    public class FloatingNumber : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI textDisplay;
        [SerializeField] private BillboardText billboardText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] private float floatDuration = 2f;
        [SerializeField] private float floatHeight = 2f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private Vector3 _startPosition;
        private float _elapsedTime;
        private bool _isActive;

        private void Start()
        {
            // Ensure children components are found if not assigned
            if (textDisplay == null)
                textDisplay = GetComponentInChildren<TextMeshProUGUI>();
            if (billboardText == null)
                billboardText = GetComponent<BillboardText>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Initially hidden
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsedTime += Time.deltaTime;
            float progress = _elapsedTime / floatDuration;

            if (progress >= 1f)
            {
                _isActive = false;
                gameObject.SetActive(false);
                return;
            }

            // Float upward
            Vector3 newPosition = _startPosition + Vector3.up * (floatHeight * progress);
            transform.position = newPosition;

            // Fade based on curve
            if (canvasGroup != null)
            {
                canvasGroup.alpha = fadeCurve.Evaluate(progress);
            }
        }

        /// <summary>
        /// Shows a floating number at the specified position.
        /// </summary>
        /// <param name="position">World position to spawn at</param>
        /// <param name="value">Number to display</param>
        /// <param name="color">Color of the text</param>
        public void Show(Vector3 position, float value, Color color)
        {
            _startPosition = position;
            transform.position = position;

            if (textDisplay != null)
            {
                textDisplay.text = value.ToString("F0");
                textDisplay.color = color;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            _elapsedTime = 0f;
            _isActive = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Shows a floating number with text.
        /// </summary>
        public void Show(Vector3 position, string text, Color color)
        {
            _startPosition = position;
            transform.position = position;

            if (textDisplay != null)
            {
                textDisplay.text = text;
                textDisplay.color = color;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            _elapsedTime = 0f;
            _isActive = true;
            gameObject.SetActive(true);
        }
    }
}
