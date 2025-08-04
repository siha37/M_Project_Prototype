using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using System.Collections;

public class Projectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private LayerMask damageableLayers = -1;
    
    // ✅ NetworkConnection으로 owner 정보 저장
    private NetworkConnection ownerConnection;
    private float damage;
    private float lifetime;
    private Vector3 direction;
    private Rigidbody2D rb;
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        // 서버에서만 물리 시뮬레이션 활성화
        rb.linearVelocity = direction * speed;
        
        LogManager.Log(LogCategory.Projectile, $"{gameObject.name} 서버에서 총알 물리 시작: {speed}", this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsServerInitialized)
        {
            // 클라이언트에서는 물리 비활성화 (네트워크 동기화만)
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            rb.isKinematic = true; // 물리 비활성화
            
            LogManager.Log(LogCategory.Projectile, $"{gameObject.name} 클라이언트에서 총알 시각 모드", this);
        }
    }

    // ✅ NetworkConnection으로 초기화
    public void Initialize(NetworkConnection owner, float bulletDamage, float bulletLifetime, Vector3 bulletDirection)
    {
        ownerConnection = owner;
        damage = bulletDamage;
        lifetime = bulletLifetime;
        direction = bulletDirection.normalized;
        
        // 서버에서만 생명주기 관리
        if (IsServerInitialized)
        {
            StartCoroutine(DestroyAfterTime(lifetime));
        }
    }

    public void Initialize(float bulletDamage, float bulletLifetime, Vector3 bulletDirection)
    {
        
    }
    
    // ✅ Owner GameObject 안전하게 가져오기
    private GameObject GetOwnerGameObject()
    {
        if (ownerConnection != null && ownerConnection.IsValid)
        {
            // FirstObject는 연결된 첫 번째 NetworkObject
            if (ownerConnection.FirstObject != null)
            {
                return ownerConnection.FirstObject.gameObject;
            }
            
            // 또는 특정 조건으로 NetworkObject 찾기
            foreach (var networkObj in ownerConnection.Objects)
            {
                // 플레이어나 적 태그로 식별
                if (networkObj.gameObject.CompareTag("Player") || 
                    networkObj.gameObject.CompareTag("Enemy"))
                {
                    return networkObj.gameObject;
                }
            }
        }
        
        return null;
    }
    
    private IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        if (IsServerInitialized)
        {
            ServerManager.Despawn(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 서버에서만 충돌 처리
        if (!IsServerInitialized) return;
        
        GameObject hitObject = collision.gameObject;
        GameObject ownerGameObject = GetOwnerGameObject();
        
        // ✅ Owner와의 충돌 무시
        if (ownerGameObject != null && hitObject == ownerGameObject)
        {
            return;
        }

        string tag = hitObject.tag;

        switch (tag)
        {
            case "Enemy":
            case "Player":
                // ✅ 서버에서만 실제 데미지 처리
                if (damage > 0)
                {
                    ApplyDamage(hitObject, collision.ClosestPoint(transform.position));
                }
                
                // 총알 제거
                DestroyBullet();
                break;

            case "Wall":
                // 벽 충돌 시 총알 제거
                ShowHitEffect(collision.ClosestPoint(transform.position));
                DestroyBullet();
                break;
        }
    }
    
    private void ApplyDamage(GameObject target, Vector3 hitPoint)
    {
        if (!IsServerInitialized) return;
        
        Vector2 hitDirection = rb.linearVelocity.normalized;
        
        // ✅ NetworkConnection 정보와 함께 데미지 처리
        AgentNetworkSync agentSync = target.GetComponent<AgentNetworkSync>();
        if (agentSync != null)
        {
            agentSync.RequestTakeDamage(damage, hitDirection, ownerConnection);
            LogManager.Log(LogCategory.Projectile, $"{gameObject.name} {target.name}에게 {damage} 데미지 (공격자: {ownerConnection?.ClientId})", this);
        }
        
        // 히트 이펙트 표시
        ShowHitEffect(hitPoint);
    }
    
    private void DestroyBullet()
    {
        if (IsServerInitialized)
        {
            ServerManager.Despawn(gameObject);
        }
    }
    
    [ObserversRpc]
    private void ShowHitEffect(Vector3 position)
    {
        PlayHitEffect(position);
    }
    
    // 충돌 시 시각/음향 효과
    private void PlayHitEffect(Vector3 position)
    {
        LogManager.Log(LogCategory.Projectile, $"{gameObject.name} 충돌 효과 재생: {position}", this);
        
        // TODO: 충돌 파티클, 사운드 등 구현
        // ParticleSystem hitEffect = GetComponent<ParticleSystem>();
        // if (hitEffect != null) 
        // {
        //     hitEffect.transform.position = position;
        //     hitEffect.Play();
        // }
        
        // AudioSource.PlayClipAtPoint(hitSound, position);
    }
    
    // ✅ 디버그 정보
    private void OnValidate()
    {
        if (Application.isPlaying && ownerConnection != null)
        {
            LogManager.Log(LogCategory.Projectile, $"{gameObject.name} Owner: {ownerConnection.ClientId}, Damage: {damage}, Lifetime: {lifetime}", this);
        }
    }
} 