using System;
using FlappyBird.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlappyBird.Player
{
    /// <summary>
    /// Event-driven adapter for the Unity Input System.
    /// Subscribes to InputAction callbacks asynchronously with zero polling or heap allocations in Update().
    /// Sets an atomic pending flag for the physics simulation step.
    /// </summary>
    public class BirdInputReader : MonoBehaviour
    {
        [Header("Input Asset Configuration")]
        [Tooltip("Optional reference to Input Actions asset. Falls back to default bindings if unassigned.")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Gameplay";
        [SerializeField] private string _flapActionName = "Flap";
        [SerializeField] private string _pauseActionName = "Pause";

        [Header("Direct Action References (Optional Override)")]
        [SerializeField] private InputActionReference _flapActionRef;
        [SerializeField] private InputActionReference _pauseActionRef;

        private InputAction _flapAction;
        private InputAction _pauseAction;
        private bool _isLocalActionMap;

        // Atomic pending flag consumed by BirdPhysics in FixedUpdate()
        private volatile bool _isFlapPending;

        /// <summary>
        /// Indicates whether a flap input has been received and is waiting to be processed by FixedUpdate.
        /// </summary>
        public bool IsFlapPending
        {
            get => _isFlapPending;
            set => _isFlapPending = value;
        }

        /// <summary>
        /// Local event fired immediately when a flap input is detected.
        /// </summary>
        public event Action OnFlapRequested;

        /// <summary>
        /// Local event fired immediately when a pause input is detected.
        /// </summary>
        public event Action OnPauseRequested;

        private void Awake()
        {
            InitializeActions();
        }

        private void InitializeActions()
        {
            if (_flapAction != null) return;

            if (_flapActionRef != null)
            {
                _flapAction = _flapActionRef.action;
            }
            if (_pauseActionRef != null)
            {
                _pauseAction = _pauseActionRef.action;
            }

            if (_flapAction == null && _inputActions != null)
            {
                var map = _inputActions.FindActionMap(_actionMapName, false) ?? _inputActions.FindActionMap("Player", false);
                if (map != null)
                {
                    _flapAction = map.FindAction(_flapActionName, false) ?? map.FindAction("Jump", false);
                    _pauseAction = map.FindAction(_pauseActionName, false);
                }
            }

            // Fallback: create standalone actions programmatically to ensure zero setup failure
            if (_flapAction == null)
            {
                _isLocalActionMap = true;
                _flapAction = new InputAction("Flap", InputActionType.Button);
                _flapAction.AddBinding("<Keyboard>/space");
                _flapAction.AddBinding("<Mouse>/leftButton");
                _flapAction.AddBinding("<Touchscreen>/primaryTouch/tap");
            }

            if (_pauseAction == null)
            {
                _isLocalActionMap = true;
                _pauseAction = new InputAction("Pause", InputActionType.Button);
                _pauseAction.AddBinding("<Keyboard>/escape");
            }
        }

        private void OnEnable()
        {
            if (_flapAction == null)
            {
                InitializeActions();
            }

            if (_flapAction != null)
            {
                _flapAction.performed += HandleFlapPerformed;
                _flapAction.Enable();
            }

            if (_pauseAction != null)
            {
                _pauseAction.performed += HandlePausePerformed;
                _pauseAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (_flapAction != null)
            {
                _flapAction.performed -= HandleFlapPerformed;
                _flapAction.Disable();
            }

            if (_pauseAction != null)
            {
                _pauseAction.performed -= HandlePausePerformed;
                _pauseAction.Disable();
            }
        }

        private void OnDestroy()
        {
            if (_isLocalActionMap)
            {
                _flapAction?.Dispose();
                _pauseAction?.Dispose();
            }
        }

        private void HandleFlapPerformed(InputAction.CallbackContext context)
        {
            _isFlapPending = true;
            OnFlapRequested?.Invoke();
            GameEvents.TriggerFlapRequested();
        }

        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            OnPauseRequested?.Invoke();
            GameEvents.TriggerPauseRequested();
        }

        /// <summary>
        /// Consumes and resets the pending flap flag atomically.
        /// </summary>
        /// <returns>True if a flap was pending, false otherwise.</returns>
        public bool ConsumeFlapPending()
        {
            if (_isFlapPending)
            {
                _isFlapPending = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears any pending input state.
        /// </summary>
        public void ResetInput()
        {
            _isFlapPending = false;
        }
    }
}
