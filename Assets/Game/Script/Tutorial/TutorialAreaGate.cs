using Game.Core.DI;
using Game.Core.Events;
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
        [SerializeField] private Animator animator;
        [Tooltip("Fallback: hide this object if no Animator is assigned.")]
        [SerializeField] private GameObject visualRoot;

        private IEventBus _eventBus;

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
            if (evt.StepIndex != stepIndexToUnlock) return;

            if (barrierCollider != null)
                barrierCollider.enabled = false;

            if (animator != null)
                animator.SetTrigger("Open");
            else if (visualRoot != null)
                visualRoot.SetActive(false);
        }
    }
}
