using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Environment.Landslide
{
    [DisallowMultipleComponent]
    public class LandslideDecalService : MonoBehaviour
    {
        private struct ActiveDecalEntry
        {
            public GameObject Decal;
            public float FadeDuration;

            public ActiveDecalEntry(GameObject decal, float fadeDuration)
            {
                Decal = decal;
                FadeDuration = fadeDuration;
            }
        }

        private readonly Queue<ActiveDecalEntry> _activeDecals = new Queue<ActiveDecalEntry>();
        private readonly Dictionary<GameObject, Queue<GameObject>> _decalPoolByPrefab = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> _decalInstanceToPrefab = new Dictionary<GameObject, GameObject>();

        private Transform _poolParent;
        private int _maxDecalPoolSizePerPrefab = 120;
        private float _delayBetweenDecalCleanup = 0.15f;
        private bool _isFadingAllDecals;
        private Coroutine _decalCleanupRoutine;

        public bool IsCleanupIdle => !_isFadingAllDecals && _decalCleanupRoutine == null && _activeDecals.Count == 0;

        public void Configure(Transform poolParent, int maxDecalPoolSizePerPrefab, float delayBetweenDecalCleanup)
        {
            _poolParent = poolParent;
            _maxDecalPoolSizePerPrefab = Mathf.Max(1, maxDecalPoolSizePerPrefab);
            _delayBetweenDecalCleanup = Mathf.Max(0f, delayBetweenDecalCleanup);
        }

        public void PrewarmDecalPool(GameObject prefab, int prewarmCount)
        {
            if (prefab == null || prewarmCount <= 0)
            {
                return;
            }

            Queue<GameObject> queue = GetOrCreateDecalQueue(prefab);
            int toCreate = Mathf.Max(0, prewarmCount - queue.Count);
            for (int i = 0; i < toCreate; i++)
            {
                GameObject decal = Instantiate(prefab, _poolParent != null ? _poolParent : transform);
                decal.SetActive(false);
                _decalInstanceToPrefab[decal] = prefab;
                queue.Enqueue(decal);
            }
        }

        public void RegisterSpawnedDecal(GameObject decal, float fadeDuration)
        {
            if (decal == null)
            {
                return;
            }

            _activeDecals.Enqueue(new ActiveDecalEntry(decal, Mathf.Max(0.01f, fadeDuration)));
        }

        public GameObject RentDecal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            Queue<GameObject> queue = GetOrCreateDecalQueue(prefab);
            GameObject decal = null;

            while (queue.Count > 0 && decal == null)
            {
                decal = queue.Dequeue();
            }

            if (decal == null)
            {
                decal = Instantiate(prefab);
                _decalInstanceToPrefab[decal] = prefab;
            }

            PrepareDecalForUse(decal, position, rotation);
            return decal;
        }

        public void ReturnDecal(GameObject decal)
        {
            if (decal == null)
            {
                return;
            }

            if (!_decalInstanceToPrefab.TryGetValue(decal, out GameObject prefab) || prefab == null)
            {
                Destroy(decal);
                return;
            }

            Queue<GameObject> queue = GetOrCreateDecalQueue(prefab);
            if (queue.Count >= _maxDecalPoolSizePerPrefab)
            {
                _decalInstanceToPrefab.Remove(decal);
                Destroy(decal);
                return;
            }

            DOTween.Kill(decal.transform);
            DecalProjector projector = decal.GetComponent<DecalProjector>();
            if (projector == null)
            {
                projector = decal.GetComponentInChildren<DecalProjector>();
            }

            if (projector != null)
            {
                DOTween.Kill(projector);
            }

            decal.SetActive(false);
            decal.transform.SetParent(_poolParent != null ? _poolParent : transform, worldPositionStays: false);
            queue.Enqueue(decal);
        }

        public void FadeAndDestroyAllDecals()
        {
            if (_isFadingAllDecals || _activeDecals.Count == 0)
            {
                return;
            }

            _isFadingAllDecals = true;
            _decalCleanupRoutine = StartCoroutine(FadeAndDestroyAllDecalsSequentially());
        }

        public void StopCleanupRoutine()
        {
            if (_decalCleanupRoutine == null)
            {
                return;
            }

            StopCoroutine(_decalCleanupRoutine);
            _decalCleanupRoutine = null;
            _isFadingAllDecals = false;
        }

        public void DestroyAllDecalsImmediate()
        {
            while (_activeDecals.Count > 0)
            {
                GameObject decal = _activeDecals.Dequeue().Decal;
                if (decal != null)
                {
                    ReturnDecal(decal);
                }
            }
            _decalCleanupRoutine = null;
            _isFadingAllDecals = false;
        }

        private IEnumerator FadeAndDestroyAllDecalsSequentially()
        {
            while (_activeDecals.Count > 0)
            {
                ActiveDecalEntry entry = _activeDecals.Dequeue();
                GameObject decal = entry.Decal;
                if (decal == null)
                {
                    continue;
                }

                DecalProjector projector = decal.GetComponent<DecalProjector>();
                if (projector == null)
                {
                    projector = decal.GetComponentInChildren<DecalProjector>();
                }

                if (projector == null)
                {
                    ReturnDecal(decal);
                    continue;
                }

                DOTween.Kill(projector);
                DOTween.Sequence()
                    .SetUpdate(true)
                    .SetTarget(projector)
                    .Append(DOTween.To(() => projector.size.x, value => SetProjectorWidth(projector, value), 0.01f, entry.FadeDuration).SetEase(Ease.InQuad))
                    .Join(DOTween.To(() => projector.size.y, value => SetProjectorHeight(projector, value), 0.01f, entry.FadeDuration).SetEase(Ease.InQuad))
                    .OnComplete(() => ReturnDecal(decal));

                yield return new WaitForSecondsRealtime(entry.FadeDuration);

                if (_delayBetweenDecalCleanup > 0f)
                {
                    yield return new WaitForSecondsRealtime(_delayBetweenDecalCleanup);
                }
            }

            _decalCleanupRoutine = null;
            _isFadingAllDecals = false;
        }

        private Queue<GameObject> GetOrCreateDecalQueue(GameObject prefab)
        {
            if (!_decalPoolByPrefab.TryGetValue(prefab, out Queue<GameObject> queue) || queue == null)
            {
                queue = new Queue<GameObject>();
                _decalPoolByPrefab[prefab] = queue;
            }

            return queue;
        }

        private static void PrepareDecalForUse(GameObject decal, Vector3 position, Quaternion rotation)
        {
            DOTween.Kill(decal.transform);

            DecalProjector projector = decal.GetComponent<DecalProjector>();
            if (projector == null)
            {
                projector = decal.GetComponentInChildren<DecalProjector>();
            }

            if (projector != null)
            {
                DOTween.Kill(projector);
            }

            decal.transform.SetParent(null, worldPositionStays: true);
            decal.transform.SetPositionAndRotation(position, rotation);
            decal.SetActive(true);
        }

        private static void SetProjectorWidth(DecalProjector projector, float width)
        {
            if (projector == null)
            {
                return;
            }

            Vector3 size = projector.size;
            size.x = Mathf.Max(0.01f, width);
            projector.size = size;
        }

        private static void SetProjectorHeight(DecalProjector projector, float height)
        {
            if (projector == null)
            {
                return;
            }

            Vector3 size = projector.size;
            size.y = Mathf.Max(0.01f, height);
            projector.size = size;
        }
    }
}
