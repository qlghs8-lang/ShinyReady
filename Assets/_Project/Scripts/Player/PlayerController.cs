using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ShinyReady.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 7f;
        [SerializeField] private float _rotationSpeed = 10f;

        [Header("Joystick UI")]
        [SerializeField] private RectTransform _joystickContainer;
        [SerializeField] private RectTransform _joystickBackground;
        [SerializeField] private RectTransform _joystickHandle;
        [SerializeField] private float _joystickRadius = 80f;

        private CharacterController _characterController;
        private Camera _mainCamera;
        private Animator _animator;

        private static readonly int AnimRun = Animator.StringToHash("run");
        private static readonly int AnimIdle = Animator.StringToHash("idle");

        private bool _isMoving;

        private Vector2 _inputDirection;
        private int _activeTouchId = -1;
        private Vector2 _joystickOrigin;
        private bool _movementEnabled = true;
        private float _verticalVelocity;

#if UNITY_EDITOR
        private bool _mouseHeld;
#endif

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _mainCamera = Camera.main;
            _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            HideJoystick();
        }

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled)
            {
                _inputDirection = Vector2.zero;
                HideJoystick();
                if (_animator != null && _isMoving)
                {
                    _isMoving = false;
                    _animator.SetTrigger(AnimIdle);
                }
            }
        }

        /// <summary>UpgradeManager에서 이동 속도 업그레이드 적용 시 호출.</summary>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0.1f, speed);
        }

        private void Update()
        {
            if (!_movementEnabled) return;
#if UNITY_EDITOR
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
            MoveCharacter();
        }

#if UNITY_EDITOR
        private void HandleMouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame && !_mouseHeld)
            {
                _mouseHeld = true;
                _joystickOrigin = mouse.position.ReadValue();
                ShowJoystick(_joystickOrigin);
            }

            if (_mouseHeld)
            {
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    _mouseHeld = false;
                    _inputDirection = Vector2.zero;
                    HideJoystick();
                    return;
                }

                Vector2 delta = mouse.position.ReadValue() - _joystickOrigin;
                Vector2 clamped = Vector2.ClampMagnitude(delta, _joystickRadius);
                _inputDirection = clamped / _joystickRadius;
                UpdateJoystickHandle(clamped);
            }
        }
#endif

        private void HandleTouchInput()
        {
            var touches = Touch.activeTouches;

            if (touches.Count == 0)
            {
                if (_activeTouchId != -1)
                {
                    _inputDirection = Vector2.zero;
                    _activeTouchId = -1;
                    HideJoystick();
                }
                return;
            }

            // 새 터치 시작
            foreach (var touch in touches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && _activeTouchId == -1)
                {
                    _activeTouchId = touch.touchId;
                    _joystickOrigin = touch.screenPosition;
                    ShowJoystick(_joystickOrigin);
                    break;
                }
            }

            // 활성 터치 추적
            foreach (var touch in touches)
            {
                if (touch.touchId != _activeTouchId) continue;

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _inputDirection = Vector2.zero;
                    _activeTouchId = -1;
                    HideJoystick();
                    break;
                }

                Vector2 delta = touch.screenPosition - _joystickOrigin;
                Vector2 clamped = Vector2.ClampMagnitude(delta, _joystickRadius);
                _inputDirection = clamped / _joystickRadius;

                UpdateJoystickHandle(clamped);
                break;
            }
        }

        private void MoveCharacter()
        {
            if (_characterController.isGrounded)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 horizontalMove = Vector3.zero;

            if (_inputDirection.sqrMagnitude >= 0.01f)
            {
                Vector3 camForward = _mainCamera.transform.forward;
                Vector3 camRight = _mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 moveDir = (camForward * _inputDirection.y + camRight * _inputDirection.x).normalized;
                horizontalMove = moveDir * _moveSpeed;

                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }

            bool isMoving = horizontalMove.sqrMagnitude >= 0.01f;
            if (_animator != null && isMoving != _isMoving)
            {
                _isMoving = isMoving;
                _animator.SetTrigger(isMoving ? AnimRun : AnimIdle);
            }

            Vector3 finalMove = horizontalMove + Vector3.up * _verticalVelocity;
            _characterController.Move(finalMove * Time.deltaTime);
        }

        private void ShowJoystick(Vector2 screenPosition)
        {
            if (_joystickContainer == null) return;

            _joystickContainer.gameObject.SetActive(true);

            Canvas canvas = _joystickContainer.GetComponentInParent<Canvas>();
            if (canvas != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.GetComponent<RectTransform>(),
                    screenPosition,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCamera,
                    out Vector2 localPoint))
            {
                _joystickBackground.anchoredPosition = localPoint;
            }

            if (_joystickHandle != null)
                _joystickHandle.anchoredPosition = Vector2.zero;
        }

        private void HideJoystick()
        {
            if (_joystickContainer == null) return;
            _joystickContainer.gameObject.SetActive(false);
        }

        private void UpdateJoystickHandle(Vector2 offset)
        {
            if (_joystickHandle == null) return;
            _joystickHandle.anchoredPosition = offset;
        }
    }
}
