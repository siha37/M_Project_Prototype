using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces;
using MyFolder._1._Scripts._0._Object._00._Actor._4._Components._2._Movement;
using UnityEngine;
using UnityEngine.AI;

namespace MyFolder._1._Scripts._00._Actor._4._Components.Movement
{
    /// <summary>
    /// AI 이동을 처리하는 컴포넌트 (NavMeshAgent 기반)
    /// </summary>
    public class AiMoveComponent : IActorComponent, IActorUpdatable, IMovable, IConfigurable<MovementSettings>
    {
        public int Priority => 10;

        private Actor _owner;
        private ActorEventBus _eventBus;
        private NavMeshAgent _navAgent;

        // 이동 상태
        private Vector3 _currentDestination;
        private bool _hasDestination = false;
        private float _lastPathUpdateTime = 0f;

        // 설정
        private MovementSettings _settings = MovementSettings.Default;
        private const float PathUpdateInterval = 0.1f; // 경로 갱신 간격

        // 속성
        public float CurrentSpeed => _navAgent != null ? _navAgent.velocity.magnitude : 0f;
        public bool IsMoving => _navAgent != null && _navAgent.hasPath && _navAgent.remainingDistance > _navAgent.stoppingDistance;

        public void Init(Actor actor)
        {
            _owner = actor;
            _eventBus = actor.EventBus;
            
            // NavMeshAgent 찾기
            actor.TryGetComponent(out _navAgent);
            
            if (_navAgent == null)
            {
                Debug.LogError($"[AiMoveComponent] NavMeshAgent not found on {actor.name}");
                return;
            }

            // 2D용 설정
            _navAgent.updateRotation = false;
            _navAgent.updateUpAxis = false;
            
            // Transform 회전 초기화
            actor.transform.rotation = Quaternion.identity;
        }

        public void OnEnable()
        {
            if (_eventBus == null) return;

            // 이벤트 구독
            _eventBus.MoveToRequested += OnMoveToRequested;
        }

        public void OnDisable()
        {
            if (_eventBus == null) return;

            // 이벤트 구독 해제
            _eventBus.MoveToRequested -= OnMoveToRequested;
        }

        public void ApplyConfig(in MovementSettings settings)
        {
            _settings = settings;
            
            if (_navAgent != null)
            {
                _navAgent.speed = settings.MaxSpeed;
                _navAgent.acceleration = settings.Acceleration;
                _navAgent.stoppingDistance = settings.StoppingDistance;
            }
        }

        private void OnMoveToRequested(Vector3 destination)
        {
            SetDestination(destination);
        }

        public void SetDirection(Vector2 direction)
        {
            // NavMeshAgent는 방향 입력을 직접 지원하지 않음
            // 현재 위치에서 방향으로 일정 거리만큼 이동하도록 목적지 설정
            if (direction.magnitude > 0.01f)
            {
                Vector3 targetPos = _owner.transform.position + (Vector3)direction * 2f;
                SetDestination(targetPos);
            }
            else
            {
                Stop();
            }
        }

        public void SetDestination(Vector3 destination)
        {
            if (_navAgent == null) return;

            // NavMesh 위의 유효한 위치 찾기
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _currentDestination = hit.position;
                _hasDestination = true;
                _navAgent.SetDestination(_currentDestination);
                
                Debug.Log($"[AiMoveComponent] Moving to {_currentDestination}");
            }
            else
            {
                Debug.LogWarning($"[AiMoveComponent] Invalid destination: {destination}");
            }
        }

        public void Stop()
        {
            if (_navAgent == null) return;

            _navAgent.isStopped = true;
            _hasDestination = false;
            
            Debug.Log("[AiMoveComponent] Movement stopped");
        }

        public void Resume()
        {
            if (_navAgent == null) return;

            _navAgent.isStopped = false;
        }

        public void Update()
        {
            if (_navAgent == null || !_hasDestination) return;

            // 주기적으로 경로 상태 확인
            if (Time.time - _lastPathUpdateTime >= PathUpdateInterval)
            {
                CheckPathStatus();
                _lastPathUpdateTime = Time.time;
            }
        }

        private void CheckPathStatus()
        {
            if (!_navAgent.pathPending)
            {
                // 목적지에 도달했는지 확인
                if (!_navAgent.hasPath || _navAgent.remainingDistance <= _navAgent.stoppingDistance)
                {
                    if (_hasDestination)
                    {
                        _hasDestination = false;
                        Debug.Log("[AiMoveComponent] Destination reached");
                    }
                }
                // 경로가 유효하지 않으면 재계산
                else if (_navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    Debug.LogWarning("[AiMoveComponent] Path invalid, recalculating...");
                    _navAgent.SetDestination(_currentDestination);
                }
            }
        }

        public void FixedUpdate() { }
        public void LateUpdate() { }
        public void Dispose() 
        {
            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_navAgent == null || !_hasDestination) return;

            // 목적지 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_currentDestination, 0.5f);
            
            // 경로 표시
            if (_navAgent.hasPath)
            {
                Gizmos.color = Color.yellow;
                var path = _navAgent.path;
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                }
            }
        }
        #endif
    }
}
