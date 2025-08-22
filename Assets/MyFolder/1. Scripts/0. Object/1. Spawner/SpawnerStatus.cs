using MyFolder._1._Scripts._0._Object;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

public class SpawnerStatus : Status
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
            // NetworkEnemyManager의 최대 적 수량 감소
            NetworkEnemyManager.Instance.RemoveSpawner();
            
            // 사망 시퀀스 시작
            StartCoroutine(DeathSequence());
        }
    }

    protected override void OnDeath()
    {
        // 스포너 제거
        base.OnDeath();
    }
}
