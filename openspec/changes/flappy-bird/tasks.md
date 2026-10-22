# Tasks: 2D Flappy Bird Architecture & Implementation

## Document Overview
- **Change**: `flappy-bird`
- **Target Engine**: Unity 6 (6000.5.9f1)
- **Architecture**: Clean Architecture & SOLID, Event-Driven, New Input System, UI Toolkit, Zero-GC Hot Loops
- **Artifact Store**: `openspec`
- **Specification References**:
  - `openspec/changes/flappy-bird/proposal.md`
  - `openspec/changes/flappy-bird/specs/gameplay/spec.md`
  - `openspec/changes/flappy-bird/specs/player/spec.md`
  - `openspec/changes/flappy-bird/specs/obstacles/spec.md`
  - `openspec/changes/flappy-bird/specs/ui/spec.md`
  - `openspec/changes/flappy-bird/design.md`

---

## Phase 1: Core Foundation & Contracts

- [x] 1.1 Project Directory Structure Setup
  - **Description**: Establish clean architectural directory separation across Domain, Simulation, Presentation, and Settings.
  - **Actions**:
    - Create `Assets/Scripts/Core/` and `Assets/Scripts/Core/States/` for domain state machine and scoring.
    - Create `Assets/Scripts/Player/` for simulation, input reading, physics, and view components.
    - Create `Assets/Scripts/Obstacles/` for pooling, movement, spawning, and score triggers.
    - Create `Assets/Scripts/UI/` for UI Toolkit presenters and zero-GC utility classes.
    - Create `Assets/Prefabs/` for entity prefabs (`Bird`, `PipePair`, `GameManager`).
    - Create `Assets/UI/` for UXML templates, USS stylesheets, and PanelSettings assets.
    - Create `Assets/Settings/Input/` for dedicated Input System assets.
  - **Acceptance Criteria**: All target directories exist in the Unity project root.

- [x] 1.2 Define Core Game Lifecycle Interface (`IGameState.cs`)
  - **Description**: Create the formal state pattern lifecycle contract.
  - **File**: `Assets/Scripts/Core/Interfaces/IGameState.cs`
  - **Namespace**: `FlappyBird.Core`
  - **Signatures**:
    - `void Enter();`
    - `void Update();`
    - `void FixedUpdate();`
    - `void Exit();`
  - **Acceptance Criteria**: Interface compiles cleanly with descriptive XML docstrings for each lifecycle phase.

- [x] 1.3 Define Game State Enum (`GameState.cs`)
  - **Description**: Define strongly typed states for state tracking and event notifications.
  - **File**: `Assets/Scripts/Core/Enums/GameState.cs`
  - **Namespace**: `FlappyBird.Core`
  - **Values**: `Ready`, `Playing`, `Paused`, `GameOver`.
  - **Acceptance Criteria**: Enum defined with underlying byte or int, usable across domain and UI layers.

- [x] 1.4 Define Obstacle Object Pool Interface (`IPoolable.cs`)
  - **Description**: Create the lifecycle interface for pooled GameObjects to eliminate runtime allocations.
  - **File**: `Assets/Scripts/Core/Interfaces/IPoolable.cs`
  - **Namespace**: `FlappyBird.Core`
  - **Signatures**:
    - `void OnSpawn();`
    - `void OnDespawn();`
  - **Acceptance Criteria**: Interface compiles cleanly without engine-specific coupling.

- [x] 1.5 Implement Static Zero-GC Numeric String Cache (`IntToStringLookup.cs`)
  - **Description**: Implement a static lookup table pre-allocating string instances for numbers $0 \dots 999$ to ensure zero-GC UI score updates.
  - **File**: `Assets/Scripts/Core/Utils/IntToStringLookup.cs`
  - **Namespace**: `FlappyBird.Core.Utils`
  - **Implementation**:
    - Static array `string[] Cache = new string[1000];` populated in a static constructor.
    - Method `public static string GetString(int value)` returning pre-cached string if within $[0, 999]$; fallback to `value.ToString()` if out-of-bounds.
  - **Acceptance Criteria**: Calling `IntToStringLookup.GetString(score)` returns pre-cached string reference without allocating heap memory.

---

## Phase 2: Player Simulation & Input Integration

- [x] 2.1 Input Action Map Configuration (`InputSystem_Actions.inputactions` / `PlayerInputActions.inputactions`)
  - **Description**: Configure or extend the New Input System action asset to support multi-platform gameplay controls.
  - **File**: `Assets/Settings/Input/PlayerInputActions.inputactions`
  - **Action Map**: `Gameplay`
  - **Actions & Bindings**:
    - `Flap` (Button):
      - Keyboard: `<Keyboard>/space`
      - Mouse: `<Mouse>/leftButton`
      - Touchscreen: `<Touchscreen>/primaryTouch/tap`
    - `Pause` (Button):
      - Keyboard: `<Keyboard>/escape`
  - **Acceptance Criteria**: Action asset contains active bindings; C# wrapper class generated or action asset loaded via reference.

- [x] 2.2 Implement Event-Driven Input Adapter (`BirdInputReader.cs`)
  - **Description**: Subscribe to Input System events asynchronously with zero frame polling in `Update()`.
  - **File**: `Assets/Scripts/Player/BirdInputReader.cs`
  - **Namespace**: `FlappyBird.Player`
  - **Signatures**:
    - `public event Action OnFlapRequested;`
    - `public event Action OnPauseRequested;`
    - `private void OnEnable()`: Enables action map and hooks `InputAction.performed` callbacks.
    - `private void OnDisable()`: Disables action map and unhooks callbacks to prevent memory leaks.
  - **Constraints**: MUST NOT call `wasPressedThisFrame` or poll in `Update()`.
  - **Acceptance Criteria**: Pressing Space, Left Click, or Touch tap invokes `OnFlapRequested`. Pressing Escape invokes `OnPauseRequested`.

- [x] 2.3 Implement Rigidbody2D Physical Simulation (`BirdPhysics.cs`)
  - **Description**: Implement deterministic physics impulse, velocity clamping, and angular rotation calculation.
  - **File**: `Assets/Scripts/Player/BirdPhysics.cs`
  - **Namespace**: `FlappyBird.Player`
  - **Parameters**:
    - `float _flapImpulse = 7.5f;`
    - `float _maxFallSpeed = 10.5f;`
    - `float _maxUpwardAngle = 30f;`
    - `float _maxDownwardAngle = -90f;`
    - `float _tiltSmoothing = 15f;`
  - **Signatures**:
    - `public event Action<float> OnAngularRotationChanged;`
    - `public void Initialize(Rigidbody2D rb)`
    - `public void RequestFlap()`: Sets `_isFlapPending = true`.
    - `public void ResetPhysics(Vector2 startPosition)`: Sets position, zeroes velocity, resets rotation.
    - `public void SetSimulated(bool isSimulated)`: Toggles `_rb.simulated`.
    - `private void FixedUpdate()`:
      - If `_isFlapPending`: zeroes `linearVelocity.y`, applies upward impulse, sets `_isFlapPending = false`.
      - Clamps `linearVelocity.y` to `-maxFallSpeed`.
      - Computes target Z-tilt based on velocity and invokes `OnAngularRotationChanged`.
  - **Constraints**: All physics and velocity modifications occur strictly in `FixedUpdate()`. Multiple inputs within one physics step coalesce into one flap.
  - **Acceptance Criteria**: Velocity is capped at `-10.5 m/s`; impulse reliably launches bird; zero heap allocations.

- [x] 2.4 Implement Layer-Filtered Collision & Trigger Detection (`BirdCollisionHandler.cs`)
  - **Description**: Detect player collisions with pipes and ground boundaries with atomic one-shot death dispatch.
  - **File**: `Assets/Scripts/Player/BirdCollisionHandler.cs`
  - **Namespace**: `FlappyBird.Player`
  - **Fields**:
    - `[SerializeField] private LayerMask _obstacleLayerMask;`
    - `[SerializeField] private LayerMask _groundLayerMask;`
    - `private bool _isDead;`
  - **Signatures**:
    - `public event Action OnDied;`
    - `public void ResetHandler()`: Resets `_isDead = false`.
    - `private void OnCollisionEnter2D(Collision2D collision)`: Evaluates layer mask; if matching and `!_isDead`, sets `_isDead = true` and invokes `OnDied`.
  - **Constraints**: Subsequent collision events while `_isDead` is true must be immediately ignored.
  - **Acceptance Criteria**: Contact with obstacle or ground triggers `OnDied` exactly once per death.

- [x] 2.5 Implement Decoupled Player Visual Representation (`BirdView.cs`)
  - **Description**: Decouple visual sprite presentation from physics simulation by rotating the visual transform.
  - **File**: `Assets/Scripts/Player/BirdView.cs`
  - **Namespace**: `FlappyBird.Player`
  - **Signatures**:
    - `[SerializeField] private Transform _visualTransform;`
    - `[SerializeField] private BirdPhysics _birdPhysics;`
    - `public void UpdateRotation(float zDegrees)`: Sets `_visualTransform.localRotation = Quaternion.Euler(0, 0, zDegrees)`.
    - `private void OnEnable()`: Subscribes to `_birdPhysics.OnAngularRotationChanged`.
    - `private void OnDisable()`: Unsubscribes from `_birdPhysics.OnAngularRotationChanged`.
  - **Acceptance Criteria**: Bird visual rotates smoothly upward on flap (+30°) and dives nose-down (-90°) as downward velocity increases.

---

## Phase 3: Obstacle System & Pooling Infrastructure

- [x] 3.1 Implement Single-Entry Gap Score Trigger (`ScoreTrigger.cs`)
  - **Description**: Trigger one score increment when the player navigates through the pipe gap.
  - **File**: `Assets/Scripts/Obstacles/ScoreTrigger.cs`
  - **Namespace**: `FlappyBird.Obstacles`
  - **Fields**:
    - `[SerializeField] private Collider2D _triggerCollider;`
    - `private bool _isTriggered;`
  - **Signatures**:
    - `public event Action OnScored;`
    - `public void ResetTrigger()`: Sets `_isTriggered = false`, re-enables `_triggerCollider.enabled = true`.
    - `private void OnTriggerEnter2D(Collider2D other)`: Checks for player layer/tag; if `!_isTriggered`, sets `_isTriggered = true`, disables collider, and fires `OnScored`.
  - **Acceptance Criteria**: Passing through gap fires `OnScored` once; multiple triggers within the same pass are impossible.

- [x] 3.2 Implement Kinematic Horizontal Movement & Despawn Detection (`PipeMover.cs`)
  - **Description**: Move pipe pairs leftward at constant speed and trigger recycling upon crossing the despawn boundary.
  - **File**: `Assets/Scripts/Obstacles/PipeMover.cs`
  - **Namespace**: `FlappyBird.Obstacles`
  - **Fields**:
    - `[SerializeField] private float _scrollSpeed = 3.5f;`
    - `[SerializeField] private float _despawnX = -10.0f;`
    - `[SerializeField] private PipePair _parentPipe;`
    - `private bool _isScrolling;`
  - **Signatures**:
    - `public void SetScrolling(bool isScrolling)`
    - `private void FixedUpdate()`:
      - If `_isScrolling`: translates transform left by `_scrollSpeed * Time.fixedDeltaTime`.
      - If `transform.position.x <= _despawnX`: calls `_parentPipe.ReturnToPool()`.
  - **Acceptance Criteria**: Pipes scroll leftward at $3.5\,\text{m/s}$ in `FixedUpdate`; automatically return to pool past $-10.0\,\text{m}$.

- [x] 3.3 Implement Composite Pooled Obstacle Entity (`PipePair.cs`)
  - **Description**: Composite obstacle managing top/bottom pipe visuals, colliders, gap adjustment, and pooling lifecycle.
  - **File**: `Assets/Scripts/Obstacles/PipePair.cs`
  - **Namespace**: `FlappyBird.Obstacles`
  - **Interfaces**: `IPoolable`
  - **Fields**:
    - `[SerializeField] private Transform _topPipe;`
    - `[SerializeField] private Transform _bottomPipe;`
    - `[SerializeField] private ScoreTrigger _scoreTrigger;`
    - `[SerializeField] private PipeMover _mover;`
    - `private PipePool _originPool;`
  - **Signatures**:
    - `public void Initialize(PipePool pool)`
    - `public void SetGap(float centerY, float gapSize)`: Positions top pipe at `centerY + gapSize/2` and bottom pipe at `centerY - gapSize/2`.
    - `public void OnSpawn()`: Enables GameObject, calls `_scoreTrigger.ResetTrigger()`, starts mover scrolling.
    - `public void OnDespawn()`: Stops scrolling, deactivates GameObject.
    - `public void ReturnToPool()`: Calls `_originPool.Return(this)`.
  - **Acceptance Criteria**: Implements `IPoolable`; correctly positions top and bottom pipes relative to center Y and gap size.

- [x] 3.4 Implement Zero-Allocation Pre-Warmed Object Pool (`PipePool.cs`)
  - **Description**: Pre-warm and manage reusable `PipePair` instances without runtime `Instantiate` or `Destroy`.
  - **File**: `Assets/Scripts/Obstacles/PipePool.cs`
  - **Namespace**: `FlappyBird.Obstacles`
  - **Fields**:
    - `[SerializeField] private PipePair _prefab;`
    - `[SerializeField] private int _poolSize = 8;`
    - `[SerializeField] private Transform _poolRoot;`
    - `private Queue<PipePair> _inactiveQueue;`
    - `private List<PipePair> _activeList;`
  - **Signatures**:
    - `public void Initialize()`: Pre-allocates `_poolSize` instances under `_poolRoot`, deactivates them, and enqueues them.
    - `public PipePair Get()`: Dequeues instance, adds to `_activeList`, calls `OnSpawn()`, returns instance.
    - `public void Return(PipePair pipe)`: Calls `pipe.OnDespawn()`, removes from `_activeList`, enqueues to `_inactiveQueue`.
    - `public void ResetAll()`: Iterates `_activeList`, calls `OnDespawn()`, enqueues all to `_inactiveQueue`, clears `_activeList`.
  - **Acceptance Criteria**: Pre-warms 8 instances at initialization; zero runtime allocations during gameplay; `ResetAll()` cleanly reclaims all active obstacles.

- [x] 3.5 Implement Obstacle Spawner with Randomized Gap Offsets (`PipeSpawner.cs`)
  - **Description**: Control periodic obstacle spawning with randomized vertical positioning during `PlayingState`.
  - **File**: `Assets/Scripts/Obstacles/PipeSpawner.cs`
  - **Namespace**: `FlappyBird.Obstacles`
  - **Fields**:
    - `[SerializeField] private PipePool _pool;`
    - `[SerializeField] private float _spawnInterval = 1.6f;`
    - `[SerializeField] private float _spawnX = 10.0f;`
    - `[SerializeField] private float _minY = -1.5f;`
    - `[SerializeField] private float _maxY = 2.0f;`
    - `[SerializeField] private float _gapSize = 3.2f;`
    - `private float _timer;`
    - `private bool _isSpawning;`
  - **Signatures**:
    - `public void SetSpawning(bool isSpawning)`
    - `public void ResetSpawner()`: Resets `_timer = _spawnInterval`, `_isSpawning = false`.
    - `private void Update()`: If `_isSpawning`, decrements `_timer`; when `_timer <= 0`, calls `SpawnPipe()` and resets timer.
    - `private void SpawnPipe()`: Retrieves `PipePair` from `_pool`, calculates random Y between `_minY` and `_maxY`, positions at `(_spawnX, randomY, 0)`, calls `pipe.SetGap(randomY, _gapSize)`.
  - **Acceptance Criteria**: Spawns pipes at 1.6s interval only during `PlayingState`; vertical gap randomized within configured bounds.

---

## Phase 4: Game Loop & Scoring Domain

- [x] 4.1 Implement Score Domain & High Score Persistence (`ScoreManager.cs`)
  - **Description**: Maintain active run score, compare against high score, and persist high score in `PlayerPrefs`.
  - **File**: `Assets/Scripts/Core/ScoreManager.cs`
  - **Namespace**: `FlappyBird.Core`
  - **Fields**:
    - `private const string HIGH_SCORE_KEY = "FlappyBird_HighScore";`
    - `private int _currentScore;`
    - `private int _highScore;`
  - **Signatures**:
    - `public int CurrentScore => _currentScore;`
    - `public int HighScore => _highScore;`
    - `public event Action<int> OnScoreChanged;`
    - `public event Action<int, int, bool> OnGameOverScores;` (currentScore, highScore, isNewBest)
    - `public void Initialize()`: Reads `PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0)`.
    - `public void IncrementScore()`: Increments `_currentScore`, dispatches `OnScoreChanged(_currentScore)`.
    - `public void ResetScore()`: Sets `_currentScore = 0`, dispatches `OnScoreChanged(0)`.
    - `public void FinalizeScore()`: Checks if `_currentScore > _highScore`. If true, saves new high score to `PlayerPrefs.SetInt` and `PlayerPrefs.Save()`. Dispatches `OnGameOverScores`.
  - **Acceptance Criteria**: High score persists across sessions; dispatches score events; zero runtime allocations on increment.

- [x] 4.2 Implement FSM State Classes (`ReadyState`, `PlayingState`, `PausedState`, `GameOverState`)
  - **Description**: Implement isolated state behaviors adhering to `IGameState`.
  - **Files**:
    - `Assets/Scripts/Core/States/ReadyState.cs`
    - `Assets/Scripts/Core/States/PlayingState.cs`
    - `Assets/Scripts/Core/States/PausedState.cs`
    - `Assets/Scripts/Core/States/GameOverState.cs`
  - **Namespace**: `FlappyBird.Core.States`
  - **Behaviors**:
    - `ReadyState`: `Time.timeScale = 1.0f`; resets bird physics to $(-1.5, 0.0)$; freezes simulation; resets spawner; reclaims pool pipes; resets score.
    - `PlayingState`: `Time.timeScale = 1.0f`; activates bird physics simulation; applies first flap impulse; enables spawner.
    - `PausedState`: `Time.timeScale = 0.0f` on `Enter()`; restores `Time.timeScale = 1.0f` on `Exit()`.
    - `GameOverState`: Keeps `Time.timeScale = 1.0f`; disables flap input; stops pipe movers and spawner; calls `ScoreManager.FinalizeScore()`.
  - **Acceptance Criteria**: Clean state transitions with side effects strictly bounded to `Enter()` and `Exit()`.

- [x] 4.3 Implement Central Game Coordinator (`GameManager.cs`)
  - **Description**: Central state machine dispatcher and event coordinator with non-destructive restart capability.
  - **File**: `Assets/Scripts/Core/GameManager.cs`
  - **Namespace**: `FlappyBird.Core`
  - **Fields**:
    - `[SerializeField] private BirdPhysics _birdPhysics;`
    - `[SerializeField] private BirdCollisionHandler _collisionHandler;`
    - `[SerializeField] private BirdInputReader _inputReader;`
    - `[SerializeField] private PipeSpawner _spawner;`
    - `[SerializeField] private PipePool _pool;`
    - `[SerializeField] private ScoreManager _scoreManager;`
    - `private Dictionary<GameState, IGameState> _stateMap;`
    - `private IGameState _currentState;`
    - `private GameState _currentStateType;`
  - **Signatures**:
    - `public GameState CurrentState => _currentStateType;`
    - `public event Action<GameState> OnStateChanged;`
    - `public void ChangeState(GameState newState)`: Validates transition guards, calls `_currentState.Exit()`, updates state, calls `_currentState.Enter()`, fires `OnStateChanged`.
    - `public void StartGame()`: Transitions `Ready -> Playing`.
    - `public void PauseGame()`: Transitions `Playing -> Paused`.
    - `public void ResumeGame()`: Transitions `Paused -> Playing`.
    - `public void GameOver()`: Transitions `Playing -> GameOver`.
    - `public void ResetGame()`: Transitions to `ReadyState` without reloading the scene.
    - `public void QuitGame()`: Calls `Application.Quit()`.
  - **Acceptance Criteria**: Enforces state transition matrix; handles input/collision triggers; resets game cleanly without `SceneManager.LoadScene()`.

---

## Phase 5: UI Toolkit Presentation

- [x] 5.1 Author Responsive UI Toolkit Flexbox Stylesheet (`GameHUD.uss` / `FlappyBirdUI.uss`)
  - **Description**: Create the complete USS stylesheet for HUD score, ready prompt, pause modal, game over scoreboard, and buttons.
  - **File**: `Assets/UI/FlappyBirdUI.uss` (with optional alias `GameHUD.uss`)
  - **Layout & Selectors**:
    - `.root-container`: `width: 100%`, `height: 100%`, `position: absolute`.
    - `#hud-container`: `flex-direction: row`, `justify-content: center`, `align-items: flex-start`, `padding-top: 50px`.
    - `#hud-score`: Font size `72px`, color `#FFFFFF`, text-shadow / outline.
    - `#btn-pause`: `position: absolute`, `right: 30px`, `top: 40px`, `width: 80px`, `height: 80px`, rounded corners.
    - `.modal-backdrop`: `position: absolute`, `width: 100%`, `height: 100%`, `background-color: rgba(0, 0, 0, 0.65)`, `justify-content: center`, `align-items: center`.
    - `#panel-ready`, `#panel-pause`, `#panel-gameover`: Centered flexbox containers.
    - `.scoreboard`: Background `#DEB887` / card background, border radius `16px`, padding `24px`.
    - `.badge-new-best`: Gold / amber badge `#F1C40F`, uppercase bold label.
    - `.btn-action`: Min height `64px`, border radius `12px`, background `#2ECC71`, hover `#27AE60`.
  - **Acceptance Criteria**: Valid USS stylesheet with flexbox rules supporting responsive scaling without distortion.

- [x] 5.2 Author Semantic UI Toolkit Document Hierarchy (`GameHUD.uxml` / `FlappyBirdUI.uxml`)
  - **Description**: Construct the semantic UXML hierarchy containing HUD, Ready overlay, Pause modal, and Game Over modal.
  - **File**: `Assets/UI/FlappyBirdUI.uxml` (with optional alias `GameHUD.uxml`)
  - **Element Hierarchy**:
    ```xml
    <ui:UXML xmlns:ui="UnityEngine.UIElements">
      <ui:VisualElement name="root-container" class="root-container">
        <!-- HUD Layer -->
        <ui:VisualElement name="hud-container">
          <ui:Label name="hud-score" text="0" />
          <ui:Button name="btn-pause" text="||" />
        </ui:VisualElement>
        <!-- Ready Overlay -->
        <ui:VisualElement name="panel-ready" class="modal-backdrop">
          <ui:Label name="lbl-ready-title" text="GET READY" />
          <ui:Label name="lbl-ready-hint" text="TAP OR PRESS SPACE TO FLAP" />
        </ui:VisualElement>
        <!-- Pause Modal -->
        <ui:VisualElement name="panel-pause" class="modal-backdrop">
          <ui:Label name="lbl-pause-title" text="PAUSED" />
          <ui:Button name="btn-resume" text="RESUME" class="btn-action" />
          <ui:Button name="btn-restart-pause" text="RESTART" class="btn-action" />
          <ui:Button name="btn-quit" text="QUIT" class="btn-action" />
        </ui:VisualElement>
        <!-- Game Over Modal -->
        <ui:VisualElement name="panel-gameover" class="modal-backdrop">
          <ui:Label name="lbl-gameover-title" text="GAME OVER" />
          <ui:VisualElement name="box-scoreboard" class="scoreboard">
            <ui:Label name="lbl-final-score-title" text="SCORE" />
            <ui:Label name="lbl-final-score" text="0" />
            <ui:Label name="lbl-high-score-title" text="BEST" />
            <ui:Label name="lbl-high-score" text="0" />
            <ui:Label name="badge-new-best" text="NEW BEST!" class="badge-new-best" />
          </ui:VisualElement>
          <ui:Button name="btn-restart" text="PLAY AGAIN" class="btn-action" />
        </ui:VisualElement>
      </ui:VisualElement>
    </ui:UXML>
    ```
  - **Acceptance Criteria**: UXML compiles cleanly in Unity UI Toolkit; all visual elements have exact matching names.

- [x] 5.3 Configure UI Toolkit PanelSettings Asset (`FlappyBirdPanelSettings.asset`)
  - **Description**: Configure UI Toolkit panel settings to scale with screen size for standard 9:16 mobile/portrait aspect ratios.
  - **File**: `Assets/UI/FlappyBirdPanelSettings.asset`
  - **Configuration**:
    - `Scale Mode`: `Scale With Screen Size`
    - `Reference Resolution`: $1080 \times 1920$
    - `Screen Match Mode`: `Match Width Or Height`
    - `Match`: $0.5$ (or $0.0$ for width match)
  - **Acceptance Criteria**: PanelSettings asset created and configured; prevents UI clipping across varied viewport ratios.

- [x] 5.4 Implement Reactive UI Presenter (`GameUIPresenter.cs`)
  - **Description**: Query and cache all visual elements once in `Awake`/`OnEnable`, manage modal visibility, and handle user interactions.
  - **File**: `Assets/Scripts/UI/GameUIPresenter.cs`
  - **Namespace**: `FlappyBird.UI`
  - **Fields**:
    - `[SerializeField] private UIDocument _uiDocument;`
    - `[SerializeField] private GameManager _gameManager;`
    - `[SerializeField] private ScoreManager _scoreManager;`
    - Cached references: `_hudContainer`, `_hudScoreLabel`, `_btnPause`, `_panelReady`, `_panelPause`, `_btnResume`, `_btnRestartPause`, `_btnQuit`, `_panelGameOver`, `_lblFinalScore`, `_lblHighScore`, `_badgeNewBest`, `_btnRestart`.
  - **Signatures & Lifecycle**:
    - `private void Awake()` / `OnEnable()`: Caches all elements using `root.Q<T>()`. Subscribes button `clicked` events and domain events (`GameManager.OnStateChanged`, `ScoreManager.OnScoreChanged`, `ScoreManager.OnGameOverScores`).
    - `private void OnDisable()`: Unregisters all button `clicked` events and unhooks domain events.
    - `private void HandleStateChanged(GameState newState)`: Updates `style.display` (`Flex` or `None`) for Ready, HUD, Pause, and GameOver panels according to state.
    - `private void HandleScoreChanged(int score)`: Updates `_hudScoreLabel.text = IntToStringLookup.GetString(score)`.
    - `private void HandleGameOverScores(int finalScore, int highScore, bool isNewBest)`: Sets final and high score labels; toggles `_badgeNewBest.style.display`.
  - **Constraints**: Zero calls to `Q<T>()` or `Query<T>()` in `Update()` or event callbacks.
  - **Acceptance Criteria**: UI overlays toggle correctly based on game state; zero GC allocations on score updates; buttons dispatch to `GameManager`.

---

## Phase 6: Unity Scene Assembly & Collision Matrix

- [x] 6.1 Configure Project Tags, Physics Layers & Collision Matrix
  - **Description**: Configure 2D physics layers and collision matrix to prevent unnecessary physics checks.
  - **Actions**:
    - Add Tags: `Player`, `Obstacle`, `Ground`, `ScoreZone`.
    - Add Layers: `Player` (Layer 8), `Obstacle` (Layer 9), `Ground` (Layer 10).
    - Update `Physics2DSettings` Collision Matrix:
      - `Player` collides with: `Obstacle`, `Ground`.
      - `Obstacle` collides with: `Player` only (no collision with other Obstacles or Ground).
      - `Ground` collides with: `Player` only.
  - **Acceptance Criteria**: Physics 2D collision matrix configured to ignore obstacle-to-obstacle and obstacle-to-ground checks.

- [x] 6.2 Assemble Player Bird Prefab (`Bird.prefab`)
  - **Description**: Create the complete player prefab with geometric circle mockup visual and physics components.
  - **File**: `Assets/Prefabs/Bird.prefab`
  - **Components**:
    - `Transform`: Position $(-1.5, 0.0, 0.0)$
    - `Rigidbody2D`: `Collision Detection = Continuous`, `Interpolate = Interpolate`, `Constraints = Freeze Rotation Z`, `Gravity Scale = 2.5`
    - `CircleCollider2D`: `Radius = 0.4`, `isTrigger = false`
    - `BirdInputReader`: Action asset reference linked
    - `BirdPhysics`: `flapImpulse = 7.5`, `maxFallSpeed = 10.5`, `tiltSmoothing = 15.0`
    - `BirdCollisionHandler`: `_obstacleLayerMask = Obstacle`, `_groundLayerMask = Ground`
    - Child `BirdVisual`: `SpriteRenderer` (Circle primitive, Canary Yellow `#FFD700`), `BirdView`
  - **Acceptance Criteria**: Prefab saved with all serialized fields correctly assigned.

- [x] 6.3 Assemble Composite Pipe Obstacle Prefab (`PipePair.prefab`)
  - **Description**: Create the composite obstacle prefab with top and bottom pipes, caps, score trigger, and mover.
  - **File**: `Assets/Prefabs/PipePair.prefab`
  - **Components**:
    - Root: `PipePair`, `PipeMover` (`scrollSpeed = 3.5`, `despawnX = -10.0`)
    - Child `TopPipe`: `SpriteRenderer` (Box primitive, Emerald Green `#2ECC71`), `BoxCollider2D` (Layer `Obstacle`)
      - Child `PipeCap`: `SpriteRenderer` (Box primitive, Forest Green `#27AE60`)
    - Child `BottomPipe`: `SpriteRenderer` (Box primitive, Emerald Green `#2ECC71`), `BoxCollider2D` (Layer `Obstacle`)
      - Child `PipeCap`: `SpriteRenderer` (Box primitive, Forest Green `#27AE60`)
    - Child `ScoreZone`: `BoxCollider2D` (`isTrigger = true`, Size $(0.5, 3.2)$), `ScoreTrigger` (Layer `Default` or `ScoreZone`)
  - **Acceptance Criteria**: Prefab saved with `ScoreTrigger`, `PipeMover`, and `PipePair` references wired.

- [x] 6.4 Assemble Main Scene Hierarchy in `SampleScene.unity`
  - **Description**: Assemble camera, ground/ceiling boundaries, managers, bird instance, and UIDocument.
  - **File**: `Assets/Scenes/SampleScene.unity`
  - **Hierarchy Setup**:
    - `Main Camera`: Orthographic Size `5.0`, Clear Flags `Solid Color` (`#70C5CE`)
    - `Environment`:
      - `Ground`: Transform $(0, -4.5, 0)$, `SpriteRenderer` (Box, `#D35400`), `BoxCollider2D` (Layer `Ground`)
      - `Ceiling`: Transform $(0, 5.5, 0)$, `BoxCollider2D` (Layer `Ground`)
    - `Managers`:
      - `GameManager`: References wired to bird, spawner, pool, scoreManager
      - `ScoreManager`: Attached and wired
      - `PipePool`: `poolSize = 8`, `prefab = PipePair.prefab`
      - `PipeSpawner`: `spawnInterval = 1.6`, `spawnX = 10.0`, `minY = -1.5`, `maxY = 2.0`
    - `Bird`: Instance of `Bird.prefab` at $(-1.5, 0.0, 0.0)$
    - `UI`:
      - `UIDocument`: `panelSettings = FlappyBirdPanelSettings.asset`, `visualTreeAsset = FlappyBirdUI.uxml`
      - `GameUIPresenter`: Attached and wired to `GameManager`, `ScoreManager`, and `UIDocument`
  - **Acceptance Criteria**: Scene is fully assembled; entering Play Mode shows Ready screen with bird resting at $(-1.5, 0.0)$.

---

## Phase 7: Verification & Quality Assurance

- [ ] 7.1 Code Compilation & Static Analysis Validation
  - **Description**: Trigger Unity asset recompilation and verify zero compiler errors, obsolete API warnings, or unassigned serialized references.
  - **Tools**: Unity Editor recompilation / MCP console log check.
  - **Pass Criteria**: `check_compile_errors` reports 0 errors and 0 critical warnings.

- [ ] 7.2 State Machine & Gameplay Loop Playmode Verification
  - **Description**: Validate complete end-to-end game lifecycle in Play Mode.
  - **Test Matrix**:
    - Ready State: Bird suspended at $(-1.5, 0.0)$, Ready overlay visible.
    - Flap Input: Space / Click transitions to Playing; bird applies initial impulse; pipes begin spawning.
    - Pause Input: Escape / Pause button transitions to Paused; `Time.timeScale` becomes `0`; Pause modal appears; Resume restores gameplay.
    - Collision & Game Over: Hitting pipe or ground halts motion; Game Over modal displays run score and persistent high score; New Best badge displays if record beaten.
    - Restart Action: Play Again button resets bird, score, and pools without reloading scene.
  - **Pass Criteria**: All state transitions succeed without null references or state deadlocks.

- [ ] 7.3 Physics & Input Determinism Verification
  - **Description**: Verify physical simulation bounds and continuous collision safety.
  - **Checks**:
    - Bird vertical fall speed is strictly clamped at $-10.5\,\text{m/s}$.
    - No physics tunneling through ground or ceiling boundaries occurs at high speeds.
    - Multiple rapid inputs within a single physics timestep ($0.02\,\text{s}$) coalesce into a single flap impulse.
    - Bird visual tilts predictably between $+30^\circ$ and $-90^\circ$.
  - **Pass Criteria**: Physics behavior remains smooth, predictable, and arcade-authentic.

- [ ] 7.4 Zero-GC Profiler Audit
  - **Description**: Profile active gameplay using the Unity Profiler over a continuous 60-second session.
  - **Target Metrics**:
    - Steady-state GC allocation in `Update()`: **0 Bytes**.
    - Steady-state GC allocation in `FixedUpdate()`: **0 Bytes**.
    - Score increment event handling via `IntToStringLookup`: **0 Bytes**.
    - Pipe spawn and return via `PipePool`: **0 Bytes**.
  - **Pass Criteria**: Zero GC allocation spikes during active flight, pipe scrolling, and scoring.
