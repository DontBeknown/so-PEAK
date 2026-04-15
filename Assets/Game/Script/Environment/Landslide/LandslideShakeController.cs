using System.Collections.Generic;
using DG.Tweening;
using Game.Core.DI;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Environment.Landslide
{
    [DisallowMultipleComponent]
    public class LandslideShakeController : MonoBehaviour
    {
        private CinemachinePlayerCamera _playerCamera;
        private CinemachineBasicMultiChannelPerlin[] _perlinComponents;
        private Tween _shakeTween;
        private float _currentShakeAmplitude;

        public void CachePerlinComponents()
        {
            ResolvePerlinComponents(forceRefresh: true);
        }

        public void TransitionShake(float targetAmplitude, float duration)
        {
            ResolvePerlinComponents(forceRefresh: true);
            if (_perlinComponents == null || _perlinComponents.Length == 0)
            {
                return;
            }

            _shakeTween?.Kill();

            float target = Mathf.Max(0f, targetAmplitude);
            if (duration <= 0f)
            {
                _currentShakeAmplitude = target;
                SetAllShakeAmplitudes(target);
                return;
            }

            _shakeTween = DOTween.To(
                () => _currentShakeAmplitude,
                value =>
                {
                    _currentShakeAmplitude = value;
                    SetAllShakeAmplitudes(value);
                },
                target,
                duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        public void StopAndReset()
        {
            _shakeTween?.Kill();
            _shakeTween = null;
            _currentShakeAmplitude = 0f;
            SetAllShakeAmplitudes(0f);
        }

        private void ResolvePerlinComponents(bool forceRefresh)
        {
            _playerCamera ??= ServiceContainer.Instance?.TryGet<CinemachinePlayerCamera>();
            if (_playerCamera == null)
            {
                _perlinComponents = null;
                return;
            }

            CinemachineCamera[] cameras = _playerCamera.GetCinemachineCameras(forceRefresh);
            if (cameras == null || cameras.Length == 0)
            {
                _perlinComponents = null;
                return;
            }

            List<CinemachineBasicMultiChannelPerlin> found = new List<CinemachineBasicMultiChannelPerlin>();
            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineCamera camera = cameras[i];
                if (camera == null)
                {
                    continue;
                }

                CinemachineBasicMultiChannelPerlin perlin = camera.GetComponent<CinemachineBasicMultiChannelPerlin>();
                if (perlin != null)
                {
                    found.Add(perlin);
                }
            }

            _perlinComponents = found.Count > 0 ? found.ToArray() : null;
        }

        private void SetAllShakeAmplitudes(float amplitude)
        {
            if (_perlinComponents == null)
            {
                return;
            }

            for (int i = 0; i < _perlinComponents.Length; i++)
            {
                CinemachineBasicMultiChannelPerlin perlin = _perlinComponents[i];
                if (perlin != null)
                {
                    perlin.AmplitudeGain = amplitude;
                }
            }
        }
    }
}
