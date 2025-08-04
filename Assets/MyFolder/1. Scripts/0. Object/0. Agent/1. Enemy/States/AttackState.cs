using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using UnityEngine;

/// <summary>
/// 공격 상태 - 사격 범위 내에서 공격을 수행하는 상태
/// 조준, 발사, 재장전 등의 전투 로직을 처리
/// </summary>
public class AttackState : EnemyAIState
{
    public override EnemyAIStateType StateType => EnemyAIStateType.Attack;
    public override string StateName => "공격";
    
    // 공격 관련 변수들
    private float attackStartTime;
    private float lastAttackTime;
    private float lastLineOfSightTime;
    private bool hadLineOfSight;
    private Vector3 lastTargetPosition;
    
    public override void Enter(EnemyAI ai)
    {
        Log(ai, $"공격 상태 진입 - 타겟: {ai.CurrentTarget?.name}");
        
        attackStartTime = Time.time;
        lastAttackTime = 0f;
        lastLineOfSightTime = Time.time;
        hadLineOfSight = true;
        
        if (ai.CurrentTarget != null)
        {
            lastTargetPosition = ai.CurrentTarget.position;
        }
        
        // 이동 정지 (공격에 집중)
        var config = GetConfig(ai);
        if (config != null)
        {
            ai.Movement?.SetSpeed(0f);
        }
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateEntered?.SafeInvoke(StateType);
        GetEvents(ai)?.OnAttackStarted?.SafeInvoke();
    }
    
    public override void Update(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (!config) return;
        
        // 타겟 유효성 확인
        if (!IsTargetValid(ai))
        {
            Log(ai, "타겟이 유효하지 않음 - 추적 상태로 전환");
            ai.ChangeState(EnemyAIStateType.Chase);
            return;
        }
        
        float distanceToTarget = GetDistanceToTarget(ai);
        
        // 타겟이 너무 가까이 있는지 확인
        if (distanceToTarget < config.minDistanceToTarget)
        {
            Log(ai, $"타겟이 너무 가까움 - 거리: {distanceToTarget:F1}m, 후퇴");
            ai.ChangeState(EnemyAIStateType.Retreat);
            return;
        }
        
        // 타겟이 공격 범위를 벗어났는지 확인
        if (distanceToTarget > config.attackRange)
        {
            Log(ai, $"타겟이 공격 범위 벗어남 - 거리: {distanceToTarget:F1}m");
            ai.ChangeState(EnemyAIStateType.Chase);
            return;
        }
        
        // 시야 확인 및 공격 로직
        HandleAttackLogic(ai);
    }
    
    public override void Exit(EnemyAI ai)
    {
        Log(ai, "공격 상태 종료");
        
        // 이동 속도 복구
        var config = GetConfig(ai);
        if (config != null)
        {
            ai.Movement?.SetSpeed(config.defaultSpeed);
        }
        
        // 이벤트 발생
        GetEvents(ai)?.OnStateExited?.SafeInvoke(StateType);
        GetEvents(ai)?.OnAttackEnded?.SafeInvoke();
    }
    
    public override bool CanTransitionTo(EnemyAI ai, EnemyAIStateType toState)
    {
        // 공격 중에는 Chase, Retreat로만 전환 가능
        // Patrol은 타겟을 완전히 잃었을 때만
        return toState == EnemyAIStateType.Chase || 
               toState == EnemyAIStateType.Retreat ||
               (toState == EnemyAIStateType.Patrol && ai.CurrentTarget == null);
    }
    
    public override int GetPriority()
    {
        return 5; // 높은 우선순위 (전투는 중요)
    }
    
    public override string GetDebugInfo(EnemyAI ai)
    {
        string info = $"{StateName} - ";
        
        if (ai.CurrentTarget != null)
        {
            float distance = GetDistanceToTarget(ai);
            float attackTime = Time.time - attackStartTime;
            bool canShoot = ai.Combat != null && ai.Combat.CanShoot;
            bool isReloading = ai.Combat != null && ai.Combat.IsReloading;
            
            info += $"타겟: {ai.CurrentTarget.name}, 거리: {distance:F1}m, ";
            info += $"공격 시간: {attackTime:F1}초, ";
            info += $"발사 가능: {canShoot}, 재장전 중: {isReloading}";
            
            if (!hadLineOfSight)
            {
                float noSightTime = Time.time - lastLineOfSightTime;
                info += $", 시야 차단: {noSightTime:F1}초";
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
        
        // 최소 거리 표시 (주황색)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(ai.transform.position, config.minDistanceToTarget);
        
        // 타겟과의 시야선 표시
        if (ai.CurrentTarget != null)
        {
            bool hasLineOfSight = ai.Perception != null && ai.Perception.LineOfSight(ai.CurrentTarget);
            
            Gizmos.color = hasLineOfSight ? Color.green : Color.red;
            Gizmos.DrawLine(ai.transform.position, ai.CurrentTarget.position);
            
            // 타겟 위치 표시
            Gizmos.DrawWireSphere(ai.CurrentTarget.position, 1f);
        }
        
        // 조준 방향 표시 (Combat 컴포넌트가 있는 경우)
        if (ai.Combat != null)
        {
            // 여기서는 간단히 타겟 방향으로 선을 그음
            if (ai.CurrentTarget != null)
            {
                Vector3 aimDirection = (ai.CurrentTarget.position - ai.transform.position).normalized;
                Vector3 aimEndPoint = ai.transform.position + aimDirection * 3f;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(ai.transform.position, aimDirection * 3f);
            }
        }
    }
    
    public override EnemyStateData CreateSyncData(EnemyAI ai)
    {
        var baseData = base.CreateSyncData(ai);
        
        bool isAttacking = ai.Combat != null && ai.Combat.CanShoot && hadLineOfSight;
        bool isReloading = ai.Combat != null && ai.Combat.IsReloading;
        float lookAngle = 0f;
        
        // 조준 각도 계산
        if (ai.CurrentTarget != null)
        {
            Vector2 direction = ai.CurrentTarget.position - ai.transform.position;
            lookAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
        
        return baseData.WithCombat(isAttacking, isReloading, lookAngle);
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 타겟이 유효한지 확인
    /// </summary>
    private bool IsTargetValid(EnemyAI ai)
    {
        if (ai.CurrentTarget == null) return false;
        
        // 타겟이 살아있는지 확인
        PlayerNetworkSync targetNetworkSync = ai.CurrentTarget.GetComponent<PlayerNetworkSync>();
        if (targetNetworkSync && targetNetworkSync.IsDead())
        {
            Log(ai, "타겟이 사망함");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 공격 로직 처리
    /// </summary>
    private void HandleAttackLogic(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (config == null || ai.CurrentTarget == null) return;
        
        // 시야 확인
        bool currentLineOfSight = ai.Perception && ai.Perception.LineOfSight(ai.CurrentTarget);
        
        if (currentLineOfSight)
        {
            // 시야에 타겟이 있음
            if (!hadLineOfSight)
            {
                Log(ai, "타겟 시야 확보", EnemyConfig.LogLevel.Verbose);
            }
            
            hadLineOfSight = true;
            lastLineOfSightTime = Time.time;
            lastTargetPosition = ai.CurrentTarget.position;
            
            // 조준 및 공격
            HandleAimingAndShooting(ai);
        }
        else
        {
            // 시야에 타겟이 없음
            if (hadLineOfSight)
            {
                Log(ai, "타겟 시야 상실", EnemyConfig.LogLevel.Verbose);
            }
            
            hadLineOfSight = false;
            
            // 일정 시간 동안 시야를 잃었다면 새로운 위치 탐색
            float noSightTime = Time.time - lastLineOfSightTime;
            if (noSightTime >= config.attackRetryInterval)
            {
                Log(ai, $"시야 차단 지속 - 위치 변경 필요 ({noSightTime:F1}초)");
                ai.ChangeState(EnemyAIStateType.Chase);
            }
        }
    }
    
    /// <summary>
    /// 조준 및 발사 처리
    /// </summary>
    private void HandleAimingAndShooting(EnemyAI ai)
    {
        var config = GetConfig(ai);
        if (!config || !ai.CurrentTarget || !ai.Combat) return;
        
        // 타겟을 향해 조준
        ai.Combat.AimAt(ai.CurrentTarget.position);
        
        // 발사 시도
        if (ai.Combat.CanShoot)
        {
            // 공격 간격 체크
            if (Time.time - lastAttackTime >= config.attackInterval)
            {
                if (ai.Combat.TryShoot())
                {
                    lastAttackTime = Time.time;
                    
                    Log(ai, $"발사 성공 - 타겟: {ai.CurrentTarget.name}", EnemyConfig.LogLevel.Verbose);
                    
                    // 발사 이벤트
                    GetEvents(ai)?.OnShoot?.SafeInvoke(ai.transform.position, 
                        GetLookAngleToTarget(ai));
                }
                else
                {
                    Log(ai, "발사 실패 - 재장전 필요할 수 있음", EnemyConfig.LogLevel.Verbose);
                }
            }
        }
        else if (ai.Combat.IsReloading)
        {
            Log(ai, "재장전 중...", EnemyConfig.LogLevel.Verbose);
        }
        else
        {
            // 탄약이 없으면 재장전 시작
            Log(ai, "탄약 부족 - 재장전 시작");
            ai.Combat.StartReload();
            
            // 재장전 이벤트
            GetEvents(ai)?.OnReloadStarted?.SafeInvoke();
        }
    }
    
    /// <summary>
    /// 타겟을 향한 조준 각도 계산
    /// </summary>
    private float GetLookAngleToTarget(EnemyAI ai)
    {
        if (ai.CurrentTarget == null) return 0f;
        
        Vector2 direction = ai.CurrentTarget.position - ai.transform.position;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
} 