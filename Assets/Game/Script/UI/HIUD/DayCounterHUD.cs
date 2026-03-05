using UnityEngine;
using TMPro;
using Game.Core.DI;
using Game.Core.Events;
using Game.Environment.DayNight;

namespace Game.UI
{
    /// <summary>
    /// Always-visible corner HUD element that displays the current in-game day number.
    /// Place on a TextMeshProUGUI GameObject in a Screen Space - Overlay canvas.
    /// Automatically updates whenever a new day begins.
    /// </summary>
    public class DayCounterHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI dayText;

        [Header("Format")]
        [SerializeField] private string displayFormat = "Day {0}";

        private IDayNightCycleService _dayNightService;
        private IEventBus _eventBus;

        private void Start()
        {
            _dayNightService = ServiceContainer.Instance.TryGet<IDayNightCycleService>();
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();

            if (_dayNightService == null)
            {
                Debug.LogWarning("[DayCounterHUD] IDayNightCycleService not found in ServiceContainer. " +
                                 "Ensure DayNightCycleManager registers itself before this HUD's Start().");
                return;
            }

            if (_eventBus != null)
            {
                _eventBus.Subscribe<DayCompletedEvent>(OnDayCompleted);
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<DayCompletedEvent>(OnDayCompleted);
        }

        private void OnDayCompleted(DayCompletedEvent evt)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (dayText == null || _dayNightService == null) return;
            dayText.text = string.Format(displayFormat, _dayNightService.CurrentDay);
        }
    }
}
