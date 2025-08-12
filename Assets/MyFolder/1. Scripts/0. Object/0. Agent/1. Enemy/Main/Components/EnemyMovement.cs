using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;
using UnityEngine.AI;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemyMovement : IEnemyUpdateComponent
    {
        private NavMeshAgent navMeshAgent;
        private EnemyControll agent;
        private bool isMoving;
        
        private Vector3 currentDestination;
        private float lastPathUpdateTime = 0;
        private float pathUpdateInterval = 0;
        
        
        
        /// <summary>
        /// 목적지에 도달했는지 여부
        /// </summary>
        public bool HasReachedDestination => navMeshAgent && !navMeshAgent.pathPending && navMeshAgent.hasPath && 
                                             navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance;
        
        public void Init(EnemyControll agent)
        {
            this.agent = agent;

            pathUpdateInterval = agent.Config.aiUpdateInterval;

            if (!agent.TryGetComponent(out navMeshAgent))
                LogManager.LogError(LogCategory.Enemy, $"The agent is set to navMeshAgent : {navMeshAgent}");
            else
            {
                navMeshAgent.updateRotation = false;
                navMeshAgent.updateUpAxis = false;
                agent.transform.rotation = Quaternion.identity;
                SetSpeed(this.agent.Config.defaultSpeed);
                SetStoppingDistance(this.agent.Config.stoppingDistance);
            }
        }

        public void OnEnable()
        {
            if(agent)
                Init(agent);
        }

        public void OnDisable()
        {
        }

        public void ChangedState(IEnemyState oldstate, IEnemyState newstate)
        {
                return;
        }

        public void Update()
        {
            UpdateMovementState();
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
                return;
        }
        /// <summary>
        /// 목적지로 이동
        /// </summary>
        public void MoveTo(Vector3 destination)
        {
            if (!agent) return;
        
            // NavMesh 위의 유효한 위치인지 확인
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Vector3 validDestination = hit.position;
            
                // 경로 설정
                SetDestination(validDestination);
                isMoving = true;
            
                LogManager.Log(LogCategory.Enemy, $"이동 시작: {validDestination}");
            }
            else
            {
                LogManager.LogWarning(LogCategory.Enemy, $"유효하지 않은 목적지: {destination}");
            }
        }
        
        /// <summary>
        /// 이동 정지
        /// </summary>
        public void Stop()
        {
            if (navMeshAgent == null) return;
        
            navMeshAgent.isStopped = true;
            isMoving = false;
        }    
        
        /// <summary>
        /// 이동 재개
        /// </summary>
        public void Resume()
        {
            if (navMeshAgent == null) return;
        
            navMeshAgent.isStopped = false;
            isMoving = true;
        }

        public void SetDestination(Vector3 destination)
        {
            currentDestination = destination;
            navMeshAgent.destination = destination;
        }
        public void SetSpeed(float speed)
        {
            if(navMeshAgent)
                navMeshAgent.speed = speed;
        }
        public void SetStoppingDistance(float stopDistance)
        {
            if(navMeshAgent)
                navMeshAgent.stoppingDistance = stopDistance;
        }
        
        /// <summary>
        /// 현재 속도 가져오기
        /// </summary>
        public float GetCurrentSpeed()
        {
            return agent != null ? navMeshAgent.velocity.magnitude : 0f;
        }
        /// <summary>
        /// 목적지까지의 거리
        /// </summary>
        public float GetDistanceToDestination()
        {
            return agent != null ? navMeshAgent.remainingDistance : float.MaxValue;
        }
        /// <summary>
        /// 경로가 유효한지 확인
        /// </summary>
        public bool HasValidPath()
        {
            return agent && navMeshAgent.hasPath && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance;
        }
        
        
        
        /// <summary>
        /// 이동 상태 업데이트
        /// </summary>
        private void UpdateMovementState()
        {
            if (!agent) return;
        
            // 목적지 도달 확인
            if (isMoving && HasReachedDestination)
            {
                isMoving = false;
                LogManager.Log(LogCategory.Enemy, $"목적지 도달: {currentDestination}");
            }
        
            // 경로 업데이트 (성능 최적화)
            if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
            {
                // 경로가 유효하지 않으면 재계산
                if (isMoving && !HasValidPath())
                {
                    LogManager.LogWarning(LogCategory.Enemy, "경로 재계산 필요");
                    SetDestination(currentDestination);
                }
            
                lastPathUpdateTime = Time.time;
            }
        }

    }
}
