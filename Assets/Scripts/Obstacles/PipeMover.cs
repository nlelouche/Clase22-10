using UnityEngine;

namespace FlappyBird.Obstacles
{
    /// <summary>
    /// Translates obstacle pipe pairs leftward at constant speed in FixedUpdate.
    /// Detects when the obstacle crosses the leftward despawn threshold and returns it to the pool.
    /// </summary>
    public class PipeMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal leftward scroll velocity in meters/second.")]
        [SerializeField] private float _scrollSpeed = 3.5f;

        [Tooltip("Horizontal X boundary threshold where the pipe pair is recycled back into the pool.")]
        [SerializeField] private float _despawnX = -10.0f;

        [Header("Parent References")]
        [Tooltip("Reference to the parent composite pipe pair.")]
        [SerializeField] private PipePair _parentPipe;

        private bool _isScrolling;

        /// <summary>
        /// Gets or sets the horizontal scroll speed in units/second.
        /// </summary>
        public float ScrollSpeed
        {
            get => _scrollSpeed;
            set => _scrollSpeed = value;
        }

        /// <summary>
        /// Gets or sets the despawn X boundary.
        /// </summary>
        public float DespawnX
        {
            get => _despawnX;
            set => _despawnX = value;
        }

        /// <summary>
        /// Indicates if horizontal scrolling is currently active.
        /// </summary>
        public bool IsScrolling => _isScrolling;

        private void Awake()
        {
            if (_parentPipe == null)
            {
                _parentPipe = GetComponent<PipePair>();
            }
        }

        /// <summary>
        /// Links this mover to its managing composite PipePair.
        /// </summary>
        /// <param name="parentPipe">Parent PipePair entity.</param>
        public void Initialize(PipePair parentPipe)
        {
            _parentPipe = parentPipe;
        }

        /// <summary>
        /// Enables or pauses horizontal scrolling translation.
        /// </summary>
        /// <param name="isScrolling">True to move leftward, false to halt.</param>
        public void SetScrolling(bool isScrolling)
        {
            _isScrolling = isScrolling;
        }

        private void FixedUpdate()
        {
            if (!_isScrolling) return;

            // Zero-allocation kinematic translation in FixedUpdate
            transform.position += Vector3.left * (_scrollSpeed * Time.fixedDeltaTime);

            // Check if boundary crossed
            if (transform.position.x <= _despawnX)
            {
                _isScrolling = false;
                if (_parentPipe != null)
                {
                    _parentPipe.ReturnToPool();
                }
            }
        }
    }
}
