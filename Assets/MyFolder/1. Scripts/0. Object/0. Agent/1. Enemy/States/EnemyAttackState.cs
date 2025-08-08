using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyAttackState: EnemyBaseState
    {
        private EnemyCombat combat;
        private EnemyMovement movement;
        private float lastPathUpdateTime = 0;
        private float pathUpdateInterval;
        public override void Init(EnemyControll controll)
        {
            base.Init(controll);
            combat = (EnemyCombat)agent.GetEnemyActiveComponent(typeof(EnemyCombat));
            movement = (EnemyMovement)agent.GetEnemyActiveComponent(typeof(EnemyMovement));
            pathUpdateInterval = agent.Config.aiUpdateInterval;
        }

        public override void Update()
        {
            // 경로 업데이트 (성능 최적화) / 공격 중 이동 처리
            if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
            {
                movement.MoveTo(agent.CurrentTarget.transform.position);
                lastPathUpdateTime = Time.time;
            }
        }

        public override void OnStateEnter()
        {
            movement.SetSpeed(agent.Config.attackSpeed);
            combat.AttackOn();
        }

        public override void OnStateExit()
        {
            combat.AttackOff();
        }

    }
}