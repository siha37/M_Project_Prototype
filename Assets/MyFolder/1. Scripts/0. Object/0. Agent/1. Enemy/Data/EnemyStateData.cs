using UnityEngine;
using System;

/// <summary>
/// 적 상태 네트워크 동기화용 최적화된 데이터 구조체
/// 네트워크 대역폭 절약을 위해 필요한 데이터만 포함
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
    /// 현재 적의 위치 (Vector2로 최적화)
    /// </summary>
    public Vector2 currentPosition;
    
    /// <summary>
    /// 목표 위치 (Vector2로 최적화)
    /// </summary>
    public Vector2 targetPosition;
    
    [Header("Combat Data")]
    /// <summary>
    /// 조준 각도 (발사 방향)
    /// </summary>
    public float lookAngle;
    
    /// <summary>
    /// 전투 상태 플래그 (비트 플래그로 최적화)
    /// </summary>
    public byte combatFlags; // 0: isAttacking, 1: isReloading, 2: isChasing, 3: isStrafing
    
    [Header("Target Data")]
    /// <summary>
    /// 현재 타겟의 ClientId
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
    
    // ========== Combat Flags ==========
    
    /// <summary>
    /// 공격 중인지 여부
    /// </summary>
    public bool isAttacking
    {
        get => (combatFlags & 1) != 0;
        set => combatFlags = value ? (byte)(combatFlags | 1) : (byte)(combatFlags & ~1);
    }
    
    /// <summary>
    /// 재장전 중인지 여부
    /// </summary>
    public bool isReloading
    {
        get => (combatFlags & 2) != 0;
        set => combatFlags = value ? (byte)(combatFlags | 2) : (byte)(combatFlags & ~2);
    }
    
    /// <summary>
    /// 추적 중인지 여부
    /// </summary>
    public bool isChasing
    {
        get => (combatFlags & 4) != 0;
        set => combatFlags = value ? (byte)(combatFlags | 4) : (byte)(combatFlags & ~4);
    }
    
    /// <summary>
    /// 회피 기동 중인지 여부
    /// </summary>
    public bool isStrafing
    {
        get => (combatFlags & 8) != 0;
        set => combatFlags = value ? (byte)(combatFlags | 8) : (byte)(combatFlags & ~8);
    }
    
    /// <summary>
    /// 기본 생성자 (초기 상태)
    /// </summary>
    public static EnemyStateData Default => new EnemyStateData
    {
        currentState = EnemyAIStateType.Patrol,
        currentPosition = Vector2.zero,
        targetPosition = Vector2.zero,
        lookAngle = 0f,
        combatFlags = 0,
        targetClientId = -1,
        hasValidTarget = false,
        timestamp = 0f
    };
    
    /// <summary>
    /// 생성자
    /// </summary>
    public EnemyStateData(Vector2 position, EnemyAIStateType state = EnemyAIStateType.Patrol)
    {
        currentState = state;
        currentPosition = position;
        targetPosition = position;
        lookAngle = 0f;
        combatFlags = 0;
        targetClientId = -1;
        hasValidTarget = false;
        timestamp = Time.time;
    }
    
    // ========== IEquatable Implementation ==========
    
    public bool Equals(EnemyStateData other)
    {
        return currentState == other.currentState &&
               currentPosition == other.currentPosition &&
               targetPosition == other.targetPosition &&
               lookAngle == other.lookAngle &&
               combatFlags == other.combatFlags &&
               targetClientId == other.targetClientId &&
               hasValidTarget == other.hasValidTarget;
    }
    
    public override bool Equals(object obj)
    {
        return obj is EnemyStateData other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)currentState;
            hashCode = (hashCode * 397) ^ currentPosition.GetHashCode();
            hashCode = (hashCode * 397) ^ targetPosition.GetHashCode();
            hashCode = (hashCode * 397) ^ lookAngle.GetHashCode();
            hashCode = (hashCode * 397) ^ combatFlags.GetHashCode();
            hashCode = (hashCode * 397) ^ targetClientId;
            hashCode = (hashCode * 397) ^ hasValidTarget.GetHashCode();
            return hashCode;
        }
    }
    
    // ========== Utility Methods ==========
    
    public override string ToString()
    {
        return $"EnemyStateData(State: {currentState}, Pos: {currentPosition}, Target: {targetPosition}, " +
               $"Angle: {lookAngle:F1}°, Flags: {combatFlags:X2}, TargetId: {targetClientId})";
    }
    
    /// <summary>
    /// 타임스탬프 업데이트
    /// </summary>
    public EnemyStateData UpdateTimestamp()
    {
        timestamp = Time.time;
        return this;
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    public EnemyStateData WithState(EnemyAIStateType newState)
    {
        currentState = newState;
        return this;
    }
    
    /// <summary>
    /// 위치 변경
    /// </summary>
    public EnemyStateData WithPosition(Vector2 current, Vector2 target)
    {
        currentPosition = current;
        targetPosition = target;
        return this;
    }
    
    /// <summary>
    /// 전투 상태 변경
    /// </summary>
    public EnemyStateData WithCombat(bool attacking, bool reloading, float angle)
    {
        isAttacking = attacking;
        isReloading = reloading;
        lookAngle = angle;
        return this;
    }
} 