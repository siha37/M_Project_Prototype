using UnityEngine;

public class AgentState : State
{
    public const float speed = 5f;
    public const float bulletSpeed = 10f;
    public const float bulletDamage = 45f;
    public const float bulletDelay = 0.3f;
    public const float bulletRange = 10f;
    public const float bulletReloadTime = 2f;
    public const float bulletMaxCount = 10f;

    public float bulletCurrentCount;
    // ✅ AgentUI 참조 제거 (NetworkSync에서 처리)

    protected override void Start()
    {
        base.Start();
        bulletCurrentCount = bulletMaxCount;
        
        // ✅ UI 초기화는 NetworkSync에서 처리
    }

    // ✅ UI 업데이트 완전 제거, 순수 데미지 계산만
    public override void TakeDamage(float damage, Vector2 hitDirection = default)
    {
        if (isDead) return;
        
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        
        if (currentHp <= 0)
        {
            isDead = true;
        }
    }

    // ✅ UI 업데이트 완전 제거, 순수 탄약 계산만
    public void UpdateBulletCount(float count)
    {
        bulletCurrentCount += count;
        bulletCurrentCount = Mathf.Clamp(bulletCurrentCount, 0, bulletMaxCount);
    }
}
