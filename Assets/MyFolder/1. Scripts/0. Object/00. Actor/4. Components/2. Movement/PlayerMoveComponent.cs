using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._4._Components.Movement
{
    /// <summary>
    /// 플레이어 이동을 처리하는 컴포넌트 (Rigidbody2D 기반)
    /// </summary>
    public class PlayerMoveComponent : IActorComponent, IActorUpdatable, IMovable, IConfigurable<MovementSettings>
    {
        public int Priority => 10;

        private Actor _owner;
        private ActorEventBus _eventBus;
        private Rigidbody2D _rigidbody;

        // 이동 상태
        private Vector2 _moveDirection = Vector2.zero;
        private Vector3 _targetDestination;
        private bool _hasDestination = false;

        // 설정
        private MovementSettings _settings = MovementSettings.Default;

        // 속성
        public float CurrentSpeed => _rigidbody != null ? _rigidbody.linearVelocity.magnitude : 0f;
        public bool IsMoving => _moveDirection.magnitude > 0.01f || _hasDestination;

        public void Init(Actor actor)
        {
            _owner = actor;
            _eventBus = actor.EventBus;
            
            // Rigidbody2D 찾기
            actor.TryGetComponent(out _rigidbody);
            
            if (_rigidbody == null)
            {
                Debug.LogError($"[PlayerMoveComponent] Rigidbody2D not found on {actor.name}");
            }
        }

        public void OnEnable()
        {
            if (_eventBus == null) return;

            // 이벤트 구독
            _eventBus.MoveRequested += OnMoveRequested;
        }

        public void OnDisable()
        {
            if (_eventBus == null) return;

            // 이벤트 구독 해제
            _eventBus.MoveRequested -= OnMoveRequested;
        }

        public void ApplyConfig(in MovementSettings settings)
        {
            _settings = settings;
        }

        private void OnMoveRequested(Vector2 direction)
        {
            SetDirection(direction);
        }

        public void SetDirection(Vector2 direction)
        {
            _moveDirection = direction.normalized;
            _hasDestination = false; // 방향 입력이 있으면 목적지 이동 취소
        }

        public void SetDestination(Vector3 destination)
        {
            _targetDestination = destination;
            _hasDestination = true;
        }

        public void Stop()
        {
            _moveDirection = Vector2.zero;
            _hasDestination = false;
            
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }

        public void Update()
        {
            // 목적지 이동 처리 (AI나 자동 이동용)
            if (_hasDestination)
            {
                Vector2 currentPos = _owner.transform.position;
                Vector2 targetPos = _targetDestination;
                Vector2 direction = (targetPos - currentPos).normalized;
                
                // 목적지에 도달했는지 확인
                float distance = Vector2.Distance(currentPos, targetPos);
                if (distance <= _settings.StoppingDistance)
                {
                    _hasDestination = false;
                    _moveDirection = Vector2.zero;
                }
                else
                {
                    _moveDirection = direction;
                }
            }
        }

        public void FixedUpdate()
        {
            if (!_rigidbody) return;

            // 이동 적용
            Vector2 targetVelocity = _moveDirection * _settings.MaxSpeed;
            
            _rigidbody.linearVelocity = targetVelocity;
        }

        public void LateUpdate() { }
        public void Dispose() { }
    }

    /// <summary>
    /// 이동 설정
    /// </summary>
    [System.Serializable]
    public struct MovementSettings
    {
        public float MaxSpeed;
        public float Acceleration;
        public float Deceleration;
        public float MaxAcceleration;
        public float StoppingDistance;

        public static MovementSettings Default => new MovementSettings
        {
            MaxSpeed = 6f,
            Acceleration = 20f,
            Deceleration = 30f,
            MaxAcceleration = 50f,
            StoppingDistance = 0.1f
        };
    }
}
