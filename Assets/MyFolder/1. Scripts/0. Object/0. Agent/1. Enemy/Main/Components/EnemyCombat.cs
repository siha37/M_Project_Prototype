using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemyCombat: IEnemyUpdateComponent
    {
        private EnemyConfig config;
        private EnemyControll agent;
        
        public void Init(EnemyControll agent)
        {
            this.agent = agent;
            config = agent.Config;
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
            if (newstate.GetType() == typeof(EnemyAttackState))
            {
                
            }
        }

        public void Update()
        {
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        /*==================Private ========================*/

    }
}