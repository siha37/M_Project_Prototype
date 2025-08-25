using MyFolder._1._Scripts._0._Object._3._QuestAgent;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public class SurvivalGlobalQuest : GlobalQuestBase
    {
        private QuestAgentStatus status;
        public SurvivalGlobalQuest(QuestSpawner spawner,float limitTime)
        {
            this.spawner = spawner;
            this.limitTime = limitTime;
            this.progress = 50;
        }
        public override GlobalQuestType Type => GlobalQuestType.Survival;
        public override bool IsComplete => progress> 0 && currentTime >= limitTime;
        public override bool IsFaile => currentTime < limitTime && progress <= 0;
        public override string QuestName => "생존";


        public override void ActiveQuest()
        {
            spawner.SpawnStart();
            spawner.OnSpawned += OnObjectiveSpawned;
            spawner.OnDespawned += OnObjectiveDespawned;
            NetworkEnemyManager.Instance.enemyRemoveCallback += OnEnemyKilled; // 처치 보상
            IsActive = true;
        }

        private void OnObjectiveSpawned(GameObject go)
        {
            if (go.TryGetComponent(out QuestAgentStatus hp))
            {
                status = hp;
            }
        }
        private void OnObjectiveDespawned(GameObject go)
        {
        }
        public override void Complete()
        {
            NetworkEnemyManager.Instance.enemyRemoveCallback -= OnEnemyKilled; // 처치 보상
            IsEnd = true;
        }

        public override void Fail()
        {
            NetworkEnemyManager.Instance.enemyRemoveCallback -= OnEnemyKilled; // 처치 보상
            IsEnd = true;
        }

        public override void Update()
        {
            if(IsEnd)
                return;
            if(!IsActive)
            {
                //대기 시간 연산
                if (waitingTime <= 0)
                {
                    //퀘스트 활성화
                    ActiveQuest();
                }
                waitingTime -= Time.deltaTime;
                return;
            }
            currentTime += Time.deltaTime;
            
            progress = Mathf.Max(0f, progress);
            if(IsComplete)
                Complete();
            if(IsFaile)
                Fail();
        }

        private void OnEnemyKilled()
        {
            if (Random.value < 0.3f)
                progress += 4;
        }
        
    }
}
