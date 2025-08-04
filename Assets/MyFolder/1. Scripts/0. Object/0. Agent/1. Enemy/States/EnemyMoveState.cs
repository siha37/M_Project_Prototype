namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyMoveState : IEnemyState
    {
        public override bool CanStateChange()
        {
            return true;
        }

        public override void Update()
        {
        }

        public override void OnStateEnter()
        {
        }

        public override void OnStateExit()
        {
        }

        public override string GetName()
        {
            return GetType().Name;
        }
    }
}