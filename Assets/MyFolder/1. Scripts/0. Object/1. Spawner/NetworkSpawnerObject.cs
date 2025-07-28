using System.Collections;
using FishNet;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._1._Spawner
{
    public class NetworkSpawnerObject : NetworkBehaviour
    {
        [Header("스폰 설정")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float spawnInterval = 5f; // 스폰 간격
        [SerializeField] private float spawnDelay = 0f; // 초기 스폰 지연 시간
        [SerializeField] private int maxSpawnCount = 5; // 이 스포너가 생성할 최대 적 수량
        [SerializeField] private bool enableDebugLogs = true; // 디버그 로그 활성화
    
        private int currentSpawnedCount = 0;
        private Coroutine spawnCoroutine;
    
        public override void OnStartServer()
        {
            if (enemyPrefab == null)
            {
                LogError("스폰할 프리팹이 설정되지 않았습니다.");
                enabled = false;
                return;
            }

            // Enemy 프리팹에 NetworkObject가 있는지 확인
            var enemyNetworkObject = enemyPrefab.GetComponent<NetworkObject>();
            if (enemyNetworkObject == null)
            {
                LogError("Enemy 프리팹에 NetworkObject 컴포넌트가 없습니다. 네트워크 스폰이 불가능합니다.");
                enabled = false;
                return;
            }

            // NetworkEnemyManager 대기 및 등록
            StartCoroutine(WaitForNetworkEnemyManagerAndRegister());
        
            Log("서버에서 네트워크 스포너 시작");
        }

        public override void OnStartClient()
        {
            if (!IsServerInitialized)
            {
                // 클라이언트에서는 스폰 로직 비활성화
                enabled = false;
                Log("클라이언트에서 스포너 비활성화");
            }
        }

        private IEnumerator WaitForNetworkEnemyManagerAndRegister()
        {
            // NetworkEnemyManager 초기화 대기
            float waitTime = 0f;
            const float maxWaitTime = 10f; // 최대 10초 대기
        
            while (waitTime < maxWaitTime)
            {
                // NetworkEnemyManager 인스턴스 존재 확인
                if (NetworkEnemyManager.Instance != null)
                {
                    // NetworkEnemyManager가 서버로 완전히 초기화되었는지 확인
                    if (NetworkEnemyManager.Instance.IsServer)
                    {
                        Log("NetworkEnemyManager 서버 초기화 완료 - 연결 성공");
                        break;
                    }
                    else
                    {
                        Log("NetworkEnemyManager 인스턴스 존재하지만 서버 초기화 대기 중...");
                    }
                }
                else
                {
                    Log("NetworkEnemyManager 인스턴스 대기 중...");
                }
            
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
        
            if (!NetworkEnemyManager.Instance)
            {
                LogError("NetworkEnemyManager 초기화 타임아웃! 스포너를 비활성화합니다.");
                enabled = false;
                yield break;
            }
        
            if (!NetworkEnemyManager.Instance.IsServer)
            {
                LogError("NetworkEnemyManager가 서버로 초기화되지 않았습니다! 스포너를 비활성화합니다.");
                enabled = false;
                yield break;
            }

            // 안전하게 NetworkEnemyManager에 스포너 등록
            Log("NetworkEnemyManager 연결 완료 - 스포너 등록 시작");
            Log($"스포너 등록 전 상태 확인 - NetworkEnemyManager IsServer: {NetworkEnemyManager.Instance.IsServer}, IsNetworked: {NetworkEnemyManager.Instance.IsNetworked}");
        
            NetworkEnemyManager.Instance.AddSpawner();
        
            Log("스포너 등록 완료 - 스폰 코루틴 시작 준비");

            // 스폰 코루틴 시작
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            // 초기 지연 시간 대기
            if (spawnDelay > 0f)
            {
                Log($"초기 지연 대기: {spawnDelay}초");
                yield return new WaitForSeconds(spawnDelay);
            }

            while (true)
            {
                // 서버에서만 실행하고, 최대 스폰 수량 체크
                if (IsServerInitialized && CanSpawnMoreEnemies())
                {
                    SpawnEnemy();
                }
            
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private bool CanSpawnMoreEnemies()
        {
            // 개별 스포너 제한 체크
            if (currentSpawnedCount >= maxSpawnCount)
            {
                Log($"개별 스포너 최대 수량 도달: {currentSpawnedCount}/{maxSpawnCount}");
                return false;
            }

            // 전역 적 수량 제한 체크
            bool canSpawn = NetworkEnemyManager.Instance.CanSpawnEnemy();
            if (!canSpawn)
            {
                Log($"전역 적 수량 한계 도달: {NetworkEnemyManager.Instance.CurrentEnemyCount}/{NetworkEnemyManager.Instance.MaxEnemyCount}");
            }
        
            return canSpawn;
        }

        private void SpawnEnemy()
        {
            if (!IsServerInitialized) return;

            // ✅ FishNet 올바른 방식: Instantiate 후 NetworkManager를 통해 스폰
            GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        
            // NetworkObject 컴포넌트 확인
            enemy.TryGetComponent(out NetworkObject networkObject);
            if (!networkObject)
            {
                LogError("생성된 적에 NetworkObject 컴포넌트가 없습니다.");
                Destroy(enemy);
                return;
            }

            // ✅ InstanceFinder를 통한 올바른 네트워크 스폰
            if (InstanceFinder.ServerManager)
            {
                InstanceFinder.ServerManager.Spawn(networkObject);
                Log($"네트워크 스폰 완료: {enemy.name}");
            }
            else
            {
                LogError("ServerManager를 찾을 수 없습니다.");
                Destroy(enemy);
                return;
            }

            // 타겟 설정 및 초기화
            NetworkObject targetNetworkObject = NetworkPlayerManager.Instance.GetRandomPlayer();
            GameObject target = targetNetworkObject?.gameObject;
            enemy.TryGetComponent(out EnemyController enemyController);
        
            if (enemyController && target)
            {
                // 서버에서 적 초기화
                enemyController.Init(target, transform.position);
            
                // 적 수량 카운터 업데이트
                NetworkEnemyManager.Instance.AddEnemy();
                currentSpawnedCount++;
            
                Log($"적 스폰 및 초기화 완료: {currentSpawnedCount}/{maxSpawnCount}, 타겟: {target.name}");
            }
            else
            {
                LogError($"EnemyControll({enemyController != null}) 또는 Player 타겟({target != null})을 찾을 수 없습니다.");
            
                // 실패 시 네트워크 객체 제거
                if (networkObject.IsSpawned)
                {
                    InstanceFinder.ServerManager.Despawn(networkObject);
                }
            }
        }

        // ✅ 적이 죽었을 때 호출될 메서드 (EnemyControll에서 호출)
        public void OnEnemyDestroyed()
        {
            if (IsServer)
            {
                currentSpawnedCount = Mathf.Max(0, currentSpawnedCount - 1);
                NetworkEnemyManager.Instance.RemoveEnemy();
                Log($"적 제거됨: {currentSpawnedCount}/{maxSpawnCount}");
            }
        }

        public override void OnStopServer()
        {
            // 스폰 코루틴 정리
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            // NetworkEnemyManager에서 스포너 제거
            if (NetworkEnemyManager.Instance != null)
            {
                NetworkEnemyManager.Instance.RemoveSpawner();
                Log("서버에서 스포너 정리 완료");
            }
        }

        // ✅ 디버그 정보 제공
        public int GetCurrentSpawnedCount() => currentSpawnedCount;
        public int GetMaxSpawnCount() => maxSpawnCount;
        public bool IsSpawning => spawnCoroutine != null;

        // ✅ 런타임에서 설정 변경 가능
        [ContextMenu("Force Spawn Enemy")]
        public void ForceSpawnEnemy()
        {
            if (IsServer && Application.isPlaying)
            {
                if (CanSpawnMoreEnemies())
                {
                    SpawnEnemy();
                }
                else
                {
                    Log("강제 스폰 불가: 수량 제한 도달");
                }
            }
            else
            {
                LogError("서버에서만 강제 스폰이 가능합니다.");
            }
        }

        [ContextMenu("Reset Spawn Count")]
        public void ResetSpawnCount()
        {
            if (IsServer)
            {
                currentSpawnedCount = 0;
                Log("스폰 카운트 리셋 완료");
            }
        }

        // ✅ 로깅 헬퍼 메서드
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                LogManager.Log(LogCategory.Spawner, $"NetworkSpawnerObject - {gameObject.name} {message}", this);
            }
        }

        private void LogError(string message)
        {
            LogManager.LogError(LogCategory.Spawner, $"NetworkSpawnerObject - {gameObject.name} {message}", this);
        }

        // ✅ 네트워크 상태 안전 체크 헬퍼
        private bool IsNetworkServerSafe()
        {
            try
            {
                // NetworkBehaviour가 초기화된 상태에서만 IsServer 체크
                return Application.isPlaying && IsNetworked && IsServer;
            }
            catch
            {
                // NetworkBehaviour가 초기화되지 않은 상태
                return false;
            }
        }

        private bool IsNetworkClientSafe()
        {
            try
            {
                // NetworkBehaviour가 초기화된 상태에서만 클라이언트 체크
                return Application.isPlaying && IsNetworked && !IsServer;
            }
            catch
            {
                // NetworkBehaviour가 초기화되지 않은 상태
                return false;
            }
        }

        // ✅ 기즈모로 시각화 (안전한 네트워크 상태 체크)
        private void OnDrawGizmos()
        {
            // 안전한 네트워크 상태 체크
            if (IsNetworkServerSafe())
            {
                Gizmos.color = Color.green; // 서버에서는 녹색
            }
            else if (IsNetworkClientSafe())
            {
                Gizmos.color = Color.red; // 클라이언트에서는 빨간색
            }
            else
            {
                Gizmos.color = Color.yellow; // 에디터 또는 초기화 전에는 노란색
            }
        
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        
            // 서버/클라이언트 표시 (안전한 상태에서만)
            if (Application.isPlaying)
            {
                try
                {
                    if (IsNetworked && IsServer)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.3f);
                    }
                    else if (IsNetworked)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.3f);
                    }
                }
                catch
                {
                    // NetworkBehaviour 초기화 전에는 기본 표시
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.2f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 스폰 범위 표시 (필요시)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 2f);
        
            // 정보 텍스트 (에디터에서만)
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                    $"Max: {maxSpawnCount}\nInterval: {spawnInterval}s\nDelay: {spawnDelay}s");
#endif
            }
            else
            {
#if UNITY_EDITOR
                string statusText = "초기화 중...";
            
                try
                {
                    if (IsNetworked && IsServer)
                    {
                        statusText = $"서버 모드\nSpawned: {currentSpawnedCount}/{maxSpawnCount}";
                    }
                    else if (IsNetworked)
                    {
                        statusText = $"클라이언트 모드\n스폰 비활성화";
                    }
                    else
                    {
                        statusText = "로컬 모드\n네트워크 없음";
                    }
                }
                catch
                {
                    statusText = "NetworkBehaviour\n초기화 대기 중...";
                }
            
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, statusText);
#endif
            }
        }
    }
} 