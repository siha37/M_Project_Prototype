using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Helping;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemyPercetion : IEnemyUpdateComponent
    {
        private EnemyConfig config;
        private EnemyControll agent;
        private bool hasLineOfSight;
        private Vector3 lastSeenPosition;
        
        
        public bool HasLineOfSight => hasLineOfSight;
        public Vector3 LastSeenPosition => lastSeenPosition;
        
        public void Init(EnemyControll agent)
        {
            this.agent = agent;
            config = agent.Config;
            agent.SetTarget(FindTarget());
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
        }

        public void Update()
        {
            UpdatePerception();

            UpdateAttackRange();
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        
        /*==================Private ========================*/
        
        /// <summary>
        /// 인지 시스템 업데이트
        /// </summary>
        private void UpdatePerception()
        {
            // 현재 타겟에 대한 시야 확인
            bool currentLineOfSight = false;
        
            if (agent.Currenttarget)
            {
                currentLineOfSight = LineOfSight(agent.Currenttarget.transform);
            
                if (currentLineOfSight)
                {
                    lastSeenPosition = agent.Currenttarget.transform.position;
                }
            }
        
            // 시야 상태 변경 확인
            if (hasLineOfSight != currentLineOfSight)
            {
                hasLineOfSight = currentLineOfSight;
            
                if (hasLineOfSight)
                {
                    LogManager.Log(LogCategory.Enemy, $"타겟 시야 확보: {agent.Currenttarget?.name}");
                }
                else
                {
                    LogManager.Log(LogCategory.Enemy, $"타겟 시야 상실: {agent.Currenttarget?.name}");
                }
            }
        }

        /// <summary>
        /// 공격 가능 거리 판단
        /// </summary>
        private void UpdateAttackRange()
        {
            if (hasLineOfSight)
            {
                float distance = Vector2.Distance(agent.Currenttarget.transform.position, agent.transform.position);
                if (distance <= config.attackRange)
                {
                    agent.StateMachine.StateChage(typeof(EnemyAttackState));
                }
            }
        }

        private GameObject FindTarget()
        {
            List<NetworkObject> players = NetworkPlayerManager.Instance.GetAlivePlayers();
            float distance = float.MaxValue;
            NetworkObject player = null;
            players.ForEach(e =>
            {
               float atob = Vector2.Distance(agent.transform.position, e.transform.position);
               if (atob < distance)
               {
                   player = e;
                   distance = atob;
               }
            });

            if (player)
            {
                agent.StateMachine.StateChage(typeof(EnemyMoveState));
                return player.gameObject;
            }

            return null;
        }
            
        
        /// <summary>
        /// 특정 타겟에 대한 시야 확인
        /// </summary>
        public bool LineOfSight(Transform target)
        {
            if (!target) return false;
        
            // 거리 확인 (2D 거리 계산)
            float distance = Vector2.Distance(agent.transform.position, target.position);
            if (distance > config.detectionRange) return false;
        
            // 시야각 확인
            if (!IsInFieldOfView(target.position)) return false;
        
            // 장애물 확인
            return !IsObstructed(target.position);
        }
        public bool LineOfSight() { return LineOfSight(agent.Currenttarget.transform); }
    
        
        /// <summary>
        /// 특정 위치가 시야각 내에 있는지 확인
        /// </summary>
        public bool IsInFieldOfView(Vector3 position)
        {
            Vector2 directionToTarget = ((Vector2)position - (Vector2)agent.transform.position).normalized;
        
            // 현재 타겟이 있으면 타겟 방향을 기준으로, 없으면 기본 방향 사용
            Vector2 baseDirection;
            if (agent.Currenttarget)
            {
                // 현재 타겟 방향을 기준으로 설정
                baseDirection = ((Vector2)agent.Currenttarget.transform.position - (Vector2)agent.transform.position).normalized;
            }
            else
            {
                // 기본 방향 (오른쪽)
                baseDirection = Vector2.right;
            }
        
            float angle = Vector2.Angle(baseDirection, directionToTarget);
        
            return angle <= config.fieldOfViewAngle * 0.5f;
        }
        
        /// <summary>
        /// 특정 위치가 장애물에 가려져 있는지 확인
        /// </summary>
        public bool IsObstructed(Vector3 position)
        {
        
            Vector2 direction = ((Vector2)position - (Vector2)agent.transform.position).normalized;
            float distance = Vector2.Distance(agent.transform.position, position);
        
            // 레이캐스트로 장애물 확인 (2D)
            RaycastHit2D hit = Physics2D.Raycast(agent.transform.position, direction, distance, config.obstacleLayer);
            if (hit.collider)
            {
                return true; // 장애물이 있음
            }
        
            return false; // 장애물이 없음
        }
    }
}