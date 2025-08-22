using System;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8._Time;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI
{
    public class GameStageUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeText;
        //TIME
        //MAP
        //QUEST

        private void Start()
        {
            TimeManager.instance.OnTimeChange += TimeUpdate;
        }

        private void TimeUpdate()
        {
            float currentTime = TimeManager.instance.CurrentTime;
            int min = (int)(currentTime / 60);
            int sec = (int)(currentTime % 60);
            string timeString = string.Format("{0:00}:{1:00}", min, sec);
            timeText.text = timeString;
        }
    }
}