using UnityEngine;
using DG.Tweening;

namespace Game.Interaction
{
    /// <summary>
    /// Reusable destruction animation component.
    /// Call <see cref="PlayAndDestroy"/> from any script (e.g. GatheringInteractable)
    /// to scale the GameObject to zero then destroy it.
    /// </summary>
    public class ScaleDownDestroyAnimation : MonoBehaviour
    {
        [SerializeField] private float bounceDuration = 0.12f;
        [SerializeField] private float bounceScale = 1.4f;
        [SerializeField] private float shrinkDuration = 0.25f;
        [SerializeField] private Ease bounceEase = Ease.OutQuad;
        [SerializeField] private Ease shrinkEase = Ease.InBack;

        /// <summary>
        /// Disables colliders, pops the object up in scale, then shrinks it to zero and destroys it.
        /// Safe to call multiple times — subsequent calls are ignored.
        /// </summary>
        public void PlayAndDestroy()
        {
            // Guard against being called twice
            if (!enabled) return;
            enabled = false;

            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Freeze physics so gravity/forces don't interfere with the tween
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Vector3 originalScale = transform.localScale;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(originalScale * bounceScale, bounceDuration).SetEase(bounceEase));
            seq.Append(transform.DOScale(Vector3.zero, shrinkDuration).SetEase(shrinkEase));
            seq.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
