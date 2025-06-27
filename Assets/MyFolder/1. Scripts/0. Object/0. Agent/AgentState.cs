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
    private AgentUI agentUI;

    protected override void Start()
    {
        base.Start();
        bulletCurrentCount = bulletMaxCount;

        // AgentUI 초기화
        agentUI = GetComponent<AgentUI>();
        if (agentUI != null)
        {
            agentUI.InitializeUI(currentHp, maxHp, (int)bulletCurrentCount, (int)bulletMaxCount);
        }
    }

    public override void TakeDamage(float damage, Vector2 hitDirection = default)
    {
        base.TakeDamage(damage, hitDirection);
        
        // 체력 UI 업데이트
        if (agentUI != null)
        {
            agentUI.UpdateHealthUI(currentHp, maxHp);
        }
    }

    public void UpdateBulletCount(float count)
    {
        bulletCurrentCount += count;
        bulletCurrentCount = Mathf.Clamp(bulletCurrentCount, 0, bulletMaxCount);
        // 탄약 UI 업데이트
        if (agentUI)
        {
            agentUI.UpdateAmmoUI((int)bulletCurrentCount, (int)bulletMaxCount);
        }
    }
}
