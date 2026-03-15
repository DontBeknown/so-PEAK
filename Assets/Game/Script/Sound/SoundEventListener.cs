using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

namespace Game.Sound
{
    public class SoundEventListener : MonoBehaviour
    {
        private IEventBus _eventBus;
        private SoundService _sound;

        private void Start()
        {
            _eventBus = ServiceContainer.Instance?.Get<IEventBus>();
            if(_eventBus == null)
            {
                _eventBus = new EventBus();
            }
            _sound    = ServiceContainer.Instance?.Get<SoundService>();

            _eventBus.Subscribe<PlayPositionalSFXEvent>(OnPlayPositionalSFX);
            _eventBus.Subscribe<PlayUISoundEvent>(OnPlayUISound);
            _eventBus.Subscribe<PlayMusicEvent>(OnPlayMusic);
            _eventBus.Subscribe<StopMusicEvent>(OnStopMusic);
            _eventBus.Subscribe<PlayAmbientEvent>(OnPlayAmbient);
            _eventBus.Subscribe<StopAmbientEvent>(OnStopAmbient);
            _eventBus.Subscribe<SetVolumeEvent>(OnSetVolume);
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<PlayPositionalSFXEvent>(OnPlayPositionalSFX);
            _eventBus.Unsubscribe<PlayUISoundEvent>(OnPlayUISound);
            _eventBus.Unsubscribe<PlayMusicEvent>(OnPlayMusic);
            _eventBus.Unsubscribe<StopMusicEvent>(OnStopMusic);
            _eventBus.Unsubscribe<PlayAmbientEvent>(OnPlayAmbient);
            _eventBus.Unsubscribe<StopAmbientEvent>(OnStopAmbient);
            _eventBus.Unsubscribe<SetVolumeEvent>(OnSetVolume);
        }

        private void OnPlayPositionalSFX(PlayPositionalSFXEvent e) =>
            _sound.PlayPositionalSFX(e.ClipId, e.Position, e.VolumeScale);

        private void OnPlayUISound(PlayUISoundEvent e) =>
            _sound.PlayUISound(e.ClipId, e.VolumeScale);

        private void OnPlayMusic(PlayMusicEvent e) =>
            _sound.PlayMusic(e.ClipId, e.Loop);

        private void OnStopMusic(StopMusicEvent _) =>
            _sound.StopMusic();

        private void OnPlayAmbient(PlayAmbientEvent e) =>
            _sound.PlayAmbient(e.ClipId);

        private void OnStopAmbient(StopAmbientEvent _) =>
            _sound.StopAmbient();

        private void OnSetVolume(SetVolumeEvent e) =>
            _sound.SetVolume(e.Category, e.NormalizedVolume);
    }
}
