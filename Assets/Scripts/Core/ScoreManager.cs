using System;
using UnityEngine;

namespace FlappyBird.Core
{
    /// <summary>
    /// Maintains run score, tracks high score records, and persists best scores via PlayerPrefs.
    /// Dispatches score progression events with zero heap allocations.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public const string HIGH_SCORE_KEY = "FlappyBird_HighScore";
        public const string ALT_HIGH_SCORE_KEY = "Flappy_HighScore";

        [Header("State (Read-Only)")]
        [SerializeField] private int _currentScore;
        [SerializeField] private int _highScore;

        /// <summary>
        /// Current active run score.
        /// </summary>
        public int CurrentScore => _currentScore;

        /// <summary>
        /// All-time persistent high score.
        /// </summary>
        public int HighScore => _highScore;

        /// <summary>
        /// Event fired whenever the current run score changes.
        /// Passes the updated score.
        /// </summary>
        public event Action<int> OnScoreChanged;

        /// <summary>
        /// Event fired when the run concludes and final score is evaluated against high score.
        /// Passes (currentScore, highScore, isNewBest).
        /// </summary>
        public event Action<int, int, bool> OnGameOverScores;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            GameEvents.OnScoreIncremented += IncrementScore;
            GameEvents.OnGameRestartRequested += ResetScore;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreIncremented -= IncrementScore;
            GameEvents.OnGameRestartRequested -= ResetScore;
        }

        /// <summary>
        /// Loads the stored high score from PlayerPrefs.
        /// </summary>
        public void Initialize()
        {
            _highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
            if (_highScore == 0)
            {
                _highScore = PlayerPrefs.GetInt(ALT_HIGH_SCORE_KEY, 0);
            }
        }

        /// <summary>
        /// Increases the active score by 1 and broadcasts change events.
        /// </summary>
        public void IncrementScore()
        {
            _currentScore++;
            OnScoreChanged?.Invoke(_currentScore);
            GameEvents.TriggerScoreChanged(_currentScore);
        }

        /// <summary>
        /// Resets the active run score to 0 and broadcasts change events.
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            OnScoreChanged?.Invoke(0);
            GameEvents.TriggerScoreChanged(0);
        }

        /// <summary>
        /// Evaluates current run score against high score, saves to PlayerPrefs if a new record is reached,
        /// and broadcasts game over score summary events.
        /// </summary>
        public void FinalizeScore()
        {
            bool isNewBest = false;

            if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                PlayerPrefs.SetInt(HIGH_SCORE_KEY, _highScore);
                PlayerPrefs.SetInt(ALT_HIGH_SCORE_KEY, _highScore);
                PlayerPrefs.Save();
                isNewBest = true;
            }

            OnGameOverScores?.Invoke(_currentScore, _highScore, isNewBest);
            GameEvents.TriggerGameOverScores(_currentScore, _highScore, isNewBest);
        }
    }
}
