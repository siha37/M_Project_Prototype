using System.Collections;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerStatus : AgentStatus
    {
        public const int reviveCount = 3;
        public const float reviveDelay = 8f;
        public const float reviveRange = 5f;
        public int reviveCurrentCount;

        private PlayerControll playerControll;
        private PlayerInputControll playerInputControll;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;

        protected override void Start()
        {
            base.Start();
            reviveCurrentCount = reviveCount;
            playerControll = GetComponent<PlayerControll>();
            playerInputControll = GetComponent<PlayerInputControll>();
            spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        // ✅ UI 업데이트 로직 제거, 순수 데미지 계산만
        public override void TakeDamage(float damage, Vector2 hitDirection)
        {
            if (isDead) return;
        
            // 후방 피해 확인
            if (playerControll)
            {
                float playerAngle = playerControll.LookAngle + 180;
                float hitAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            
                // 각도 차이 계산 (절대값)
                float angleDifference = Mathf.Abs(Mathf.DeltaAngle(playerAngle, hitAngle));
            
                // 후방 피해 확인 (각도 차이가 135도 이상일 때)
                if (angleDifference >= 135f)
                {
                    damage *= 2f;
                    LogManager.Log(LogCategory.Player, $"{gameObject.name} 후방 피해! 데미지 2배 증폭: {damage}", this);
                }
            }
        
            // ✅ 기본 데미지 처리만 수행 (UI 업데이트 제거)
            currentHp -= damage;
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
            OnDeathSequence();

        }

        public void OnDeathSequence()
        {
            if (currentHp <= 0)
            {
                currentHp = 0;
                if (reviveCurrentCount > 0)
                {
                    StartCoroutine(DeathSequence());
                }
                else
                {
                    // 부활 횟수가 없으면 바로 사망
                    OnDeath();
                }
            }
        }

        public void OnClientDeathSequence()
        {
            StartCoroutine(DeathSequence());   
        }
        /// <summary>
        /// 기절 상태 - 플레이어 전용
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator DeathSequence()
        {
            isDead = true;
        
            // 플레이어 컨트롤 비활성화
            if (playerControll)
            {
                playerControll.enabled = false;
            }

            // 플레이어 입력 컨트롤 비활성화
            if (playerInputControll)
            {
                playerInputControll.enabled = false;
            }

            // 스프라이트 색상 변경
            if (spriteRenderer)
            {
                spriteRenderer.color = new Color(0.5f, 0f, 0.5f, 1f);
            }

            yield return null;
        }

        /// <summary>
        /// 부활 기능 호출
        /// </summary>
        public void Revive()
        {
            if (!isDead) return;

            isDead = false;
            currentHp = maxHp;
            reviveCurrentCount--;

            // 플레이어 컨트롤 활성화
            if (playerControll)
            {
                playerControll.enabled = true;
            }

            // 플레이어 입력 컨트롤 활성화
            if (playerInputControll)
            {
                playerInputControll.enabled = true;
            }

            // 스프라이트 색상 복구
            if (spriteRenderer)
            {
                spriteRenderer.color = originalColor;
            }
        }

        public void ClientReviveEffect()
        {
            // 스프라이트 색상 복구
            if (spriteRenderer)
            {
                spriteRenderer.color = originalColor;
            }
        }
        /// <summary>
        /// 확정적 죽음 호출
        /// </summary>
        protected override void OnDeath()
        {
            // 부활 횟수가 없을 때의 최종 사망 처리
            if (playerControll)
            {
                playerControll.enabled = false;
            }
            if (playerInputControll)
            {
                playerInputControll.enabled = false;
            }
            base.OnDeath();
        }

        public void OnClientDeath()
        {
            
        }
    }
}
