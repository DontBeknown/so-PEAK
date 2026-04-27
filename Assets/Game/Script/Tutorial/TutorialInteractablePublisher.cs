using Game.Core.DI;
using Game.Core.Events;
using UnityEngine;

namespace Game.Tutorial
{
    public enum TutorialInteractableType
    {
        WaterSource,
        AssessmentTerminal,
        Campfire,
        Lighthouse
    }

    /// <summary>
    /// Attach to any HoldInteractableBase GameObject to publish a dedicated tutorial
    /// completion event when that specific object finishes its hold interaction.
    /// TutorialManager listens to these events for precise step completion matching.
    /// </summary>
    public class TutorialInteractablePublisher : MonoBehaviour
    {
        [SerializeField] private TutorialInteractableType interactableType;

        private IEventBus _eventBus;

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Subscribe<HoldInteractCompletedEvent>(OnHoldCompleted);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<HoldInteractCompletedEvent>(OnHoldCompleted);
        }

        private void OnHoldCompleted(HoldInteractCompletedEvent evt)
        {
            if (evt.Source != gameObject) return;

            switch (interactableType)
            {
                case TutorialInteractableType.WaterSource:
                    _eventBus.Publish(new CanteenRefilledTutorialEvent());
                    break;
                case TutorialInteractableType.AssessmentTerminal:
                    _eventBus.Publish(new AssessmentTerminalUsedTutorialEvent());
                    break;
                case TutorialInteractableType.Campfire:
                    _eventBus.Publish(new CampfireUsedTutorialEvent());
                    break;
                case TutorialInteractableType.Lighthouse:
                    _eventBus.Publish(new LighthouseUsedTutorialEvent());
                    break;
            }
        }
    }
}
