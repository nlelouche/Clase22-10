using UnityEngine;

namespace FlappyBird.Obstacles
{
    /// <summary>
    /// Periodic obstacle spawner that retrieves PipePair entities from PipePool,
    /// randomizes their vertical gap offsets, and initializes their movement.
    /// Only active during PlayingState.
    /// </summary>
    public class PipeSpawner : MonoBehaviour
    {
        [Header("Pool Reference")]
        [Tooltip("The pool supplying pre-warmed obstacle pairs.")]
        [SerializeField] private PipePool _pool;

        [Header("Timing")]
        [Tooltip("Interval in seconds between consecutive pipe pair spawns.")]
        [SerializeField] private float _spawnInterval = 1.6f;

        [Header("Placement Configuration")]
        [Tooltip("World X coordinate where pipes spawn.")]
        [SerializeField] private float _spawnX = 10.0f;

        [Tooltip("Minimum vertical Y offset for the gap center.")]
        [SerializeField] private float _minY = -1.5f;

        [Tooltip("Maximum vertical Y offset for the gap center.")]
        [SerializeField] private float _maxY = 2.0f;

        [Tooltip("Vertical gap clearance between top and bottom pipe segments.")]
        [SerializeField] private float _gapSize = 3.2f;

        private float _timer;
        private bool _isSpawning;

        /// <summary>
        /// Indicates if the spawner is actively counting down and spawning obstacles.
        /// </summary>
        public bool IsSpawning => _isSpawning;

        /// <summary>
        /// Gets or sets the spawn cadence in seconds.
        /// </summary>
        public float SpawnInterval
        {
            get => _spawnInterval;
            set => _spawnInterval = value;
        }

        /// <summary>
        /// Gets or sets the vertical opening gap height.
        /// </summary>
        public float GapSize
        {
            get => _gapSize;
            set => _gapSize = value;
        }

        private void Awake()
        {
            if (_pool == null)
            {
                _pool = GetComponent<PipePool>();
                if (_pool == null)
                {
#if UNITY_2023_1_OR_NEWER
                    _pool = FindAnyObjectByType<PipePool>();
#else
                    _pool = FindObjectOfType<PipePool>();
#endif
                }
            }

            _timer = _spawnInterval;
        }

        /// <summary>
        /// Enables or pauses the spawning cycle.
        /// </summary>
        /// <param name="isSpawning">True to spawn pipes periodically, false to suspend.</param>
        public void SetSpawning(bool isSpawning)
        {
            _isSpawning = isSpawning;
        }

        /// <summary>
        /// Halts spawning and resets countdown timer to initial interval.
        /// </summary>
        public void ResetSpawner()
        {
            _timer = _spawnInterval;
            _isSpawning = false;
        }

        private void Update()
        {
            if (!_isSpawning) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                SpawnPipe();
                _timer = _spawnInterval;
            }
        }

        private void SpawnPipe()
        {
            if (_pool == null)
            {
                Debug.LogWarning("[PipeSpawner] PipePool reference is missing!");
                return;
            }

            PipePair pipe = _pool.Get();
            if (pipe == null) return;

            float randomY = Random.Range(_minY, _maxY);
            pipe.transform.position = new Vector3(_spawnX, randomY, 0f);
            pipe.SetGap(randomY, _gapSize);
        }
    }
}
