using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace MyFolder._1._Scripts._4._Network._0._InReady
{
    public class SceneBasedPlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject spawnParent;
        [SerializeField] private Transform[] spawnPoints;

        private NetworkManager networkManager;
        private Dictionary<NetworkConnection, NetworkObject> spawnedPlayers = new();
        private bool isInitialized = false;
        
        private void Start()
        {
            if (isInitialized) return;
            isInitialized = true;
            
            networkManager = InstanceFinder.NetworkManager;

            if (networkManager == null)
            {
                Debug.LogError("[SceneBasedPlayerSpawner] NetworkManager not found.");
                return;
            }
            
            // 클라이언트가 씬을 로드했을 때 서버에서 호출됨
            networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedReadyScene;
        
            // 클라이언트 연결 해제 이벤트
            networkManager.ServerManager.OnRemoteConnectionState += OnClientDisconnected;

            // 호스트 플레이어는 씬 로딩 완료 후 안전하게 스폰
            if (networkManager.IsHostStarted)
            {
                StartCoroutine(SpawnHostPlayerWithDelay());
            }

            Debug.Log("[SceneBasedPlayerSpawner] 초기화 완료");
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

        private System.Collections.IEnumerator SpawnHostPlayerWithDelay()
        {
            // FishNet 씬 매니저의 로딩 완료를 기다림
            yield return new WaitForSeconds(0.5f);
            
            if (networkManager.IsHostStarted && IsInReadyScene())
            {
                var hostConn = networkManager.ClientManager.Connection;
                if (hostConn != null && !spawnedPlayers.ContainsKey(hostConn))
                {
                    Debug.Log("[SceneBasedPlayerSpawner] 호스트 플레이어 안전 스폰 시도");
                    SpawnPlayer(hostConn);
                }
            }
        }

        private void OnClientLoadedReadyScene(NetworkConnection conn, bool asServer)
        {
            Debug.Log($"[SceneBasedPlayerSpawner] OnClientLoadedReadyScene 호출 - ClientID: {conn.ClientId}, asServer: {asServer}, IsServer: {networkManager.IsServer}, 현재 스폰된 플레이어: {spawnedPlayers.Count}명");

            // 서버에서만 실행 (클라이언트에서는 실행하지 않음)
            if (!asServer || !networkManager.IsServer) 
            {
                Debug.Log($"[SceneBasedPlayerSpawner] 서버가 아니므로 스폰 건너뜀 - ClientID: {conn.ClientId}");
                return;
            }

            Debug.Log($"[SceneBasedPlayerSpawner] 클라이언트 씬 로드: {conn.ClientId}, IsHost: {networkManager.IsHostStarted}");

            if (IsInReadyScene())
            {
                // 이미 스폰된 플레이어인지 확인
                if (spawnedPlayers.ContainsKey(conn))
                {
                    Debug.Log($"[SceneBasedPlayerSpawner] 이미 스폰된 플레이어: {conn.ClientId} (총 {spawnedPlayers.Count}명)");
                    return;
                }

                // 호스트 플레이어는 딜레이 코루틴에서 처리되므로 건너뜀
                if (networkManager.IsHostStarted && conn == networkManager.ClientManager.Connection)
                {
                    Debug.Log($"[SceneBasedPlayerSpawner] 호스트 플레이어는 딜레이 코루틴에서 처리됨: {conn.ClientId}");
                    return;
                }

                // 연결이 유효한지 확인
                if (conn == null || !conn.IsValid)
                {
                    Debug.LogWarning($"[SceneBasedPlayerSpawner] 유효하지 않은 연결: {conn?.ClientId}");
                    return;
                }

                // 추가 검증: 이미 스폰된 플레이어 중 같은 ClientID가 있는지 확인
                foreach (var kvp in spawnedPlayers)
                {
                    if (kvp.Key.ClientId == conn.ClientId)
                    {
                        Debug.LogWarning($"[SceneBasedPlayerSpawner] 같은 ClientID의 플레이어가 이미 존재: {conn.ClientId}");
                        return;
                    }
                }

                Debug.Log($"[SceneBasedPlayerSpawner] 게스트 플레이어 스폰 시작: {conn.ClientId} (현재 스폰된 플레이어: {spawnedPlayers.Count}명)");
                SpawnPlayer(conn);
            }
            else
            {
                Debug.Log($"[SceneBasedPlayerSpawner] Ready 씬이 아니므로 스폰하지 않음");
            }
        }

        private void OnClientDisconnected(NetworkConnection conn, RemoteConnectionStateArgs stateArgs)
        {
            if (stateArgs.ConnectionState == RemoteConnectionState.Stopped)
            {
                Debug.Log($"[SceneBasedPlayerSpawner] 클라이언트 연결 해제: {conn.ClientId}");
            
                if (spawnedPlayers.ContainsKey(conn))
                {
                    // 플레이어 객체 제거
                    NetworkObject player = spawnedPlayers[conn];
                    if (player != null && player.IsSpawned)
                    {
                        networkManager.ServerManager.Despawn(player);
                        Debug.Log($"[SceneBasedPlayerSpawner] 플레이어 제거 완료: {conn.ClientId}");
                    }
                
                    spawnedPlayers.Remove(conn);
                }
            }
        }


        private void SpawnPlayer(NetworkConnection conn)
        {
            Debug.Log($"[SceneBasedPlayerSpawner] SpawnPlayer 호출 - ClientID: {conn.ClientId}, IsServer: {networkManager.IsServer}");

            if (!playerPrefab)
            {
                Debug.LogError("[SceneBasedPlayerSpawner] Player Prefab이 설정되지 않았습니다!");
                return;
            }

            // 서버에서만 플레이어 생성 및 스폰
            if (!networkManager.IsServer)
            {
                Debug.LogWarning($"[SceneBasedPlayerSpawner] 서버가 아닌 클라이언트에서 스폰 시도 - ClientID: {conn.ClientId}");
                return;
            }

            // 이미 스폰된 플레이어인지 한번 더 확인
            if (spawnedPlayers.ContainsKey(conn))
            {
                Debug.LogWarning($"[SceneBasedPlayerSpawner] 이미 스폰된 플레이어 - ClientID: {conn.ClientId}");
                return;
            }

            Vector3 spawnPos = GetRandomSpawnPoint();
            
            // 일반 Instantiate 사용 (Object Pooling 비활성화)
            GameObject playerObj;
            if(spawnParent)
                playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity,spawnParent.transform);
            else
                playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            NetworkObject player = playerObj.GetComponent<NetworkObject>();
            
            if (!player)
            {
                Debug.LogError("[SceneBasedPlayerSpawner] Player Prefab에 NetworkObject 컴포넌트가 없습니다!");
                Destroy(playerObj);
                return;
            }
            
            // 모든 클라이언트에게 스폰 (conn만이 아닌 모든 클라이언트에게)
            networkManager.ServerManager.Spawn(player, conn);
            
            // 특정 연결에 소유권 부여
            player.GiveOwnership(conn);
            
            spawnedPlayers[conn] = player;

            Debug.Log($"[SceneBasedPlayerSpawner] 플레이어 스폰 완료 - ClientID: {conn.ClientId}, Position: {spawnPos}, Owner: {player.Owner?.ClientId}, 총 스폰된 플레이어: {spawnedPlayers.Count}명");
        }

        private void DespawnAllPlayers()
        {
            if (networkManager == null) return;

            Debug.Log($"[SceneBasedPlayerSpawner] 모든 플레이어 제거 시작 - 총 {spawnedPlayers.Count}명");
        
            foreach (var kvp in spawnedPlayers.ToList())
            {
                NetworkConnection conn = kvp.Key;
                NetworkObject player = kvp.Value;
            
                if (player != null && player.IsSpawned)
                {
                    try
                    {
                        networkManager.ServerManager.Despawn(player);
                        Debug.Log($"[SceneBasedPlayerSpawner] 플레이어 제거: {conn?.ClientId}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SceneBasedPlayerSpawner] 플레이어 제거 중 오류: {ex.Message}");
                    }
                }
            }
        
            spawnedPlayers.Clear();
            Debug.Log("[SceneBasedPlayerSpawner] 모든 플레이어 제거 완료");
        }

        private bool IsInReadyScene()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isReady = currentScene.Contains("Ready");
            Debug.Log($"[SceneBasedPlayerSpawner] 현재 씬: {currentScene} -> IsReady: {isReady}");
            return isReady;
        }

        private Vector3 GetRandomSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                Debug.Log($"[SceneBasedPlayerSpawner] 스폰 포인트 선택: {spawnPos}");
                return spawnPos;
            }

            Debug.LogWarning("[SceneBasedPlayerSpawner] 스폰 포인트가 없어 기본 위치 사용");
            return Vector3.zero;
        }
    }
} 