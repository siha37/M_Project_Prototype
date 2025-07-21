using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using System.Collections.Generic;
using FishNet.Transporting;

public class ReadyPlayerSpawner : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    
    private NetworkManager networkManager;
    private Dictionary<NetworkConnection, NetworkObject> spawnedPlayers = new Dictionary<NetworkConnection, NetworkObject>();
    
    void Start()
    {
        // NetworkManager 찾기
        networkManager = InstanceFinder.NetworkManager;
        
        // 이벤트 구독
        if (networkManager != null)
        {
            // 클라이언트가 초기 씬을 로드했을 때 감지
            networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            
            // 로컬 클라이언트(호스트) 감지
            networkManager.ClientManager.OnClientConnectionState += OnLocalClientConnectionState;
        }
        
        Debug.Log("[ReadyPlayerSpawner] 초기화 완료");
        
        // 초기 상태 로깅
        LogCurrentState();
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (networkManager != null)
        {
            networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
            networkManager.ClientManager.OnClientConnectionState -= OnLocalClientConnectionState;
        }
        
        // 모든 스폰된 플레이어 제거
        DespawnAllPlayers();
    }
    
    // 현재 상태 로깅
    void LogCurrentState()
    {
        Debug.Log($"[ReadyPlayerSpawner] 현재 상태:");
        Debug.Log($"  - IsServer: {networkManager?.IsServer}");
        Debug.Log($"  - IsClient: {networkManager?.IsClient}");
        Debug.Log($"  - IsHost: {networkManager?.IsHost}");
        Debug.Log($"  - Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"  - Connected Clients: {networkManager?.ServerManager?.Clients?.Count ?? 0}");
    }
    
    // 클라이언트가 초기 씬을 로드했을 때 호출
    void OnClientLoadedStartScenes(NetworkConnection connection, bool asServer)
    {
        if (asServer) // 서버에서만 실행
        {
            Debug.Log($"[ReadyPlayerSpawner] 클라이언트가 초기 씬을 로드함: {connection.ClientId}");
            
            // Ready 씬에 있는 동안에만 스폰
            if (IsReadyScene())
            {
                SpawnPlayerForConnection(connection);
            }
        }
    }
    
    // 로컬 클라이언트(호스트) 연결 상태 감지
    void OnLocalClientConnectionState(ClientConnectionStateArgs stateArgs)
    {
        Debug.Log($"[ReadyPlayerSpawner] 로컬 클라이언트 상태 변화: {stateArgs.ConnectionState}");
        
        if (stateArgs.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("[ReadyPlayerSpawner] 로컬 클라이언트(호스트) 연결됨");
            
            // 약간의 지연 후 호스트 플레이어 스폰 시도
            StartCoroutine(SpawnHostPlayerWithDelay());
        }
    }
    
    // 호스트 플레이어 스폰을 지연시켜 실행
    System.Collections.IEnumerator SpawnHostPlayerWithDelay()
    {
        yield return new WaitForSeconds(0.5f); // 0.5초 지연
        
        if (IsReadyScene() && networkManager.IsHost)
        {
            Debug.Log("[ReadyPlayerSpawner] 지연 후 호스트 플레이어 스폰 시도");
            SpawnPlayerForHost();
        }
        else
        {
            Debug.Log($"[ReadyPlayerSpawner] 호스트 플레이어 스폰 조건 불충족 - IsReadyScene: {IsReadyScene()}, IsHost: {networkManager.IsHost}");
        }
    }
    
    // Ready 씬인지 확인
    bool IsReadyScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isReady = sceneName.Contains("Ready");
        Debug.Log($"[ReadyPlayerSpawner] 씬 확인: {sceneName} -> IsReady: {isReady}");
        return isReady;
    }
    
    // 플레이어 스폰 (중복 방지)
    void SpawnPlayerForConnection(NetworkConnection connection)
    {
        // 이미 스폰된 플레이어가 있는지 체크
        if (spawnedPlayers.ContainsKey(connection))
        {
            Debug.Log($"[ReadyPlayerSpawner] 이미 스폰된 플레이어가 있습니다: {connection.ClientId}");
            return;
        }
        
        if (playerPrefab == null)
        {
            Debug.LogError("[ReadyPlayerSpawner] Player Prefab이 설정되지 않았습니다!");
            return;
        }
        
        // 스폰 위치 결정
        Vector3 spawnPos = GetSpawnPosition();
        
        // 플레이어 생성 및 스폰
        NetworkObject player = networkManager.GetPooledInstantiated(playerPrefab, spawnPos, Quaternion.identity, true);
        networkManager.ServerManager.Spawn(player, connection);
        
        // 스폰된 플레이어 기록
        spawnedPlayers[connection] = player;
        
        Debug.Log($"[ReadyPlayerSpawner] 플레이어 스폰 완료 - Connection: {connection.ClientId}, Owner: {player.Owner?.ClientId}, Position: {spawnPos}");
    }
    
    // 호스트용 플레이어 스폰
    void SpawnPlayerForHost()
    {
        var hostConnection = networkManager.ClientManager.Connection;
        
        Debug.Log($"[ReadyPlayerSpawner] 호스트 플레이어 스폰 시도 - Connection: {hostConnection?.ClientId}");
        
        if (spawnedPlayers.ContainsKey(hostConnection))
        {
            Debug.Log("[ReadyPlayerSpawner] 호스트 플레이어가 이미 스폰되어 있습니다.");
            return;
        }
        
        if (hostConnection == null)
        {
            Debug.LogWarning("[ReadyPlayerSpawner] 호스트 연결이 null입니다.");
            return;
        }
        
        if (playerPrefab == null)
        {
            Debug.LogError("[ReadyPlayerSpawner] Player Prefab이 설정되지 않았습니다!");
            return;
        }
        
        Vector3 spawnPos = GetSpawnPosition();
        NetworkObject player = networkManager.GetPooledInstantiated(playerPrefab, spawnPos, Quaternion.identity, true);
        networkManager.ServerManager.Spawn(player, hostConnection);
        
        spawnedPlayers[hostConnection] = player;
        
        Debug.Log($"[ReadyPlayerSpawner] 호스트 플레이어 스폰 완료 - Position: {spawnPos}");
    }
    
    // 플레이어 제거
    void RemovePlayerForConnection(NetworkConnection connection)
    {
        if (spawnedPlayers.TryGetValue(connection, out NetworkObject player))
        {
            if (player != null)
            {
                networkManager.ServerManager.Despawn(player);
                Debug.Log($"[ReadyPlayerSpawner] 플레이어 제거 완료: {connection.ClientId}");
            }
            spawnedPlayers.Remove(connection);
        }
    }
    
    // 모든 플레이어 제거
    void DespawnAllPlayers()
    {
        foreach (var kvp in spawnedPlayers)
        {
            if (kvp.Value != null)
            {
                networkManager.ServerManager.Despawn(kvp.Value);
            }
        }
        spawnedPlayers.Clear();
        Debug.Log("[ReadyPlayerSpawner] 모든 플레이어 제거 완료");
    }
    
    // 스폰 위치 결정
    Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // 랜덤 스폰 포인트 선택
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        }
        
        return Vector3.zero; // 기본 위치
    }
} 