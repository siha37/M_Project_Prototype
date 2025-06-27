using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyState : AgentState
{
    public const float targetDistance = 5f;
    
    private NavMeshAgent navMeshAgent;
    private EnemyControll enemyControll;

    protected override void Start()
    {
        base.Start();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyControll = GetComponent<EnemyControll>();
    }

    public override void TakeDamage(float damage, Vector2 hitDirection = default)
    {
        if (isDead) return;
        base.TakeDamage(damage, hitDirection);
        
        if (currentHp <= 0)
        {
            // EnemyControll 비활성화
            if (enemyControll != null)
            {
                enemyControll.enabled = false;
            }

            // NavMeshAgent 비활성화
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }

            // EnemyManager에 적 제거 알림
            EnemyManager.Instance.RemoveEnemy();

            // 사망 시퀀스 시작
            StartCoroutine(DeathSequence());
        }
    }
}
