using UnityEngine;

/// <summary>
/// 적 AI 설정 ScriptableObject
/// 하드코딩된 매직 넘버들을 외부 설정 파일로 분리하여 
/// 디자이너가 쉽게 조정할 수 있도록 함
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Config", menuName = "Enemy/Enemy Config", order = 1)]
public class EnemyConfig : ScriptableObject
{
    [Header("=== 탐지 설정 ===")]
    [Tooltip("적이 플레이어를 탐지할 수 있는 최대 거리")]
    [Range(5f, 20f)]
    public float detectionRange = 10f;
    
    [Tooltip("공격을 시작할 수 있는 거리")]
    [Range(2f, 10f)]
    public float attackRange = 5f;
    
    [Tooltip("추적을 포기하는 거리 (탐지 범위보다 커야 함)")]
    [Range(10f, 25f)]
    public float loseTargetRange = 15f;
    
    [Tooltip("시야각 (도)")]
    [Range(30f, 180f)]
    public float fieldOfViewAngle = 90f;
    
    [Header("=== 이동 설정 ===")]
    [Tooltip("일반 이동 속도")]
    [Range(1f, 8f)]
    public float normalSpeed = 3f;
    
    [Tooltip("추적 시 이동 속도")]
    [Range(2f, 10f)]
    public float chaseSpeed = 5f;
    
    [Tooltip("경로 업데이트 간격 (초)")]
    [Range(0.1f, 2f)]
    public float pathUpdateInterval = 0.5f;
    
    [Tooltip("순찰 반경")]
    [Range(3f, 15f)]
    public float patrolRadius = 8f;
    
    [Tooltip("순찰 지점 대기 시간")]
    [Range(1f, 10f)]
    public float patrolWaitTime = 3f;
    
    [Header("=== 전투 설정 ===")]
    [Tooltip("타겟과의 최소 유지 거리")]
    [Range(1f, 5f)]
    public float minDistanceToTarget = 2f;
    
    [Tooltip("후퇴 시 이동할 거리")]
    [Range(2f, 8f)]
    public float retreatDistance = 4f;
    
    [Tooltip("조준 정밀도 (0: 완벽, 1: 부정확)")]
    [Range(0f, 1f)]
    public float aimPrecision = 0.1f;
    
    [Tooltip("공격 후 다음 공격까지의 최소 간격")]
    [Range(0.1f, 3f)]
    public float attackInterval = 0.5f;
    
    [Header("=== 회피 기동 설정 ===")]
    [Tooltip("회피 기동 거리")]
    [Range(1f, 6f)]
    public float strafeDistance = 3f;
    
    [Tooltip("회피 방향 변경 간격")]
    [Range(1f, 5f)]
    public float strafeChangeInterval = 2f;
    
    [Tooltip("회피 기동 속도 배율")]
    [Range(0.5f, 2f)]
    public float strafeSpeedMultiplier = 1.2f;
    
    [Header("=== 위치 탐색 설정 ===")]
    [Tooltip("사격 가능 위치 탐색 반경")]
    [Range(2f, 10f)]
    public float searchRadius = 5f;
    
    [Tooltip("사격 가능 위치 탐색 각도")]
    [Range(15f, 90f)]
    public float searchAngle = 45f;
    
    [Tooltip("위치 탐색 최대 시도 횟수")]
    [Range(5, 20)]
    public int maxSearchAttempts = 10;
    
    [Tooltip("위치 탐색 간격 (초)")]
    [Range(0.1f, 1f)]
    public float searchInterval = 0.5f;
    
    [Header("=== 인지 설정 ===")]
    [Tooltip("시야 차단 장애물 레이어")]
    public LayerMask obstacleLayer = -1;
    
    [Tooltip("탐지 대상 레이어")]
    public LayerMask targetLayer = -1;
    
    [Tooltip("시야 확인 간격 (초)")]
    [Range(0.05f, 0.5f)]
    public float visionCheckInterval = 0.1f;
    
    [Header("=== AI 행동 설정 ===")]
    [Tooltip("순찰 상태에서 대기 시간")]
    [Range(1f, 10f)]
    public float patrolIdleTime = 2f;
    
    [Tooltip("추적 포기 후 경계 시간")]
    [Range(3f, 15f)]
    public float alertTime = 5f;
    
    [Tooltip("공격 실패 시 재시도 간격")]
    [Range(0.5f, 3f)]
    public float attackRetryInterval = 1f;
    
    [Header("=== 디버그 설정 ===")]
    [Tooltip("디버그 정보 표시")]
    public bool showDebugInfo = false;
    
    [Tooltip("기즈모 표시")]
    public bool showGizmos = true;
    
    [Tooltip("로그 레벨")]
    public LogLevel logLevel = LogLevel.Warning;
    
    /// <summary>
    /// 로그 레벨 열거형
    /// </summary>
    public enum LogLevel
    {
        None,
        Error,
        Warning,
        Info,
        Verbose
    }
    
    [Header("=== 성능 설정 ===")]
    [Tooltip("AI 업데이트 간격 (초) - 성능 최적화용")]
    [Range(0.02f, 0.2f)]
    public float aiUpdateInterval = 0.05f;
    
    [Tooltip("먼 거리에서 AI 업데이트 간격 (초)")]
    [Range(0.1f, 1f)]
    public float distantUpdateInterval = 0.2f;
    
    [Tooltip("먼 거리 기준")]
    [Range(15f, 50f)]
    public float distantThreshold = 25f;
    
    /// <summary>
    /// 설정 유효성 검증
    /// </summary>
    private void OnValidate()
    {
        // 논리적 오류 방지
        if (loseTargetRange <= detectionRange)
        {
            loseTargetRange = detectionRange + 5f;
        }
        
        if (attackRange > detectionRange)
        {
            attackRange = detectionRange * 0.8f;
        }
        
        if (minDistanceToTarget >= attackRange)
        {
            minDistanceToTarget = attackRange * 0.5f;
        }
        
        if (retreatDistance <= minDistanceToTarget)
        {
            retreatDistance = minDistanceToTarget + 1f;
        }
        
        // 성능 관련 검증
        if (aiUpdateInterval > pathUpdateInterval)
        {
            aiUpdateInterval = pathUpdateInterval * 0.5f;
        }
    }
    
    /// <summary>
    /// 설정 정보를 문자열로 반환 (디버깅용)
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"EnemyConfig[{name}] - Detection:{detectionRange} Attack:{attackRange} Speed:{normalSpeed}/{chaseSpeed}";
    }
    
    /// <summary>
    /// 기본 설정값으로 리셋
    /// </summary>
    [ContextMenu("Reset to Default")]
    public void ResetToDefault()
    {
        detectionRange = 10f;
        attackRange = 5f;
        loseTargetRange = 15f;
        fieldOfViewAngle = 90f;
        normalSpeed = 3f;
        chaseSpeed = 5f;
        pathUpdateInterval = 0.5f;
        patrolRadius = 8f;
        minDistanceToTarget = 2f;
        retreatDistance = 4f;
        aimPrecision = 0.1f;
        strafeDistance = 3f;
        strafeChangeInterval = 2f;
        searchRadius = 5f;
        searchAngle = 45f;
        
        LogManager.Log(LogCategory.Enemy, $"{name} 설정을 기본값으로 리셋했습니다.", this);
    }
} 