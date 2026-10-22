using System.Collections.Generic;
using UnityEngine;

namespace FlappyBird.Obstacles
{
    /// <summary>
    /// Zero-allocation, pre-warmed object pool managing reusable PipePair instances.
    /// Eliminates runtime Instantiate and Destroy calls to guarantee zero-GC steady state.
    /// </summary>
    public class PipePool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [Tooltip("Prefab instantiated to populate the pool.")]
        [SerializeField] private PipePair _prefab;

        [Tooltip("Pre-warm capacity for obstacle instances.")]
        [SerializeField] private int _poolSize = 8;

        [Tooltip("Parent transform container holding pooled inactive entities.")]
        [SerializeField] private Transform _poolRoot;

        private Queue<PipePair> _inactiveQueue;
        private List<PipePair> _activeList;
        private bool _isInitialized;

        /// <summary>
        /// Total pre-warmed pool capacity.
        /// </summary>
        public int PoolSize => _poolSize;

        /// <summary>
        /// Count of currently inactive/available pipe pairs in the pool.
        /// </summary>
        public int InactiveCount => _inactiveQueue != null ? _inactiveQueue.Count : 0;

        /// <summary>
        /// Count of currently active pipe pairs in the scene.
        /// </summary>
        public int ActiveCount => _activeList != null ? _activeList.Count : 0;

        private void Awake()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Pre-warms the pool with PipePair instances up to poolSize.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_poolRoot == null)
            {
                var container = new GameObject("PoolContainer");
                container.transform.SetParent(transform, false);
                _poolRoot = container.transform;
            }

            _inactiveQueue = new Queue<PipePair>(_poolSize);
            _activeList = new List<PipePair>(_poolSize);

            if (_prefab != null)
            {
                for (int i = 0; i < _poolSize; i++)
                {
                    PipePair instance = Instantiate(_prefab, _poolRoot);
                    instance.Initialize(this);
                    instance.OnDespawn();
                    _inactiveQueue.Enqueue(instance);
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Retrieves an inactive PipePair from the pool, moves it to the active list, and activates it.
        /// </summary>
        /// <returns>Ready PipePair instance, or null if uninstantiated.</returns>
        public PipePair Get()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            PipePair pipe;

            if (_inactiveQueue.Count > 0)
            {
                pipe = _inactiveQueue.Dequeue();
            }
            else
            {
                Debug.LogWarning("[PipePool] Pool capacity exhausted! Instantiating emergency instance.");
                pipe = Instantiate(_prefab, _poolRoot);
                pipe.Initialize(this);
            }

            _activeList.Add(pipe);
            pipe.OnSpawn();
            return pipe;
        }

        /// <summary>
        /// Reclaims an active PipePair back into the pool, invokes OnDespawn, and enqueues it.
        /// </summary>
        /// <param name="pipe">The PipePair to return.</param>
        public void Return(PipePair pipe)
        {
            if (pipe == null) return;

            pipe.OnDespawn();
            _activeList.Remove(pipe);

            if (!_inactiveQueue.Contains(pipe))
            {
                _inactiveQueue.Enqueue(pipe);
            }
        }

        /// <summary>
        /// Reclaims all currently active pipes back to the pool without destroying GameObjects.
        /// Essential for non-destructive game restart.
        /// </summary>
        public void ResetAll()
        {
            if (_activeList == null) return;

            for (int i = 0; i < _activeList.Count; i++)
            {
                PipePair pipe = _activeList[i];
                if (pipe != null)
                {
                    pipe.OnDespawn();
                    if (!_inactiveQueue.Contains(pipe))
                    {
                        _inactiveQueue.Enqueue(pipe);
                    }
                }
            }

            _activeList.Clear();
        }
    }
}
