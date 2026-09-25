using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class PlayerControlBlocker : MonoBehaviour
    {
        [SerializeField] private bool controlsActive = true;

        private StarterAssetsInputs _inputs;
        private Animator _animator;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        private AnimatorUpdateMode _originalUpdateMode;

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
            _animator = GetComponent<Animator>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
        }

        private void Start()
        {
            //SetControlsActive(controlsActive);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                SetControlsActive(controlsActive);
            }
        }

        public void DisableControls()
        {
            controlsActive = false;

#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null) _playerInput.enabled = false;
#endif

            if (_inputs != null)
            {
                _inputs.MoveInput(Vector2.zero);
                _inputs.LookInput(Vector2.zero);
                _inputs.JumpInput(false);
                _inputs.SprintInput(false);
            }

            if (_animator != null)
            {
                _animator.SetFloat("Speed", 0f);
                _animator.SetFloat("MotionSpeed", 0f);

                _originalUpdateMode = _animator.updateMode;
                _animator.updateMode = AnimatorUpdateMode.Fixed;
            }
        }

        public void EnableControls()
        {
            controlsActive = true;

#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null) _playerInput.enabled = true;
#endif

            if (_animator != null)
            {
                _animator.updateMode = _originalUpdateMode;
            }
        }

        public void SetControlsActive(bool active)
        {
            if (active) EnableControls();
            else DisableControls();
        }
    }
}