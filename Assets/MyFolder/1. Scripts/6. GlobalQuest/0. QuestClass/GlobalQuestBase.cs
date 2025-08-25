using System.Security.Cryptography;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public abstract class GlobalQuestBase
    {
        //현 타입
        public abstract GlobalQuestType Type { get; }
        public abstract string QuestName { get; }
        protected QuestSpawner spawner;
        
        //퀘스트 완료 여부
        public abstract bool IsComplete { get; }
        public abstract bool IsFaile { get; }
        public bool IsActive = false;
        public bool IsEnd = false;
        
        
        
        protected float waitingTime = 30;
        public float limitTime;
        public float currentTime = 0f;
        public float target;
        public float progress;
        public float max_progress = 100f;

        //생성자
        public GlobalQuestBase() { }
        public virtual void ReportProgress(int amount) { }
        public abstract void Complete();
        public abstract void Fail();
        public abstract void Update();
        public abstract void ActiveQuest();

    }
}
