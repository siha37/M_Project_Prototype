using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._2._Projectile
{
    public class BulletManager : NetworkBehaviour
    {
        public static BulletManager Instance { get; private set; }
    
        [Header("Bullet Pool Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject bulletsParent;
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
    
        // ✅ ID 기반 시각 총알 관리 추가
        private Dictionary<uint, GameObject> visualBulletsById = new Dictionary<uint, GameObject>();
        private Dictionary<uint, Coroutine> bulletCoroutines = new Dictionary<uint, Coroutine>();
    
        public override void OnStartServer()
        {
            if (!Instance)
            {
                Instance = this;
                InitializeServerPool();
                LogManager.Log(LogCategory.Projectile, "BulletManager 서버 초기화 완료 - 발사 준비됨", this);
            }
            else
            {
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 서버 인스턴스가 이미 존재합니다.", this);
            }
        }
    
        public override void OnStartClient()
        {
            if (Instance == null)
            {
                Instance = this;
                LogManager.Log(LogCategory.Projectile, "BulletManager 클라이언트 인스턴스 설정됨", this);
            }
        
            // ✅ Host 모드 지원: 서버에서도 시각 풀 초기화 (Host일 때 시각적 표현 필요)
            InitializeVisualPool();
            LogManager.Log(LogCategory.Projectile, "BulletManager 시각 풀 초기화 완료 (Host 모드 지원)", this);
        }
    
        private void InitializeServerPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                ServerBullet bullet = new ServerBullet();
                bulletPool.Enqueue(bullet);
            }
            LogManager.Log(LogCategory.Projectile, $"BulletManager 서버 총알 풀 초기화: {initialPoolSize}개", this);
        }
    
        // 서버 풀 동적 확장
        private bool ExpandServerPool()
        {
            if (!enableDynamicExpansion)
            {
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 동적 확장이 비활성화되어 있습니다.", this);
                return false;
            }
        
            int currentTotalSize = bulletPool.Count + activeBullets.Count;
            if (currentTotalSize >= maxPoolSize)
            {
                LogManager.LogError(LogCategory.Projectile, $"BulletManager 최대 풀 크기 도달: {maxPoolSize}개", this);
                return false;
            }
        
            int actualExpandSize = Mathf.Min(expandSize, maxPoolSize - currentTotalSize);
        
            for (int i = 0; i < actualExpandSize; i++)
            {
                ServerBullet bullet = new ServerBullet();
                bulletPool.Enqueue(bullet);
            }
        
            LogManager.Log(LogCategory.Projectile, $"BulletManager 서버 풀 확장: +{actualExpandSize}개 (총 {currentTotalSize + actualExpandSize}개)", this);
            return true;
        }
    
        private void InitializeVisualPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateVisualBulletPoolObject();
            }
            LogManager.Log(LogCategory.Projectile, $"BulletManager 클라이언트 시각 풀 초기화: {initialPoolSize}개", this);
        }
    
        private void CreateVisualBulletPoolObject()
        {
            GameObject bullet;
            if(bulletsParent)
                bullet = Instantiate(bulletPrefab,bulletsParent.transform);
            else
                bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
        
            // 시각 전용 설정
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb) rb.bodyType = RigidbodyType2D.Kinematic;
        
            // 네트워크 컴포넌트 제거 (시각 전용이므로)
            NetworkObject netObj = bullet.GetComponent<NetworkObject>();
            if (netObj) DestroyImmediate(netObj);
        
            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj) DestroyImmediate(proj);
        
            visualBulletPool.Enqueue(bullet);
        }
    
        // 시각 풀 동적 확장
        private bool ExpandVisualPool()
        {
            if (!enableDynamicExpansion)
            {
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 동적 확장이 비활성화되어 있습니다.", this);
                return false;
            }
        
            int currentTotalSize = visualBulletPool.Count + activeVisualBullets.Count;
            if (currentTotalSize >= maxPoolSize)
            {
                LogManager.LogError(LogCategory.Projectile, $"BulletManager 최대 시각 풀 크기 도달: {maxPoolSize}개", this);
                return false;
            }
        
            int actualExpandSize = Mathf.Min(expandSize, maxPoolSize - currentTotalSize);
        
            for (int i = 0; i < actualExpandSize; i++)
            {
                CreateVisualBulletPoolObject();
            }
        
            LogManager.Log(LogCategory.Projectile, $"BulletManager 시각 풀 확장: +{actualExpandSize}개 (총 {currentTotalSize + actualExpandSize}개)", this);
            return true;
        }
    
        // ✅ NetworkConnection을 지원하는 새로운 메서드 추가
        [ServerRpc(RequireOwnership = false)]
        public void FireBulletWithConnection(Vector3 startPos, float angle, float speed, float damage, float lifetime, NetworkConnection shooter)
        {
            if (!IsServerInitialized) return;
        
            if (bulletPool.Count > 0)
            {
                ServerBullet bullet = bulletPool.Dequeue();
                bullet.InitializeWithConnection(startPos, angle, speed, damage, lifetime, shooter);
                activeBullets.Add(bullet);
            
                // ✅ bulletId 포함하여 시각 총알 생성 요청
                CreateVisualBulletRpc(startPos, angle, speed, lifetime, bullet.bulletId);
            }
            else
            {
                // ✅ 풀 확장 시도
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 서버 총알 풀이 고갈되었습니다! 풀 확장을 시도합니다...", this);
            
                if (ExpandServerPool() && bulletPool.Count > 0)
                {
                    // 확장 성공 시 다시 시도
                    ServerBullet bullet = bulletPool.Dequeue();
                    bullet.InitializeWithConnection(startPos, angle, speed, damage, lifetime, shooter);
                    activeBullets.Add(bullet);
                
                    CreateVisualBulletRpc(startPos, angle, speed, lifetime, bullet.bulletId);
                    LogManager.Log(LogCategory.Projectile, $"BulletManager 풀 확장 후 서버 총알 발사: {activeBullets.Count}개 활성", this);
                }
                else
                {
                    LogManager.LogError(LogCategory.Projectile, "BulletManager 풀 확장 실패! 총알 발사를 취소합니다.", this);
                }
            }
        }
        // ✅ 적군 AI 전용 발사 메서드 추가
        [ServerRpc(RequireOwnership = false)]
        public void FireBulletForEnemy(Vector3 startPos, float angle, float speed, float damage, float lifetime, GameObject enemyObject)
        {
            if (!IsServerInitialized) return;
        
            if (bulletPool.Count > 0)
            {
                ServerBullet bullet = bulletPool.Dequeue();
                bullet.InitializeForEnemy(startPos, angle, speed, damage, lifetime, enemyObject);
                activeBullets.Add(bullet);
            
                CreateVisualBulletRpc(startPos, angle, speed, lifetime, bullet.bulletId);
            }
            else
            {
                // 풀 확장 시도
                if (ExpandServerPool() && bulletPool.Count > 0)
                {
                    ServerBullet bullet = bulletPool.Dequeue();
                    bullet.InitializeForEnemy(startPos, angle, speed, damage, lifetime, enemyObject);
                    activeBullets.Add(bullet);
                
                    CreateVisualBulletRpc(startPos, angle, speed, lifetime, bullet.bulletId);
                }
            }
        }
        // ✅ bulletId 파라미터 추가
        [ObserversRpc]
        private void CreateVisualBulletRpc(Vector3 startPos, float angle, float speed, float lifetime, uint bulletId)
        {
            // ✅ Host 모드 지원: 서버도 시각 총알 생성 (Host일 때 시각적 표현 필요)
        
            if (visualBulletPool.Count > 0)
            {
                GameObject visualBullet = visualBulletPool.Dequeue();
                visualBullet.transform.position = startPos;
                visualBullet.transform.rotation = Quaternion.Euler(0, 0, angle);
                visualBullet.SetActive(true);
            
                // ✅ ID로 매칭 저장
                visualBulletsById[bulletId] = visualBullet;
                Coroutine moveCoroutine = StartCoroutine(MoveVisualBulletWithPhysics(visualBullet, speed, lifetime, bulletId));
                bulletCoroutines[bulletId] = moveCoroutine;
            
                activeVisualBulletsSet.Add(visualBullet); // HashSet에 추가
                activeVisualBullets.Add(visualBullet); // List에 추가 (순회용)
            
                string roleText = IsServer ? "(Host/Server)" : "(Client)";
                LogManager.Log(LogCategory.Projectile, $"BulletManager 시각 총알 생성 {roleText} ID:{bulletId}: {activeVisualBullets.Count}개 활성", this);
            }
            else
            {
                // ✅ 시각 풀 확장 시도
                LogManager.LogWarning(LogCategory.Projectile, "BulletManager 시각 총알 풀이 고갈되었습니다! 풀 확장을 시도합니다...", this);
            
                if (ExpandVisualPool() && visualBulletPool.Count > 0)
                {
                    // 확장 성공 시 다시 시도
                    GameObject visualBullet = visualBulletPool.Dequeue();
                    visualBullet.transform.position = startPos;
                    visualBullet.transform.rotation = Quaternion.Euler(0, 0, angle);
                    visualBullet.SetActive(true);
                
                    visualBulletsById[bulletId] = visualBullet;
                    Coroutine moveCoroutine = StartCoroutine(MoveVisualBulletWithPhysics(visualBullet, speed, lifetime, bulletId));
                    bulletCoroutines[bulletId] = moveCoroutine;
                
                    activeVisualBulletsSet.Add(visualBullet); // HashSet에 추가
                    activeVisualBullets.Add(visualBullet); // List에 추가 (순회용)
                    string roleText = IsServer ? "(Host/Server)" : "(Client)";
                    LogManager.Log(LogCategory.Projectile, $"BulletManager 풀 확장 후 시각 총알 생성 {roleText} ID:{bulletId}: {activeVisualBullets.Count}개 활성", this);
                }
                else
                {
                    LogManager.LogError(LogCategory.Projectile, "BulletManager 시각 풀 확장 실패! 시각 총알 생성을 취소합니다.", this);
                }
            }
        }
    
        // ✅ bulletId 파라미터 추가된 시각 총알 이동
        private System.Collections.IEnumerator MoveVisualBulletWithPhysics(GameObject bullet, float speed, float lifetime, uint bulletId)
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
        
            // ✅ ID 기반 정리
            ReturnVisualBulletById(bulletId);
        }
    
        // ✅ ID 기반 시각 총알 반납
        private void ReturnVisualBulletById(uint bulletId)
        {
            if (visualBulletsById.TryGetValue(bulletId, out GameObject bullet))
            {
                // Dictionary에서 제거
                visualBulletsById.Remove(bulletId);
                bulletCoroutines.Remove(bulletId);
            
                // 기존 컬렉션에서 제거
                if (activeVisualBulletsSet.Contains(bullet))
                {
                    activeVisualBulletsSet.Remove(bullet);
                    activeVisualBullets.Remove(bullet);
                    bullet.SetActive(false);
                    visualBulletPool.Enqueue(bullet);
                }
            }
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
            if (!IsServerInitialized) return;
        
            // ✅ 공격자 NetworkConnection 복원
            NetworkConnection attacker = null;
            if (bullet.ownerNetworkId != 111)
            {
                // FishNet 공식 방식: ServerManager를 통한 안전한 Connection 조회
                InstanceFinder.ServerManager.Clients.TryGetValue((int)bullet.ownerNetworkId, out attacker);
            }
        
            // ✅ 충돌 로깅 개선
            string ownerTypeText = bullet.ownerType.ToString();
            string targetTag = target.tag;
            LogManager.Log(LogCategory.Projectile, 
                $"총알 충돌: {ownerTypeText} 총알(ID:{bullet.bulletId}) -> {targetTag}({target.name})", this);
        
            // 데미지 처리
            if (bullet.damage > 0)
            {
                AgentNetworkSync agentSync = target.GetComponent<AgentNetworkSync>();
                if (agentSync)
                {
                    Vector2 hitDirection = bullet.GetDirection();
                    // ✅ 공격자 정보와 함께 데미지 처리
                    agentSync.RequestTakeDamage(bullet.damage, hitDirection, attacker);
                    LogManager.Log(LogCategory.Projectile, 
                        $"데미지 적용: {bullet.damage} (공격자:{attacker?.ClientId}, 타겟:{target.name})", this);
                }
            }
        
            // ✅ bulletId 포함하여 충돌 효과 전송
            ShowBulletHitEffect(bullet.position, target.transform.position, bullet.bulletId);
        
            // 총알 반납
            ReturnServerBullet(bullet);
        }
    
        // ✅ bulletId로 정확한 시각 총알 삭제
        [ObserversRpc]
        private void ShowBulletHitEffect(Vector3 bulletPos, Vector3 hitPos, uint bulletId)
        {
            // 정확한 총알 찾기 및 삭제
            if (visualBulletsById.TryGetValue(bulletId, out GameObject targetBullet))
            {
                // 코루틴 정지
                if (bulletCoroutines.TryGetValue(bulletId, out Coroutine coroutine))
                {
                    StopCoroutine(coroutine);
                }
            
                // 총알 반납
                ReturnVisualBulletById(bulletId);
            
                LogManager.Log(LogCategory.Projectile, $"BulletManager 시각 총알 충돌 삭제 ID:{bulletId}", this);
            }
            else
            {
                LogManager.LogWarning(LogCategory.Projectile, $"BulletManager 시각 총알을 찾을 수 없음 ID:{bulletId}", this);
            }
        
            // 충돌 효과 표시
            PlayHitEffect(bulletPos, hitPos);
        }
    
        // ✅ 충돌 효과 재생 (별도 메서드)
        private void PlayHitEffect(Vector3 bulletPos, Vector3 hitPos)
        {
            LogManager.Log(LogCategory.Projectile, $"BulletManager 총알 충돌 효과: {bulletPos} -> {hitPos}", this);
            // TODO: 파티클, 사운드 등 충돌 효과 구현
            // ParticleSystem hitEffect = GetHitEffect(hitPos);
            // if (hitEffect != null) hitEffect.Play();
            // AudioSource.PlayClipAtPoint(hitSound, hitPos);
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
                LogManager.Log(LogCategory.Projectile, $"BulletManager 서버 - 활성: {activeBullets.Count}, 풀: {bulletPool.Count}, 총계: {totalServer}/{maxPoolSize}", this);
            }
            else
            {
                int totalVisual = activeVisualBullets.Count; // HashSet은 직접 크기를 가져올 수 없으므로 List 크기를 사용
                LogManager.Log(LogCategory.Projectile, $"BulletManager 클라이언트 - 활성: {activeVisualBullets.Count}, 풀: {visualBulletPool.Count}, 총계: {totalVisual}/{maxPoolSize}", this);
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
    
        // ✅ 풀 상태 실시간 모니터링 (통합된 방식)
        [System.Serializable]
        public class PoolDebugInfo
        {
            public string role;              // "Host", "Client"  
            public int activeBullets;        // 현재 활성 총알 수
            public int pooledBullets;        // 풀에 있는 총알 수
            public int totalBullets;         // 총 총알 수
            public float utilization;        // 사용률
        
            // 호스트용 상세 정보 (디버깅용)
            public int serverLogicBullets;   // 서버 연산 총알 (호스트만)
            public int visualBullets;        // 시각 총알
        
            public override string ToString()
            {
                if (role == "Host")
                {
                    return $"{role}: Active {activeBullets}, Pooled {pooledBullets}, Total {totalBullets}/500 ({utilization:P1}) [Logic:{serverLogicBullets}, Visual:{visualBullets}]";
                }
                else
                {
                    return $"{role}: Active {activeBullets}, Pooled {pooledBullets}, Total {totalBullets}/500 ({utilization:P1})";
                }
            }
        }

        public PoolDebugInfo GetDebugInfo()
        {
            if (IsServer) // 호스트
            {
                return new PoolDebugInfo
                {
                    role = "Host",
                    activeBullets = activeBullets.Count,  // 메인은 연산 총알
                    pooledBullets = bulletPool.Count,
                    totalBullets = activeBullets.Count + bulletPool.Count,
                    utilization = (float)activeBullets.Count / maxPoolSize,
                
                    // 상세 정보
                    serverLogicBullets = activeBullets.Count,
                    visualBullets = activeVisualBullets.Count
                };
            }
            else // 게스트
            {
                return new PoolDebugInfo
                {
                    role = "Client",
                    activeBullets = activeVisualBullets.Count,  // 메인은 시각 총알
                    pooledBullets = visualBulletPool.Count,
                    totalBullets = activeVisualBullets.Count + visualBulletPool.Count,
                    utilization = (float)activeVisualBullets.Count / maxPoolSize,
                
                    // 상세 정보 (게스트에서는 의미 없으므로 0)
                    serverLogicBullets = 0,
                    visualBullets = activeVisualBullets.Count
                };
            }
        }
    
        // ✅ 인스펙터에서 실시간 확인 가능
        [Header("Debug Info (Runtime Only)")]
        [SerializeField] private PoolDebugInfo debugInfo;
    
        private void LateUpdate()
        {
            // ✅ 매 프레임 디버그 정보 업데이트 (모든 환경에서)
            debugInfo = GetDebugInfo();
        }
    }

// 서버 전용 총알 클래스 (NetworkObject 없음)
    [System.Serializable]
    public class ServerBullet
    {
        public uint bulletId;           // ✅ 고유 ID 추가
        public Vector3 position;
        public Vector3 direction;
        public float speed;
        public float damage;
        public float lifetime;
        public float elapsed;
        public uint ownerNetworkId;
        public GameObject ownerGameObject;
    
        // ✅ 발사자 타입 구분 추가
        public BulletOwnerType ownerType;
    
        private static uint nextBulletId = 1; // ✅ 고유 ID 생성기
    
        // ✅ 발사자 타입 열거형
        public enum BulletOwnerType
        {
            Player,     // 플레이어가 발사한 총알
            Enemy,      // 적군이 발사한 총알
            Neutral     // 중립 (환경 등)
        }
    
        public void Initialize(Vector3 startPos, float angle, float speed, float damage, float lifetime, uint ownerNetworkId)
        {
            this.bulletId = nextBulletId++; // ✅ 고유 ID 할당
            this.position = startPos;
            this.direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            this.speed = speed;
            this.damage = damage;
            this.lifetime = lifetime;
            this.elapsed = 0f;
            this.ownerNetworkId = ownerNetworkId;
            this.ownerType = BulletOwnerType.Player; // 기본값
        }

        // ✅ FishNet 공식 권장: NetworkConnection에서 안전하게 Owner ID 추출
        public void InitializeWithConnection(Vector3 startPos, float angle, float speed, float damage, float lifetime, NetworkConnection shooter)
        {
            this.bulletId = nextBulletId++; // ✅ 고유 ID 할당
            this.position = startPos;
            this.direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            this.speed = speed;
            this.damage = damage;
            this.lifetime = lifetime;
            this.elapsed = 0f;
        
            // ✅ FishNet 공식 권장: 안전한 Owner ID 추출
            this.ownerNetworkId = (uint)(shooter?.ClientId ?? 0);  // OwnerId 대신 ClientId 사용
        
            // ✅ 발사자 타입 자동 감지
            this.ownerType = DetermineOwnerType(shooter);
            
            this.ownerGameObject = shooter?.FirstObject.gameObject;
        }
    
// ServerBullet 클래스에 적군 초기화 메서드 추가
        public void InitializeForEnemy(Vector3 startPos, float angle, float speed, float damage, float lifetime, GameObject enemyObject)
        {
            this.bulletId = nextBulletId++;
            this.position = startPos;
            this.direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            this.speed = speed;
            this.damage = damage;
            this.lifetime = lifetime;
            this.elapsed = 0f;
        
            // ✅ 적군은 NetworkConnection 대신 GameObject 참조 저장
            this.ownerNetworkId = 111; // 적군은 NetworkConnection이 없으므로 111
            this.ownerType = BulletOwnerType.Enemy;
        
            // ✅ GameObject 참조를 위한 추가 필드 필요
            this.ownerGameObject = enemyObject;
        }

    
        // ✅ 발사자 타입 감지 메서드
        private BulletOwnerType DetermineOwnerType(NetworkConnection shooter)
        {
            if (shooter == null) return BulletOwnerType.Neutral;
        
            // ✅ 적군 AI는 NetworkConnection이 null이거나 FirstObject가 비어있을 수 있음
            // 이 경우 GameObject를 직접 확인해야 함
            GameObject shooterObj = null;
        
            if (shooter.FirstObject)
            {
                shooterObj = shooter.FirstObject.gameObject;
            }
            else
            {
                // ✅ FirstObject가 비어있는 경우, 다른 방법으로 발사자 확인
                // 예: 발사 위치나 다른 식별자로 판단
                LogManager.Log(LogCategory.Projectile, 
                    $"발사자 NetworkConnection의 FirstObject가 비어있음. ClientId: {shooter.ClientId}");
            
                // ✅ 적군 AI는 보통 특정 태그나 컴포넌트로 식별 가능
                // 이 부분은 실제 게임 구조에 따라 조정 필요
                return BulletOwnerType.Enemy; // 기본적으로 적군으로 가정
            }
        
            if (shooterObj)
            {
                // 적군 컴포넌트 확인
                if (shooterObj.CompareTag("Enemy") || shooterObj.TryGetComponent(out EnemyControll controller))
                {
                    return BulletOwnerType.Enemy;
                }
            
                // 플레이어 컴포넌트 확인
                if (shooterObj.CompareTag("Player"))
                {
                    return BulletOwnerType.Player;
                }
            }
        
            return BulletOwnerType.Neutral;
        }
    
        public void Update(float deltaTime)
        {
            position += direction * (speed * deltaTime);
            elapsed += deltaTime;
        
            // ✅ 개선된 충돌 검사 구현
            CheckCollisions();
        }
    
        private void CheckCollisions()
        {
            // ✅ 레이어 필터링과 Owner 체크 추가
            LayerMask targetLayers = LayerMask.GetMask("Player","Enemy", "Wall");
            Collider2D hit = Physics2D.OverlapCircle(position, 0.1f, targetLayers);
        
            if (hit)
            {
                // ✅ Owner와의 충돌 무시
                GameObject ownerObject = GetOwnerGameObject();
                if (ownerObject && hit.gameObject == ownerObject)
                    return;
                
                // ✅ 태그별 처리
                if (hit.CompareTag("Wall"))
                {
                    // 벽 충돌 시 총알 즉시 제거
                    BulletManager.Instance.OnBulletHit(this, hit.gameObject);
                    return;
                }
            
                // ✅ 팀/진영 체크 추가
                if (ShouldHitTarget(hit.gameObject))
                {
                    BulletManager.Instance.OnBulletHit(this, hit.gameObject);
                }
            }
        }
    
        // ✅ 팀/진영 충돌 체크 로직
        private bool ShouldHitTarget(GameObject target)
        {
            // 플레이어가 발사한 총알
            if (ownerType == BulletOwnerType.Player)
            {
                // 플레이어 총알은 적군과 플레이어 모두에게 데미지 (팀킬 가능)
                bool shouldHit = target.CompareTag("Player") || target.CompareTag("Enemy");
                if (target == ownerGameObject)
                    shouldHit = false;
                if (shouldHit)
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"플레이어 총알 -> {target.tag} 허용 (팀킬 가능)");
                }
                return shouldHit;
            }
            // 적군이 발사한 총알
            else if (ownerType == BulletOwnerType.Enemy)
            {
                // 적군 총알은 오직 플레이어에게만 데미지
                bool shouldHit = target.CompareTag("Player");
                if (shouldHit)
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"적군 총알 -> {target.tag} 허용 (플레이어만)");
                }
                else if (target.CompareTag("Enemy"))
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"적군 총알 -> {target.tag} 차단 (적군끼리 맞지 않음)");
                }
                return shouldHit;
            }
        
            // 중립 총알 (환경 등)
            else if (ownerType == BulletOwnerType.Neutral)
            {
                // 중립 총알은 모든 대상에게 데미지
                bool shouldHit = target.CompareTag("Player") || target.CompareTag("Enemy");
                if (shouldHit)
                {
                    LogManager.Log(LogCategory.Projectile, 
                        $"중립 총알 -> {target.tag} 허용 (모든 대상)");
                }
                return shouldHit;
            }
        
            return false;
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
            bulletId = 0;               // ✅ ID 초기화 추가
            position = Vector3.zero;
            direction = Vector3.zero;
            speed = 0f;
            damage = 0f;
            lifetime = 0f;
            elapsed = 0f;
            ownerNetworkId = 0;
            ownerType = BulletOwnerType.Neutral; // ✅ 타입 초기화 추가
        }
    }
}