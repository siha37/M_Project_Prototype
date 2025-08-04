using System.Collections;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object
{
    public class Status : MonoBehaviour
    {
        public const float maxHp = 100f;
        public float currentHp;
        public bool isDead = false;

        public bool IsDead => isDead;
        protected virtual void Start()
        {
            currentHp = maxHp;
        }

        public virtual void TakeDamage(float damage, Vector2 hitDirection = default)
        {
            if (isDead) return;
        
            currentHp -= damage;
        }

        protected virtual IEnumerator DeathSequence()
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

        protected virtual void OnDeath()
        {
            // 기본 사망 처리 - 오브젝트 제거
            Destroy(gameObject);
        }
    }
}
