using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._4._Network._0._InReady
{
    public class SceneBasedPlayerSpawner : MonoBehaviour
    {
        [Header("플레이어 스폰 설정")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject spawnParent;
        [SerializeField] private Transform[] spawnPoints;
        
        [Header("씬 설정")]
        [SerializeField] private string[] targetSceneNames = { "Ready", "MainScene" };

        private NetworkManager networkManager;
        private Dictionary<NetworkConnection, NetworkObject> spawnedPlayers = new();
        private bool isInitialized = false;
        
        private void Start()
        {
            if (isInitialized) return;
            isInitialized = true;
            
            // ✅ 상태 확인 기반 초기화
            StartCoroutine(InitializeWithStateCheck());
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedReadyScene;
                networkManager.ServerManager.OnRemoteConnectionState -= OnClientDisconnected;
            }
        
            // 안전하게 모든 플레이어 제거
            DespawnAllPlayers();
        }

        // ✅ 상태 확인 기반 초기화
        private System.Collections.IEnumerator InitializeWithStateCheck()
        {
            // NetworkManager 초기화 대기
            while (InstanceFinder.NetworkManager == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            networkManager = InstanceFinder.NetworkManager;
            LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner NetworkManager 확인 완료", this);
            
            // 이벤트 등록
            networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedReadyScene;
            networkManager.ServerManager.OnRemoteConnectionState += OnClientDisconnected;

            LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner 이벤트 등록 완료", this);
            
            // ✅ 서버인 경우 즉시 기존 플레이어들 스폰 확인
            if (networkManager.IsServer)
            {
                StartCoroutine(CheckAndSpawnExistingPlayersWithStateCheck());
            }
        }

        // ✅ 상태 확인 후 기존 플레이어들 스폰
        private System.Collections.IEnumerator CheckAndSpawnExistingPlayersWithStateCheck()
        {
            // 서버 상태와 씬 상태 확인 대기
            while (!networkManager.IsServer || 
                   !IsInTargetScene() ||
                   networkManager.ServerManager.Clients.Count == 0)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // 추가 안전 대기 (1프레임)
            yield return new WaitForEndOfFrame();
            
            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 네트워크 준비 완료 - 연결된 플레이어: {networkManager.ServerManager.Clients.Count}명", this);
            
            // 모든 연결된 플레이어에 대해 스폰 확인
            foreach (var conn in networkManager.ServerManager.Clients.Values)
            {
                if (conn != null && conn.IsValid && !spawnedPlayers.ContainsKey(conn))
                {
                    LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 기존 플레이어 스폰: {conn.ClientId}", this);
                    SpawnPlayer(conn);
                    
                    // 스폰 간격 (1프레임씩)
                    yield return null;
                }
            }
            
            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 모든 기존 플레이어 스폰 완료 - 총 {spawnedPlayers.Count}명", this);
        }

        // ✅ 단순화된 OnClientLoadedReadyScene (로그용으로만 사용)
        private void OnClientLoadedReadyScene(NetworkConnection conn, bool asServer)
        {
            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner OnClientLoadedScene 호출 - ClientID: {conn.ClientId}, asServer: {asServer}, IsServer: {networkManager.IsServer}, 현재 스폰된 플레이어: {spawnedPlayers.Count}명", this);

            // 서버에서만 실행
            if (!asServer || !networkManager.IsServer) 
            {
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 서버가 아니므로 스폰 건너뜀 - ClientID: {conn.ClientId}", this);
                return;
            }

            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 클라이언트 씬 로드 감지: {conn.ClientId}", this);

            if (IsInTargetScene())
            {
                // 이미 스폰된 플레이어인지 확인
                if (spawnedPlayers.ContainsKey(conn))
                {
                    LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 이미 스폰된 플레이어: {conn.ClientId} (총 {spawnedPlayers.Count}명)", this);
                    return;
                }

                // 연결이 유효한지 확인
                if (conn == null || !conn.IsValid)
                {
                    LogManager.LogWarning(LogCategory.Network, $"SceneBasedPlayerSpawner 유효하지 않은 연결: {conn?.ClientId}", this);
                    return;
                }

                // 추가 검증: 이미 스폰된 플레이어 중 같은 ClientID가 있는지 확인
                foreach (var kvp in spawnedPlayers)
                {
                    if (kvp.Key.ClientId == conn.ClientId)
                    {
                        LogManager.LogWarning(LogCategory.Network, $"SceneBasedPlayerSpawner 같은 ClientID의 플레이어가 이미 존재: {conn.ClientId}", this);
                        return;
                    }
                }

                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 새 플레이어 스폰 시작: {conn.ClientId} (현재 스폰된 플레이어: {spawnedPlayers.Count}명)", this);
                SpawnPlayer(conn);
            }
            else
            {
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 타겟 씬이 아니므로 스폰하지 않음", this);
            }
        }

        private void OnClientDisconnected(NetworkConnection conn, RemoteConnectionStateArgs stateArgs)
        {
            if (stateArgs.ConnectionState == RemoteConnectionState.Stopped)
            {
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 클라이언트 연결 해제: {conn.ClientId}", this);
            
                if (spawnedPlayers.ContainsKey(conn))
                {
                    NetworkObject player = spawnedPlayers[conn];
                    
                    // ✅ NetworkPlayerManager에서 해제
                    if (NetworkPlayerManager.Instance != null && player != null)
                    {
                        NetworkPlayerManager.Instance.UnregisterPlayer(player);
                    }
                    
                    // FishNet에서 디스폰
                    if (player != null && player.IsSpawned)
                    {
                        networkManager.ServerManager.Despawn(player);
                        LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 플레이어 제거 완료: {conn.ClientId}", this);
                    }
                
                    spawnedPlayers.Remove(conn);
                }
            }
        }

        private void SpawnPlayer(NetworkConnection conn)
        {
            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner SpawnPlayer 호출 - ClientID: {conn.ClientId}, IsServer: {networkManager.IsServer}", this);

            if (!playerPrefab)
            {
                LogManager.LogError(LogCategory.Network, "SceneBasedPlayerSpawner Player Prefab이 설정되지 않았습니다!", this);
                return;
            }

            // 서버에서만 플레이어 생성 및 스폰
            if (!networkManager.IsServer)
            {
                LogManager.LogWarning(LogCategory.Network, $"SceneBasedPlayerSpawner 서버가 아닌 클라이언트에서 스폰 시도 - ClientID: {conn.ClientId}", this);
                return;
            }

            // 이미 스폰된 플레이어인지 한번 더 확인
            if (spawnedPlayers.ContainsKey(conn))
            {
                LogManager.LogWarning(LogCategory.Network, $"SceneBasedPlayerSpawner 이미 스폰된 플레이어 - ClientID: {conn.ClientId}", this);
                return;
            }

            Vector3 spawnPos = GetRandomSpawnPoint();
            
            // 플레이어 생성
            GameObject playerObj;
            if(spawnParent)
                playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity, spawnParent.transform);
            else
                playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            
            NetworkObject player = playerObj.GetComponent<NetworkObject>();
            
            if (!player)
            {
                LogManager.LogError(LogCategory.Network, "SceneBasedPlayerSpawner Player Prefab에 NetworkObject 컴포넌트가 없습니다!", this);
                Destroy(playerObj);
                return;
            }
            
            // FishNet에 스폰
            networkManager.ServerManager.Spawn(player, conn);
            player.GiveOwnership(conn);
            
            // 로컬 딕셔너리에 저장
            spawnedPlayers[conn] = player;

            // ✅ NetworkPlayerManager에 등록
            if (NetworkPlayerManager.Instance != null)
            {
                NetworkPlayerManager.Instance.RegisterPlayer(player);
            }

            LogManager.Log(LogCategory.Network, 
                $"SceneBasedPlayerSpawner 플레이어 스폰 완료 - ClientID: {conn.ClientId}, Position: {spawnPos}, Owner: {player.Owner?.ClientId}, 총 스폰된 플레이어: {spawnedPlayers.Count}명", this);
        }

        private void DespawnAllPlayers()
        {
            if (networkManager == null) return;

            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 모든 플레이어 제거 시작 - 총 {spawnedPlayers.Count}명", this);
        
            foreach (var kvp in spawnedPlayers.ToList())
            {
                NetworkConnection conn = kvp.Key;
                NetworkObject player = kvp.Value;
            
                // ✅ NetworkPlayerManager에서 해제
                if (NetworkPlayerManager.Instance != null && player != null)
                {
                    NetworkPlayerManager.Instance.UnregisterPlayer(player);
                }
                
                if (player != null && player.IsSpawned)
                {
                    try
                    {
                        networkManager.ServerManager.Despawn(player);
                        LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 플레이어 제거: {conn?.ClientId}", this);
                    }
                    catch (System.Exception ex)
                    {
                        LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner 플레이어 제거 중 오류: {ex.Message}", this);
                    }
                }
            }
        
            spawnedPlayers.Clear();
            LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner 모든 플레이어 제거 완료", this);
        }

        private bool IsInTargetScene()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            if (targetSceneNames == null || targetSceneNames.Length == 0)
            {
                LogManager.LogWarning(LogCategory.Network, "SceneBasedPlayerSpawner 타겟 씬 이름이 설정되지 않았습니다!", this);
                return false;
            }
            
            foreach (string targetSceneName in targetSceneNames)
            {
                if (currentScene.Contains(targetSceneName))
                {
                    LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 현재 씬: {currentScene} -> 타겟 씬 '{targetSceneName}'과 일치", this);
                    return true;
                }
            }
            
                            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 현재 씬: {currentScene} -> 타겟 씬과 불일치 (타겟: {string.Join(", ", targetSceneNames)})", this);
            return false;
        }

        private Vector3 GetRandomSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 스폰 포인트 선택: {spawnPos}", this);
                return spawnPos;
            }

            LogManager.LogWarning(LogCategory.Network, "SceneBasedPlayerSpawner 스폰 포인트가 없어 기본 위치 사용", this);
            return Vector3.zero;
        }

        // ✅ 상태 확인용 공개 메서드들
        public bool IsNetworkReady()
        {
            return networkManager != null && networkManager.IsServer;
        }

        public int GetSpawnedPlayerCount()
        {
            return spawnedPlayers.Count;
        }

        public int GetConnectedPlayerCount()
        {
            return networkManager?.ServerManager?.Clients?.Count ?? 0;
        }

        // ✅ 수동 플레이어 스폰 확인 (디버그용)
        [ContextMenu("Check All Players")]
        public void CheckAllPlayersManually()
        {
            if (networkManager?.IsServer == true)
            {
                StartCoroutine(CheckAndSpawnExistingPlayersWithStateCheck());
            }
            else
            {
                LogManager.LogWarning(LogCategory.Network, "SceneBasedPlayerSpawner 서버에서만 플레이어 확인이 가능합니다.", this);
            }
        }
    }
} 