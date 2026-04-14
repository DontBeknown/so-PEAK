using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Environment.Landslide
{
    [DisallowMultipleComponent]
    public class LandslideShakeController : MonoBehaviour
    {
        private CinemachineBasicMultiChannelPerlin[] _perlinComponents;
        private Tween _shakeTween;
        private float _currentShakeAmplitude;

        public void CachePerlinComponents()
        {
            CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            List<CinemachineBasicMultiChannelPerlin> found = new List<CinemachineBasicMultiChannelPerlin>();
            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineBasicMultiChannelPerlin perlin = cameras[i].GetComponent<CinemachineBasicMultiChannelPerlin>();
                if (perlin != null)
                {
                    found.Add(perlin);
                }
            }

            _perlinComponents = found.ToArray();
        }

        public void TransitionShake(float targetAmplitude, float duration)
        {
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
