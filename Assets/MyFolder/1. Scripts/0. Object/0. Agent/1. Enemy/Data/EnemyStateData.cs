using UnityEngine;
using System;

/// <summary>
/// 적 상태 네트워크 동기화용 통합 데이터 구조체
/// 기존의 여러 개별 SyncVar을 하나로 통합하여 네트워크 효율성 향상
/// </summary>
[Serializable]
public struct EnemyStateData : IEquatable<EnemyStateData>
{
    [Header("AI State")]
    /// <summary>
    /// 현재 AI 상태 (Patrol, Chase, Attack, Retreat)
    /// </summary>
    public EnemyAIStateType currentState;
    
    [Header("Position Data")]
    /// <summary>
    /// 현재 적의 위치
    /// </summary>
    public Vector3 currentPosition;
    
    /// <summary>
    /// 목표 위치 (이동 목적지)
    /// </summary>
    public Vector3 targetPosition;
    
    /// <summary>
    /// 마지막으로 알려진 플레이어 위치
    /// </summary>
    public Vector3 lastKnownTargetPosition;
    
    [Header("Combat Data")]
    /// <summary>
    /// 조준 각도 (발사 방향)
    /// </summary>
    public float lookAngle;
    
    /// <summary>
    /// 현재 공격 중인지 여부
    /// </summary>
    public bool isAttacking;
    
    /// <summary>
    /// 현재 재장전 중인지 여부
    /// </summary>
    public bool isReloading;
    
    /// <summary>
    /// 공격 쿨다운 시간
    /// </summary>
    public float attackCooldown;
    
    [Header("Movement Data")]
    /// <summary>
    /// 현재 추적 중인지 여부
    /// </summary>
    public bool isChasing;
    
    /// <summary>
    /// 회피 기동 중인지 여부
    /// </summary>
    public bool isStrafing;
    
    /// <summary>
    /// 회피 방향 (-1 또는 1)
    /// </summary>
    public float strafeDirection;
    
    [Header("Target Data")]
    /// <summary>
    /// 현재 타겟의 ClientId (NetworkObject 대신 사용)
    /// </summary>
    public int targetClientId;
    
    /// <summary>
    /// 타겟이 유효한지 여부
    /// </summary>
    public bool hasValidTarget;
    
    [Header("Timestamp")]
    /// <summary>
    /// 상태 업데이트 시간 (동기화 검증용)
    /// </summary>
    public float timestamp;
    
    /// <summary>
    /// 기본 생성자 (초기 상태)
    /// </summary>
    public static EnemyStateData Default => new EnemyStateData
    {
        currentState = EnemyAIStateType.Patrol,
        currentPosition = Vector3.zero,
        targetPosition = Vector3.zero,
        lastKnownTargetPosition = Vector3.zero,
        lookAngle = 0f,
        isAttacking = false,
        isReloading = false,
        attackCooldown = 0f,
        isChasing = false,
        isStrafing = false,
        strafeDirection = 1f,
        targetClientId = -1,
        hasValidTarget = false,
        timestamp = 0f
    };
    
    /// <summary>
    /// 특정 값으로 초기화하는 생성자
    /// </summary>
    public EnemyStateData(Vector3 position, EnemyAIStateType state = EnemyAIStateType.Patrol)
    {
        currentState = state;
        currentPosition = position;
        targetPosition = position;
        lastKnownTargetPosition = position;
        lookAngle = 0f;
        isAttacking = false;
        isReloading = false;
        attackCooldown = 0f;
        isChasing = false;
        isStrafing = false;
        strafeDirection = 1f;
        targetClientId = -1;
        hasValidTarget = false;
        timestamp = Time.time;
    }
    
    /// <summary>
    /// 상태 데이터 비교 (네트워크 동기화 최적화용)
    /// </summary>
    public bool Equals(EnemyStateData other)
    {
        return currentState == other.currentState &&
               Vector3.Distance(currentPosition, other.currentPosition) < 0.01f &&
               Vector3.Distance(targetPosition, other.targetPosition) < 0.01f &&
               Mathf.Abs(lookAngle - other.lookAngle) < 0.1f &&
               isAttacking == other.isAttacking &&
               isReloading == other.isReloading &&
               isChasing == other.isChasing &&
               isStrafing == other.isStrafing &&
               Mathf.Abs(strafeDirection - other.strafeDirection) < 0.01f &&
               targetClientId == other.targetClientId &&
               hasValidTarget == other.hasValidTarget;
    }
    
    public override bool Equals(object obj)
    {
        return obj is EnemyStateData other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(
            currentState,
            currentPosition,
            targetPosition,
            lookAngle,
            isAttacking,
            isReloading,
            isChasing,
            targetClientId
        );
    }
    
    public static bool operator ==(EnemyStateData left, EnemyStateData right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(EnemyStateData left, EnemyStateData right)
    {
        return !left.Equals(right);
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"EnemyState[{currentState.ToDisplayString()}] " +
               $"Pos:{currentPosition} Target:{targetPosition} " +
               $"Attacking:{isAttacking} Chasing:{isChasing} " +
               $"ClientId:{targetClientId}";
    }
    
    /// <summary>
    /// 타임스탬프 업데이트
    /// </summary>
    public EnemyStateData UpdateTimestamp()
    {
        var updated = this;
        updated.timestamp = Time.time;
        return updated;
    }
    
    /// <summary>
    /// 상태 변경 시 사용하는 헬퍼 메서드
    /// </summary>
    public EnemyStateData WithState(EnemyAIStateType newState)
    {
        var updated = this;
        updated.currentState = newState;
        updated.timestamp = Time.time;
        return updated;
    }
    
    /// <summary>
    /// 위치 업데이트 시 사용하는 헬퍼 메서드
    /// </summary>
    public EnemyStateData WithPosition(Vector3 current, Vector3 target)
    {
        var updated = this;
        updated.currentPosition = current;
        updated.targetPosition = target;
        updated.timestamp = Time.time;
        return updated;
    }
    
    /// <summary>
    /// 전투 상태 업데이트 시 사용하는 헬퍼 메서드
    /// </summary>
    public EnemyStateData WithCombat(bool attacking, bool reloading, float angle)
    {
        var updated = this;
        updated.isAttacking = attacking;
        updated.isReloading = reloading;
        updated.lookAngle = angle;
        updated.timestamp = Time.time;
        return updated;
    }
} 