using System;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data
{
    /// <summary>
    /// 적 AI 이벤트 시스템
    /// 컴포넌트 간 느슨한 결합을 위한 이벤트 기반 통신 제공
    /// </summary>
    [Serializable]
    public class EnemyEvents
    {
        [Header("=== AI 상태 이벤트 ===")]
    
        /// <summary>
        /// AI 상태가 변경될 때 발생
        /// </summary>
        public Action<EnemyAIStateType, EnemyAIStateType> OnStateChanged;
    
        /// <summary>
        /// 상태 진입 시 발생
        /// </summary>
        public Action<EnemyAIStateType> OnStateEntered;
    
        /// <summary>
        /// 상태 종료 시 발생
        /// </summary>
        public Action<EnemyAIStateType> OnStateExited;
    
        [Header("=== 타겟 관련 이벤트 ===")]
    
        /// <summary>
        /// 새로운 타겟을 발견했을 때 발생
        /// </summary>
        public Action<Transform> OnTargetFound;
    
        /// <summary>
        /// 타겟을 잃었을 때 발생
        /// </summary>
        public Action<Transform> OnTargetLost;
    
        /// <summary>
        /// 타겟이 변경되었을 때 발생
        /// </summary>
        public Action<Transform, Transform> OnTargetChanged; // oldTarget, newTarget
    
        /// <summary>
        /// 타겟과의 거리가 변경되었을 때 발생
        /// </summary>
        public Action<float> OnDistanceToTargetChanged;
    
        [Header("=== 전투 이벤트 ===")]
    
        /// <summary>
        /// 공격을 시작할 때 발생
        /// </summary>
        public Action OnAttackStarted;
    
        /// <summary>
        /// 공격이 끝났을 때 발생
        /// </summary>
        public Action OnAttackEnded;
    
        /// <summary>
        /// 발사했을 때 발생
        /// </summary>
        public Action<Vector3, float> OnShoot; // position, angle
    
        /// <summary>
        /// 재장전을 시작할 때 발생
        /// </summary>
        public Action OnReloadStarted;
    
        /// <summary>
        /// 재장전이 완료되었을 때 발생
        /// </summary>
        public Action OnReloadCompleted;
    
        /// <summary>
        /// 재장전 진행률 업데이트 시 발생
        /// </summary>
        public Action<float> OnReloadProgress; // 0.0 ~ 1.0
    
        [Header("=== 이동 이벤트 ===")]
    
        /// <summary>
        /// 이동을 시작할 때 발생
        /// </summary>
        public Action<Vector3> OnMovementStarted; // destination
    
        /// <summary>
        /// 목적지에 도달했을 때 발생
        /// </summary>
        public Action<Vector3> OnDestinationReached;
    
        /// <summary>
        /// 회피 기동을 시작할 때 발생
        /// </summary>
        public Action<float> OnStrafeStarted; // direction
    
        /// <summary>
        /// 회피 기동이 끝났을 때 발생
        /// </summary>
        public Action OnStrafeEnded;
    
        /// <summary>
        /// 후퇴를 시작할 때 발생
        /// </summary>
        public Action<Vector3> OnRetreatStarted; // retreat position
    
        [Header("=== 인지 이벤트 ===")]
    
        /// <summary>
        /// 시야에 타겟이 들어올 때 발생
        /// </summary>
        public Action<Transform> OnTargetInSight;
    
        /// <summary>
        /// 시야에서 타겟이 사라질 때 발생
        /// </summary>
        public Action<Transform> OnTargetOutOfSight;
    
        /// <summary>
        /// 장애물이 감지되었을 때 발생
        /// </summary>
        public Action<Vector3> OnObstacleDetected;
    
        /// <summary>
        /// 사격 가능한 위치를 찾았을 때 발생
        /// </summary>
        public Action<Vector3> OnShootingPositionFound;
    
        [Header("=== 데미지 및 상태 이벤트 ===")]
    
        /// <summary>
        /// 데미지를 받았을 때 발생
        /// </summary>
        public Action<float, Vector3> OnDamageReceived; // damage, direction
    
        /// <summary>
        /// 체력이 변경되었을 때 발생
        /// </summary>
        public Action<float, float> OnHealthChanged; // currentHealth, maxHealth
    
        /// <summary>
        /// 사망했을 때 발생
        /// </summary>
        public Action<Transform> OnDeath; // killer
    
        /// <summary>
        /// 부활했을 때 발생
        /// </summary>
        public Action OnRevived;
    
        [Header("=== 디버그 이벤트 ===")]
    
        /// <summary>
        /// 디버그 메시지 발생 시
        /// </summary>
        public Action<string, EnemyConfig.LogLevel> OnDebugMessage;
    
        /// <summary>
        /// 성능 메트릭 업데이트 시 발생
        /// </summary>
        public Action<EnemyPerformanceMetrics> OnPerformanceMetricsUpdated;
    
        /// <summary>
        /// 모든 이벤트 구독 해제
        /// </summary>
        public void UnsubscribeAll()
        {
            OnStateChanged = null;
            OnStateEntered = null;
            OnStateExited = null;
        
            OnTargetFound = null;
            OnTargetLost = null;
            OnTargetChanged = null;
            OnDistanceToTargetChanged = null;
        
            OnAttackStarted = null;
            OnAttackEnded = null;
            OnShoot = null;
            OnReloadStarted = null;
            OnReloadCompleted = null;
            OnReloadProgress = null;
        
            OnMovementStarted = null;
            OnDestinationReached = null;
            OnStrafeStarted = null;
            OnStrafeEnded = null;
            OnRetreatStarted = null;
        
            OnTargetInSight = null;
            OnTargetOutOfSight = null;
            OnObstacleDetected = null;
            OnShootingPositionFound = null;
        
            OnDamageReceived = null;
            OnHealthChanged = null;
            OnDeath = null;
            OnRevived = null;
        
            OnDebugMessage = null;
            OnPerformanceMetricsUpdated = null;
        }
    
        /// <summary>
        /// 이벤트 발생 수 확인 (디버깅용)
        /// </summary>
        public int GetSubscriberCount()
        {
            int count = 0;
            count += OnStateChanged?.GetInvocationList().Length ?? 0;
            count += OnTargetFound?.GetInvocationList().Length ?? 0;
            count += OnAttackStarted?.GetInvocationList().Length ?? 0;
            count += OnMovementStarted?.GetInvocationList().Length ?? 0;
            // ... 필요에 따라 더 추가
        
            return count;
        }
    }

    /// <summary>
    /// 적 성능 메트릭 데이터
    /// </summary>
    [Serializable]
    public struct EnemyPerformanceMetrics
    {
        /// <summary>
        /// 초당 AI 업데이트 횟수
        /// </summary>
        public float aiUpdatesPerSecond;
    
        /// <summary>
        /// 초당 이동 업데이트 횟수
        /// </summary>
        public float movementUpdatesPerSecond;
    
        /// <summary>
        /// 초당 시야 확인 횟수
        /// </summary>
        public float visionChecksPerSecond;
    
        /// <summary>
        /// 평균 경로 찾기 시간 (ms)
        /// </summary>
        public float averagePathfindingTime;
    
        /// <summary>
        /// 메모리 사용량 (bytes)
        /// </summary>
        public long memoryUsage;
    
        /// <summary>
        /// 활성 코루틴 수
        /// </summary>
        public int activeCoroutines;
    
        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public float lastUpdateTime;
    
        public override string ToString()
        {
            return $"AI:{aiUpdatesPerSecond:F1}/s Movement:{movementUpdatesPerSecond:F1}/s " +
                   $"Vision:{visionChecksPerSecond:F1}/s Pathfinding:{averagePathfindingTime:F2}ms " +
                   $"Memory:{memoryUsage / 1024}KB Coroutines:{activeCoroutines}";
        }
    }

    /// <summary>
    /// 이벤트 시스템 확장 메서드
    /// </summary>
    public static class EnemyEventsExtensions
    {
        /// <summary>
        /// 안전한 이벤트 호출 (null 체크 포함)
        /// </summary>
        public static void SafeInvoke<T>(this Action<T> action, T parameter)
        {
            try
            {
                action?.Invoke(parameter);
            }
            catch (Exception e)
            {
                LogManager.LogError(LogCategory.Enemy, $"이벤트 호출 중 오류 발생: {e.Message}");
            }
        }

        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 parameter1, T2 parameter2)
        {
            try
            {
                action?.Invoke(parameter1, parameter2);
            }
            catch (Exception e)
            {
                LogManager.LogError(LogCategory.Enemy, $"이벤트 호출 중 오류 발생: {e.Message}");
            }
        }
        
        /// <summary>
        /// 안전한 이벤트 호출 (파라미터 없음)
        /// </summary>
        public static void SafeInvoke(this Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                LogManager.LogError(LogCategory.Enemy, $"이벤트 호출 중 오류 발생: {e.Message}");
            }
        }
    }
}