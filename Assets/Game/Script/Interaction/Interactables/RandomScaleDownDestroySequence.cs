using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Runs ScaleDownDestroyAnimation for a target list in random order with staggered timing.
    /// </summary>
    public class RandomScaleDownDestroySequence : MonoBehaviour
    {
        [SerializeField] private List<GameObject> targets = new();
        [SerializeField] private float delayBetweenObjects = 0.05f;
        [SerializeField] private bool addMissingAnimationComponent = true;

        private Sequence _sequence;
        private bool _hasRun;

        public void SetDelay(float delay)
        {
            delayBetweenObjects = Mathf.Max(0f, delay);
        }

        public void SetTargets(IEnumerable<GameObject> source)
        {
            targets.Clear();
            if (source == null) return;

            foreach (GameObject target in source)
            {
                if (target != null)
                    targets.Add(target);
            }
        }

        public void CollectFromParentDirectChildren(Transform parent, bool clearExisting = true, bool includeParentWhenNoChildren = true)
        {
            if (clearExisting)
                targets.Clear();

            if (parent == null)
                return;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null)
                    targets.Add(child.gameObject);
            }

            if (includeParentWhenNoChildren && parent.childCount == 0)
                targets.Add(parent.gameObject);
        }

        public void Play()
        {
            if (_hasRun)
                return;

            if (targets.Count == 0)
                return;

            _hasRun = true;
            _sequence?.Kill();

            List<GameObject> randomizedTargets = new(targets);
            Shuffle(randomizedTargets);

            _sequence = DOTween.Sequence();

            bool hasAnyStep = false;
            for (int i = 0; i < randomizedTargets.Count; i++)
            {
                GameObject target = randomizedTargets[i];
                if (target == null)
                    continue;

                if (hasAnyStep)
                    _sequence.AppendInterval(delayBetweenObjects);

                _sequence.AppendCallback(() => TryPlayDestroy(target));
                hasAnyStep = true;
            }

            if (!hasAnyStep)
                _hasRun = false;
        }

        private void TryPlayDestroy(GameObject target)
        {
            if (target == null)
                return;

            ScaleDownDestroyAnimation animation = target.GetComponent<ScaleDownDestroyAnimation>();
            if (animation == null && addMissingAnimationComponent)
                animation = target.AddComponent<ScaleDownDestroyAnimation>();

            animation?.PlayAndDestroy();
        }

        private static void Shuffle(List<GameObject> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }
    }
}