using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class NetworkEnemyManager : NetworkBehaviour
{
    // ✅ FishNet 올바른 SyncVar 구문
    private readonly SyncVar<int> syncCurrentEnemyCount = new SyncVar<int>();
    private readonly SyncVar<int> syncMaxEnemyCount = new SyncVar<int>();

    private static NetworkEnemyManager instance;
    public static NetworkEnemyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NetworkEnemyManager>();
            }
            return instance;
        }
    }

    // 프로퍼티로 SyncVar 값 접근
    public int CurrentEnemyCount => syncCurrentEnemyCount.Value;
    public int MaxEnemyCount => syncMaxEnemyCount.Value;

    private void Awake()
    {
        // ✅ DontDestroyOnLoad 제거 (FishNet NetworkBehaviour와 충돌 방지)
        if (instance == null)
        {
            instance = this;
            Debug.Log("[NetworkEnemyManager] 인스턴스 생성 완료");
        }
        else if (instance != this)
        {
            Debug.LogWarning("[NetworkEnemyManager] 중복 인스턴스 제거");
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        // 서버에서 초기값 설정
        syncCurrentEnemyCount.Value = 0;
        syncMaxEnemyCount.Value = 0;
        
        Debug.Log("[NetworkEnemyManager] 서버 초기화 완료");
    }

    public override void OnStartClient()
    {
        // SyncVar 변경 콜백 등록
        syncCurrentEnemyCount.OnChange += OnCurrentEnemyCountChanged;
        syncMaxEnemyCount.OnChange += OnMaxEnemyCountChanged;
        
        Debug.Log("[NetworkEnemyManager] 클라이언트 동기화 설정 완료");
    }

    public override void OnStopClient()
    {
        // 콜백 해제
        if (syncCurrentEnemyCount != null)
            syncCurrentEnemyCount.OnChange -= OnCurrentEnemyCountChanged;
        if (syncMaxEnemyCount != null)
            syncMaxEnemyCount.OnChange -= OnMaxEnemyCountChanged;
    }

    // ✅ 서버에서만 실행되는 스포너 관리 메서드들
    [ServerRpc(RequireOwnership = false)]
    public void AddSpawnerServerRpc()
    {
        if (!IsServer) return;
        
        syncMaxEnemyCount.Value += 5;
        Debug.Log($"[NetworkEnemyManager] 스포너 추가됨. 최대 적 수량: {syncMaxEnemyCount.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveSpawnerServerRpc()
    {
        if (!IsServer) return;
        
        syncMaxEnemyCount.Value = Mathf.Max(0, syncMaxEnemyCount.Value - 5);
        Debug.Log($"[NetworkEnemyManager] 스포너 제거됨. 최대 적 수량: {syncMaxEnemyCount.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddEnemyServerRpc()
    {
        if (!IsServer) return;
        
        syncCurrentEnemyCount.Value++;
        Debug.Log($"[NetworkEnemyManager] 적 생성됨. 현재 적 수량: {syncCurrentEnemyCount.Value}/{syncMaxEnemyCount.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveEnemyServerRpc()
    {
        if (!IsServer) return;
        
        syncCurrentEnemyCount.Value = Mathf.Max(0, syncCurrentEnemyCount.Value - 1);
        Debug.Log($"[NetworkEnemyManager] 적 제거됨. 현재 적 수량: {syncCurrentEnemyCount.Value}/{syncMaxEnemyCount.Value}");
    }

    // ✅ 스폰 가능 여부 체크 (모든 클라이언트에서 호출 가능)
    public bool CanSpawnEnemy()
    {
        return syncCurrentEnemyCount.Value < syncMaxEnemyCount.Value;
    }

    // ✅ 기존 EnemyManager 호환성을 위한 메서드들
    public void AddSpawner()
    {
        if (IsServer)
        {
            AddSpawnerServerRpc();
        }
        else
        {
            // 클라이언트에서도 호출 가능하도록 ServerRpc 직접 호출
            AddSpawnerServerRpc();
        }
    }

    public void RemoveSpawner()
    {
        if (IsServer)
        {
            RemoveSpawnerServerRpc();
        }
        else
        {
            RemoveSpawnerServerRpc();
        }
    }

    public void AddEnemy()
    {
        if (IsServer)
        {
            AddEnemyServerRpc();
        }
        else
        {
            AddEnemyServerRpc();
        }
    }

    public void RemoveEnemy()
    {
        if (IsServer)
        {
            RemoveEnemyServerRpc();
        }
        else
        {
            RemoveEnemyServerRpc();
        }
    }

    // ✅ SyncVar 변경 콜백들
    private void OnCurrentEnemyCountChanged(int previousValue, int newValue, bool asServer)
    {
        Debug.Log($"[NetworkEnemyManager] 현재 적 수량 변경: {previousValue} → {newValue} (서버: {asServer})");
        
        // UI 업데이트나 기타 로직을 여기에 추가 가능
        OnEnemyCountUpdated?.Invoke(newValue, syncMaxEnemyCount.Value);
    }

    private void OnMaxEnemyCountChanged(int previousValue, int newValue, bool asServer)
    {
        Debug.Log($"[NetworkEnemyManager] 최대 적 수량 변경: {previousValue} → {newValue} (서버: {asServer})");
        
        OnMaxEnemyCountUpdated?.Invoke(newValue);
    }

    // ✅ 이벤트 시스템 (옵션)
    public System.Action<int, int> OnEnemyCountUpdated; // current, max
    public System.Action<int> OnMaxEnemyCountUpdated; // max

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
} 