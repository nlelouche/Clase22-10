using System;
using UnityEngine;

namespace FlappyBird.Player
{
    /// <summary>
    /// Deterministic 2D physics simulation for the bird entity.
    /// Handles flap impulses, terminal fall velocity clamping, and procedural tilt calculation.
    /// Operates strictly within FixedUpdate() with 0 bytes GC allocation per tick.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BirdPhysics : MonoBehaviour
    {
        [Header("Physics Tuning")]
        [Tooltip("Upward impulse force applied on flap.")]
        [SerializeField] private float _flapImpulse = 7.5f;

        [Tooltip("Maximum allowed downward vertical velocity in m/s (terminal velocity).")]
        [SerializeField] private float _maxFallSpeed = 10.5f;

        [Header("Procedural Rotation")]
        [Tooltip("Max upward pitch angle (degrees) when flapping/climbing.")]
        [SerializeField] private float _maxUpwardAngle = 30f;

        [Tooltip("Max downward pitch angle (degrees) when diving/falling.")]
        [SerializeField] private float _maxDownwardAngle = -90f;

        [Tooltip("Smoothing factor for angular interpolation.")]
        [SerializeField] private float _tiltSmoothing = 15f;

        [Header("Optional Dependencies")]
        [SerializeField] private BirdInputReader _inputReader;

        private Rigidbody2D _rb;
        private Transform _cachedTransform;
        private volatile bool _isFlapPending;
        private float _currentAngle;

        /// <summary>
        /// Local event fired each physics step with the updated Z rotation in degrees.
        /// Decoupled view components subscribe to this event.
        /// </summary>
        public event Action<float> OnAngularRotationChanged;

#if UNITY_6000_0_OR_NEWER
        private Vector2 LinearVelocity
        {
            get => _rb.linearVelocity;
            set => _rb.linearVelocity = value;
        }
#else
        private Vector2 LinearVelocity
        {
            get => _rb.velocity;
            set => _rb.velocity = value;
        }
#endif

        private void Awake()
        {
            _cachedTransform = transform;
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
            }

            if (_inputReader == null)
            {
                _inputReader = GetComponent<BirdInputReader>();
            }
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnFlapRequested += RequestFlap;
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnFlapRequested -= RequestFlap;
            }
        }

        /// <summary>
        /// Explicit initialization hook for dependency injection or dynamic instantiation.
        /// </summary>
        /// <param name="rb">The Rigidbody2D component to simulate.</param>
        public void Initialize(Rigidbody2D rb)
        {
            //mnjnjklknlkmlknk
            _rb = rb;
        }

        /// <summary>
        /// Flags a flap impulse to be applied in the next FixedUpdate tick.
        /// Thread-safe and frame-rate independent.
        /// </summary>
        public void RequestFlap()
        {
            _isFlapPending = true;
        }

        /// <summary>
        /// Enables or disables physical simulation.
        /// Used by ReadyState and PausedState to suspend gravity and motion.
        /// </summary>
        /// <param name="isSimulated">True to enable Rigidbody2D simulation, false to freeze.</param>
        public void SetSimulated(bool isSimulated)
        {
            if (_rb != null)
            {
                _rb.simulated = isSimulated;
            }
        }

        /// <summary>
        /// Resets position, zeroes linear and angular velocities, and restores idle rotation.
        /// </summary>
        /// <param name="startPosition">Coordinates to position the bird at.</param>
        public void ResetPhysics(Vector2 startPosition)
        {
            _cachedTransform.position = new Vector3(startPosition.x, startPosition.y, _cachedTransform.position.z);

            if (_rb != null)
            {
                LinearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            _isFlapPending = false;
            _currentAngle = 0f;
            OnAngularRotationChanged?.Invoke(0f);
        }

        private void FixedUpdate()
        {
            if (_rb == null || !_rb.simulated) return;

            // Check internal pending flag or query input reader atomically
            bool hasPendingFlap = _isFlapPending;
            if (_inputReader != null && _inputReader.ConsumeFlapPending())
            {
                hasPendingFlap = true;
            }

            if (hasPendingFlap)
            {
                _isFlapPending = false;
                ApplyFlapImpulse();
            }

            ClampVelocity();
            UpdateProceduralRotation();
        }

        private void ApplyFlapImpulse()
        {
            // Cancel existing downward momentum for crisp arcade responsiveness
            Vector2 vel = LinearVelocity;
            vel.y = 0f;
            LinearVelocity = vel;

            _rb.AddForce(Vector2.up * _flapImpulse, ForceMode2D.Impulse);
        }

        private void ClampVelocity()
        {
            Vector2 vel = LinearVelocity;
            if (vel.y < -_maxFallSpeed)
            {
                vel.y = -_maxFallSpeed;
                LinearVelocity = vel;
            }
        }

        private void UpdateProceduralRotation()
        {
            float velY = LinearVelocity.y;
            float targetAngle;

            if (velY > 0f)
            {
                // Climbing: interpolate upward angle based on current climb speed
                float t = Mathf.Clamp01(velY / _flapImpulse);
                targetAngle = Mathf.Lerp(0f, _maxUpwardAngle, t);
            }
            else
            {
                // Falling: interpolate downward angle based on fall ratio
                float t = Mathf.Clamp01(-velY / _maxFallSpeed);
                targetAngle = Mathf.Lerp(0f, _maxDownwardAngle, t);
            }

            _currentAngle = Mathf.Lerp(_currentAngle, targetAngle, _tiltSmoothing * Time.fixedDeltaTime);
            OnAngularRotationChanged?.Invoke(_currentAngle);
        }
    }
}
