using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// 적 AI 메인 컴포넌트
/// 상태 머신을 관리하고 다른 AI 컴포넌트들과 협력
/// </summary>
public class EnemyAI : NetworkBehaviour
{
    [Header("=== AI 설정 ===")]
    [SerializeField] private EnemyConfig config;
    
    [Header("=== 컴포넌트 참조 ===")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyCombat combat;
    [SerializeField] private EnemyPerception perception;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 상태 머신
    private EnemyAIStateMachine stateMachine;
    
    // 타겟 관리
    private Transform currentTarget;
    private Vector3 lastKnownTargetPosition;
    
    // 이벤트 시스템
    private EnemyEvents events;
    
    // 성능 최적화
    private float lastAIUpdateTime;
    private float aiUpdateInterval = 0.05f; // 20 FPS
    
    // 네트워크 동기화
    private EnemyNetworkSync networkSync;
    
    // ========== Properties ==========
    
    /// <summary>
    ///AI 설정
    /// </summary>
    public EnemyConfig Config => config;
    
    /// <summary>
    /// 이동 컴포넌트
    /// </summary>
    public EnemyMovement Movement => movement;
    
    /// <summary>
    /// 전투 컴포넌트
    /// </summary>
    public EnemyCombat Combat => combat;
    
    /// <summary>
    /// 인지 컴포넌트
    /// </summary>
    public EnemyPerception Perception => perception;
    
    /// <summary>
    /// 현재 타겟
    /// </summary>
    public Transform CurrentTarget => currentTarget;
    
    /// <summary>
    /// 이벤트 시스템
    /// </summary>
    public EnemyEvents Events => events;
    
    /// <summary>
    /// 현재 AI 상태
    /// </summary>
    public EnemyAIStateType CurrentStateType => stateMachine?.CurrentStateType ?? EnemyAIStateType.Patrol;
    
    /// <summary>
    /// 현재 상태 이름
    /// </summary>
    public string CurrentStateName => stateMachine?.CurrentState?.StateName ?? "Unknown";
    
    // ========== Unity Lifecycle ==========
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (combat == null) combat = GetComponent<EnemyCombat>();
        if (perception == null) perception = GetComponent<EnemyPerception>();
        if (networkSync == null) networkSync = GetComponent<EnemyNetworkSync>();
        
        // 이벤트 시스템 초기화
        events = new EnemyEvents();
        
        // 상태 머신 초기화
        stateMachine = new EnemyAIStateMachine();
        
        LogManager.Log(LogCategory.Enemy, "EnemyAI 컴포넌트 초기화 완료", this);
    }
    
    public override void OnStartServer()
    {
        // 서버에서만 AI 로직 실행
        if (config == null)
        {
            LogManager.LogError(LogCategory.Enemy, "EnemyConfig가 설정되지 않았습니다!", this);
            return;
        }
        
        // 상태 머신 초기화
        stateMachine.Initialize(this);
        
        // AI 업데이트 간격 설정
        aiUpdateInterval = config.aiUpdateInterval;
        
        LogManager.Log(LogCategory.Enemy, "EnemyAI 서버 초기화 완료", this);
    }
    
    public override void OnStartClient()
    {
        // 클라이언트에서는 시각화만
        if (config != null && config.showDebugInfo)
        {
            showDebugInfo = true;
        }
        
        LogManager.Log(LogCategory.Enemy, "EnemyAI 클라이언트 초기화 완료", this);
    }
    
    private void Update()
    {
        // 서버에서만 AI 업데이트
        if (!IsServer) return;
        
        // 성능 최적화: 업데이트 간격 조절
        if (Time.time - lastAIUpdateTime < aiUpdateInterval) return;
        lastAIUpdateTime = Time.time;
        
        // AI 업데이트
        UpdateAI();
    }
    
    private void FixedUpdate()
    {
        // 서버에서만 물리 업데이트
        if (!IsServer) return;
        
        stateMachine?.FixedUpdate();
    }
    
    private void OnDestroy()
    {
        // 정리 작업
        stateMachine?.Cleanup();
        events?.UnsubscribeAll();
        
        LogManager.Log(LogCategory.Enemy, "EnemyAI 정리 완료", this);
    }
    
    // ========== Public Methods ==========
    
    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(Transform target)
    {
        Transform oldTarget = currentTarget;
        currentTarget = target;
        
        if (target != null)
        {
            lastKnownTargetPosition = target.position;
            
            // 타겟 변경 이벤트
            events?.OnTargetChanged?.SafeInvoke(oldTarget, target);
            
            LogManager.Log(LogCategory.Enemy, $"타겟 설정: {target.name}", this);
        }
        else
        {
            // 타겟 제거 이벤트
            events?.OnTargetLost?.SafeInvoke(oldTarget);
            
            LogManager.Log(LogCategory.Enemy, "타겟 제거", this);
        }
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    public void ChangeState(EnemyAIStateType newState)
    {
        if (stateMachine != null)
        {
            bool success = stateMachine.ChangeState(newState);
            
            if (success)
            {
                LogManager.Log(LogCategory.Enemy, $"상태 변경: {newState.ToDisplayString()}", this);
            }
            else
            {
                LogManager.LogWarning(LogCategory.Enemy, $"상태 변경 실패: {newState.ToDisplayString()}", this);
            }
        }
    }
    
    /// <summary>
    /// 현재 상태 정보 가져오기
    /// </summary>
    public string GetCurrentStateInfo()
    {
        return stateMachine?.GetCurrentStateInfo() ?? "상태 머신 없음";
    }
    
    /// <summary>
    /// 디버그 정보 가져오기
    /// </summary>
    public string GetDebugInfo()
    {
        if (!showDebugInfo) return "";
        
        string info = $"=== EnemyAI Debug ===\n";
        info += $"현재 상태: {CurrentStateName}\n";
        info += $"타겟: {(currentTarget?.name ?? "없음")}\n";
        
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            info += $"타겟 거리: {distance:F1}m\n";
        }
        
        info += $"설정: {(config?.name ?? "없음")}\n";
        info += $"이동: {(movement != null ? "있음" : "없음")}\n";
        info += $"전투: {(combat != null ? "있음" : "없음")}\n";
        info += $"인지: {(perception != null ? "있음" : "없음")}\n";
        
        return info;
    }
    
    /// <summary>
    /// 네트워크 동기화 데이터 생성
    /// </summary>
    public EnemyStateData CreateNetworkSyncData()
    {
        if (stateMachine?.CurrentState != null)
        {
            return stateMachine.CurrentState.CreateSyncData(this);
        }
        
        return new EnemyStateData(transform.position, CurrentStateType);
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// AI 업데이트 메인 로직
    /// </summary>
    private void UpdateAI()
    {
        if (stateMachine == null) return;
        
        // 상태 머신 업데이트
        stateMachine.Update();
        
        // 네트워크 동기화 (필요한 경우)
        if (networkSync != null)
        {
            networkSync.UpdateFromAI(this);
        }
        
        // 성능 메트릭 업데이트
        UpdatePerformanceMetrics();
    }
    
    /// <summary>
    /// 성능 메트릭 업데이트
    /// </summary>
    private void UpdatePerformanceMetrics()
    {
        // 간단한 성능 추적
        var metrics = new EnemyPerformanceMetrics
        {
            aiUpdatesPerSecond = 1f / aiUpdateInterval,
            lastUpdateTime = Time.time
        };
        
        events?.OnPerformanceMetricsUpdated?.SafeInvoke(metrics);
    }
    
    // ========== Gizmos ==========
    
    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // 상태 머신 기즈모
        stateMachine?.DrawGizmos();
        
        // 타겟 연결선
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
        
        // 설정 기즈모
        if (config != null && config.showGizmos)
        {
            // 탐지 범위
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, config.detectionRange);
            
            // 공격 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, config.attackRange);
        }
    }
    
    // ========== Validation ==========
    
    private void OnValidate()
    {
        // 컴포넌트 자동 할당 (에디터에서)
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (combat == null) combat = GetComponent<EnemyCombat>();
        if (perception == null) perception = GetComponent<EnemyPerception>();
        if (networkSync == null) networkSync = GetComponent<EnemyNetworkSync>();
    }
} 