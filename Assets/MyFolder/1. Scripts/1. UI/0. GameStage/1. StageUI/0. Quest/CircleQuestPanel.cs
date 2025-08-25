using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest
{
    public class CircleQuestPanel : QuestPanel
    {
        [SerializeField] private TextMeshProUGUI progressText;
        public override void Initialize(string questName)
        {
            base.Initialize(questName);
        }

        public override void ProgressUpdate(float currentProgress, float maxProgress)
        {
            progressText.text = $"{currentProgress/maxProgress}";
            base.ProgressUpdate(currentProgress, maxProgress);
        }
    }
}