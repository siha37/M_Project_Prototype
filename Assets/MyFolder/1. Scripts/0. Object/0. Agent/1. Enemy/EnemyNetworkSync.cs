using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using MyFolder._1._Scripts._3._SingleTone;

/// <summary>
/// 적 네트워크 동기화 컴포넌트 (리팩토링 버전)
/// 기존의 여러 개별 SyncVar을 하나의 EnemyStateData로 통합
/// 새로운 컴포넌트 기반 구조와 연동
/// </summary>
public class EnemyNetworkSync : NetworkBehaviour
{
    [Header("=== 네트워크 동기화 설정 ===")]
    [SerializeField] private float syncInterval = 0.1f; // 10 FPS 동기화
    [SerializeField] private bool enableDetailedLogging = false;
    
    // 통합된 상태 데이터 동기화
    private readonly SyncVar<EnemyStateData> syncStateData = new SyncVar<EnemyStateData>();
    
    // 컴포넌트 참조
    private EnemyController controller;
    private EnemyAI ai;
    private EnemyMovement movement;
    private EnemyCombat combat;
    private EnemyPerception perception;
    
    // 동기화 상태
    private bool isInitialized = false;
    private float lastSyncTime;
    private EnemyStateData lastSyncedData;
    
    // 이벤트
    public System.Action<EnemyStateData> OnStateDataChanged;
    public System.Action<EnemyAIStateType> OnAIStateChanged;
    public System.Action<Transform> OnTargetChanged;
    
    // ========== Properties ==========
    
    /// <summary>
    /// 현재 동기화된 상태 데이터
    /// </summary>
    public EnemyStateData CurrentStateData => syncStateData.Value;
    
    /// <summary>
    /// 현재 AI 상태
    /// </summary>
    public EnemyAIStateType CurrentAIState => syncStateData.Value.currentState;
    
    /// <summary>
    /// 현재 타겟 ClientId
    /// </summary>
    public int CurrentTargetClientId => syncStateData.Value.targetClientId;
    
    /// <summary>
    /// 현재 위치
    /// </summary>
    public Vector3 CurrentPosition => syncStateData.Value.currentPosition;
    
    /// <summary>
    /// 목표 위치
    /// </summary>
    public Vector3 TargetPosition => syncStateData.Value.targetPosition;
    
    /// <summary>
    /// 조준 각도
    /// </summary>
    public float LookAngle => syncStateData.Value.lookAngle;
    
    /// <summary>
    /// 공격 중인지 여부
    /// </summary>
    public bool IsAttacking => syncStateData.Value.isAttacking;
    
    /// <summary>
    /// 재장전 중인지 여부
    /// </summary>
    public bool IsReloading => syncStateData.Value.isReloading;
    
    /// <summary>
    /// 추적 중인지 여부
    /// </summary>
    public bool IsChasing => syncStateData.Value.isChasing;
    
    /// <summary>
    /// 회피 기동 중인지 여부
    /// </summary>
    public bool IsStrafing => syncStateData.Value.isStrafing;
    
    
    // ========== Unity Lifecycle ==========
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        AssignComponents();
        
        // 초기 상태 데이터 설정
        syncStateData.Value = EnemyStateData.Default;
        lastSyncedData = syncStateData.Value;
        
        LogManager.Log(LogCategory.Enemy, "EnemyNetworkSync 컴포넌트 초기화 완료", this);
    }
    
    public override void OnStartServer()
    {
        // 서버에서만 초기화
        isInitialized = true;
        
        // 초기 상태 데이터 설정
        UpdateStateData();
        
        LogManager.Log(LogCategory.Enemy, "EnemyNetworkSync 서버 초기화 완료", this);
    }
    
    public override void OnStartClient()
    {
        // 클라이언트에서는 상태 데이터 변경 이벤트 등록
        syncStateData.OnChange += OnStateDataChangedCallback;
        
        LogManager.Log(LogCategory.Enemy, "EnemyNetworkSync 클라이언트 초기화 완료", this);
    }
    
    public override void OnStopClient()
    {
        // 클라이언트 종료 시 이벤트 해제
        if (syncStateData != null)
        {
            syncStateData.OnChange -= OnStateDataChangedCallback;
        }
    }
    
    private void Update()
    {
        // 서버에서만 동기화 업데이트
        if (!IsServer || !isInitialized) return;
        
        // 동기화 간격 체크
        if (Time.time - lastSyncTime >= syncInterval)
        {
            UpdateStateData();
            lastSyncTime = Time.time;
        }
    }
    
    // ========== Public Methods ==========
    
    /// <summary>
    /// EnemyController로부터 초기화
    /// </summary>
    public void Initialize(EnemyController controller)
    {
        this.controller = controller;
        
        // 컴포넌트 재할당
        AssignComponents();
        
        LogManager.Log(LogCategory.Enemy, "EnemyNetworkSync 초기화 완료", this);
    }
    
    /// <summary>
    /// AI로부터 상태 데이터 업데이트
    /// </summary>
    public void UpdateFromAI(EnemyAI ai)
    {
        if (!IsServer || ai == null) return;
        
        // AI로부터 상태 데이터 생성
        EnemyStateData newStateData = ai.CreateNetworkSyncData();
        
        // 상태 데이터가 변경되었는지 확인
        if (!lastSyncedData.Equals(newStateData))
        {
            syncStateData.Value = newStateData.UpdateTimestamp();
            lastSyncedData = newStateData;
            
            if (enableDetailedLogging)
            {
                LogManager.Log(LogCategory.Enemy, $"상태 동기화: {newStateData.currentState.ToDisplayString()}", this);
            }
        }
    }
    
    /// <summary>
    /// 강제 상태 데이터 업데이트
    /// </summary>
    public void ForceUpdateStateData(EnemyStateData stateData)
    {
        if (!IsServer) return;
        
        syncStateData.Value = stateData.UpdateTimestamp();
        lastSyncedData = stateData;
        
        LogManager.Log(LogCategory.Enemy, $"강제 상태 업데이트: {stateData.currentState.ToDisplayString()}", this);
    }
    
    /// <summary>
    /// 특정 상태로 강제 변경
    /// </summary>
    public void ForceSetState(EnemyAIStateType state)
    {
        if (!IsServer) return;
        
        var newStateData = syncStateData.Value.WithState(state);
        syncStateData.Value = newStateData.UpdateTimestamp();
        lastSyncedData = newStateData;
        
        LogManager.Log(LogCategory.Enemy, $"강제 상태 변경: {state.ToDisplayString()}", this);
    }
    
    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(int clientId)
    {
        if (!IsServer) return;
        
        var newStateData = syncStateData.Value;
        newStateData.targetClientId = clientId;
        newStateData.hasValidTarget = clientId >= 0;
        syncStateData.Value = newStateData.UpdateTimestamp();
        lastSyncedData = newStateData;
        
        LogManager.Log(LogCategory.Enemy, $"타겟 설정: ClientId {clientId}", this);
    }
    
    /// <summary>
    /// 위치 업데이트
    /// </summary>
    public void UpdatePosition(Vector3 currentPos, Vector3 targetPos)
    {
        if (!IsServer) return;
        
        var newStateData = syncStateData.Value.WithPosition(currentPos, targetPos);
        syncStateData.Value = newStateData.UpdateTimestamp();
        lastSyncedData = newStateData;
    }
    
    /// <summary>
    /// 전투 상태 업데이트
    /// </summary>
    public void UpdateCombatState(bool attacking, bool reloading, float angle)
    {
        if (!IsServer) return;
        
        var newStateData = syncStateData.Value.WithCombat(attacking, reloading, angle);
        syncStateData.Value = newStateData.UpdateTimestamp();
        lastSyncedData = newStateData;
    }
    
    /// <summary>
    /// 현재 상태 정보 가져오기
    /// </summary>
    public string GetStateInfo()
    {
        var data = syncStateData.Value;
        string info = $"=== Network Sync State ===\n";
        info += $"AI 상태: {data.currentState.ToDisplayString()}\n";
        info += $"위치: {data.currentPosition}\n";
        info += $"목표: {data.targetPosition}\n";
        info += $"타겟 ClientId: {data.targetClientId}\n";
        info += $"조준 각도: {data.lookAngle:F1}°\n";
        info += $"공격 중: {data.isAttacking}\n";
        info += $"재장전 중: {data.isReloading}\n";
        info += $"추적 중: {data.isChasing}\n";
        info += $"회피 중: {data.isStrafing}\n";
        info += $"타임스탬프: {data.timestamp:F2}s\n";
        
        return info;
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 컴포넌트 자동 할당
    /// </summary>
    private void AssignComponents()
    {
        if (ai == null) ai = GetComponent<EnemyAI>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (combat == null) combat = GetComponent<EnemyCombat>();
        if (perception == null) perception = GetComponent<EnemyPerception>();
    }
    
    /// <summary>
    /// 상태 데이터 업데이트
    /// </summary>
    private void UpdateStateData()
    {
        if (ai == null) return;
        
        // AI로부터 상태 데이터 생성
        EnemyStateData newStateData = ai.CreateNetworkSyncData();
        
        // 상태 데이터가 변경되었는지 확인
        if (!lastSyncedData.Equals(newStateData))
        {
            syncStateData.Value = newStateData.UpdateTimestamp();
            lastSyncedData = newStateData;
            
            if (enableDetailedLogging)
            {
                LogManager.Log(LogCategory.Enemy, $"상태 동기화: {newStateData.currentState.ToDisplayString()}", this);
            }
        }
    }
    
    /// <summary>
    /// 상태 데이터 변경 콜백 (클라이언트)
    /// </summary>
    private void OnStateDataChangedCallback(EnemyStateData previousValue, EnemyStateData newValue, bool asServer)
    {
        if (asServer) return; // 서버는 이미 처리했으므로 건너뜀
        
        // 상태 변경 확인
        if (previousValue.currentState != newValue.currentState)
        {
            OnAIStateChanged?.Invoke(newValue.currentState);
            
            LogManager.Log(LogCategory.Enemy, $"클라이언트 상태 변경: {previousValue.currentState.ToDisplayString()} → {newValue.currentState.ToDisplayString()}", this);
        }
        
        // 타겟 변경 확인
        if (previousValue.targetClientId != newValue.targetClientId)
        {
            // ClientId로부터 Transform 찾기 (필요한 경우)
            Transform target = null;
            if (newValue.targetClientId >= 0)
            {
                var playerObj = NetworkPlayerManager.Instance?.GetPlayerByClientId(newValue.targetClientId);
                target = playerObj?.transform;
            }
            
            OnTargetChanged?.Invoke(target);
        }
        
        // 이벤트 발생
        OnStateDataChanged?.Invoke(newValue);
    }
    
    
    // ========== Validation ==========
    
    private void OnValidate()
    {
        // 설정값 검증
        if (syncInterval < 0.01f) syncInterval = 0.1f;
    }
} 