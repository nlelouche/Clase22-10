namespace FlappyBird.Core
{
    /// <summary>
    /// Contract for pooled GameObjects to handle activation, recycling, and reset lifecycle
    /// without incurring runtime GameObject.Instantiate or GameObject.Destroy allocations.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Invoked immediately upon retrieval from an object pool.
        /// Re-enables components, resets transient simulation state, and activates the entity.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Invoked immediately prior to returning to an object pool.
        /// Ceases active movement/coroutines, deactivates colliders, and disables the entity.
        /// </summary>
        void OnDespawn();
    }
}
