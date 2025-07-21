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

    // 네트워크 동기화를 위해 public으로 변경
    public IEnumerator DeathSequence()
    {
        isDead = true;
        
        // 첫 번째 자식 오브젝트의 SpriteRenderer 컴포넌트 가져오기
        if (transform.childCount > 0)
        {
            SpriteRenderer spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // 어두운 보라색으로 변경
                spriteRenderer.color = new Color(0.5f, 0f, 0.5f, 1f);
            }
        }

        // 2초 대기
        yield return new WaitForSeconds(2f);

        // 사망 처리
        OnDeath();
    }
}
