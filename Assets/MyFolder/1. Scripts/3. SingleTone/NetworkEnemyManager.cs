using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._3._SingleTone;

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
            LogManager.Log(LogCategory.Enemy, "NetworkEnemyManager 인스턴스 생성 완료", this);
        }
        else if (instance != this)
        {
            LogManager.LogWarning(LogCategory.Enemy, "NetworkEnemyManager 중복 인스턴스 제거", this);
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        // 서버에서 초기값 설정
        syncCurrentEnemyCount.Value = 0;
        syncMaxEnemyCount.Value = 0;
        
        LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 서버 초기화 완료 - IsServer: {IsServer}, IsServerInitialized: {IsServerInitialized}", this);
    }

    public override void OnStartClient()
    {
        // SyncVar 변경 콜백 등록
        syncCurrentEnemyCount.OnChange += OnCurrentEnemyCountChanged;
        syncMaxEnemyCount.OnChange += OnMaxEnemyCountChanged;
        
        LogManager.Log(LogCategory.Enemy, "NetworkEnemyManager 클라이언트 동기화 설정 완료", this);
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
        LogManager.Log(LogCategory.Enemy, $"AddSpawnerServerRPC 호출됨 - IsServer: {IsServer}, IsServerInitialized: {IsServerInitialized}", this);
        
        if (!IsServerInitialized)
        {
            LogManager.LogError(LogCategory.Enemy, "AddSpawnerServerRPC 실패 - 서버가 초기화되지 않음", this);
            return;
        }
        
        int previousValue = syncMaxEnemyCount.Value;
        syncMaxEnemyCount.Value += 5;
        int newValue = syncMaxEnemyCount.Value;
        
        LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 스포너 추가됨. 최대 적 수량: {previousValue} -> {newValue}", this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveSpawnerServerRpc()
    {
        if (!IsServerInitialized) return;
        
        syncMaxEnemyCount.Value = Mathf.Max(0, syncMaxEnemyCount.Value - 5);
        LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 스포너 제거됨. 최대 적 수량: {syncMaxEnemyCount.Value}", this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddEnemyServerRpc()
    {
        if (!IsServerInitialized) return;
        
        syncCurrentEnemyCount.Value++;
        LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 적 생성됨. 현재 적 수량: {syncCurrentEnemyCount.Value}/{syncMaxEnemyCount.Value}", this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveEnemyServerRpc()
    {
        if (!IsServerInitialized) return;
        
        syncCurrentEnemyCount.Value = Mathf.Max(0, syncCurrentEnemyCount.Value - 1);
        LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 적 제거됨. 현재 적 수량: {syncCurrentEnemyCount.Value}/{syncMaxEnemyCount.Value}", this);
    }

    // ✅ 스폰 가능 여부 체크 (모든 클라이언트에서 호출 가능)
    public bool CanSpawnEnemy()
    {
        return syncCurrentEnemyCount.Value < syncMaxEnemyCount.Value;
    }

    // ✅ 기존 EnemyManager 호환성을 위한 메서드들
    public void AddSpawner()
    {
        LogManager.Log(LogCategory.Enemy, $"AddSpawner 호출됨 - IsServer: {IsServer}, IsServerInitialized: {IsServerInitialized}, IsNetworked: {IsNetworked}", this);
        
        // 서버에서만 직접 값 변경
        if (IsServer && IsServerInitialized)
        {
            LogManager.Log(LogCategory.Enemy, "AddSpawner - 직접 SyncVar 값 변경 시작", this);
            
            int previousValue = syncMaxEnemyCount.Value;
            syncMaxEnemyCount.Value += 5;
            int newValue = syncMaxEnemyCount.Value;
            
            LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 스포너 추가됨. 최대 적 수량: {previousValue} -> {newValue}", this);
        }
        else
        {
            LogManager.LogWarning(LogCategory.Enemy, $"AddSpawner() - 서버에서만 호출 가능합니다. IsServer: {IsServer}, IsServerInitialized: {IsServerInitialized}", this);
        }
    }

    public void RemoveSpawner()
    {
        // 서버에서만 직접 값 변경
        if (IsServer && IsServerInitialized)
        {
            syncMaxEnemyCount.Value = Mathf.Max(0, syncMaxEnemyCount.Value - 5);
            LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 스포너 제거됨. 최대 적 수량: {syncMaxEnemyCount.Value}", this);
        }
        else
        {
            LogManager.LogWarning(LogCategory.Enemy, "RemoveSpawner() - 서버에서만 호출 가능합니다.", this);
        }
    }

    public void AddEnemy()
    {
        // 서버에서만 직접 값 변경
        if (IsServerInitialized && IsServerInitialized)
        {
            syncCurrentEnemyCount.Value++;
            LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 적 생성됨. 현재 적 수량: {syncCurrentEnemyCount.Value}/{syncMaxEnemyCount.Value}", this);
        }
        else
        {
            LogManager.LogWarning(LogCategory.Enemy, "AddEnemy() - 서버에서만 호출 가능합니다.", this);
        }
    }

    public void RemoveEnemy()
    {
        // 서버에서만 직접 값 변경
        if (IsServerInitialized && IsServerInitialized)
        {
            syncCurrentEnemyCount.Value = Mathf.Max(0, syncCurrentEnemyCount.Value - 1);
            LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 적 제거됨. 현재 적 수량: {syncCurrentEnemyCount.Value}/{syncMaxEnemyCount.Value}", this);
        }
        else
        {
            LogManager.LogWarning(LogCategory.Enemy, "RemoveEnemy() - 서버에서만 호출 가능합니다.", this);
        }
    }

    // ✅ SyncVar 변경 콜백들
    private void OnCurrentEnemyCountChanged(int previousValue, int newValue, bool asServer)
    {
        LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 현재 적 수량 변경: {previousValue} → {newValue} (서버: {asServer})", this);
        
        // UI 업데이트나 기타 로직을 여기에 추가 가능
        OnEnemyCountUpdated?.Invoke(newValue, syncMaxEnemyCount.Value);
    }

    private void OnMaxEnemyCountChanged(int previousValue, int newValue, bool asServer)
    {
        LogManager.Log(LogCategory.Enemy, $"NetworkEnemyManager 최대 적 수량 변경: {previousValue} → {newValue} (서버: {asServer})", this);
        
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