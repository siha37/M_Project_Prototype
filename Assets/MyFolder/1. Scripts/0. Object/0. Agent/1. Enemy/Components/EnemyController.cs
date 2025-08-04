using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// 적 메인 네트워크 컨트롤러
/// EnemyControll.cs를 대체하는 새로운 메인 컴포넌트
/// 모든 AI 컴포넌트들을 조율하고 네트워크 동기화를 담당
/// </summary>
public class EnemyController : NetworkBehaviour
{
    [Header("=== AI 컴포넌트들 ===")]
    [SerializeField] private EnemyAI ai;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyCombat combat;
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private EnemyNetworkSync networkSync;
    
    [Header("=== 설정 ===")]
    [SerializeField] private EnemyConfig config;
    [SerializeField] private bool autoAssignComponents = true;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool logStateChanges = true;
    
    // 상태 관리
    private bool isInitialized = false;
    
    // 이벤트
    public System.Action<EnemyController> OnEnemySpawned;
    public System.Action<EnemyController> OnEnemyDespawned;
    public System.Action<EnemyStateData> OnStateDataChanged;
    
    // ========== Properties ==========
    
    /// <summary>
    /// AI 컴포넌트
    /// </summary>
    public EnemyAI AI => ai;
    
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
    /// 네트워크 동기화 컴포넌트
    /// </summary>
    public EnemyNetworkSync NetworkSync => networkSync;
    
    /// <summary>
    /// AI 설정
    /// </summary>
    public EnemyConfig Config => config;
    
    /// <summary>
    /// 현재 상태 데이터
    /// </summary>
    public EnemyStateData CurrentStateData => networkSync?.CurrentStateData ?? EnemyStateData.Default;
    
    /// <summary>
    /// 현재 AI 상태
    /// </summary>
    public EnemyAIStateType CurrentStateType => ai?.CurrentStateType ?? EnemyAIStateType.Patrol;
    
    /// <summary>
    /// 현재 타겟
    /// </summary>
    public Transform CurrentTarget => ai?.CurrentTarget;
    
    // ========== Unity Lifecycle ==========
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (autoAssignComponents)
        {
            AssignComponents();
        }
        
        // 컴포넌트 초기화 준비
        
        LogManager.Log(LogCategory.Enemy, "EnemyController 컴포넌트 초기화 완료", this);
    }
    
    public override void OnStartServer()
    {
        // 서버에서만 초기화
        if (config == null)
        {
            LogManager.LogError(LogCategory.Enemy, "EnemyConfig가 설정되지 않았습니다!", this);
            return;
        }
        
        // AI 컴포넌트들 초기화
        InitializeComponents();
        
        // 네트워크 동기화 설정
        if (networkSync != null)
        {
            networkSync.Initialize(this);
        }
        
        isInitialized = true;
        
        LogManager.Log(LogCategory.Enemy, "EnemyController 서버 초기화 완료", this);
        
        // 이벤트 발생
        OnEnemySpawned?.Invoke(this);
    }
    
    public override void OnStartClient()
    {
        // 클라이언트에서는 시각화만
        LogManager.Log(LogCategory.Enemy, "EnemyController 클라이언트 초기화 완료", this);
    }
    
    public override void OnStopServer()
    {
        // 서버 종료 시 정리
        CleanupComponents();
        
        LogManager.Log(LogCategory.Enemy, "EnemyController 서버 종료", this);
        
        // 이벤트 발생
        OnEnemyDespawned?.Invoke(this);
    }
    
    
    private void OnDestroy()
    {
        // 정리 작업
        CleanupComponents();
        
        LogManager.Log(LogCategory.Enemy, "EnemyController 정리 완료", this);
    }
    
    // ========== Public Methods ==========
    
    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(Transform target)
    {
        if (!IsServerInitialized) return;
        
        ai?.SetTarget(target);
        
        if (logStateChanges)
        {
            LogManager.Log(LogCategory.Enemy, $"타겟 설정: {(target?.name ?? "없음")}", this);
        }
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    public void ChangeState(EnemyAIStateType newState)
    {
        if (!IsServerInitialized) return;
        
        ai?.ChangeState(newState);
        
        if (logStateChanges)
        {
            LogManager.Log(LogCategory.Enemy, $"상태 변경: {newState.ToDisplayString()}", this);
        }
    }
    
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damage, Vector3 direction)
    {
        if (!IsServerInitialized) return;
        
        LogManager.Log(LogCategory.Enemy, $"데미지 받음: {damage}", this);
        
        // 여기서 체력 시스템이 있다면 처리
        // 현재는 로그만 출력
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    public void Die()
    {
        if (!IsServerInitialized) return;
        
        LogManager.Log(LogCategory.Enemy, "적 사망", this);
        
        // 사망 처리 로직
        // 예: 애니메이션, 이펙트, 점수 등
        
        // 네트워크에서 제거
        if (IsSpawned)
        {
            NetworkManager.ServerManager.Despawn(gameObject);
        }
    }
    
    /// <summary>
    /// 현재 상태 정보 가져오기
    /// </summary>
    public string GetStatusInfo()
    {
        string info = $"=== Enemy Status ===\n";
        info += $"상태: {CurrentStateType.ToDisplayString()}\n";
        info += $"타겟: {(CurrentTarget?.name ?? "없음")}\n";
        
        if (CurrentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, CurrentTarget.position);
            info += $"타겟 거리: {distance:F1}m\n";
        }
        
        info += $"체력: {(combat?.CurrentAmmo ?? 0)}/{(combat?.MaxAmmo ?? 0)}\n";
        info += $"재장전 중: {(combat?.IsReloading ?? false)}\n";
        info += $"이동 중: {(movement?.IsMoving ?? false)}\n";
        info += $"시야 확보: {(perception?.LineOfSight() ?? false)}\n";
        
        return info;
    }
    
    /// <summary>
    /// 디버그 정보 가져오기
    /// </summary>
    public string GetDebugInfo()
    {
        if (!showDebugInfo) return "";
        
        string info = $"=== EnemyController Debug ===\n";
        info += $"초기화됨: {isInitialized}\n";
        info += $"서버: {IsServerInitialized}\n";
        info += $"클라이언트: {IsClientInitialized}\n";
        info += $"AI: {(ai != null ? "있음" : "없음")}\n";
        info += $"이동: {(movement != null ? "있음" : "없음")}\n";
        info += $"전투: {(combat != null ? "있음" : "없음")}\n";
        info += $"인지: {(perception != null ? "있음" : "없음")}\n";
        info += $"네트워크: {(networkSync != null ? "있음" : "없음")}\n";
        
        if (ai != null)
        {
            info += ai.GetDebugInfo();
        }
        
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
        if (networkSync == null) networkSync = GetComponent<EnemyNetworkSync>();
    }
    
    /// <summary>
    /// 컴포넌트들 초기화 (의존성 순서에 따라)
    /// </summary>
    private void InitializeComponents()
    {
        if (config == null)
        {
            LogManager.LogError(LogCategory.Enemy, "EnemyConfig가 설정되지 않았습니다!", this);
            return;
        }

        LogManager.Log(LogCategory.Enemy, "컴포넌트 초기화 시작", this);

        // 1단계: 기본 컴포넌트들 설정 적용 (다른 컴포넌트에 의존하지 않는 것들)
        InitializeMovementComponent();
        InitializeCombatComponent();
        InitializePerceptionComponent();
        
        // 2단계: AI 컴포넌트 초기화 (다른 컴포넌트들을 참조함)
        InitializeAIComponent();
        
        // 3단계: 네트워크 동기화 컴포넌트 초기화 (모든 컴포넌트 참조)
        InitializeNetworkSyncComponent();
        
        LogManager.Log(LogCategory.Enemy, "컴포넌트 초기화 완료", this);
    }

    /// <summary>
    /// 이동 컴포넌트 초기화
    /// </summary>
    private void InitializeMovementComponent()
    {
        if (movement == null) return;

        movement.SetSpeed(config.defaultSpeed);
        movement.SetStoppingDistance(config.stoppingDistance);
        movement.SetRotationSpeed(config.rotationSpeed);
        movement.SetStrafeDistance(config.strafeDistance);
        movement.SetStrafeSpeedMultiplier(config.strafeSpeedMultiplier);
        movement.SetStrafeChangeInterval(config.strafeChangeInterval);
        movement.SetAgent();

        LogManager.Log(LogCategory.Enemy, "이동 컴포넌트 초기화 완료", this);
    }

    /// <summary>
    /// 전투 컴포넌트 초기화
    /// </summary>
    private void InitializeCombatComponent()
    {
        if (combat == null) return;

        combat.SetFireRate(config.attackInterval);
        combat.SetAimPrecision(config.aimPrecision);
        combat.SetReloadTime(config.reloadTime);

        LogManager.Log(LogCategory.Enemy, "전투 컴포넌트 초기화 완료", this);
    }

    /// <summary>
    /// 인지 컴포넌트 초기화
    /// </summary>
    private void InitializePerceptionComponent()
    {
        if (perception == null) return;

        perception.SetConfig(config);

        LogManager.Log(LogCategory.Enemy, "인지 컴포넌트 초기화 완료", this);
    }

    /// <summary>
    /// AI 컴포넌트 초기화
    /// </summary>
    private void InitializeAIComponent()
    {
        if (ai == null) return;

        // AI가 다른 컴포넌트들을 참조할 수 있도록 설정
        ai.Initialize(this, movement, combat, perception, config);

        LogManager.Log(LogCategory.Enemy, "AI 컴포넌트 초기화 완료", this);
    }

    /// <summary>
    /// 네트워크 동기화 컴포넌트 초기화
    /// </summary>
    private void InitializeNetworkSyncComponent()
    {
        if (networkSync == null) return;

        // 네트워크 동기화가 모든 컴포넌트를 참조할 수 있도록 설정
        networkSync.Initialize(this);

        LogManager.Log(LogCategory.Enemy, "네트워크 동기화 컴포넌트 초기화 완료", this);
    }
    
    /// <summary>
    /// 컴포넌트들 정리
    /// </summary>
    private void CleanupComponents()
    {
        // 각 컴포넌트 정리
        ai?.Events?.UnsubscribeAll();
        
        LogManager.Log(LogCategory.Enemy, "컴포넌트 정리 완료", this);
    }
    

    
    // ========== Gizmos ==========
    
    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // 현재 상태에 따른 색상 설정
        Color gizmoColor = CurrentStateType switch
        {
            EnemyAIStateType.Patrol => Color.green,
            EnemyAIStateType.Chase => Color.yellow,
            EnemyAIStateType.Attack => Color.red,
            EnemyAIStateType.Retreat => Color.cyan,
            _ => Color.white
        };
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // 타겟 연결선
        if (CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }
    }
    
    // ========== Validation ==========
    
    private void OnValidate()
    {
        // 컴포넌트 자동 할당 (에디터에서)
        if (autoAssignComponents)
        {
            AssignComponents();
        }
    }
} 