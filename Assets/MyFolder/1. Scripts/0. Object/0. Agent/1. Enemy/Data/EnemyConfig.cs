using UnityEngine;
using UnityEngine.Serialization;

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
    [Range(5f, 60f)]
    public float detectionRange = 10f;
    
    [Tooltip("공격을 시작할 수 있는 거리")]
    [Range(2f, 10f)]
    public float attackRange = 5f;
    
    [Tooltip("추적을 포기하는 거리 (탐지 범위보다 커야 함)")]
    [Range(10f, 80f)]
    public float loseTargetRange = 15f;
    
    [Tooltip("시야각 (도)")]
    [Range(30f, 180f)]
    public float fieldOfViewAngle = 90f;
    
    
    
    
    [Header("=== 이동 설정 ===")]
    
    [Tooltip("일반 이동 속도")]
    [Range(1f, 8f)]
    public float defaultSpeed = 3f;
    
    [Tooltip("공격 중 이동 속도")]
    [Range(1f, 8f)]
    public float attackSpeed = 5f;
    
    [Tooltip("이동 최소 거리")]
    [Range(0f, 20f)]
    public float stoppingDistance = 5f;
    
    [Tooltip("회전 속도")]
    [Range(1,20)]
    public float rotationSpeed = 5f;


    
    
    [Header("=== 전투 설정 ===")]
    [Tooltip("타겟과의 최소 유지 거리")]
    [Range(1f, 5f)]
    public float minDistanceToTarget = 0.5f;
    
    [Tooltip("후퇴 시 이동할 거리")]
    [Range(2f, 8f)]
    public float retreatDistance = 4f;
    
    [Tooltip("조준 정밀도 (0: 완벽, 1: 부정확)")]
    [Range(0f, 1f)]
    public float aimPrecision = 0.1f;
    
    [Tooltip("공격 후 다음 공격까지의 최소 간격")]
    [Range(0.1f, 3f)]
    public float attackInterval = 0.5f;
    
    [Tooltip("재장전 시간")]
    [Range(1f, 5f)]
    public float reloadTime = 2f;
    
    
    [Header("=== 인지 설정 ===")]
    [Tooltip("시야 차단 장애물 레이어")]
    public LayerMask obstacleLayer = -1;
    
    [Tooltip("탐지 대상 레이어")]
    public LayerMask targetLayer = -1;
    
    [Tooltip("시야 확인 간격 (초)")]
    [Range(0.05f, 0.5f)]
    public float visionCheckInterval = 0.1f;
    
    [Tooltip("시야 확인용 레이 개수")]
    [Range(1, 21)] 
    public int visionRayCount = 8;
    
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
    }
    
    /// <summary>
    /// 설정 정보를 문자열로 반환 (디버깅용)
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"EnemyConfig[{name}] - Detection:{detectionRange} Attack:{attackRange} Speed:{defaultSpeed}";
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
        defaultSpeed = 3f;
        minDistanceToTarget = 2f;
        retreatDistance = 4f;
        aimPrecision = 0.1f;
        
        LogManager.Log(LogCategory.Enemy, $"{name} 설정을 기본값으로 리셋했습니다.", this);
    }
} 