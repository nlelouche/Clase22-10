using System;

namespace FlappyBird.Core
{
    /// <summary>
    /// Centralized event bus mediating decoupled communication across Domain,
    /// Simulation, and Presentation layers without circular object references.
    /// </summary>
    public static class GameEvents
    {
        // Player & Input Events
        public static event Action OnFlapRequested;
        public static event Action OnPauseRequested;
        public static event Action OnPauseToggled
        {
            add => OnPauseRequested += value;
            remove => OnPauseRequested -= value;
        }
        public static event Action OnPlayerDied;

        // Game State Events
        public static event Action<GameState> OnStateChanged;
        public static event Action OnGameRestartRequested;

        // Score Events
        public static event Action OnScoreIncremented;
        public static event Action<int> OnScoreChanged;
        public static event Action<int, int, bool> OnGameOverScores; // currentScore, highScore, isNewBest

        #region Dispatch Methods

        public static void TriggerFlapRequested()
        {
            OnFlapRequested?.Invoke();
        }

        public static void TriggerPauseRequested()
        {
            OnPauseRequested?.Invoke();
        }

        public static void TriggerPauseToggled()
        {
            OnPauseRequested?.Invoke();
        }

        public static void TriggerPlayerDied()
        {
            OnPlayerDied?.Invoke();
        }

        public static void TriggerStateChanged(GameState newState)
        {
            OnStateChanged?.Invoke(newState);
        }

        public static void TriggerGameRestartRequested()
        {
            OnGameRestartRequested?.Invoke();
        }

        public static void TriggerScoreIncremented()
        {
            OnScoreIncremented?.Invoke();
        }

        public static void TriggerScoreChanged(int currentScore)
        {
            OnScoreChanged?.Invoke(currentScore);
        }

        public static void TriggerGameOverScores(int currentScore, int highScore, bool isNewBest)
        {
            OnGameOverScores?.Invoke(currentScore, highScore, isNewBest);
        }

        #endregion

        /// <summary>
        /// Clears all event delegate subscriptions. Useful for testing or non-destructive scene reloads.
        /// </summary>
        public static void ResetAllEvents()
        {
            OnFlapRequested = null;
            OnPauseRequested = null;
            OnPlayerDied = null;
            OnStateChanged = null;
            OnGameRestartRequested = null;
            OnScoreIncremented = null;
            OnScoreChanged = null;
            OnGameOverScores = null;
        }
    }
}
