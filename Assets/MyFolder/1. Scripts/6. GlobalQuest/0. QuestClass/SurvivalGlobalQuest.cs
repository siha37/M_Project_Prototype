namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public class SurvivalGlobalQuest : GlobalQuestBase
    {
        public float limitTime;
        private float currentTime = 0;
        private float max_progress = 100f;
        public float progress;
        public SurvivalGlobalQuest(float limitTime)
        {
            this.limitTime = limitTime;
            this.progress = 50;
        }
        public override GlobalQuestType Type => GlobalQuestType.Survival;
        public override bool IsComplete => progress> 0 && currentTime >= limitTime;
        public override bool IsFaile => currentTime < limitTime && progress <= 0;
        
    }
}
