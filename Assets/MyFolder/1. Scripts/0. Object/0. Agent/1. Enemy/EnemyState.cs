using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyState : AgentState
{
    public const float targetDistance = 5f;
    
    private NavMeshAgent navMeshAgent;
    private EnemyControll enemyControll;
    // ✅ EnemyNetworkSync 참조 제거 (순환 참조 해결)

    protected override void Start()
    {
        base.Start();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyControll = GetComponent<EnemyControll>();
    }

    // ✅ 네트워크 동기화 코드 완전 제거, 순수 로컬 로직만 유지
    public override void TakeDamage(float damage, Vector2 hitDirection = default)
    {
        // ✅ 네트워크 동기화는 EnemyNetworkSync에서 처리
        // 여기서는 기본 데미지 계산만 수행 (UI 업데이트 제거)
        if (isDead) return;
        
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        
        if (currentHp <= 0)
        {
            isDead = true;
            HandleDeath();
        }
    }

    // ✅ 서버에서만 호출되는 사망 처리 (NetworkSync에서 호출)
    public void HandleDeath()
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

        // NetworkEnemyManager에 적 제거 알림
        if (NetworkEnemyManager.Instance != null)
        {
            NetworkEnemyManager.Instance.RemoveEnemy();
        }

        // 사망 시퀀스 시작
        StartCoroutine(DeathSequence());
    }

    // ✅ 네트워크 동기화를 위해 public으로 유지
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
