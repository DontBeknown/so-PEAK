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
        [SerializeField] private GameplayTipData mapViewerTip;

        [Header("Temperature Poll Interval (seconds)")]
        [SerializeField] private float temperaturePollInterval = 0.5f;

        [Header("Config")]
        [SerializeField] private PlayerConfig playerConfig;

        private IEventBus _eventBus;
        private ISaveLoadService _saveLoadService;
        private PlayerStats _playerStats;
        private readonly System.Collections.Generic.List<string> _seenGameplayTipsBackup = new();

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
            _eventBus?.Subscribe<PanelOpenedEvent>(OnPanelOpened);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<NaturalDisasterEvent>(OnNaturalDisaster);
            _eventBus?.Unsubscribe<PanelOpenedEvent>(OnPanelOpened);
        }

        private void Update()
        {
            if (!_initialized || _playerStats == null || hotTemperatureTip == null || playerConfig == null) return;

            _temperaturePollTimer += Time.deltaTime;
            if (_temperaturePollTimer < temperaturePollInterval) return;
            _temperaturePollTimer = 0f;

            if (_playerStats.TemperatureStat.Current >= playerConfig.tempHotThirstPenaltyThreshold)
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

        private void OnPanelOpened(PanelOpenedEvent evt)
        {
            if (mapViewerTip != null && evt.PanelName == "MapViewer")
                ShowTip(mapViewerTip);
        }

        public void ShowTip(GameplayTipData tip)
        {
            if (HasSeenTip(tip.tipId)) return;
            MarkTipSeen(tip.tipId);
            _eventBus?.Publish(new ShowGameplayTipEvent(tip));
        }

        private bool HasSeenTip(string id)
        {
            var worldState = _saveLoadService?.CurrentWorldSave?.worldState;
            return worldState?.seenGameplayTips?.Contains(id) ?? _seenGameplayTipsBackup.Contains(id);
        }

        private void MarkTipSeen(string id)
        {
            var worldState = _saveLoadService?.CurrentWorldSave?.worldState;
            if (!_seenGameplayTipsBackup.Contains(id))
                _seenGameplayTipsBackup.Add(id);

            if (worldState == null) return;

            worldState.seenGameplayTips ??= new System.Collections.Generic.List<string>();
            foreach (var tipId in _seenGameplayTipsBackup)
            {
                if (!worldState.seenGameplayTips.Contains(tipId))
                    worldState.seenGameplayTips.Add(tipId);
            }
        }
    }
}
