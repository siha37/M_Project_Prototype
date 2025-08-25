using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._3._QuestAgent
{
    public class QuestAgentStatus : AgentStatus
    {
        
        protected override void Start()
        {
            base.Start();
        }

        public override void TakeDamage(float damage, Vector2 hitDirection = default)
        {
            if (isDead) return;
        
            base.TakeDamage(damage, hitDirection);
        
            if (currentHp <= 0)
            {
                // 사망 시퀀스 시작
                StartCoroutine(DeathSequence());
            }
        }

        protected override void OnDeath()
        {
            base.OnDeath();
        }
    }
}