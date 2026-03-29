using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Environment.DayNight;
public class WatchTower : MonoBehaviour
{
    [SerializeField] private GameObject nightlight;
    private IEventBus _eventBus;

    void Start()
    {
        _eventBus = ServiceContainer.Instance.Get<IEventBus>();
        _eventBus?.Subscribe<TimeOfDayChangedEvent>(OnDayNightCycleChanged);
    }

    private void OnDestroy()
    {
        _eventBus?.Unsubscribe<TimeOfDayChangedEvent>(OnDayNightCycleChanged);
    }

    private void OnDayNightCycleChanged(TimeOfDayChangedEvent e )
    {
        if (nightlight == null)
        {
            return;
        }

        if (e.newTimeOfDay is TimeOfDay.Night)
        {
            nightlight.SetActive(true);
        }
        else
        {
            nightlight.SetActive(false);
        }
    }
}
