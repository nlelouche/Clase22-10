namespace FlappyBird.Core
{
    /// <summary>
    /// Lifecycle interface for game states within the core finite state machine.
    /// Encapsulates state-specific logic, execution hooks, and transition side-effects.
    /// </summary>
    public interface IGameState
    {
        /// <summary>
        /// Invoked when entering this state. Used for initialization, subscriptions, and camera/UI setup.
        /// </summary>
        void Enter();

        /// <summary>
        /// Per-frame update hook delegated from GameManager.Update().
        /// Hot loop: MUST remain strictly zero-allocation.
        /// </summary>
        void Update();

        /// <summary>
        /// Physics timestep update hook delegated from GameManager.FixedUpdate().
        /// Hot loop: MUST remain strictly zero-allocation.
        /// </summary>
        void FixedUpdate();

        /// <summary>
        /// Invoked when exiting this state. Used for cleanups, unsubscriptions, and state teardown.
        /// </summary>
        void Exit();
    }
}
