using System;
using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyFolder._1._Scripts._0._Object._00._Actor._4._Components.Input
{
    /// <summary>
    /// 플레이어 입력을 처리하는 컴포넌트 (Owner 전용)
    /// </summary>
    public class PlayerInputComponent : IActorComponent, IActorUpdatable, IInputProvider
    {
        public int Priority => 0; // 최우선 실행

        // 입력 이벤트
        public event Action<Vector2> MoveRequested;
        public event Action<Vector2> LookRequested;
        public event Action FireStarted;
        public event Action FireCanceled;
        public event Action ReloadRequested;
        public event Action InteractStarted;
        public event Action InteractCanceled;
        public event Action Skill1Started;
        public event Action Skill1Canceled;

        private Actor _owner;
        private ActorEventBus _eventBus;
        private PlayerInput _playerInput;

        // 입력 액션들
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private InputAction _interactAction;
        private InputAction _skill1Action;

        // 설정
        [SerializeField] private float gamepadDeadzone = 0.15f;
        [SerializeField] private float mouseSensitivity = 1f;

        public void Init(Actor actor)
        {
            _owner = actor;
            _eventBus = actor.EventBus;
            
            // PlayerInput 컴포넌트 찾기
            actor.TryGetComponent(out _playerInput);
        }

        public void OnEnable()
        {
            if (_playerInput == null || !_owner.IsOwner) return;

            // 입력 액션 등록
            RegisterInputActions();
            
            // 이벤트 바인딩
            BindEventBus();
        }

        public void OnDisable()
        {
            UnregisterInputActions();
        }

        private void RegisterInputActions()
        {
            if (_playerInput?.currentActionMap == null) return;

            // 액션 찾기
            _moveAction = _playerInput.currentActionMap.FindAction("Move");
            _lookAction = _playerInput.currentActionMap.FindAction("Look");
            _fireAction = _playerInput.currentActionMap.FindAction("Attack");
            _reloadAction = _playerInput.currentActionMap.FindAction("Reload");
            _interactAction = _playerInput.currentActionMap.FindAction("Interact");
            _skill1Action = _playerInput.currentActionMap.FindAction("Skill_1");

            // 콜백 등록
            if (_moveAction != null)
            {
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMoveCancel;
            }

            if (_lookAction != null)
            {
                _lookAction.performed += OnLook;
            }

            if (_fireAction != null)
            {
                _fireAction.started += OnFireStart;
                _fireAction.canceled += OnFireCancel;
            }

            if (_reloadAction != null)
            {
                _reloadAction.started += OnReload;
            }

            if (_interactAction != null)
            {
                _interactAction.started += OnInteractStart;
                _interactAction.canceled += OnInteractCancel;
            }

            if (_skill1Action != null)
            {
                _skill1Action.started += OnSkill1Start;
                _skill1Action.canceled += OnSkill1Cancel;
            }
        }

        private void UnregisterInputActions()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMoveCancel;
            }

            if (_lookAction != null)
            {
                _lookAction.performed -= OnLook;
            }

            if (_fireAction != null)
            {
                _fireAction.started -= OnFireStart;
                _fireAction.canceled -= OnFireCancel;
            }

            if (_reloadAction != null)
            {
                _reloadAction.started -= OnReload;
            }

            if (_interactAction != null)
            {
                _interactAction.started -= OnInteractStart;
                _interactAction.canceled -= OnInteractCancel;
            }

            if (_skill1Action != null)
            {
                _skill1Action.started -= OnSkill1Start;
                _skill1Action.canceled -= OnSkill1Cancel;
            }
        }

        private void BindEventBus()
        {
            // 인터페이스 이벤트를 EventBus로 브릿지
            MoveRequested += _eventBus.PublishMove;
            LookRequested += _eventBus.PublishLook;
            FireStarted += _eventBus.PublishFireStart;
            FireCanceled += _eventBus.PublishFireCancel;
            ReloadRequested += _eventBus.PublishReload;
            InteractStarted += _eventBus.PublishInteractStart;
            InteractCanceled += _eventBus.PublishInteractCancel;
        }

        // 입력 콜백들
        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            
            // 데드존 적용 (게임패드용)
            if (input.magnitude < gamepadDeadzone)
                input = Vector2.zero;

            MoveRequested?.Invoke(input);
        }

        private void OnMoveCancel(InputAction.CallbackContext context)
        {
            MoveRequested?.Invoke(Vector2.zero);
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            
            // 마우스 감도 적용
            input *= mouseSensitivity;
            
            LookRequested?.Invoke(input);
        }

        private void OnFireStart(InputAction.CallbackContext context)
        {
            FireStarted?.Invoke();
        }

        private void OnFireCancel(InputAction.CallbackContext context)
        {
            FireCanceled?.Invoke();
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            ReloadRequested?.Invoke();
        }

        private void OnInteractStart(InputAction.CallbackContext context)
        {
            InteractStarted?.Invoke();
        }

        private void OnInteractCancel(InputAction.CallbackContext context)
        {
            InteractCanceled?.Invoke();
        }

        private void OnSkill1Start(InputAction.CallbackContext context)
        {
            Skill1Started?.Invoke();
        }

        private void OnSkill1Cancel(InputAction.CallbackContext context)
        {
            Skill1Canceled?.Invoke();
        }

        public void Update() { }
        public void FixedUpdate() { }
        public void LateUpdate() { }
        public void Dispose() { }
    }
}
