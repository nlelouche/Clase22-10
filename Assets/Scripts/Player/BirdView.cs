using UnityEngine;

namespace FlappyBird.Player
{
    /// <summary>
    /// Decoupled presentation component for the player bird.
    /// Manages visual orientation by reacting to angular updates from BirdPhysics.
    /// Operates with 0 bytes GC allocation.
    /// </summary>
    public class BirdView : MonoBehaviour
    {
        [Header("Transform References")]
        [Tooltip("The visual child transform to rotate. If unassigned, defaults to this transform.")]
        [SerializeField] private Transform _visualTransform;

        [Tooltip("Physics simulation component to listen to for angular updates.")]
        [SerializeField] private BirdPhysics _birdPhysics;

        private Transform _cachedTransform;
        private Rigidbody2D _cachedRb;

        private void Awake()
        {
            _cachedTransform = transform;
            if (_visualTransform == null)
            {
                _visualTransform = _cachedTransform;
            }

            if (_birdPhysics == null)
            {
                _birdPhysics = GetComponentInParent<BirdPhysics>() ?? GetComponent<BirdPhysics>();
            }

            _cachedRb = GetComponentInParent<Rigidbody2D>() ?? GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            if (_birdPhysics != null)
            {
                _birdPhysics.OnAngularRotationChanged += HandleRotationChanged;
            }
        }

        private void OnDisable()
        {
            if (_birdPhysics != null)
            {
                _birdPhysics.OnAngularRotationChanged -= HandleRotationChanged;
            }
        }

        /// <summary>
        /// Explicitly updates the visual transform rotation around the Z-axis.
        /// </summary>
        /// <param name="zDegrees">Target Z-axis angle in degrees.</param>
        public void UpdateRotation(float zDegrees)
        {
            if (_visualTransform != null)
            {
                _visualTransform.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
            }
        }

        private void HandleRotationChanged(float zDegrees)
        {
            UpdateRotation(zDegrees);
        }
    }
}
