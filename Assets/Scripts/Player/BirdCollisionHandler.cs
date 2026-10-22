using System;
using FlappyBird.Core;
using UnityEngine;

namespace FlappyBird.Player
{
    /// <summary>
    /// Detects fatal collisions between the bird and obstacles/boundaries.
    /// Filters collisions via LayerMask and Tag, guarantees atomic one-shot death dispatch,
    /// and immediately disables itself to prevent duplicate events.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BirdCollisionHandler : MonoBehaviour
    {
        [Header("Layer Collision Filters")]
        [Tooltip("Layer mask representing deadly obstacle pipes.")]
        [SerializeField] private LayerMask _obstacleLayerMask;

        [Tooltip("Layer mask representing deadly ground or ceiling boundaries.")]
        [SerializeField] private LayerMask _groundLayerMask;

        [Header("Tag Collision Fallbacks")]
        [SerializeField] private string _obstacleTag = "Obstacle";
        [SerializeField] private string _groundTag = "Ground";

        private bool _isDead;

        /// <summary>
        /// Local event dispatched upon the first fatal collision.
        /// </summary>
        public event Action OnDied;

        /// <summary>
        /// Indicates if the bird has sustained a fatal collision.
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// Resets the death state and re-enables collision monitoring for a new game run.
        /// </summary>
        public void ResetHandler()
        {
            _isDead = false;
            enabled = true;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDead) return;

            if (IsFatalCollision(collision.gameObject))
            {
                TriggerDeath();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDead) return;

            // Ignore score trigger zones
            if (other.isTrigger && other.CompareTag("ScoreZone"))
            {
                return;
            }

            if (IsFatalCollision(other.gameObject))
            {
                TriggerDeath();
            }
        }

        private bool IsFatalCollision(GameObject target)
        {
            int targetLayerBit = 1 << target.layer;

            // Check layer masks
            if ((_obstacleLayerMask.value & targetLayerBit) != 0 ||
                (_groundLayerMask.value & targetLayerBit) != 0)
            {
                return true;
            }

            // Fallback: check tags if layer masks are unconfigured
            if (!string.IsNullOrEmpty(_obstacleTag) && target.CompareTag(_obstacleTag))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(_groundTag) && target.CompareTag(_groundTag))
            {
                return true;
            }

            return false;
        }

        private void TriggerDeath()
        {
            _isDead = true;
            enabled = false;

            OnDied?.Invoke();
            GameEvents.TriggerPlayerDied();
        }
    }
}
