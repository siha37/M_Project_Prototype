using System;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public sealed class ExterminationGlobalQuest : GlobalQuestBase
    {
        public override GlobalQuestType Type => GlobalQuestType.Extermination;
        public override bool IsComplete => progress >= target;
        public override bool IsFaile => !IsComplete && currentTime >= limitTime ; 
        public int target;
        public int progress;
        public float limitTime;
        private float currentTime;

        public ExterminationGlobalQuest(int target, float limitTime)
        {
            this.target = target;
            this.limitTime = limitTime;
        }

    }
}
