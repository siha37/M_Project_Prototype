using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 후퇴 상태 - 타겟과 거리 조절을 위한 후퇴 상태
/// 타겟이 너무 가까이 왔을 때 적절한 거리를 확보
/// </summary>
public class RetreatState : EnemyAIState
{
    public override EnemyAIStateType StateType => EnemyAIStateType.Retreat;
    public override string StateName => "후퇴";
    
    // 후퇴 관련 변수들
    private float retreatStartTime;
    private Vector3 retreatTarget;
    private bool hasReachedRetreatPosition;
    private float lastDistanceCheck;
    private Vector3 initialTargetPosition;
    
    public override void Enter(EnemyAI ai)
    {
        Log(ai, $"후퇴 상태 진입 - 타겟: {ai.CurrentTarget?.name}");
        
        retreatStartTime = Time.time;
        hasReachedRetreatPosition = false;
        lastDistanceCheck = 0f;
        
        if (ai.CurrentTarget != null)
        {
            initialTargetPosition = ai.CurrentTarget.position;
        }
        
        // 후퇴 목표 지점 설정
        CalculateRetreatPosition(ai);
        
        // 후퇴 속도 설정 (추적 속도 사용)
        var config = GetConfig(ai);
        if (config != null)
        {
            ai.Movement?.SetSpeed(config.chaseSpeed); // 빠르게 후퇴
        }
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateEntered?.SafeInvoke(StateType);
        GetEvents(ai)?.OnRetreatStarted?.SafeInvoke(retreatTarget);
    }
    
    public override void Update(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null) return;
        
        // 타겟 유효성 확인
        if (!IsTargetValid(ai))
        {
            Log(ai, "타겟이 유효하지 않음 - 순찰 상태로 전환");
            ai.ChangeState(EnemyAIStateType.Patrol);
            return;
        }
        
        float distanceToTarget = GetDistanceToTarget(ai);
        
        // 충분한 거리를 확보했는지 확인
        if (distanceToTarget >= config.minDistanceToTarget * 1.5f)
        {
            // 거리 확보 완료 - 다음 상태 결정
            HandleRetreatComplete(ai, distanceToTarget);
            return;
        }
        
        // 후퇴 이동 처리
        HandleRetreatMovement(ai);
    }
    
    public override void Exit(EnemyAI ai)
    {
        Log(ai, "후퇴 상태 종료");
        
        // 이동 속도 원래대로 복구
        var config = GetConfig(ai);
        if (config != null)
        {
            ai.Movement?.SetSpeed(config.defaultSpeed);
        }
        
        // 상태 초기화
        hasReachedRetreatPosition = false;
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateExited?.SafeInvoke(StateType);
    }
    
    public override bool CanTransitionTo(EnemyAI ai, EnemyAIStateType toState)
    {
        // 후퇴 중에는 Attack, Chase, Patrol로만 전환 가능
        return toState == EnemyAIStateType.Attack || 
               toState == EnemyAIStateType.Chase || 
               toState == EnemyAIStateType.Patrol;
    }
    
    public override int GetPriority()
    {
        return 4; // 높은 우선순위 (생존을 위한 후퇴)
    }
    
    public override string GetDebugInfo(EnemyAI ai)
    {
        string info = $"{StateName} - ";
        
        if (ai.CurrentTarget != null)
        {
            float distance = GetDistanceToTarget(ai);
            float retreatTime = Time.time - retreatStartTime;
            float distanceToRetreat = Vector3.Distance(ai.transform.position, retreatTarget);
            
            info += $"타겟: {ai.CurrentTarget.name}, 거리: {distance:F1}m, ";
            info += $"후퇴 시간: {retreatTime:F1}초, ";
            info += $"후퇴 지점까지: {distanceToRetreat:F1}m";
            
            if (hasReachedRetreatPosition)
            {
                info += " (도달 완료)";
            }
        }
        else
        {
            info += "타겟 없음";
        }
        
        return info;
    }
    
    public override void DrawGizmos(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null || !config.showGizmos) return;
        
        // 최소 거리 원 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ai.transform.position, config.minDistanceToTarget);
        
        // 안전 거리 원 표시 (녹색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(ai.transform.position, config.minDistanceToTarget * 1.5f);
        
        // 후퇴 목표 지점 표시
        if (retreatTarget != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(retreatTarget, 1f);
            Gizmos.DrawLine(ai.transform.position, retreatTarget);
        }
        
        // 타겟과의 연결선 표시
        if (ai.CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(ai.transform.position, ai.CurrentTarget.position);
            
            // 타겟 위치 표시
            Gizmos.DrawWireSphere(ai.CurrentTarget.position, 0.8f);
        }
        
        // 후퇴 방향 화살표 표시
        if (ai.CurrentTarget != null)
        {
            Vector3 retreatDirection = (ai.transform.position - ai.CurrentTarget.position).normalized;
            Vector3 arrowEnd = ai.transform.position + retreatDirection * 2f;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(ai.transform.position, retreatDirection * 2f);
        }
    }
    
    public override EnemyStateData CreateSyncData(EnemyAI ai)
    {
        var baseData = base.CreateSyncData(ai);
        return baseData.WithPosition(ai.transform.position, retreatTarget);
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 타겟이 유효한지 확인
    /// </summary>
    private bool IsTargetValid(EnemyAI ai)
    {
        if (!ai.CurrentTarget) return false;
        
        // 타겟이 살아있는지 확인
        var targetNetworkSync = ai.CurrentTarget.GetComponent<PlayerNetworkSync>();
        if (targetNetworkSync && targetNetworkSync.IsDead())
        {
            Log(ai, "타겟이 사망함");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 후퇴 위치 계산
    /// </summary>
    private void CalculateRetreatPosition(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (!config || !ai.CurrentTarget) return;
        
        // 타겟에서 멀어지는 방향 계산
        Vector3 retreatDirection = (ai.transform.position - ai.CurrentTarget.position).normalized;
        
        // 후퇴 거리 계산
        float retreatDistance = config.retreatDistance;
        
        // 후퇴 목표 지점 계산
        Vector3 idealRetreatPosition = ai.transform.position + retreatDirection * retreatDistance;
        
        // NavMesh 위의 유효한 위치 찾기
        if (FindValidRetreatPosition(idealRetreatPosition, retreatDistance, out Vector3 validPosition))
        {
            retreatTarget = validPosition;
            
            // 이동 명령
            if (ai.Movement != null)
            {
                ai.Movement.MoveTo(retreatTarget);
            }
            
            Log(ai, $"후퇴 목표 설정: {retreatTarget} (거리: {Vector3.Distance(ai.transform.position, retreatTarget):F1}m)");
        }
        else
        {
            // 유효한 후퇴 위치를 찾지 못한 경우, 현재 위치에서 대기
            retreatTarget = ai.transform.position;
            hasReachedRetreatPosition = true;
            
            Log(ai, "유효한 후퇴 위치를 찾지 못함 - 현재 위치에서 대기", EnemyConfig.LogLevel.Warning);
        }
    }
    
    /// <summary>
    /// 유효한 후퇴 위치 찾기
    /// </summary>
    private bool FindValidRetreatPosition(Vector3 idealPosition, float maxDistance, out Vector3 validPosition)
    {
        validPosition = Vector3.zero;
        
        // 먼저 이상적인 위치 시도
        if (NavMesh.SamplePosition(idealPosition, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            validPosition = hit.position;
            return true;
        }
        
        // 여러 방향으로 후퇴 위치 탐색
        int attempts = 8; // 8방향
        for (int i = 0; i < attempts; i++)
        {
            float angle = (360f / attempts) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 testPosition = idealPosition + direction * (maxDistance * 0.5f);
            
            if (NavMesh.SamplePosition(testPosition, out hit, maxDistance * 0.5f, NavMesh.AllAreas))
            {
                validPosition = hit.position;
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 후퇴 완료 처리
    /// </summary>
    private void HandleRetreatComplete(EnemyAI ai, float distanceToTarget)
    {
        var config = GetConfig(ai);
        if (config == null) return;
        
        Log(ai, $"후퇴 완료 - 현재 거리: {distanceToTarget:F1}m");
        
        // 다음 상태 결정
        if (distanceToTarget <= config.attackRange && 
            ai.Perception != null && 
            ai.Perception.LineOfSight(ai.CurrentTarget))
        {
            // 공격 가능한 거리이고 시야가 있다면 공격 상태로
            Log(ai, "공격 가능 거리 - 공격 상태로 전환");
            ai.ChangeState(EnemyAIStateType.Attack);
        }
        else if (distanceToTarget <= config.loseTargetRange)
        {
            // 추적 범위 내라면 추적 상태로
            Log(ai, "추적 범위 내 - 추적 상태로 전환");
            ai.ChangeState(EnemyAIStateType.Chase);
        }
        else
        {
            // 너무 멀어졌다면 순찰 상태로
            Log(ai, "타겟이 너무 멀어짐 - 순찰 상태로 전환");
            ai.SetTarget(null);
            ai.ChangeState(EnemyAIStateType.Patrol);
        }
    }
    
    /// <summary>
    /// 후퇴 이동 처리
    /// </summary>
    private void HandleRetreatMovement(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (!config) return;
        
        // 후퇴 목표 지점에 도달했는지 확인
        if (!hasReachedRetreatPosition && ai.Movement)
        {
            if (ai.Movement.HasReachedDestination)
            {
                hasReachedRetreatPosition = true;
                Log(ai, "후퇴 지점 도달", EnemyConfig.LogLevel.Verbose);
                
                // 이벤트 발생
                GetEvents(ai)?.OnDestinationReached?.SafeInvoke(retreatTarget);
            }
        }
        
        // 타겟이 계속 접근하고 있다면 추가 후퇴 고려
        if (ai.CurrentTarget != null && Time.time - lastDistanceCheck >= 0.5f)
        {
            float currentDistance = GetDistanceToTarget(ai);
            float previousDistance = Vector3.Distance(ai.transform.position, initialTargetPosition);
            
            // 타겟이 계속 쫓아오고 있다면
            if (hasReachedRetreatPosition && currentDistance < config.minDistanceToTarget)
            {
                Log(ai, "타겟이 계속 접근 중 - 추가 후퇴 필요", EnemyConfig.LogLevel.Verbose);
                
                // 새로운 후퇴 위치 계산
                CalculateRetreatPosition(ai);
                hasReachedRetreatPosition = false;
            }
            
            lastDistanceCheck = Time.time;
        }
    }
} 