using System;
using FlappyBird.Core;
using UnityEngine;

namespace FlappyBird.Gameplay
{
    /// <summary>
    /// Scoring domain model managing score state, high score calculation,
    /// PlayerPrefs persistence, and event synchronization across the game.
    /// </summary>
    public class ScoreModel : IDisposable
    {
        private const string HIGH_SCORE_KEY = "Flappy_HighScore";
        private const string CANONICAL_KEY = "FlappyBird_HighScore";

        private int _currentScore;
        private int _highScore;

        public int CurrentScore => _currentScore;
        public int HighScore => _highScore;

        public event Action<int, int> OnScoreUpdated;

        public ScoreModel()
        {
            LoadHighScore();
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            GameEvents.OnScoreIncremented += HandleScoreIncremented;
            GameEvents.OnGameRestartRequested += HandleRestartRequested;
        }

        private void UnsubscribeEvents()
        {
            GameEvents.OnScoreIncremented -= HandleScoreIncremented;
            GameEvents.OnGameRestartRequested -= HandleRestartRequested;
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void LoadHighScore()
        {
            _highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
            if (_highScore == 0)
            {
                _highScore = PlayerPrefs.GetInt(CANONICAL_KEY, 0);
            }
        }

        private void HandleScoreIncremented()
        {
            _currentScore++;

            if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                PlayerPrefs.SetInt(HIGH_SCORE_KEY, _highScore);
                PlayerPrefs.SetInt(CANONICAL_KEY, _highScore);
                PlayerPrefs.Save();
            }

            OnScoreUpdated?.Invoke(_currentScore, _highScore);
            GameEvents.TriggerScoreChanged(_currentScore);
        }

        private void HandleRestartRequested()
        {
            _currentScore = 0;
            OnScoreUpdated?.Invoke(_currentScore, _highScore);
            GameEvents.TriggerScoreChanged(0);
        }
    }
}
