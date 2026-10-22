using FlappyBird.Obstacles;
using FlappyBird.Player;
using UnityEngine;

namespace FlappyBird.Core.States
{
    /// <summary>
    /// Terminal state reached upon bird collision.
    /// Halts obstacle spawning, keeps Time.timeScale at 1.0 to permit post-death
    /// visual effects and UI animations, and triggers high-score finalization.
    /// </summary>
    public class GameOverState : IGameState
    {
        private readonly GameManager _gameManager;
        private readonly BirdPhysics _birdPhysics;
        private readonly PipeSpawner _spawner;
        private readonly ScoreManager _scoreManager;

        public GameOverState(
            GameManager gameManager,
            BirdPhysics birdPhysics,
            PipeSpawner spawner,
            ScoreManager scoreManager)
        {
            _gameManager = gameManager;
            _birdPhysics = birdPhysics;
            _spawner = spawner;
            _scoreManager = scoreManager;
        }

        public void Enter()
        {
            // Maintain timescale at 1.0 for post-death animations and UI transitions
            Time.timeScale = 1.0f;

            if (_spawner != null)
            {
                _spawner.SetSpawning(false);
            }

            if (_scoreManager != null)
            {
                _scoreManager.FinalizeScore();
            }
        }

        public void Update()
        {
            // Frame update during game over
        }

        public void FixedUpdate()
        {
            // Fixed timestep update during game over
        }

        public void Exit()
        {
            // Clean transition out of game over
        }
    }
}
