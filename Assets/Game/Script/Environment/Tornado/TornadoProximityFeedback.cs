using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Environment.Tornado
{
    /// <summary>
    /// Drives tornado proximity feedback on the player using a post-process volume and optional UI CanvasGroup.
    /// TornadoPlayerPull updates this component with the current player distance to the tornado.
    /// </summary>
    public class TornadoProximityFeedback : MonoBehaviour
    {
        [Header("Volume Profile")]
        [Tooltip("Post-process profile used when the player is close to the tornado.")]
        [SerializeField] private VolumeProfile tornadoVolumeProfile;
        [SerializeField] private int volumePriority = 1026;

        [Header("Distance Settings")]
        [Tooltip("Distance at which the tornado effect is fully faded out.")]
        [SerializeField, Min(0.01f)] private float maxEffectDistance = 15f;
        [Tooltip("Distance at which the tornado effect is at full strength.")]
        [SerializeField, Min(0.01f)] private float fullEffectDistance = 2f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float fadeInSpeed = 2.5f;
        [SerializeField, Min(0f)] private float fadeOutSpeed = 3.5f;

        [Header("UI Feedback")]
        [Tooltip("UI CanvasGroup whose alpha follows the tornado effect strength.")]
        [SerializeField] private CanvasGroup tornadoFeedbackCanvasGroup;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        private GameObject volumeObject;
        private Volume volume;
        private float targetWeight;
        private float currentWeight;
        private float frameTargetWeight;
        private int frameTargetWeightFrame = -1;

        private void Awake()
        {
            CreateVolume();
        }

        private void Update()
        {
            float speed = targetWeight > currentWeight ? fadeInSpeed : fadeOutSpeed;
            if (speed <= 0f)
            {
                currentWeight = targetWeight;
            }
            else
            {
                currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, speed * Time.deltaTime);
            }

            ApplyFeedback(currentWeight);
        }

        private void OnDestroy()
        {
            if (volumeObject != null)
            {
                Destroy(volumeObject);
            }
        }

        public void SetTornadoDistance(float distance)
        {
            float target = CalculateWeight(distance);

            if (frameTargetWeightFrame != Time.frameCount)
            {
                frameTargetWeightFrame = Time.frameCount;
                frameTargetWeight = target;
            }
            else
            {
                frameTargetWeight = Mathf.Max(frameTargetWeight, target);
            }

            targetWeight = frameTargetWeight;

            if (enableDebugLogs)
            {
                Debug.Log($"{name}: tornado distance={distance:F2} effect={target:F2}", this);
            }
        }

        public void ClearTornadoDistance()
        {
            targetWeight = 0f;
            frameTargetWeight = 0f;
            frameTargetWeightFrame = -1;
        }

        private float CalculateWeight(float distance)
        {
            if (maxEffectDistance <= fullEffectDistance)
            {
                return distance <= fullEffectDistance ? 1f : 0f;
            }

            float clampedDistance = Mathf.Clamp(distance, 0f, maxEffectDistance);
            return Mathf.Clamp01(Mathf.InverseLerp(maxEffectDistance, fullEffectDistance, clampedDistance));
        }

        private void CreateVolume()
        {
            if (tornadoVolumeProfile == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"{name}: Tornado volume profile is not assigned.", this);
                }

                return;
            }

            volumeObject = new GameObject("TornadoProximityVolume");
            volumeObject.transform.SetParent(transform, false);

            volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = volumePriority;
            volume.profile = tornadoVolumeProfile;
            volume.weight = 0f;
        }

        private void ApplyFeedback(float weight)
        {
            if (volume != null)
            {
                volume.weight = weight;
            }

            if (tornadoFeedbackCanvasGroup != null)
            {
                tornadoFeedbackCanvasGroup.alpha = weight;
            }
        }
    }
}