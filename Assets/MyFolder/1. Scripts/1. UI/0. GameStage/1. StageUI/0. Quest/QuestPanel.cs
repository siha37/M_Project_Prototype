using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest
{
    public abstract class QuestPanel : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI nameText;
        [SerializeField] protected TextMeshProUGUI timeText;
        [SerializeField] protected Image progressImage;
        public virtual void Initialize(string questName)
        {
            nameText.text = questName;
        }

        public virtual void ProgressUpdate(float currentProgress, float maxProgress)
        {
            progressImage.fillAmount = currentProgress / maxProgress;
        }

        public virtual void TimeUpdate(float elapsedTime,float limitTime)
        {
            if(limitTime - elapsedTime <= 15f)
                timeText.color = Color.red;
            else
                timeText.color = Color.white;
            timeText.text = (limitTime-elapsedTime).ToString("0.00");
        }
    }
}