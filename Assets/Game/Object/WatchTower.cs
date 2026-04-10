using UnityEngine;
using System.Collections;
using Game.Core.DI;
using Game.Core.Events;
using Game.Environment.DayNight;
public class WatchTower : MonoBehaviour
{
    [SerializeField] private GameObject nightlight;
    [SerializeField, Min(0.1f)] private float eventBusRetryInterval = 0.5f;
    [SerializeField] private bool enableRetryLogs = false;

    private IEventBus _eventBus;
    private Coroutine _subscribeRoutine;
    private bool _isSubscribed;

    void Start()
    {
        TrySubscribeToEventBus();

        if (!_isSubscribed)
        {
            _subscribeRoutine = StartCoroutine(RetrySubscribeToEventBus());
        }
    }

    private void OnDisable()
    {
        if (_subscribeRoutine != null)
        {
            StopCoroutine(_subscribeRoutine);
            _subscribeRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (_isSubscribed)
        {
            _eventBus?.Unsubscribe<TimeOfDayChangedEvent>(OnDayNightCycleChanged);
            _isSubscribed = false;
        }
    }

    private IEnumerator RetrySubscribeToEventBus()
    {
        while (!_isSubscribed)
        {
            TrySubscribeToEventBus();

            if (!_isSubscribed)
            {
                yield return new WaitForSeconds(eventBusRetryInterval);
            }
        }

        _subscribeRoutine = null;
    }

    private void TrySubscribeToEventBus()
    {
        if (_isSubscribed)
        {
            return;
        }

        _eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
        if (_eventBus == null)
        {
            if (enableRetryLogs)
            {
                Debug.Log("[WatchTower] EventBus not ready yet. Will retry subscription.");
            }

            return;
        }

        _eventBus.Subscribe<TimeOfDayChangedEvent>(OnDayNightCycleChanged);
        _isSubscribed = true;
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
