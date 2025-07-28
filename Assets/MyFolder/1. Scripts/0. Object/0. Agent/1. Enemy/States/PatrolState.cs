using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 순찰 상태 - 기본 이동 및 경계 상태
/// 타겟을 찾기 전까지 기본적으로 머무르는 상태
/// </summary>
public class PatrolState : EnemyAIState
{
    public override EnemyAIStateType StateType => EnemyAIStateType.Patrol;
    public override string StateName => "순찰";
    
    // 순찰 관련 변수들
    private Vector3 currentPatrolTarget;
    private float patrolStartTime;
    private float nextPatrolTime;
    private bool isWaitingAtPatrolPoint;
    private Vector3 initialPosition;
    
    public override void Enter(EnemyAI ai)
    {
        Log(ai, "순찰 상태 진입");
        
        // 초기 위치 기록
        initialPosition = ai.transform.position;
        patrolStartTime = Time.time;
        isWaitingAtPatrolPoint = false;
        
        // 새로운 순찰 지점 설정
        SetNewPatrolTarget(ai);
        
        // 이동 속도를 일반 속도로 설정
        var config = GetConfig(ai);
        if (config != null)
        {
            ai.Movement?.SetSpeed(config.normalSpeed);
        }
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateEntered?.SafeInvoke(StateType);
    }
    
    public override void Update(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null) return;
        
        // 타겟 탐지 확인 (최우선)
        if (CheckForTargets(ai))
        {
            return; // 타겟을 발견했으면 상태 전환이 일어남
        }
        
        // 순찰 로직 실행
        HandlePatrolMovement(ai);
    }
    
    public override void Exit(EnemyAI ai)
    {
        Log(ai, "순찰 상태 종료");
        
        // 대기 상태 해제
        isWaitingAtPatrolPoint = false;
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateExited?.SafeInvoke(StateType);
    }
    
    public override bool CanTransitionTo(EnemyAI ai, EnemyAIStateType toState)
    {
        // 순찰 상태에서는 모든 전환을 허용 (타겟 발견 시 즉시 전환 가능)
        return true;
    }
    
    public override int GetPriority()
    {
        return 1; // 가장 낮은 우선순위 (기본 상태)
    }
    
    public override string GetDebugInfo(EnemyAI ai)
    {
        string info = $"{StateName} - ";
        
        if (isWaitingAtPatrolPoint)
        {
            float remainingWaitTime = nextPatrolTime - Time.time;
            info += $"대기 중 ({remainingWaitTime:F1}초 남음)";
        }
        else
        {
            float distanceToPatrol = Vector3.Distance(ai.transform.position, currentPatrolTarget);
            info += $"이동 중 (목표까지 {distanceToPatrol:F1}m)";
        }
        
        return info;
    }
    
    public override void DrawGizmos(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null || !config.showGizmos) return;
        
        // 순찰 반경 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(initialPosition, config.patrolRadius);
        
        // 현재 순찰 목표 지점 표시
        if (currentPatrolTarget != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentPatrolTarget, 0.5f);
            Gizmos.DrawLine(ai.transform.position, currentPatrolTarget);
        }
        
        // 탐지 범위 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(ai.transform.position, config.detectionRange);
    }
    
    public override EnemyStateData CreateSyncData(EnemyAI ai)
    {
        var baseData = base.CreateSyncData(ai);
        return baseData.WithPosition(ai.transform.position, currentPatrolTarget);
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 타겟 탐지 확인
    /// </summary>
    private bool CheckForTargets(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null) return false;
        
        // NetworkPlayerManager에서 살아있는 플레이어들을 가져옴
        var alivePlayers = NetworkPlayerManager.Instance?.GetAlivePlayers();
        if (alivePlayers == null || alivePlayers.Count == 0) return false;
        
        // 가장 가까운 플레이어 찾기
        Transform closestTarget = null;
        float closestDistance = float.MaxValue;
        
        foreach (var playerObj in alivePlayers)
        {
            if (playerObj == null) continue;
            
            float distance = Vector3.Distance(ai.transform.position, playerObj.transform.position);
            
            // 탐지 범위 내에 있고, 시야에 있는지 확인
            if (distance <= config.detectionRange && 
                ai.Perception != null && 
                ai.Perception.LineOfSight(playerObj.transform))
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = playerObj.transform;
                }
            }
        }
        
        // 타겟을 발견했다면
        if (closestTarget != null)
        {
            Log(ai, $"타겟 발견: {closestTarget.name} (거리: {closestDistance:F1}m)");
            
            // AI에 타겟 설정
            ai.SetTarget(closestTarget);
            
            // 추적 상태로 전환
            ai.ChangeState(EnemyAIStateType.Chase);
            
            // 이벤트 발생
            GetEvents(ai)?.OnTargetFound?.SafeInvoke(closestTarget);
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 순찰 이동 처리
    /// </summary>
    private void HandlePatrolMovement(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null) return;
        
        // 대기 중인 경우
        if (isWaitingAtPatrolPoint)
        {
            if (Time.time >= nextPatrolTime)
            {
                // 대기 시간이 끝났으면 새로운 순찰 지점으로 이동
                SetNewPatrolTarget(ai);
                isWaitingAtPatrolPoint = false;
            }
            return;
        }
        
        // 순찰 지점에 도달했는지 확인
        if (ai.Movement != null && ai.Movement.HasReachedDestination())
        {
            // 순찰 지점에 도달 - 잠시 대기
            isWaitingAtPatrolPoint = true;
            nextPatrolTime = Time.time + config.patrolWaitTime;
            
            Log(ai, $"순찰 지점 도달 - {config.patrolWaitTime}초 대기", EnemyConfig.LogLevel.Verbose);
            
            // 이벤트 발생
            GetEvents(ai)?.OnDestinationReached?.SafeInvoke(currentPatrolTarget);
        }
    }
    
    /// <summary>
    /// 새로운 순찰 목표 지점 설정
    /// </summary>
    private void SetNewPatrolTarget(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null) return;
        
        Vector3 newTarget = GetRandomPatrolPoint(ai, config.patrolRadius);
        
        // 이동 명령
        if (ai.Movement != null)
        {
            ai.Movement.MoveTo(newTarget);
            currentPatrolTarget = newTarget;
            
            Log(ai, $"새로운 순찰 지점 설정: {newTarget}", EnemyConfig.LogLevel.Verbose);
            
            // 이벤트 발생
            GetEvents(ai)?.OnMovementStarted?.SafeInvoke(newTarget);
        }
    }
    
    /// <summary>
    /// 랜덤 순찰 지점 생성
    /// </summary>
    private Vector3 GetRandomPatrolPoint(EnemyAI ai, float radius)
    {
        Vector3 basePosition = initialPosition;
        Vector3 randomPoint = Vector3.zero;
        bool validPointFound = false;
        int attempts = 0;
        const int maxAttempts = 10;
        
        while (!validPointFound && attempts < maxAttempts)
        {
            // 반경 내 랜덤 지점 생성
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 testPoint = basePosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // NavMesh 위의 유효한 위치인지 확인
            if (NavMesh.SamplePosition(testPoint, out NavMeshHit hit, radius * 0.5f, NavMesh.AllAreas))
            {
                randomPoint = hit.position;
                validPointFound = true;
            }
            
            attempts++;
        }
        
        // 유효한 지점을 찾지 못했다면 현재 위치 사용
        if (!validPointFound)
        {
            randomPoint = ai.transform.position;
            Log(ai, "유효한 순찰 지점을 찾지 못해 현재 위치 사용", EnemyConfig.LogLevel.Warning);
        }
        
        return randomPoint;
    }
} 