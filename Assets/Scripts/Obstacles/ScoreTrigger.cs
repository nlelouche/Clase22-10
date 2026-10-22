using System;
using FlappyBird.Core;
using UnityEngine;

namespace FlappyBird.Obstacles
{
    /// <summary>
    /// Detects when the player navigates through the obstacle gap.
    /// Ensures atomic one-shot score increment dispatch and instantly disables its
    /// collider to prevent duplicate triggers during the same pass.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ScoreTrigger : MonoBehaviour
    {
        [Header("Collider Configuration")]
        [Tooltip("The trigger collider detecting player entry.")]
        [SerializeField] private Collider2D _triggerCollider;

        [Header("Layer & Tag Filtering")]
        [Tooltip("Layer mask representing the player bird.")]
        [SerializeField] private LayerMask _playerLayerMask;

        [Tooltip("Tag fallback representing the player bird.")]
        [SerializeField] private string _playerTag = "Player";

        private bool _isTriggered;

        /// <summary>
        /// Local event dispatched once when the player successfully enters the score gap.
        /// </summary>
        public event Action OnScored;

        /// <summary>
        /// Indicates whether this trigger has already dispatched a score event for the current pass.
        /// </summary>
        public bool IsTriggered => _isTriggered;

        private void Awake()
        {
            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<Collider2D>();
            }

            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
        }

        /// <summary>
        /// Resets the trigger flag and re-enables the collider when the pipe pair is spawned from the pool.
        /// </summary>
        public void ResetTrigger()
        {
            _isTriggered = false;

            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<Collider2D>();
            }

            if (_triggerCollider != null)
            {
                _triggerCollider.enabled = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered) return;

            if (IsPlayer(other.gameObject))
            {
                _isTriggered = true;

                // Instantly disable collider to prevent any multi-trigger within the same physics step
                if (_triggerCollider != null)
                {
                    _triggerCollider.enabled = false;
                }

                OnScored?.Invoke();
                GameEvents.TriggerScoreIncremented();
            }
        }

        private bool IsPlayer(GameObject target)
        {
            int targetLayerBit = 1 << target.layer;

            // Check layer mask if configured
            if (_playerLayerMask.value != 0 && (_playerLayerMask.value & targetLayerBit) != 0)
            {
                return true;
            }

            // Tag check fallback
            if (!string.IsNullOrEmpty(_playerTag) && target.CompareTag(_playerTag))
            {
                return true;
            }

            return false;
        }
    }
}
