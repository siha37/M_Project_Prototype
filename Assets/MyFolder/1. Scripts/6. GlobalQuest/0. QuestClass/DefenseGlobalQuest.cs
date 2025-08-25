using System;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public class DefenseGlobalQuest : GlobalQuestBase
    {
        public Vector3? questPosition
        {
            get
            {
                if (spawner == null || spawner.spawnPoints == null || spawner.spawnPoints.Count == 0 || spawner.spawnPoints[0] == null)
                    return null;
                return spawner.spawnPoints[0].position;
            }
        }
        
        public DefenseGlobalQuest(QuestSpawner spawner,float _limitTime)
        {
            this.spawner = spawner;
            limitTime = _limitTime;
        }
        public override GlobalQuestType Type => GlobalQuestType.Defense;
        public override bool IsComplete => currentTime >= limitTime;
        public override bool IsFaile => currentTime < limitTime && spawner?.CurrentCreateAmount <= 0;
        public override string QuestName => "방어";

        public override void Complete()
        {
            IsEnd = true;
        }

        public override void Fail()
        {
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
            spawner.SpawnStart();
            IsActive = true;
        }
    }
}
