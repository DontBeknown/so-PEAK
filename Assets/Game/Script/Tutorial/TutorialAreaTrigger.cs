using Game.Core.DI;
using Game.Core.Events;
using UnityEngine;
using Game.Player;

namespace Game.Tutorial
{
    /// <summary>
    /// Detects when the player enters a tutorial area via trigger collider.
    /// Can optionally publish events or complete tutorial steps.
    /// Attach to an empty GameObject with a Trigger Collider (Box, Sphere, or Capsule).
    /// </summary>
    public class TutorialAreaTrigger : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private string areaName = "Tutorial Area";
        [SerializeField] private bool oneTimeOnly = true;
        [SerializeField] private bool debugLogs = true;

        [Header("Actions on Entry")]
        [SerializeField] private bool completeCurrentStep = false;
        [Tooltip("If true, completes whatever step is currently active in the tutorial.")]
        [SerializeField] private bool publishCustomEvent = false;
        [SerializeField] private TutorialAreaEnteredEvent.AreaType areaType = TutorialAreaEnteredEvent.AreaType.MovementZone;

        private bool _triggered;
        private IEventBus _eventBus;

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (oneTimeOnly && _triggered) return;

            var player = other.GetComponentInParent<PlayerControllerRefactored>();
            if (player == null) return;

            _triggered = true;

            if (debugLogs)
                Debug.Log($"[TutorialAreaTrigger] Player entered: {areaName}");

            // Complete the current tutorial step
            if (completeCurrentStep)
            {
                var tutorialManager = ServiceContainer.Instance.TryGet<ITutorialManager>();
                if (tutorialManager != null && tutorialManager.IsActive)
                {
                    if (debugLogs)
                        Debug.Log($"[TutorialAreaTrigger] Completing current step for area: {areaName}");
                    tutorialManager.CompleteCurrentStep();
                }
            }

            // Publish a custom event for area-specific logic
            if (publishCustomEvent)
            {
                _eventBus?.Publish(new TutorialAreaEnteredEvent(areaName, areaType));
            }

            if (oneTimeOnly)
                enabled = false;
        }
    }

    /// <summary>
    /// Event published when player enters a tutorial area.
    /// </summary>
    public class TutorialAreaEnteredEvent
    {
        public enum AreaType { MovementZone, FoodZone, WaterZone, RestZone, FinalZone }
        
        public string AreaName { get; }
        public AreaType Type { get; }

        public TutorialAreaEnteredEvent(string areaName, AreaType type)
        {
            AreaName = areaName;
            Type = type;
        }
    }
}
