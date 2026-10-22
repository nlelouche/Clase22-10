using System;
using System.Collections.Generic;
using FlappyBird.Core.States;
using FlappyBird.Obstacles;
using FlappyBird.Player;
using UnityEngine;

namespace FlappyBird.Core
{
    /// <summary>
    /// Central game lifecycle coordinator and Finite State Machine controller.
    /// Manages state transitions across Ready, Playing, Paused, and GameOver states.
    /// Supports non-destructive game resets without reloading scenes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Player Dependencies")]
        [SerializeField] private BirdPhysics _birdPhysics;
        [SerializeField] private BirdCollisionHandler _collisionHandler;
        [SerializeField] private BirdInputReader _inputReader;

        [Header("Obstacle Dependencies")]
        [SerializeField] private PipeSpawner _spawner;
        [SerializeField] private PipePool _pool;

        [Header("Domain Dependencies")]
        [SerializeField] private ScoreManager _scoreManager;

        private Dictionary<GameState, IGameState> _stateMap;
        private IGameState _currentState;
        private GameState _currentStateType;
        private bool _isInitialized;

        /// <summary>
        /// Currently active GameState enum.
        /// </summary>
        public GameState CurrentState => _currentStateType;

        /// <summary>
        /// Event fired whenever the game state changes.
        /// </summary>
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            InitializeDependencies();
            InitializeStateMachine();
        }

        private void Start()
        {
            // Enter the initial ReadyState on startup
            if (_currentState != null)
            {
                _currentState.Enter();
                OnStateChanged?.Invoke(_currentStateType);
                GameEvents.TriggerStateChanged(_currentStateType);
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            _currentState?.Update();
        }

        private void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }

        private void InitializeDependencies()
        {
#if UNITY_2023_1_OR_NEWER
            if (_birdPhysics == null) _birdPhysics = FindAnyObjectByType<BirdPhysics>();
            if (_collisionHandler == null) _collisionHandler = FindAnyObjectByType<BirdCollisionHandler>();
            if (_inputReader == null) _inputReader = FindAnyObjectByType<BirdInputReader>();
            if (_spawner == null) _spawner = FindAnyObjectByType<PipeSpawner>();
            if (_pool == null) _pool = FindAnyObjectByType<PipePool>();
            if (_scoreManager == null) _scoreManager = FindAnyObjectByType<ScoreManager>();
#else
            if (_birdPhysics == null) _birdPhysics = FindObjectOfType<BirdPhysics>();
            if (_collisionHandler == null) _collisionHandler = FindObjectOfType<BirdCollisionHandler>();
            if (_inputReader == null) _inputReader = FindObjectOfType<BirdInputReader>();
            if (_spawner == null) _spawner = FindObjectOfType<PipeSpawner>();
            if (_pool == null) _pool = FindObjectOfType<PipePool>();
            if (_scoreManager == null) _scoreManager = FindObjectOfType<ScoreManager>();
#endif
        }

        private void InitializeStateMachine()
        {
            if (_isInitialized) return;

            _stateMap = new Dictionary<GameState, IGameState>
            {
                { GameState.Ready, new ReadyState(this, _birdPhysics, _spawner, _pool, _scoreManager) },
                { GameState.Playing, new PlayingState(this, _birdPhysics, _spawner) },
                { GameState.Paused, new PausedState(this) },
                { GameState.GameOver, new GameOverState(this, _birdPhysics, _spawner, _scoreManager) }
            };

            _currentStateType = GameState.Ready;
            _currentState = _stateMap[GameState.Ready];
            _isInitialized = true;
        }

        private void SubscribeEvents()
        {
            if (_inputReader != null)
            {
                _inputReader.OnFlapRequested += HandleFlapRequested;
                _inputReader.OnPauseRequested += HandlePauseRequested;
            }
            else
            {
                GameEvents.OnFlapRequested += HandleFlapRequested;
                GameEvents.OnPauseRequested += HandlePauseRequested;
            }

            if (_collisionHandler != null)
            {
                _collisionHandler.OnDied += HandlePlayerDied;
            }
            else
            {
                GameEvents.OnPlayerDied += HandlePlayerDied;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_inputReader != null)
            {
                _inputReader.OnFlapRequested -= HandleFlapRequested;
                _inputReader.OnPauseRequested -= HandlePauseRequested;
            }
            else
            {
                GameEvents.OnFlapRequested -= HandleFlapRequested;
                GameEvents.OnPauseRequested -= HandlePauseRequested;
            }

            if (_collisionHandler != null)
            {
                _collisionHandler.OnDied -= HandlePlayerDied;
            }
            else
            {
                GameEvents.OnPlayerDied -= HandlePlayerDied;
            }
        }

        #region Event Handlers

        private void HandleFlapRequested()
        {
            if (_currentStateType == GameState.Ready)
            {
                StartGame();
            }
        }

        private void HandlePauseRequested()
        {
            if (_currentStateType == GameState.Playing)
            {
                PauseGame();
            }
            else if (_currentStateType == GameState.Paused)
            {
                ResumeGame();
            }
        }

        private void HandlePlayerDied()
        {
            if (_currentStateType == GameState.Playing)
            {
                GameOver();
            }
        }

        #endregion

        #region State Machine Transitions

        /// <summary>
        /// Validates transition rules and executes Exit on the old state and Enter on the new state.
        /// </summary>
        /// <param name="newState">Target GameState.</param>
        public void ChangeState(GameState newState)
        {
            if (_stateMap == null || !_stateMap.ContainsKey(newState))
            {
                Debug.LogError($"[GameManager] Cannot change to unmapped state: {newState}");
                return;
            }

            if (!IsValidTransition(_currentStateType, newState))
            {
                Debug.LogWarning($"[GameManager] Guard prevented transition from {_currentStateType} to {newState}");
                return;
            }

            _currentState?.Exit();
            _currentStateType = newState;
            _currentState = _stateMap[newState];
            _currentState.Enter();

            OnStateChanged?.Invoke(newState);
            GameEvents.TriggerStateChanged(newState);
        }

        private bool IsValidTransition(GameState from, GameState to)
        {
            if (from == to) return false;

            switch (from)
            {
                case GameState.Ready:
                    return to == GameState.Playing;

                case GameState.Playing:
                    return to == GameState.Paused || to == GameState.GameOver;

                case GameState.Paused:
                    return to == GameState.Playing || to == GameState.Ready;

                case GameState.GameOver:
                    return to == GameState.Ready;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Transitions Ready -> Playing on the initial flap.
        /// </summary>
        public void StartGame()
        {
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// Transitions Playing -> Paused.
        /// </summary>
        public void PauseGame()
        {
            ChangeState(GameState.Paused);
        }

        /// <summary>
        /// Transitions Paused -> Playing.
        /// </summary>
        public void ResumeGame()
        {
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// Transitions Playing -> GameOver upon player death.
        /// </summary>
        public void GameOver()
        {
            ChangeState(GameState.GameOver);
        }

        /// <summary>
        /// Non-destructively resets the game to ReadyState without reloading the scene.
        /// Recycles active obstacles, repositions bird, and resets scoring.
        /// </summary>
        public void ResetGame()
        {
            // Transition directly to ReadyState
            if (_currentStateType != GameState.Ready)
            {
                _currentState?.Exit();
                _currentStateType = GameState.Ready;
                _currentState = _stateMap[GameState.Ready];
                _currentState.Enter();

                OnStateChanged?.Invoke(GameState.Ready);
                GameEvents.TriggerStateChanged(GameState.Ready);
            }
            else
            {
                _currentState?.Enter();
            }

            // Reset bird collision detection
            if (_collisionHandler != null)
            {
                _collisionHandler.ResetHandler();
            }

            GameEvents.TriggerGameRestartRequested();
        }

        /// <summary>
        /// Terminates the application or halts editor playmode.
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
