using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// Simple event bus implementation
    /// Allows decoupled communication between systems
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers 
            = new Dictionary<Type, List<Delegate>>();
        
        /// <summary>
        /// Subscribes to an event type
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                return;
            
            var eventType = typeof(TEvent);
            
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            
            _subscribers[eventType].Add(handler);
        }
        
        /// <summary>
        /// Unsubscribes from an event type
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                return;
            
            var eventType = typeof(TEvent);
            
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
        
        /// <summary>
        /// Publishes an event to all subscribers
        /// </summary>
        public void Publish<TEvent>(TEvent eventData) where TEvent : class
        {
            if (eventData == null)
                return;
            
            var eventType = typeof(TEvent);
            
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                // Create a copy to avoid modification during iteration
                var handlersCopy = new List<Delegate>(handlers);
                
                foreach (var handler in handlersCopy)
                {
                    try
                    {
                        (handler as Action<TEvent>)?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventBus] Error invoking handler for {eventType.Name}: {ex}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the number of subscribers for an event type
        /// </summary>
        public int GetSubscriberCount<TEvent>() where TEvent : class
        {
            var eventType = typeof(TEvent);
            
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                return handlers.Count;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Clears all subscriptions
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
        }
    }
}
