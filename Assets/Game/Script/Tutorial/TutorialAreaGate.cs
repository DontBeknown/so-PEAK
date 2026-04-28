using Game.Core.DI;
using Game.Core.Events;
using Game.Interaction;
using UnityEngine;

namespace Game.Tutorial
{
    /// <summary>
    /// Visible barrier that opens when a specific tutorial step is completed.
    /// Attach to a fence/gate prop and assign the barrier collider.
    /// </summary>
    public class TutorialAreaGate : MonoBehaviour
    {
        [Header("Gate Config")]
        [Tooltip("This gate opens when this step index is completed.")]
        [SerializeField] private int stepIndexToUnlock = 0;

        [Header("References")]
        [SerializeField] private Collider barrierCollider;
        [Tooltip("Root object whose direct children are destroyed in random order when unlocked.")]
        [SerializeField] private GameObject visualRoot;

        [Header("Destroy Visuals")]
        [SerializeField] private float delayBetweenVisualDestroy = 0.05f;

        private IEventBus _eventBus;
        private bool _isOpened;

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            if (_eventBus == null)
            {
                Debug.LogError("[TutorialAreaGate] IEventBus not found. Gate will not respond to tutorial steps.");
                return;
            }

            _eventBus.Subscribe<TutorialStepCompletedEvent>(OnStepCompleted);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<TutorialStepCompletedEvent>(OnStepCompleted);
        }

        private void OnStepCompleted(TutorialStepCompletedEvent evt)
        {
            if (evt.StepIndex != stepIndexToUnlock || _isOpened) return;

            _isOpened = true;

            if (barrierCollider != null)
                barrierCollider.enabled = false;

            PlayVisualDestroySequence();
        }

        private void PlayVisualDestroySequence()
        {
            if (visualRoot == null)
            {
                Debug.LogWarning("[TutorialAreaGate] visualRoot is not assigned. Skipping visual destroy sequence.");
                return;
            }

            RandomScaleDownDestroySequence sequence = GetComponent<RandomScaleDownDestroySequence>();
            if (sequence == null)
                sequence = gameObject.AddComponent<RandomScaleDownDestroySequence>();

            sequence.SetDelay(delayBetweenVisualDestroy);
            sequence.CollectFromParentDirectChildren(visualRoot.transform, clearExisting: true, includeParentWhenNoChildren: true);
            sequence.Play();
        }
    }
}
