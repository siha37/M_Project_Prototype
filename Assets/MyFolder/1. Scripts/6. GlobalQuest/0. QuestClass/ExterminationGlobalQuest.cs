using System;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public sealed class ExterminationGlobalQuest : GlobalQuestBase
    {
        public override GlobalQuestType Type => GlobalQuestType.Extermination;
        public override bool IsComplete => progress >= target;
        public override bool IsFaile => !IsComplete && currentTime >= limitTime ;
        public override string QuestName => "섬멸";

        
        public ExterminationGlobalQuest(QuestSpawner spawner,int target, float limitTime)
        {
            this.spawner = spawner;
            this.target = target;
            this.limitTime = limitTime;
        }
        
        public override void Complete()
        {
            NetworkEnemyManager.Instance.enemyRemoveCallback -= enemyCount;
            IsEnd = true;
        }

        public override void Fail()
        {
            NetworkEnemyManager.Instance.enemyRemoveCallback -= enemyCount;
            spawner.AllRemove();
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
            if(IsComplete)
                Complete();
            if(IsFaile)
                Fail();
        }

        public override void ActiveQuest()
        {
            NetworkEnemyManager.Instance.enemyRemoveCallback += enemyCount;
            IsActive = true;
        }

        private void enemyCount()
        {
            progress++;
        }

    }
}
