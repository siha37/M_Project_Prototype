using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;

namespace MyFolder._1._Scripts._6._GlobalQuest
{

    public sealed class GlobalQuestContext
    {
        public QuestSpawner Spawner;
        public GlobalQuestType type;
        public int targetAmount;
        public int defenceAmount;
        public float limitTime;
        public GlobalQuestContext(QuestSpawner spawner, int targetAmount,float limitTime,int defenceAmount )
        {
            Spawner = spawner;
            this.defenceAmount = defenceAmount;
            this.targetAmount = targetAmount;
            this.limitTime = limitTime;
        }
        public GlobalQuestContext(QuestSpawner spawner, int targetAmount, float limitTime)
        {
            Spawner = spawner;
            this.targetAmount = targetAmount;
            this.limitTime = limitTime;
        }
        public GlobalQuestContext(QuestSpawner spawner, float limitTime, int defenceAmount)
        {
            Spawner = spawner;
            this.defenceAmount = defenceAmount;
            this.limitTime = limitTime;
        }
        public GlobalQuestContext(QuestSpawner spawner,float limitTime)
        {
            Spawner = spawner;
            this.limitTime = limitTime;
        }
    }
}