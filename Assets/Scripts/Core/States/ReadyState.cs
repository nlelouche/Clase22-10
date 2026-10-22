using FlappyBird.Obstacles;
using FlappyBird.Player;
using UnityEngine;

namespace FlappyBird.Core.States
{
    /// <summary>
    /// Initial idle state. Bird rests suspended at starting coordinates (-1.5, 0.0).
    /// Obstacle pool is cleared, spawner is idle, and score is reset.
    /// Awaits the player's initial flap input to start the run.
    /// </summary>
    public class ReadyState : IGameState
    {
        private readonly GameManager _gameManager;
        private readonly BirdPhysics _birdPhysics;
        private readonly PipeSpawner _spawner;
        private readonly PipePool _pool;
        private readonly ScoreManager _scoreManager;

        private static readonly Vector2 StartPosition = new Vector2(-1.5f, 0.0f);

        public ReadyState(
            GameManager gameManager,
            BirdPhysics birdPhysics,
            PipeSpawner spawner,
            PipePool pool,
            ScoreManager scoreManager)
        {
            _gameManager = gameManager;
            _birdPhysics = birdPhysics;
            _spawner = spawner;
            _pool = pool;
            _scoreManager = scoreManager;
        }

        public void Enter()
        {
            Time.timeScale = 1.0f;

            if (_birdPhysics != null)
            {
                _birdPhysics.ResetPhysics(StartPosition);
                _birdPhysics.SetSimulated(false);
            }

            if (_spawner != null)
            {
                _spawner.ResetSpawner();
                _spawner.SetSpawning(false);
            }

            if (_pool != null)
            {
                _pool.ResetAll();
            }

            if (_scoreManager != null)
            {
                _scoreManager.ResetScore();
            }
        }

        public void Update()
        {
            // Zero-allocation loop; waits for input trigger delegated from GameManager
        }

        public void FixedUpdate()
        {
            // Physics simulation suspended in ReadyState
        }

        public void Exit()
        {
            // Hand off cleanly to next state
        }
    }
}
