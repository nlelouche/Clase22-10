using FlappyBird.Core;
using UnityEngine;

namespace FlappyBird.Obstacles
{
    /// <summary>
    /// Composite pooled obstacle entity managing top and bottom pipe segments,
    /// dynamic gap positioning, score trigger lifecycle, and horizontal movement.
    /// Implements IPoolable to support zero-allocation object recycling.
    /// </summary>
    public class PipePair : MonoBehaviour, IPoolable
    {
        [Header("Child Transforms")]
        [Tooltip("Transform of the top pipe segment.")]
        [SerializeField] private Transform _topPipe;

        [Tooltip("Transform of the bottom pipe segment.")]
        [SerializeField] private Transform _bottomPipe;

        [Header("Components")]
        [Tooltip("Score detection trigger located in the gap between pipes.")]
        [SerializeField] private ScoreTrigger _scoreTrigger;

        [Tooltip("Kinematic horizontal mover component.")]
        [SerializeField] private PipeMover _mover;

        private PipePool _originPool;

        /// <summary>
        /// Gets the child score trigger component.
        /// </summary>
        public ScoreTrigger ScoreTrigger => _scoreTrigger;

        /// <summary>
        /// Gets the horizontal mover component.
        /// </summary>
        public PipeMover Mover => _mover;

        private void Awake()
        {
            if (_mover == null)
            {
                _mover = GetComponent<PipeMover>();
            }

            if (_scoreTrigger == null)
            {
                _scoreTrigger = GetComponentInChildren<ScoreTrigger>();
            }

            if (_mover != null)
            {
                _mover.Initialize(this);
            }
        }

        /// <summary>
        /// Links this pipe pair to its origin pool for recycling.
        /// </summary>
        /// <param name="pool">The managing PipePool instance.</param>
        public void Initialize(PipePool pool)
        {
            _originPool = pool;

            if (_mover == null)
            {
                _mover = GetComponent<PipeMover>();
            }

            if (_mover != null)
            {
                _mover.Initialize(this);
            }
        }

        /// <summary>
        /// Configures the vertical center position and gap clearance between top and bottom pipes.
        /// </summary>
        /// <param name="centerY">World or local Y coordinate for the center of the gap.</param>
        /// <param name="gapSize">Total vertical distance between top and bottom pipe openings.</param>
        public void SetGap(float centerY, float gapSize)
        {
            // Position root at target vertical center
            Vector3 pos = transform.position;
            pos.y = centerY;
            transform.position = pos;

            float halfGap = gapSize * 0.5f;

            if (_topPipe != null)
            {
                _topPipe.localPosition = new Vector3(0f, halfGap, 0f);
            }

            if (_bottomPipe != null)
            {
                _bottomPipe.localPosition = new Vector3(0f, -halfGap, 0f);
            }

            if (_scoreTrigger != null)
            {
                _scoreTrigger.transform.localPosition = Vector3.zero;

                var box = _scoreTrigger.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size = new Vector2(box.size.x > 0f ? box.size.x : 0.5f, gapSize);
                }
            }
        }

        /// <summary>
        /// Invoked when retrieved from PipePool. Activates GameObject, resets trigger, and starts movement.
        /// </summary>
        public void OnSpawn()
        {
            gameObject.SetActive(true);

            if (_scoreTrigger != null)
            {
                _scoreTrigger.ResetTrigger();
            }

            if (_mover != null)
            {
                _mover.SetScrolling(true);
            }
        }

        /// <summary>
        /// Invoked when returned to PipePool. Stops movement and deactivates GameObject.
        /// </summary>
        public void OnDespawn()
        {
            if (_mover != null)
            {
                _mover.SetScrolling(false);
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Reclaims this obstacle instance back into the origin pool.
        /// </summary>
        public void ReturnToPool()
        {
            if (_originPool != null)
            {
                _originPool.Return(this);
            }
            else
            {
                OnDespawn();
            }
        }
    }
}
