namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public abstract class IEnemyState
    {
        public abstract bool CanStateChange();
        public abstract void Update();
        public abstract void OnStateEnter();
        public abstract void OnStateExit();
        public abstract string GetName();
    }
}