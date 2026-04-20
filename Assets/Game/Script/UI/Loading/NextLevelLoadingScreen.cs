using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Reusable loading overlay for level transitions and async scene-loading phases.
    /// Supports fade-in/out, direct progress/status updates, and optional simulated stepped progress.
    /// </summary>
    public class NextLevelLoadingScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image loadingImage;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        [SerializeField] private Ease fadeInEase = Ease.OutQuad;
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;

        [Header("Next Level Simulated Sequence")]
        [SerializeField] private float stepDuration = 0.2f;

        private Tween _fadeTween;
        private Coroutine _simulateRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            KillRunningTransitions();
        }

        public void Show(string initialStatus = "Loading...", bool fade = true)
        {
            KillRunningTransitions();

            EnsureVisibleForTransition();
            SetStatus(initialStatus);

            if (fade && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                _fadeTween = canvasGroup.DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase)
                    .SetLink(gameObject)
                    .OnComplete(() => _fadeTween = null);
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public void Hide(Action onComplete = null, bool fade = true)
        {
            if (root == null || !root.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            if (canvasGroup == null || !fade)
            {
                HideImmediate();
                onComplete?.Invoke();
                return;
            }

            KillRunningTransitions();

            _fadeTween = canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(fadeOutEase)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    HideImmediate();
                    _fadeTween = null;
                    onComplete?.Invoke();
                });
        }

        public void HideImmediate()
        {
            KillRunningTransitions();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (root != null)
            {
                root.SetActive(false);
            }

            SetProgress(0f);
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(message) ? "Loading..." : message;
            }
        }

        public void SetProgress(float progress01)
        {
            float value = Mathf.Clamp01(progress01);

            if (progressSlider != null)
            {
                progressSlider.value = value;
            }
        }

        public void SetLoadingImageVisible(bool visible)
        {
            if (loadingImage != null)
            {
                loadingImage.enabled = visible;
            }
        }

        public void PlayNextLevelSequence(Action onComplete)
        {
            KillRunningTransitions();
            Show("Saving progress...", true);

            _simulateRoutine = StartCoroutine(PlayNextLevelSequenceRoutine(onComplete));
        }

        private IEnumerator PlayNextLevelSequenceRoutine(Action onComplete)
        {
            SetLoadingImageVisible(true);

            SetProgress(0.2f);
            SetStatus("Saving progress...");
            yield return new WaitForSeconds(stepDuration);

            SetProgress(0.55f);
            SetStatus("Preparing next level...");
            yield return new WaitForSeconds(stepDuration);

            SetProgress(0.85f);
            SetStatus("Loading next level...");
            yield return new WaitForSeconds(stepDuration);

            SetProgress(1f);
            SetStatus("Entering world...");
            yield return new WaitForSeconds(stepDuration * 0.75f);

            _simulateRoutine = null;
            onComplete?.Invoke();
        }

        private void EnsureVisibleForTransition()
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
            }

            SetProgress(0f);
            SetLoadingImageVisible(true);
        }

        private void KillRunningTransitions()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
            {
                _fadeTween.Kill();
                _fadeTween = null;
            }

            if (_simulateRoutine != null)
            {
                StopCoroutine(_simulateRoutine);
                _simulateRoutine = null;
            }
        }
    }
}
