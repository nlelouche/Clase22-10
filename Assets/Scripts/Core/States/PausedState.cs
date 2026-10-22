using UnityEngine;

namespace FlappyBird.Core.States
{
    /// <summary>
    /// Suspended simulation state.
    /// Freezes Time.timeScale to 0.0, pausing all physics calculations, obstacle movements,
    /// and spawn timers while keeping UI interactions active.
    /// </summary>
    public class PausedState : IGameState
    {
        private readonly GameManager _gameManager;
        private float _previousTimeScale = 1.0f;

        public PausedState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1.0f;
            Time.timeScale = 0.0f;
        }

        public void Update()
        {
            // Simulation paused; UI Toolkit updates using unscaled time
        }

        public void FixedUpdate()
        {
            // FixedUpdate does not execute while Time.timeScale == 0
        }

        public void Exit()
        {
            Time.timeScale = _previousTimeScale;
        }
    }
}
