using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet;

public class BulletManager : NetworkBehaviour
{
    public static BulletManager Instance { get; private set; }
    
    [Header("Bullet Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private int expandSize = 50;           // 확장 시 추가할 개수
    [SerializeField] private int maxPoolSize = 500;         // 최대 풀 크기
    [SerializeField] private bool enableDynamicExpansion = true; // 동적 확장 활성화
    
    // 서버용: 실제 게임 로직 총알들
    private List<ServerBullet> activeBullets = new List<ServerBullet>();
    private Queue<ServerBullet> bulletPool = new Queue<ServerBullet>();
    
    // 클라이언트용: 시각 전용 총알들  
    private Queue<GameObject> visualBulletPool = new Queue<GameObject>();
    // ✅ 성능 최적화: HashSet과 List 병용
    private HashSet<GameObject> activeVisualBulletsSet = new HashSet<GameObject>();
    private List<GameObject> activeVisualBullets = new List<GameObject>(); // 순회 및 디버깅용
    
    public override void OnStartServer()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeServerPool();
            Debug.Log("[BulletManager] 서버 초기화 완료 - 발사 준비됨");
        }
        else
        {
            Debug.LogWarning("[BulletManager] 서버 인스턴스가 이미 존재합니다.");
        }
    }
    
    public override void OnStartClient()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[BulletManager] 클라이언트 인스턴스 설정됨");
        }
        
        // ✅ Host 모드 지원: 서버에서도 시각 풀 초기화 (Host일 때 시각적 표현 필요)
        InitializeVisualPool();
        Debug.Log("[BulletManager] 시각 풀 초기화 완료 (Host 모드 지원)");
    }
    
    private void InitializeServerPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            ServerBullet bullet = new ServerBullet();
            bulletPool.Enqueue(bullet);
        }
        Debug.Log($"[BulletManager] 서버 총알 풀 초기화: {initialPoolSize}개");
    }
    
    // 서버 풀 동적 확장
    private bool ExpandServerPool()
    {
        if (!enableDynamicExpansion)
        {
            Debug.LogWarning("[BulletManager] 동적 확장이 비활성화되어 있습니다.");
            return false;
        }
        
        int currentTotalSize = bulletPool.Count + activeBullets.Count;
        if (currentTotalSize >= maxPoolSize)
        {
            Debug.LogError($"[BulletManager] 최대 풀 크기 도달: {maxPoolSize}개");
            return false;
        }
        
        int actualExpandSize = Mathf.Min(expandSize, maxPoolSize - currentTotalSize);
        
        for (int i = 0; i < actualExpandSize; i++)
        {
            ServerBullet bullet = new ServerBullet();
            bulletPool.Enqueue(bullet);
        }
        
        Debug.Log($"[BulletManager] 서버 풀 확장: +{actualExpandSize}개 (총 {currentTotalSize + actualExpandSize}개)");
        return true;
    }
    
    private void InitializeVisualPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateVisualBulletPoolObject();
        }
        Debug.Log($"[BulletManager] 클라이언트 시각 풀 초기화: {initialPoolSize}개");
    }
    
    private void CreateVisualBulletPoolObject()
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bullet.SetActive(false);
        
        // 시각 전용 설정
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null) rb.isKinematic = true;
        
        // 네트워크 컴포넌트 제거 (시각 전용이므로)
        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null) DestroyImmediate(netObj);
        
        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null) DestroyImmediate(proj);
        
        visualBulletPool.Enqueue(bullet);
    }
    
    // 시각 풀 동적 확장
    private bool ExpandVisualPool()
    {
        if (!enableDynamicExpansion)
        {
            Debug.LogWarning("[BulletManager] 동적 확장이 비활성화되어 있습니다.");
            return false;
        }
        
        int currentTotalSize = visualBulletPool.Count + activeVisualBullets.Count;
        if (currentTotalSize >= maxPoolSize)
        {
            Debug.LogError($"[BulletManager] 최대 시각 풀 크기 도달: {maxPoolSize}개");
            return false;
        }
        
        int actualExpandSize = Mathf.Min(expandSize, maxPoolSize - currentTotalSize);
        
        for (int i = 0; i < actualExpandSize; i++)
        {
            CreateVisualBulletPoolObject();
        }
        
        Debug.Log($"[BulletManager] 시각 풀 확장: +{actualExpandSize}개 (총 {currentTotalSize + actualExpandSize}개)");
        return true;
    }
    
    // ✅ NetworkConnection을 지원하는 새로운 메서드 추가
    [ServerRpc(RequireOwnership = false)]
    public void FireBulletWithConnection(Vector3 startPos, float angle, float speed, float damage, float lifetime, NetworkConnection shooter)
    {
        if (!IsServer) return;
        
        if (bulletPool.Count > 0)
        {
            ServerBullet bullet = bulletPool.Dequeue();
            bullet.InitializeWithConnection(startPos, angle, speed, damage, lifetime, shooter);
            activeBullets.Add(bullet);
            
            // 모든 클라이언트에 시각 총알 생성 요청
            CreateVisualBulletRpc(startPos, angle, speed, lifetime);
        }
        else
        {
            // ✅ 풀 확장 시도
            Debug.LogWarning("[BulletManager] 서버 총알 풀이 고갈되었습니다! 풀 확장을 시도합니다...");
            
            if (ExpandServerPool() && bulletPool.Count > 0)
            {
                // 확장 성공 시 다시 시도
                ServerBullet bullet = bulletPool.Dequeue();
                bullet.InitializeWithConnection(startPos, angle, speed, damage, lifetime, shooter);
                activeBullets.Add(bullet);
                
                CreateVisualBulletRpc(startPos, angle, speed, lifetime);
                Debug.Log($"[BulletManager] 풀 확장 후 서버 총알 발사: {activeBullets.Count}개 활성");
            }
            else
            {
                Debug.LogError("[BulletManager] 풀 확장 실패! 총알 발사를 취소합니다.");
            }
        }
    }
    
    // 모든 클라이언트에서 시각 총알 생성
    [ObserversRpc]
    private void CreateVisualBulletRpc(Vector3 startPos, float angle, float speed, float lifetime)
    {
        // ✅ Host 모드 지원: 서버도 시각 총알 생성 (Host일 때 시각적 표현 필요)
        
        if (visualBulletPool.Count > 0)
        {
            GameObject visualBullet = visualBulletPool.Dequeue();
            visualBullet.transform.position = startPos;
            visualBullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            visualBullet.SetActive(true);
            
            // 시각 총알 이동
            StartCoroutine(MoveVisualBulletWithPhysics(visualBullet, speed, lifetime));
            activeVisualBulletsSet.Add(visualBullet); // HashSet에 추가
            activeVisualBullets.Add(visualBullet); // List에 추가 (순회용)
            
            string roleText = IsServer ? "(Host/Server)" : "(Client)";
            Debug.Log($"[BulletManager] 시각 총알 생성 {roleText}: {activeVisualBullets.Count}개 활성");
        }
        else
        {
            // ✅ 시각 풀 확장 시도
            Debug.LogWarning("[BulletManager] 시각 총알 풀이 고갈되었습니다! 풀 확장을 시도합니다...");
            
            if (ExpandVisualPool() && visualBulletPool.Count > 0)
            {
                // 확장 성공 시 다시 시도
                GameObject visualBullet = visualBulletPool.Dequeue();
                visualBullet.transform.position = startPos;
                visualBullet.transform.rotation = Quaternion.Euler(0, 0, angle);
                visualBullet.SetActive(true);
                
                StartCoroutine(MoveVisualBulletWithPhysics(visualBullet, speed, lifetime));
                activeVisualBulletsSet.Add(visualBullet); // HashSet에 추가
                activeVisualBullets.Add(visualBullet); // List에 추가 (순회용)
                string roleText = IsServer ? "(Host/Server)" : "(Client)";
                Debug.Log($"[BulletManager] 풀 확장 후 시각 총알 생성 {roleText}: {activeVisualBullets.Count}개 활성");
            }
            else
            {
                Debug.LogError("[BulletManager] 시각 풀 확장 실패! 시각 총알 생성을 취소합니다.");
            }
        }
    }
    
    // ✅ 서버 총알과 동일한 물리 적용
    private System.Collections.IEnumerator MoveVisualBulletWithPhysics(GameObject bullet, float speed, float lifetime)
    {
        Vector3 direction = bullet.transform.right;
        float elapsed = 0f;
        LayerMask wallLayer = LayerMask.GetMask("Wall");
        
        while (elapsed < lifetime && bullet.activeInHierarchy)
        {
            Vector3 nextPos = bullet.transform.position + direction * speed * Time.deltaTime;
            
            // ✅ 벽 충돌 검사 (시각 총알도 벽에서 멈춤)
            if (Physics2D.Linecast(bullet.transform.position, nextPos, wallLayer))
            {
                break; // 벽에 충돌하면 이동 중단
            }
            
            bullet.transform.position = nextPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 시각 총알 반납
        ReturnVisualBullet(bullet);
    }
    
    private void ReturnVisualBullet(GameObject bullet)
    {
        if (activeVisualBulletsSet.Contains(bullet))
        {
            activeVisualBulletsSet.Remove(bullet);
            activeVisualBullets.Remove(bullet); // List에서도 제거
            bullet.SetActive(false);
            visualBulletPool.Enqueue(bullet);
        }
    }
    
    // 서버에서 총알 충돌 처리
    public void OnBulletHit(ServerBullet bullet, GameObject target)
    {
        if (!IsServer) return;
        
        // ✅ 공격자 NetworkConnection 복원
        NetworkConnection attacker = null;
        if (bullet.ownerNetworkId > 0)
        {
            // FishNet 공식 방식: ServerManager를 통한 안전한 Connection 조회
            InstanceFinder.ServerManager.Clients.TryGetValue((int)bullet.ownerNetworkId, out attacker);
        }
        
        // 데미지 처리
        if (bullet.damage > 0)
        {
            AgentNetworkSync agentSync = target.GetComponent<AgentNetworkSync>();
            if (agentSync != null)
            {
                Vector2 hitDirection = bullet.GetDirection();
                // ✅ 공격자 정보와 함께 데미지 처리
                agentSync.RequestTakeDamage(bullet.damage, hitDirection, attacker);
            }
        }
        
        // 모든 클라이언트에 충돌 효과 전송
        ShowBulletHitEffect(bullet.position, target.transform.position);
        
        // 총알 반납
        ReturnServerBullet(bullet);
    }
    
    [ObserversRpc]
    private void ShowBulletHitEffect(Vector3 bulletPos, Vector3 hitPos)
    {
        Debug.Log($"[BulletManager] 총알 충돌 효과: {bulletPos} -> {hitPos}");
        // TODO: 파티클, 사운드 등 충돌 효과 구현
    }
    
    private void ReturnServerBullet(ServerBullet bullet)
    {
        activeBullets.Remove(bullet);
        bullet.Reset();
        bulletPool.Enqueue(bullet);
    }
    
    private void Update()
    {
        if (IsServer)
        {
            // 서버에서 모든 활성 총알 업데이트
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                ServerBullet bullet = activeBullets[i];
                bullet.Update(Time.deltaTime);
                
                // 생명주기 종료 시 반납
                if (bullet.IsExpired())
                {
                    ReturnServerBullet(bullet);
                }
            }
        }
    }
    
    // 디버그 정보
    public void LogPoolStatus()
    {
        if (IsServer)
        {
            int totalServer = activeBullets.Count + bulletPool.Count;
            Debug.Log($"[BulletManager] 서버 - 활성: {activeBullets.Count}, 풀: {bulletPool.Count}, 총계: {totalServer}/{maxPoolSize}");
        }
        else
        {
            int totalVisual = activeVisualBullets.Count; // HashSet은 직접 크기를 가져올 수 없으므로 List 크기를 사용
            Debug.Log($"[BulletManager] 클라이언트 - 활성: {activeVisualBullets.Count}, 풀: {visualBulletPool.Count}, 총계: {totalVisual}/{maxPoolSize}");
        }
    }
    
    // 풀 통계 정보
    public PoolStats GetPoolStats()
    {
        if (IsServer)
        {
            return new PoolStats
            {
                active = activeBullets.Count,
                pooled = bulletPool.Count,
                total = activeBullets.Count + bulletPool.Count,
                maxSize = maxPoolSize,
                utilizationRate = (float)(activeBullets.Count + bulletPool.Count) / maxPoolSize
            };
        }
        else
        {
            return new PoolStats
            {
                active = activeVisualBullets.Count,
                pooled = visualBulletPool.Count,
                total = activeVisualBullets.Count + visualBulletPool.Count,
                maxSize = maxPoolSize,
                utilizationRate = (float)(activeVisualBullets.Count + visualBulletPool.Count) / maxPoolSize
            };
        }
    }
    
    [System.Serializable]
    public struct PoolStats
    {
        public int active;
        public int pooled;
        public int total;
        public int maxSize;
        public float utilizationRate;
        
        public override string ToString()
        {
            return $"Active: {active}, Pooled: {pooled}, Total: {total}/{maxSize} ({utilizationRate:P1})";
        }
    }
    
    // ✅ 풀 상태 실시간 모니터링
    [System.Serializable]
    public class PoolDebugInfo
    {
        public int serverActive;
        public int serverPooled;
        public int clientActive; 
        public int clientPooled;
        public float serverUtilization;
        public float clientUtilization;
        public int totalServerBullets;
        public int totalClientBullets;
    }

    public PoolDebugInfo GetDebugInfo()
    {
        return new PoolDebugInfo
        {
            serverActive = activeBullets.Count,
            serverPooled = bulletPool.Count,
            clientActive = activeVisualBullets.Count,
            clientPooled = visualBulletPool.Count,
            serverUtilization = (float)activeBullets.Count / maxPoolSize,
            clientUtilization = (float)activeVisualBullets.Count / maxPoolSize,
            totalServerBullets = activeBullets.Count + bulletPool.Count,
            totalClientBullets = activeVisualBullets.Count + visualBulletPool.Count
        };
    }
    
    // ✅ 인스펙터에서 실시간 확인 가능
    [Header("Debug Info (Runtime Only)")]
    [SerializeField] private PoolDebugInfo debugInfo;
    
    private void LateUpdate()
    {
        // ✅ 매 프레임 디버그 정보 업데이트 (에디터에서만)
        #if UNITY_EDITOR
        debugInfo = GetDebugInfo();
        #endif
    }
}

// 서버 전용 총알 클래스 (NetworkObject 없음)
[System.Serializable]
public class ServerBullet
{
    public Vector3 position;
    public Vector3 direction;
    public float speed;
    public float damage;
    public float lifetime;
    public float elapsed;
    public uint ownerNetworkId;
    
    public void Initialize(Vector3 startPos, float angle, float speed, float damage, float lifetime, uint ownerNetworkId)
    {
        this.position = startPos;
        this.direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.elapsed = 0f;
        this.ownerNetworkId = ownerNetworkId;
    }

    // ✅ FishNet 공식 권장: NetworkConnection에서 안전하게 Owner ID 추출
    public void InitializeWithConnection(Vector3 startPos, float angle, float speed, float damage, float lifetime, NetworkConnection shooter)
    {
        this.position = startPos;
        this.direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.elapsed = 0f;
        
        // ✅ FishNet 공식 권장: 안전한 Owner ID 추출
        this.ownerNetworkId = (uint)(shooter?.ClientId ?? 0);  // OwnerId 대신 ClientId 사용
    }
    
    public void Update(float deltaTime)
    {
        position += direction * speed * deltaTime;
        elapsed += deltaTime;
        
        // ✅ 개선된 충돌 검사 구현
        CheckCollisions();
    }
    
    private void CheckCollisions()
    {
        // ✅ 레이어 필터링과 Owner 체크 추가
        LayerMask targetLayers = LayerMask.GetMask("Player", "Enemy", "Wall");
        Collider2D hit = Physics2D.OverlapCircle(position, 0.1f, targetLayers);
        
        if (hit != null)
        {
            // ✅ Owner와의 충돌 무시
            GameObject ownerObject = GetOwnerGameObject();
            if (ownerObject != null && hit.gameObject == ownerObject)
                return;
                
            // ✅ 태그별 처리
            if (hit.CompareTag("Wall"))
            {
                // 벽 충돌 시 총알 즉시 제거
                BulletManager.Instance.OnBulletHit(this, hit.gameObject);
                return;
            }
            
            if (hit.CompareTag("Player") || hit.CompareTag("Enemy"))
            {
                BulletManager.Instance.OnBulletHit(this, hit.gameObject);
            }
        }
    }
    
    private GameObject GetOwnerGameObject()
    {
        if (ownerNetworkId > 0)
        {
            // ✅ FishNet ServerManager를 통한 안전한 Connection 조회
            if (InstanceFinder.ServerManager.Clients.TryGetValue((int)ownerNetworkId, out NetworkConnection conn))
            {
                return conn.FirstObject?.gameObject;
            }
        }
        return null;
    }
    
    public bool IsExpired()
    {
        return elapsed >= lifetime;
    }
    
    public Vector2 GetDirection()
    {
        return direction;
    }
    
    public void Reset()
    {
        position = Vector3.zero;
        direction = Vector3.zero;
        speed = 0f;
        damage = 0f;
        lifetime = 0f;
        elapsed = 0f;
        ownerNetworkId = 0;
    }
} 