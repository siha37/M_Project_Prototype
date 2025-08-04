using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status
{
    public class EnemyStatus : AgentStatus
    {
        private EnemyControll enemyControll;
        private EnemyAi EnemyAi;
        
        protected override void Start()
        {
            base.Start();
            TryGetComponent(out enemyControll);
            TryGetComponent(out EnemyAi);
        }


        protected override void OnDeath()
        {
            
            base.OnDeath();
        }
        
    }
}