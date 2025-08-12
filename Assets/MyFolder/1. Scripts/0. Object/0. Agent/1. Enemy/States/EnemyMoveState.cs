using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyMoveState : EnemyBaseState
    {
        private EnemyMovement movement;
        private float lastPathUpdateTime;
        private float pathUpdateInterval;
        public override void Init(EnemyControll controll)
        {
            base.Init(controll);
            movement = (EnemyMovement)agent.GetEnemyActiveComponent(typeof(EnemyMovement));
            lastPathUpdateTime = 0;
            pathUpdateInterval = agent.Config.aiUpdateInterval;
        }

        public override void Update()
        {
            // 경로 업데이트 (성능 최적화)
            if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
            {
                movement.MoveTo(agent.CurrentTarget.transform.position);
            }
        }

        public override void OnStateEnter()
        {
            if(movement != null)
            {
                movement.SetSpeed(agent.Config.defaultSpeed);
                movement.SetStoppingDistance(agent.Config.stoppingDistance);
            }
            else
            {
                LogManager.LogError(LogCategory.Enemy,$"Movement 없음 :{movement}");
            }
        }

        public override void OnStateExit()
        {
        }
    }
}