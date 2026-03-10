using UnityEngine;
using Game.Core.DI;
using Game.Environment.DayNight;
using Game.Core.Events;

public class WatchTower : MonoBehaviour
{
    [SerializeField] private GameObject nightlight;
    private DayNightCycleManager _dayNightCycleManager;
    private IEventBus _eventBus;
    void Start()
    {
        _dayNightCycleManager = ServiceContainer.Instance.TryGet<DayNightCycleManager>();
        _eventBus = ServiceContainer.Instance.Get<IEventBus>();
        _eventBus.Subscribe<TimeOfDayChangedEvent>(OnDayNightCycleChanged);
    }
    private void OnDayNightCycleChanged(TimeOfDayChangedEvent e )
    {
        if(e.newTimeOfDay is TimeOfDay.Night)
        {
            nightlight.SetActive(true); 
        }
        else
        {
            nightlight.SetActive(false); 
        }
    }
}
