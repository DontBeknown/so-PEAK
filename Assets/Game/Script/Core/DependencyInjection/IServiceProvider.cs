namespace Game.Core.DI
{
    /// <summary>
    /// Interface for service provider
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Registers a service instance
        /// </summary>
        void Register<TService>(TService instance) where TService : class;
        
        /// <summary>
        /// Gets a registered service
        /// </summary>
        TService Get<TService>() where TService : class;
        
        /// <summary>
        /// Tries to get a service, returns null if not found
        /// </summary>
        TService TryGet<TService>() where TService : class;
        
        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        bool Has<TService>() where TService : class;
        
        /// <summary>
        /// Unregisters a service
        /// </summary>
        void Unregister<TService>() where TService : class;
    }
}
