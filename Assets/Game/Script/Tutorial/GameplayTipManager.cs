using Game.Core.Events;
using UnityEngine;

namespace Game.Tutorial
{
    /// <summary>
    /// Detects first-time game events (hot temperature, landslide, tornado) and shows
    /// contextual tip slideshows via ShowGameplayTipEvent. Registers as a service.
    /// </summary>
    public class GameplayTipManager : MonoBehaviour
    {
        [Header("Tip Data")]
        [SerializeField] private GameplayTipData hotTemperatureTip;
        [SerializeField] private GameplayTipData landslideTip;
        [SerializeField] private GameplayTipData tornadoTip;

        [Header("Temperature Poll Interval (seconds)")]
        [SerializeField] private float temperaturePollInterval = 0.5f;

        private IEventBus _eventBus;
        private ISaveLoadService _saveLoadService;
        private PlayerStats _playerStats;

        private float _temperaturePollTimer;
        private bool _initialized;

        public void Initialize(IEventBus eventBus, ISaveLoadService saveLoadService, PlayerStats playerStats)
        {
            _eventBus        = eventBus;
            _saveLoadService = saveLoadService;
            _playerStats     = playerStats;
            _initialized     = true;
        }

        private void Start()
        {
            if (_eventBus == null)
                _eventBus = Game.Core.DI.ServiceContainer.Instance.TryGet<IEventBus>();

            _eventBus?.Subscribe<NaturalDisasterEvent>(OnNaturalDisaster);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<NaturalDisasterEvent>(OnNaturalDisaster);
        }

        private void Update()
        {
            if (!_initialized || _playerStats == null || hotTemperatureTip == null) return;

            _temperaturePollTimer += Time.deltaTime;
            if (_temperaturePollTimer < temperaturePollInterval) return;
            _temperaturePollTimer = 0f;

            if (_playerStats.TemperatureStat.IsOverheating)
                ShowTip(hotTemperatureTip);
        }

        private void OnNaturalDisaster(NaturalDisasterEvent evt)
        {
            switch (evt.Type)
            {
                case NaturalDisasterEvent.DisasterType.Landslide when landslideTip != null:
                    ShowTip(landslideTip);
                    break;
                case NaturalDisasterEvent.DisasterType.Tornado when tornadoTip != null:
                    ShowTip(tornadoTip);
                    break;
            }
        }

        private void ShowTip(GameplayTipData tip)
        {
            if (HasSeenTip(tip.tipId)) return;
            MarkTipSeen(tip.tipId);
            _eventBus?.Publish(new ShowGameplayTipEvent(tip));
        }

        private bool HasSeenTip(string id)
        {
            var worldState = _saveLoadService?.CurrentWorldSave?.worldState;
            return worldState?.seenGameplayTips?.Contains(id) ?? false;
        }

        private void MarkTipSeen(string id)
        {
            var worldState = _saveLoadService?.CurrentWorldSave?.worldState;
            if (worldState == null) return;
            worldState.seenGameplayTips ??= new System.Collections.Generic.List<string>();
            if (!worldState.seenGameplayTips.Contains(id))
                worldState.seenGameplayTips.Add(id);
        }
    }
}
