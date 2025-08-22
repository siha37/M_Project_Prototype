using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public abstract class GlobalQuestBase
    {
        //현 타입
        public abstract GlobalQuestType Type { get; }
        //퀘스트 완료 여부
        public abstract bool IsComplete { get; }
        public abstract bool IsFaile { get; }

        //생성자
        public GlobalQuestBase() { }
        public virtual void ReportProgress(int amount) { }


        public virtual void Complete() { }
    }
}
