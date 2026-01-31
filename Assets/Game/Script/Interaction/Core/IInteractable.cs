using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Base interface for all interactable objects in the game.
    /// Provides a unified system for player interactions via the E key.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Display name shown in UI prompt (e.g., "Wooden Log", "Assessment Terminal")
        /// </summary>
        string InteractionPrompt { get; }
        
        /// <summary>
        /// Custom interaction key text (default: "Press E to")
        /// Examples: "Press E to", "Hold E to"
        /// </summary>
        string InteractionVerb { get; }
        
        /// <summary>
        /// Can this object be interacted with right now?
        /// </summary>
        bool CanInteract { get; }
        
        /// <summary>
        /// Visual feedback when player is near (highlight on/off)
        /// </summary>
        /// <param name="highlighted">True when this is the nearest/primary interactable</param>
        void OnHighlighted(bool highlighted);
        
        /// <summary>
        /// Execute the interaction
        /// </summary>
        /// <param name="player">The player performing the interaction</param>
        void Interact(Game.Player.PlayerControllerRefactored player);
        
        /// <summary>
        /// Distance-based priority (for when multiple items overlap)
        /// Higher values = higher priority
        /// </summary>
        float InteractionPriority { get; }
        
        /// <summary>
        /// Transform position for distance calculations
        /// </summary>
        Transform GetTransform();
    }
}
