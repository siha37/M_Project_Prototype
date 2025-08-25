using FishNet.Object;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest;
using MyFolder._1._Scripts._6._GlobalQuest;
using MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI
{
    public class GameStageUI : NetworkBehaviour
    {
        //TIME
        [SerializeField] private TextMeshProUGUI timeText;
        //MAP
        

        private void Start()
        {
            _8._Time.TimeManager.instance.OnTimeChange += TimeUpdate;
        }

        public override void OnStartServer()
        {
        }

        private void TimeUpdate()
        {
            float currentTime = _8._Time.TimeManager.instance.CurrentTime;
            int min = (int)(currentTime / 60);
            int sec = (int)(currentTime % 60);
            string timeString = $"{min:00}:{sec:00}";
            timeText.text = timeString;
        }


    }
}