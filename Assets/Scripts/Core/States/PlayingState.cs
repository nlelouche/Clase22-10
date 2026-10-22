using FlappyBird.Obstacles;
using FlappyBird.Player;
using UnityEngine;

namespace FlappyBird.Core.States
{
    /// <summary>
    /// Active gameplay simulation state.
    /// Physics simulation is active, obstacle spawning and scrolling proceed,
    /// and scoring pass events are recorded.
    /// </summary>
    public class PlayingState : IGameState
    {
        private readonly GameManager _gameManager;
        private readonly BirdPhysics _birdPhysics;
        private readonly PipeSpawner _spawner;

        public PlayingState(
            GameManager gameManager,
            BirdPhysics birdPhysics,
            PipeSpawner spawner)
        {
            _gameManager = gameManager;
            _birdPhysics = birdPhysics;
            _spawner = spawner;
        }

        public void Enter()
        {
            Time.timeScale = 1.0f;

            if (_birdPhysics != null)
            {
                _birdPhysics.SetSimulated(true);
                // Apply first flap impulse to transition smoothly into active flight
                _birdPhysics.RequestFlap();
            }

            if (_spawner != null)
            {
                _spawner.SetSpawning(true);
            }
        }

        public void Update()
        {
            // Frame update loop for Playing state
        }

        public void FixedUpdate()
        {
            // Fixed timestep loop for Playing state
        }

        public void Exit()
        {
            if (_spawner != null)
            {
                _spawner.SetSpawning(false);
            }
        }
    }
}
