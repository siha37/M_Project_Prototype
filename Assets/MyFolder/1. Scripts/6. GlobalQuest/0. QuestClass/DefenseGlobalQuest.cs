namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public class DefenseGlobalQuest : GlobalQuestBase
    {
        public float limitTime;
        private float currentTime = 0f;
        
        private int defenseObjectAmount;
        
        public DefenseGlobalQuest(float _limitTime, int _defenseObjectAmount)
        {
            limitTime = _limitTime;
            defenseObjectAmount = _defenseObjectAmount;
        }
        public override GlobalQuestType Type => GlobalQuestType.Defense;
        public override bool IsComplete => currentTime >= limitTime;
        public override bool IsFaile => currentTime < limitTime && defenseObjectAmount <= 0;        
    }
}
