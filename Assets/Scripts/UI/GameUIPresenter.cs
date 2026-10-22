using System;
using FlappyBird.Core;
using FlappyBird.Core.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace FlappyBird.UI
{
    /// <summary>
    /// Reactive UI Presenter binding domain game state, scoring events, and player input
    /// to UI Toolkit visual elements using cached zero-allocation references.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameUIPresenter : MonoBehaviour
    {
        [Header("Document & Domain References")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private ScoreManager _scoreManager;

        // Cached UI Elements
        private VisualElement _root;
        private VisualElement _hudContainer;
        private Label _hudScoreLabel;
        private Button _btnPause;

        private VisualElement _panelReady;
        private Label _lblReadyTitle;
        private Label _lblReadyHint;

        private VisualElement _panelPause;
        private Label _lblPauseTitle;
        private Button _btnResume;
        private Button _btnRestartPause;
        private Button _btnQuit;

        private VisualElement _panelGameOver;
        private Label _lblGameOverTitle;
        private VisualElement _boxScoreboard;
        private Label _lblFinalScore;
        private Label _lblHighScore;
        private VisualElement _badgeNewBest;
        private Button _btnRestart;

        private bool _isCached;

        private void Awake()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_gameManager == null)
            {
#if UNITY_2023_1_OR_NEWER
                _gameManager = FindAnyObjectByType<GameManager>();
#else
                _gameManager = FindObjectOfType<GameManager>();
#endif
            }

            if (_scoreManager == null)
            {
#if UNITY_2023_1_OR_NEWER
                _scoreManager = FindAnyObjectByType<ScoreManager>();
#else
                _scoreManager = FindObjectOfType<ScoreManager>();
#endif
            }

            CacheVisualElements();
        }

        private void OnEnable()
        {
            CacheVisualElements();
            RegisterButtonCallbacks();
            SubscribeDomainEvents();

            // Set initial state presentation
            if (_gameManager != null)
            {
                HandleStateChanged(_gameManager.CurrentState);
            }
            else
            {
                HandleStateChanged(GameState.Ready);
            }
        }

        private void OnDisable()
        {
            UnregisterButtonCallbacks();
            UnsubscribeDomainEvents();
        }

        /// <summary>
        /// Queries and caches all visual elements once to eliminate runtime DOM traversal and GC allocations.
        /// </summary>
        private void CacheVisualElements()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            // HUD Layer
            _hudContainer = _root.Q<VisualElement>("hud-container");
            _hudScoreLabel = _root.Q<Label>("hud-score") ?? _root.Q<Label>("score-label");
            _btnPause = _root.Q<Button>("btn-pause") ?? _root.Q<Button>("pause-btn");

            // Ready Overlay
            _panelReady = _root.Q<VisualElement>("panel-ready") ?? _root.Q<VisualElement>("ready-container");
            _lblReadyTitle = _root.Q<Label>("lbl-ready-title");
            _lblReadyHint = _root.Q<Label>("lbl-ready-hint");

            // Pause Modal
            _panelPause = _root.Q<VisualElement>("panel-pause") ?? _root.Q<VisualElement>("pause-modal");
            _lblPauseTitle = _root.Q<Label>("lbl-pause-title");
            _btnResume = _root.Q<Button>("btn-resume") ?? _root.Q<Button>("resume-btn");
            _btnRestartPause = _root.Q<Button>("btn-restart-pause") ?? _root.Q<Button>("pause-restart-btn");
            _btnQuit = _root.Q<Button>("btn-quit") ?? _root.Q<Button>("quit-btn");

            // Game Over Modal
            _panelGameOver = _root.Q<VisualElement>("panel-gameover") ?? _root.Q<VisualElement>("gameover-modal");
            _lblGameOverTitle = _root.Q<Label>("lbl-gameover-title");
            _boxScoreboard = _root.Q<VisualElement>("box-scoreboard") ?? _root.Q<VisualElement>("scoreboard");
            _lblFinalScore = _root.Q<Label>("lbl-final-score") ?? _root.Q<Label>("final-score-label");
            _lblHighScore = _root.Q<Label>("lbl-high-score") ?? _root.Q<Label>("best-score-label");
            _badgeNewBest = _root.Q<VisualElement>("badge-new-best");
            _btnRestart = _root.Q<Button>("btn-restart") ?? _root.Q<Button>("restart-btn");

            _isCached = true;
        }

        private void RegisterButtonCallbacks()
        {
            if (_btnPause != null) _btnPause.clicked += OnPauseButtonClicked;
            if (_btnResume != null) _btnResume.clicked += OnResumeButtonClicked;
            if (_btnRestartPause != null) _btnRestartPause.clicked += OnRestartButtonClicked;
            if (_btnQuit != null) _btnQuit.clicked += OnQuitButtonClicked;
            if (_btnRestart != null) _btnRestart.clicked += OnRestartButtonClicked;
        }

        private void UnregisterButtonCallbacks()
        {
            if (_btnPause != null) _btnPause.clicked -= OnPauseButtonClicked;
            if (_btnResume != null) _btnResume.clicked -= OnResumeButtonClicked;
            if (_btnRestartPause != null) _btnRestartPause.clicked -= OnRestartButtonClicked;
            if (_btnQuit != null) _btnQuit.clicked -= OnQuitButtonClicked;
            if (_btnRestart != null) _btnRestart.clicked -= OnRestartButtonClicked;
        }

        private void SubscribeDomainEvents()
        {
            GameEvents.OnStateChanged += HandleStateChanged;
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnGameOverScores += HandleGameOverScores;

            if (_gameManager != null)
            {
                _gameManager.OnStateChanged += HandleStateChanged;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged += HandleScoreChanged;
                _scoreManager.OnGameOverScores += HandleGameOverScores;
            }
        }

        private void UnsubscribeDomainEvents()
        {
            GameEvents.OnStateChanged -= HandleStateChanged;
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnGameOverScores -= HandleGameOverScores;

            if (_gameManager != null)
            {
                _gameManager.OnStateChanged -= HandleStateChanged;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged -= HandleScoreChanged;
                _scoreManager.OnGameOverScores -= HandleGameOverScores;
            }
        }

        #region Button Handlers

        private void OnPauseButtonClicked()
        {
            GameEvents.TriggerPauseToggled();
            if (_gameManager != null && _gameManager.CurrentState == GameState.Playing)
            {
                _gameManager.PauseGame();
            }
        }

        private void OnResumeButtonClicked()
        {
            if (_gameManager != null && _gameManager.CurrentState == GameState.Paused)
            {
                _gameManager.ResumeGame();
            }
        }

        private void OnRestartButtonClicked()
        {
            GameEvents.TriggerGameRestartRequested();
            if (_gameManager != null)
            {
                _gameManager.ResetGame();
            }
        }

        private void OnQuitButtonClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.QuitGame();
            }
        }

        #endregion

        #region Domain Event Callbacks

        /// <summary>
        /// Updates screen visibility states reactively without recreating or re-querying elements.
        /// </summary>
        /// <param name="newState">The newly activated game state.</param>
        public void HandleStateChanged(GameState newState)
        {
            if (!_isCached) CacheVisualElements();

            switch (newState)
            {
                case GameState.Ready:
                    SetElementDisplay(_panelReady, true);
                    SetElementDisplay(_hudContainer, false);
                    SetElementDisplay(_panelPause, false);
                    SetElementDisplay(_panelGameOver, false);
                    if (_hudScoreLabel != null) _hudScoreLabel.text = IntToStringLookup.Get(0);
                    break;

                case GameState.Playing:
                    SetElementDisplay(_panelReady, false);
                    SetElementDisplay(_hudContainer, true);
                    SetElementDisplay(_panelPause, false);
                    SetElementDisplay(_panelGameOver, false);
                    break;

                case GameState.Paused:
                    SetElementDisplay(_panelReady, false);
                    SetElementDisplay(_hudContainer, false);
                    SetElementDisplay(_panelPause, true);
                    SetElementDisplay(_panelGameOver, false);
                    break;

                case GameState.GameOver:
                    SetElementDisplay(_panelReady, false);
                    SetElementDisplay(_hudContainer, false);
                    SetElementDisplay(_panelPause, false);
                    SetElementDisplay(_panelGameOver, true);
                    break;
            }
        }

        /// <summary>
        /// Updates the HUD score display using pre-cached zero-allocation strings.
        /// </summary>
        /// <param name="score">The updated active score.</param>
        public void HandleScoreChanged(int score)
        {
            if (_hudScoreLabel != null)
            {
                _hudScoreLabel.text = IntToStringLookup.Get(score);
            }
        }

        /// <summary>
        /// Updates the Game Over scoreboard with final score, high score, and new best badge.
        /// </summary>
        public void HandleGameOverScores(int finalScore, int highScore, bool isNewBest)
        {
            if (_lblFinalScore != null)
            {
                _lblFinalScore.text = IntToStringLookup.Get(finalScore);
            }

            if (_lblHighScore != null)
            {
                _lblHighScore.text = IntToStringLookup.Get(highScore);
            }

            if (_badgeNewBest != null)
            {
                SetElementDisplay(_badgeNewBest, isNewBest);
            }
        }

        private static void SetElementDisplay(VisualElement element, bool visible)
        {
            if (element != null)
            {
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        #endregion
    }
}
