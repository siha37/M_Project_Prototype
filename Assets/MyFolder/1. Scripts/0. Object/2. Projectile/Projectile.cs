using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private float lifetime;
    private Rigidbody2D rb;
    private GameObject owner;  // 생성 주체 저장 변수

    public void Init(float speed, float damage, float lifetime, GameObject owner)
    {
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.owner = owner;

        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.right * speed;

        // lifetime 초 후 오브젝트 삭제
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 생성 주체와의 충돌은 무시
        if (collision.gameObject == owner) return;

        string tag = collision.gameObject.tag;

        switch (tag)
        {
            case "Enemy":
            case "Player":
                // State 컴포넌트를 통해 데미지 처리
                State targetState = collision.GetComponent<State>();
                if (targetState != null)
                {
                    // 총알의 이동 방향을 hitDirection으로 전달
                    Vector2 hitDirection = rb.linearVelocity.normalized;
                    targetState.TakeDamage(damage, hitDirection);
                }
                Destroy(gameObject);
                break;

            case "Wall":
                Destroy(gameObject);
                break;
        }
    }
} 