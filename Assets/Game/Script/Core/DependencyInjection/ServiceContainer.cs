using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.DI
{
    /// <summary>
    /// Simple dependency injection container
    /// Singleton pattern for Unity game-wide services
    /// </summary>
    public class ServiceContainer : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        private static ServiceContainer _instance;
        public static ServiceContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceContainer();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Registers a service
        /// </summary>
        public void Register<TService>(TService instance) where TService : class
        {
            if (instance == null)
            {
                Debug.LogWarning($"[ServiceContainer] Attempted to register null instance of {typeof(TService).Name}");
                return;
            }
            
            var type = typeof(TService);
            
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceContainer] Service {type.Name} is already registered. Overwriting.");
            }
            
            _services[type] = instance;
            //Debug.Log($"[ServiceContainer] Registered {type.Name}");
        }
        
        /// <summary>
        /// Gets a registered service
        /// Throws exception if not found
        /// </summary>
        public TService Get<TService>() where TService : class
        {
            var type = typeof(TService);
            
            if (_services.TryGetValue(type, out object service))
            {
                return service as TService;
            }
            
            throw new InvalidOperationException($"Service {type.Name} is not registered");
        }
        
        /// <summary>
        /// Tries to get a service, returns null if not found
        /// </summary>
        public TService TryGet<TService>() where TService : class
        {
            var type = typeof(TService);
            
            if (_services.TryGetValue(type, out object service))
            {
                return service as TService;
            }
            
            Debug.LogWarning($"[ServiceContainer] Service {type.Name} not found");
            return null;
        }
        
        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool Has<TService>() where TService : class
        {
            return _services.ContainsKey(typeof(TService));
        }
        
        /// <summary>
        /// Unregisters a service
        /// </summary>
        public void Unregister<TService>() where TService : class
        {
            var type = typeof(TService);
            
            if (_services.Remove(type))
            {
                //Debug.Log($"[ServiceContainer] Unregistered {type.Name}");
            }
        }
        
        /// <summary>
        /// Clears all registered services
        /// Useful for testing or scene transitions
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            //Debug.Log("[ServiceContainer] Cleared all services");
        }
    }
}
