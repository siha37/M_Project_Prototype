using UnityEngine;

public class State : MonoBehaviour
{
    public const float maxHp = 100f;
    public float currentHp;


    protected virtual void Start()
    {
        currentHp = maxHp;
    }

    public virtual void TakeDamage(float damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
            // 체력이 0이 되었을 때의 처리는 상속받는 클래스에서 구현
        }
    }
}
