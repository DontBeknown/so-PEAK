using System;

namespace Game.Core.Events
{
    /// <summary>
    /// Interface for event bus
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Subscribes to an event type
        /// </summary>
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        
        /// <summary>
        /// Unsubscribes from an event type
        /// </summary>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        
        /// <summary>
        /// Publishes an event
        /// </summary>
        void Publish<TEvent>(TEvent eventData) where TEvent : class;
    }
}
