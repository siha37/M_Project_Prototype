using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using UnityEngine;

/// <summary>
/// 추적 상태 - 타겟을 발견하여 추적 중인 상태
/// 타겟에게 접근하면서 공격 범위에 들어가기를 시도
/// </summary>
public class ChaseState : EnemyAIState
{
    public override EnemyAIStateType StateType => EnemyAIStateType.Chase;
    public override string StateName => "추적";
    
    // 추적 관련 변수들
    private float chaseStartTime;
    private float lastPathUpdateTime;
    private Vector3 lastKnownTargetPosition;
    private float targetLostTime;
    private bool hasLostTarget;
    
    public override void Enter(EnemyAI ai)
    {
        Log(ai, $"추적 상태 진입 - 타겟: {ai.CurrentTarget?.name}");
        
        chaseStartTime = Time.time;
        lastPathUpdateTime = 0f;
        targetLostTime = 0f;
        hasLostTarget = false;
        
        if (ai.CurrentTarget)
        {
            lastKnownTargetPosition = ai.CurrentTarget.position;
        }
        
        // 추적 속도로 변경
        var config = GetConfig(ai);
        if (config)
        {
            ai.Movement?.SetSpeed(config.chaseSpeed);
            ai.Movement?.SetStoppingDistance(config.attackRange * 0.8f); // 공격 범위보다 약간 가깝게
        }
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateEntered?.SafeInvoke(StateType);
    }
    
    public override void Update(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (!config) return;
        
        // 타겟 유효성 확인
        if (!IsTargetValid(ai))
        {
            HandleTargetLost(ai);
            return;
        }
        
        float distanceToTarget = GetDistanceToTarget(ai);
        
        // 공격 범위에 진입했는지 확인
        if (distanceToTarget <= config.attackRange)
        {
            // 시야 확인
            if (ai.Perception && ai.Perception.LineOfSight(ai.CurrentTarget))
            {
                Log(ai, $"공격 범위 진입 - 거리: {distanceToTarget:F1}m");
                ai.ChangeState(EnemyAIStateType.Attack);
                return;
            }
            else
            {
                Log(ai, "공격 범위 내이지만 시야가 막힘 - 계속 추적", EnemyConfig.LogLevel.Verbose);
            }
        }
        // 추적 이동 처리
        HandleChaseMovement(ai);
    }
    
    public override void Exit(EnemyAI ai)
    {
        Log(ai, "추적 상태 종료");
        
        // 이동 속도 원래대로 복구
        var config = GetConfig(ai);
        if (config)
        {
            ai.Movement?.SetSpeed(config.defaultSpeed);
            ai.Movement?.SetStoppingDistance(1f); // 기본값으로 복구
        }
        
        // 상태 초기화
        hasLostTarget = false;
        targetLostTime = 0f;
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateExited?.SafeInvoke(StateType);
    }
    
    public override bool CanTransitionTo(EnemyAI ai, EnemyAIStateType toState)
    {
        // 추적 중에는 Attack, Retreat, Patrol로만 전환 가능
        return toState == EnemyAIStateType.Attack || 
               toState == EnemyAIStateType.Retreat || 
               toState == EnemyAIStateType.Patrol;
    }
    
    public override int GetPriority()
    {
        return 3; // 중간 우선순위
    }
    
    public override string GetDebugInfo(EnemyAI ai)
    {
        string info = $"{StateName} - ";
        
        if (ai.CurrentTarget != null)
        {
            float distance = GetDistanceToTarget(ai);
            float chaseTime = Time.time - chaseStartTime;
            info += $"타겟: {ai.CurrentTarget.name}, 거리: {distance:F1}m, 추적 시간: {chaseTime:F1}초";
            
            if (hasLostTarget)
            {
                float lostTime = Time.time - targetLostTime;
                info += $", 놓친 시간: {lostTime:F1}초";
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
        
        // 공격 범위 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ai.transform.position, config.attackRange);
        
        // 추적 포기 범위 표시 (주황색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ai.transform.position, config.loseTargetRange);
        
        // 현재 타겟과의 연결선 표시
        if (ai.CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(ai.transform.position, ai.CurrentTarget.position);
            
            // 타겟 위치 표시
            Gizmos.DrawWireSphere(ai.CurrentTarget.position, 1f);
        }
        
        // 마지막으로 알려진 타겟 위치 표시
        if (lastKnownTargetPosition != Vector3.zero)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(lastKnownTargetPosition, 0.5f);
            Gizmos.DrawLine(ai.transform.position, lastKnownTargetPosition);
        }
    }
    
    public override EnemyStateData CreateSyncData(EnemyAI ai)
    {
        var baseData = base.CreateSyncData(ai);
        var syncData = baseData.WithPosition(ai.transform.position, 
            ai.CurrentTarget?.position ?? lastKnownTargetPosition);
        
        syncData.isChasing = true;
        syncData.hasValidTarget = ai.CurrentTarget;
        
        return syncData;
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 타겟이 유효한지 확인
    /// </summary>
    private bool IsTargetValid(EnemyAI ai)
    {
        if (!ai.CurrentTarget) return false;
        
        // 타겟이 살아있는지 확인 (PlayerNetworkSync의 IsDead 확인)
        var targetNetworkSync = ai.CurrentTarget.GetComponent<PlayerNetworkSync>();
        if (targetNetworkSync && targetNetworkSync.IsDead())
        {
            Log(ai, "타겟이 사망함");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 타겟을 잃었을 때 처리
    /// </summary>
    private void HandleTargetLost(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null) return;
        
        if (!hasLostTarget)
        {
            // 처음 타겟을 잃은 경우
            hasLostTarget = true;
            targetLostTime = Time.time;
            
            if (ai.CurrentTarget != null)
            {
                lastKnownTargetPosition = ai.CurrentTarget.position;
            }
            
            Log(ai, "타겟을 잃음 - 마지막 위치로 이동 중");
            
            // 마지막으로 알려진 위치로 이동
            if (ai.Movement != null)
            {
                ai.Movement.MoveTo(lastKnownTargetPosition);
            }
            
            // 이벤트 발생
            GetEvents(ai)?.OnTargetLost?.SafeInvoke(ai.CurrentTarget);
        }
        else
        {
            // 일정 시간 동안 타겟을 찾지 못했다면 순찰로 돌아감
            float lostTime = Time.time - targetLostTime;
            if (lostTime >= config.alertTime)
            {
                Log(ai, $"타겟 추적 포기 - {lostTime:F1}초 동안 찾지 못함");
                
                // 타겟 제거
                ai.SetTarget(null);
                
                // 순찰 상태로 전환
                ai.ChangeState(EnemyAIStateType.Patrol);
            }
        }
    }
    
    /// <summary>
    /// 추적 이동 처리
    /// </summary>
    private void HandleChaseMovement(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null || ai.CurrentTarget == null) return;
        
        // 경로 업데이트 간격 체크
        if (Time.time - lastPathUpdateTime >= config.pathUpdateInterval)
        {
            // 타겟 위치 업데이트
            lastKnownTargetPosition = ai.CurrentTarget.position;
            
            // 회피 기동 사용 여부 결정
            bool useStrafing = ShouldUseStrafing(ai);
            
            if (useStrafing)
            {
                // 회피 기동으로 이동
                if (ai.Movement != null)
                {
                    ai.Movement.StrafeAroundTarget(ai.CurrentTarget.position);
                }
                
                // 회피 기동 이벤트
                GetEvents(ai)?.OnStrafeStarted?.SafeInvoke(1f); // 방향은 Movement에서 관리
            }
            else
            {
                // 직접 추적
                if (ai.Movement != null)
                {
                    ai.Movement.MoveTo(ai.CurrentTarget.position);
                }
                
                // 이동 이벤트
                GetEvents(ai)?.OnMovementStarted?.SafeInvoke(ai.CurrentTarget.position);
            }
            
            lastPathUpdateTime = Time.time;
            
            Log(ai, $"경로 업데이트 - 타겟 위치: {ai.CurrentTarget.position}, 회피기동: {useStrafing}", 
                EnemyConfig.LogLevel.Verbose);
        }
    }
    
    /// <summary>
    /// 회피 기동을 사용할지 결정
    /// </summary>
    private bool ShouldUseStrafing(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null || ai.CurrentTarget == null) return false;
        
        float distanceToTarget = GetDistanceToTarget(ai);
        
        // 공격 범위에 가까워지면 회피 기동 사용
        float strafeThreshold = config.attackRange * 1.5f;
        
        return distanceToTarget <= strafeThreshold;
    }
} 