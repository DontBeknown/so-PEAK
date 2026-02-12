using UnityEngine;
using System.Collections;
using Game.Core.DI;

namespace Game.Interaction
{
    /// <summary>
    /// Base class for interactables that require holding the interact button.
    /// Handles common logic: progress tracking, input checking, player locking, detector management.
    /// Follows SOLID: Single Responsibility (hold mechanics), Open/Closed (extensible), DRY principle.
    /// 
    /// Derived classes only need to implement:
    /// - InteractionPrompt, CanInteract, InteractionPriority (from IInteractable)
    /// - OnHoldComplete() - what happens when hold completes
    /// - Optionally override: OnHoldStart(), OnHoldCancel(), OnHoldUpdate()
    /// </summary>
    public abstract class HoldInteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Hold Interaction Settings")]
        [SerializeField] protected float holdDuration = 3f;
        [SerializeField] protected float interactionPriority = 1.2f;
        
        [Header("Visual Feedback")]
        [SerializeField] protected GameObject highlightEffect;
        
        [Header("Audio")]
        [SerializeField] protected AudioClip holdStartSound;
        [SerializeField] protected AudioClip holdCompleteSound;
        [SerializeField] protected AudioClip holdCancelSound;
        
        // State
        protected bool isHighlighted = false;
        protected bool isCurrentlyHolding = false;
        protected float currentHoldProgress = 0f;
        
        // References
        protected Game.Player.PlayerControllerRefactored currentPlayer;
        protected Coroutine holdingCoroutine;
        protected Game.Interaction.UI.InteractionPromptUI promptUI;

        #region IInteractable Implementation

        public abstract string InteractionPrompt { get; }
        public virtual string InteractionVerb => "Hold to";
        public abstract bool CanInteract { get; }
        public virtual float InteractionPriority => interactionPriority;
        public Transform GetTransform() => transform;

        public virtual void OnHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(highlighted);
            }
        }

        public void Interact(Game.Player.PlayerControllerRefactored player)
        {
            if (!CanInteract)
                return;

            currentPlayer = player;
            StartHolding();
        }

        #endregion

        protected virtual void Update()
        {
            // Check if player is still holding button during interaction
            if (isCurrentlyHolding)
            {
                CheckHoldingInput();
            }
        }

        #region Hold Interaction Flow

        private void StartHolding()
        {
            if (isCurrentlyHolding)
                return;

            isCurrentlyHolding = true;
            currentHoldProgress = 0f;
            
            // Disable interaction detector to prevent other prompts
            DisableInteractionDetector();
            
            // Lock player movement
            if (currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(true);
            }
            
            // Play start sound
            if (holdStartSound != null)
            {
                AudioSource.PlayClipAtPoint(holdStartSound, transform.position);
            }
            
            // Get prompt UI
            if (promptUI == null)
            {
                promptUI = ServiceContainer.Instance.TryGet<Game.Interaction.UI.InteractionPromptUI>();
            }
            
            if (promptUI != null)
            {
                promptUI.ShowProgressBar();
            }
            
            // Notify derived class
            OnHoldStart();
            
            // Start holding coroutine
            holdingCoroutine = StartCoroutine(HoldingProcess());
        }

        private IEnumerator HoldingProcess()
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < holdDuration)
            {
                elapsedTime += Time.deltaTime;
                currentHoldProgress = Mathf.Clamp01(elapsedTime / holdDuration);
                
                // Update progress bar
                if (promptUI != null)
                {
                    promptUI.UpdateProgress(currentHoldProgress);
                }
                
                // Allow derived class to update per frame
                OnHoldUpdate(currentHoldProgress);
                
                yield return null;
            }
            
            // Holding complete!
            CompleteHolding();
        }

        private void CheckHoldingInput()
        {
            // Check if pickup button is still physically held
            if (currentPlayer != null)
            {
                if (!currentPlayer.IsPickupButtonPhysicallyHeld)
                {
                    // Player released button - cancel holding
                    CancelHolding("Released button");
                }
            }
            else
            {
                // No player reference - cancel
                CancelHolding("Lost player reference");
            }
        }

        private void CompleteHolding()
        {
            if (!isCurrentlyHolding)
                return;

            // Play completion sound
            if (holdCompleteSound != null)
            {
                AudioSource.PlayClipAtPoint(holdCompleteSound, transform.position);
            }
            
            // Notify derived class - THIS IS WHERE CUSTOM LOGIC GOES
            OnHoldComplete();
            
            Cleanup();
        }

        private void CancelHolding(string reason)
        {
            if (!isCurrentlyHolding)
                return;

            // Play cancel sound
            if (holdCancelSound != null)
            {
                AudioSource.PlayClipAtPoint(holdCancelSound, transform.position);
            }
            
            // Notify derived class
            OnHoldCancel(reason);
            
            Cleanup();
        }

        private void Cleanup()
        {
            isCurrentlyHolding = false;
            currentHoldProgress = 0f;
            
            // Stop holding coroutine
            if (holdingCoroutine != null)
            {
                StopCoroutine(holdingCoroutine);
                holdingCoroutine = null;
            }
            
            // Hide progress bar
            if (promptUI != null)
            {
                promptUI.HideProgressBar();
                promptUI = null;
            }
            
            // Re-enable interaction detector
            EnableInteractionDetector();
            
            // Unlock player
            if (currentPlayer != null)
            {
                currentPlayer.SetInputBlocked(false);
                currentPlayer = null;
            }
        }

        #endregion

        #region Interaction Detector Management

        private void DisableInteractionDetector()
        {
            if (currentPlayer != null)
            {
                var detector = currentPlayer.GetComponent<InteractionDetector>();
                if (detector != null)
                {
                    detector.DisableDetection();
                }
            }
        }

        private void EnableInteractionDetector()
        {
            if (currentPlayer != null)
            {
                var detector = currentPlayer.GetComponent<InteractionDetector>();
                if (detector != null)
                {
                    detector.EnableDetection();
                }
            }
        }

        #endregion

        #region Virtual Methods for Derived Classes

        /// <summary>
        /// Called when hold interaction starts. Override for custom start logic.
        /// </summary>
        protected virtual void OnHoldStart() { }

        /// <summary>
        /// Called every frame during holding. Override for custom update logic.
        /// </summary>
        /// <param name="progress">Progress from 0 to 1</param>
        protected virtual void OnHoldUpdate(float progress) { }

        /// <summary>
        /// Called when hold completes successfully. MUST be implemented by derived classes.
        /// This is where you put your custom interaction logic (gather items, refill canteen, etc.)
        /// </summary>
        protected abstract void OnHoldComplete();

        /// <summary>
        /// Called when hold is cancelled. Override for custom cancel logic.
        /// </summary>
        /// <param name="reason">Why the hold was cancelled</param>
        protected virtual void OnHoldCancel(string reason) { }

        #endregion

        #region Damage/Interruption Support

        /// <summary>
        /// Public method that can be called to interrupt the hold interaction
        /// (e.g., when player takes damage)
        /// </summary>
        public void InterruptHold()
        {
            if (isCurrentlyHolding)
            {
                CancelHolding("Interrupted");
            }
        }

        #endregion

        protected virtual void OnDestroy()
        {
            // Cleanup if destroyed during holding
            if (isCurrentlyHolding)
            {
                Cleanup();
            }
        }
    }
}
