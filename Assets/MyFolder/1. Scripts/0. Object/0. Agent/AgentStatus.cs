using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent
{
    public class AgentStatus : Status
    {
        public float speed = 5f;
        public float bulletSpeed = 10f;
        public float bulletDamage = 45f;
        public float bulletDelay = 0.3f;
        public float bulletRange = 10f;
        public float bulletReloadTime = 2f;
        public float bulletMaxCount = 10f;

        public float bulletCurrentCount;
        // ✅ AgentUI 참조 제거 (NetworkSync에서 처리)

        protected override void Start()
        {
            base.Start();
            bulletCurrentCount = bulletMaxCount;
        
            // ✅ UI 초기화는 NetworkSync에서 처리
        }

        /// <summary>
        /// 피해 적용 및 죽음 처리
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
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

        public void SetDefinition(PlayerRoleDefinition definition)
        {
            speed = definition.MoveSpeed;
            bulletSpeed = definition.BulletSpeed;
            bulletDamage = definition.BulletDamage;
            bulletDelay = definition.BulletDelay;
            bulletRange = definition.BulletRange;
            bulletReloadTime = definition.BulletReloadTime;
            bulletMaxCount = definition.BulletMaxCount;
        }
    }
}
