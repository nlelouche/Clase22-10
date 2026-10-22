namespace FlappyBird.Core
{
    /// <summary>
    /// Discrete states for the Flappy Bird gameplay loop and coordination.
    /// </summary>
    public enum GameState : byte
    {
        /// <summary>
        /// Initial idle state. Bird rests at starting position waiting for player's first flap input.
        /// </summary>
        Ready = 0,

        /// <summary>
        /// Active gameplay simulation. Physics active, obstacles spawning and scrolling, score tracked.
        /// </summary>
        Playing = 1,

        /// <summary>
        /// Suspended gameplay simulation. Time scale frozen, simulation paused, pause modal active.
        /// </summary>
        Paused = 2,

        /// <summary>
        /// Terminal run state following bird collision. Movement halted, final score evaluated and displayed.
        /// </summary>
        GameOver = 3
    }
}
