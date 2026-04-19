using UnityEngine;
using Game.Player;

namespace Game.Interaction
{
    /// <summary>
    /// Hold interactable that ties a player to an anchor, then unties on a second hold.
    /// </summary>
    public class TiedInteractable : HoldInteractableBase
    {
        [Header("Tied Settings")]
        [SerializeField] private float tiedSpeedMultiplier = 0.35f;
        [SerializeField] private float maxTiedRadius = 4f;
        [SerializeField] private Transform anchorTransform;

        [Header("Rope Visual")]
        [SerializeField] private GameObject ropeVisual;
        [SerializeField] private Transform ropePoint;
        [SerializeField] private HumanBodyBones handBone = HumanBodyBones.RightHand;

        private bool _playerIsTied;
        private PlayerControllerRefactored _tiedPlayer;
        private Transform _tiedHandTransform;

        public override string InteractionPrompt => _playerIsTied ? "Untie rope" : "Tie rope";
        public override string InteractionVerb => "Hold to";
        public override bool CanInteract => !isCurrentlyHolding;

        private void Awake()
        {
            if (anchorTransform == null)
            {
                anchorTransform = transform;
            }

            if (ropeVisual != null)
            {
                ropeVisual.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (_playerIsTied)
            {
                UpdateRopePointToHand();
            }
        }

        protected override void OnHoldComplete()
        {
            if (currentPlayer == null)
            {
                return;
            }

            if (!_playerIsTied)
            {
                _playerIsTied = true;
                _tiedPlayer = currentPlayer;
                _tiedPlayer.EnterTiedState(anchorTransform, maxTiedRadius, tiedSpeedMultiplier);
                ResolveTiedHandTransform();
                SetRopeVisualActive(true);
                UpdateRopePointToHand();
                return;
            }

            // Only the player who initiated tie can untie via this interactable.
            if (currentPlayer != _tiedPlayer)
            {
                return;
            }

            _tiedPlayer.ExitTiedState();
            _tiedPlayer = null;
            _tiedHandTransform = null;
            _playerIsTied = false;
            SetRopeVisualActive(false);
        }

        protected override void OnHoldCancel(string reason)
        {
        }

        public void ForceUntie()
        {
            if (!_playerIsTied)
            {
                return;
            }

            _tiedPlayer?.ExitTiedState();
            _tiedPlayer = null;
            _tiedHandTransform = null;
            _playerIsTied = false;
            SetRopeVisualActive(false);
        }

        private new void OnDestroy()
        {
            ForceUntie();
        }

        private void ResolveTiedHandTransform()
        {
            _tiedHandTransform = null;

            if (_tiedPlayer == null)
            {
                return;
            }

            Animator animator = _tiedPlayer.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                _tiedHandTransform = animator.GetBoneTransform(handBone);
            }

            // Fallback keeps rope anchored to player even on non-humanoid rigs.
            if (_tiedHandTransform == null)
            {
                _tiedHandTransform = _tiedPlayer.transform;
            }
        }

        private void UpdateRopePointToHand()
        {
            if (ropePoint == null || _tiedPlayer == null)
            {
                return;
            }

            if (_tiedHandTransform == null)
            {
                ResolveTiedHandTransform();
                if (_tiedHandTransform == null)
                {
                    return;
                }
            }

            ropePoint.position = _tiedHandTransform.position;
        }

        private void SetRopeVisualActive(bool active)
        {
            if (ropeVisual != null)
            {
                ropeVisual.SetActive(active);
            }
        }
    }
}