using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 인지 시스템 컴포넌트 (2D 버전)
/// 시야 확인, 장애물 감지, 타겟 탐지 등을 담당
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [SerializeField] EnemyConfig config;
    
    [Header("=== 시야 설정 ===")]
    [SerializeField] private Transform eyes; // 시야 시작점
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showVisionCone = true;
    [SerializeField] private bool showDetectionRange = true;
    [SerializeField] private bool showVisionRays = false;
    
    // 인지 상태
    private float lastVisionCheckTime;
    private bool hasLineOfSight;
    private Vector3 lastSeenPosition;
    private Transform currentTarget;
    
    // 성능 최적화 (2D용)
    private Collider2D[] detectedTargets;
    private const int maxDetectedTargets = 10;
    
    // 이벤트
    public System.Action<Transform> OnTargetInSight;
    public System.Action<Transform> OnTargetOutOfSight;
    public System.Action<Vector3> OnObstacleDetected;
    
    // ========== Properties ==========
    
    /// <summary>
    /// 탐지 범위
    /// </summary>
    public float DetectionRange => config.detectionRange;
    
    /// <summary>
    /// 시야각
    /// </summary>
    public float FieldOfViewAngle => config.fieldOfViewAngle;
    
    /// <summary>
    /// 현재 시야에 타겟이 있는지 여부
    /// </summary>
    public bool HasLineOfSight => hasLineOfSight;
    
    /// <summary>
    /// 마지막으로 본 위치
    /// </summary>
    public Vector3 LastSeenPosition => lastSeenPosition;
    
    /// <summary>
    /// 현재 타겟
    /// </summary>
    public Transform CurrentTarget => currentTarget;
    
    // ========== Unity Lifecycle ==========
    
    private void Awake()
    {
        // 시야 시작점 자동 할당
        if (eyes == null)
        {
            eyes = transform.Find("Eyes");
            if (eyes == null)
            {
                // Eyes가 없으면 자식으로 생성
                GameObject eyesObj = new GameObject("Eyes");
                eyesObj.transform.SetParent(transform);
                eyesObj.transform.localPosition = Vector3.zero; // 2D에서는 Z축 사용 안함
                eyes = eyesObj.transform;
            }
        }
        
        // 탐지 배열 초기화 (2D용)
        detectedTargets = new Collider2D[maxDetectedTargets];
        
        LogManager.Log(LogCategory.Enemy, "EnemyPerception 컴포넌트 초기화 완료", this);
    }
    
    private void Start()
    {
        // 인지 시스템 초기화
        LogManager.Log(LogCategory.Enemy, "EnemyPerception 초기화 완료", this);
    }
    
    private void Update()
    {
        // 시야 확인 간격 체크
        if (config != null && Time.time - lastVisionCheckTime >= config.visionCheckInterval)
        {
            UpdatePerception();
            lastVisionCheckTime = Time.time;
        }
    }
    
    // ========== Public Methods ==========
    
    /// <summary>
    /// 특정 타겟에 대한 시야 확인
    /// </summary>
    public bool LineOfSight(Transform target)
    {
        if (!target) return false;
        
        // 거리 확인 (2D 거리 계산)
        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > DetectionRange) return false;
        
        // 시야각 확인
        if (!IsInFieldOfView(target.position)) return false;
        
        // 장애물 확인
        return !IsObstructed(target.position);
    }

    public bool LineOfSight() { return LineOfSight(CurrentTarget); }
    
    /// <summary>
    /// 범위 내 모든 타겟 탐지
    /// </summary>
    public Transform[] DetectTargetsInRange()
    {
        // 범위 내 콜라이더 탐지 (2D)
        detectedTargets = Physics2D.OverlapCircleAll(transform.position, DetectionRange, config.targetLayer,0);
        
        // 유효한 타겟만 필터링
        List<Transform> validTargets = new List<Transform>();
        
        for (int i = 0; i < detectedTargets.Length; i++)
        {
            Transform target = detectedTargets[i].transform;
            
            // 시야 확인
            if (LineOfSight(target))
            {
                validTargets.Add(target);
            }
        }
        
        return validTargets.ToArray();
    }
    
    /// <summary>
    /// 가장 가까운 타겟 찾기
    /// </summary>
    public Transform FindClosestTarget()
    {
        Transform[] targets = DetectTargetsInRange();
        
        if (targets.Length == 0) return null;
        
        Transform closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Transform target in targets)
        {
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = target;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// 특정 위치가 시야각 내에 있는지 확인
    /// </summary>
    public bool IsInFieldOfView(Vector3 position)
    {
        Vector2 directionToTarget = ((Vector2)position - (Vector2)transform.position).normalized;
        
        // 현재 타겟이 있으면 타겟 방향을 기준으로, 없으면 기본 방향 사용
        Vector2 baseDirection;
        if (CurrentTarget != null)
        {
            // 현재 타겟 방향을 기준으로 설정
            baseDirection = ((Vector2)CurrentTarget.position - (Vector2)transform.position).normalized;
        }
        else
        {
            // 기본 방향 (오른쪽)
            baseDirection = Vector2.right;
        }
        
        float angle = Vector2.Angle(baseDirection, directionToTarget);
        
        return angle <= FieldOfViewAngle * 0.5f;
    }
    
    /// <summary>
    /// 특정 위치가 장애물에 가려져 있는지 확인
    /// </summary>
    public bool IsObstructed(Vector3 position)
    {
        if (!eyes) return false;
        
        Vector2 direction = ((Vector2)position - (Vector2)eyes.position).normalized;
        float distance = Vector2.Distance(eyes.position, position);
        
        // 레이캐스트로 장애물 확인 (2D)
        RaycastHit2D hit = Physics2D.Raycast(eyes.position, direction, distance, config.obstacleLayer);
        if (hit.collider)
        {
            return true; // 장애물이 있음
        }
        
        return false; // 장애물이 없음
    }
    
    /// <summary>
    /// 타겟 설정 (시야각 기준점 업데이트용)
    /// </summary>
    public void SetTarget(Transform target)
    {
        currentTarget = target;
        
        if (target)
        {
            lastSeenPosition = target.position;
            LogManager.Log(LogCategory.Enemy, $"EnemyPerception 타겟 설정: {target.name}", this);
        }
        else
        {
            LogManager.Log(LogCategory.Enemy, "EnemyPerception 타겟 제거", this);
        }
    }

    public void SetConfig(EnemyConfig _config)
    {
        this.config = _config; 
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 인지 시스템 업데이트
    /// </summary>
    private void UpdatePerception()
    {
        // 현재 타겟에 대한 시야 확인
        bool currentLineOfSight = false;
        
        if (CurrentTarget)
        {
            currentLineOfSight = LineOfSight(CurrentTarget);
            
            if (currentLineOfSight)
            {
                lastSeenPosition = CurrentTarget.position;
            }
        }
        
        // 시야 상태 변경 확인
        if (HasLineOfSight != currentLineOfSight)
        {
            hasLineOfSight = currentLineOfSight;
            
            if (HasLineOfSight)
            {
                OnTargetInSight?.Invoke(CurrentTarget);
                LogManager.Log(LogCategory.Enemy, $"타겟 시야 확보: {CurrentTarget?.name}", this);
            }
            else
            {
                OnTargetOutOfSight?.Invoke(CurrentTarget);
                LogManager.Log(LogCategory.Enemy, $"타겟 시야 상실: {CurrentTarget?.name}", this);
            }
        }
    }
    
    /// <summary>
    /// 시야각 내 장애물 탐지
    /// </summary>
    private void DetectObstaclesInFieldOfView()
    {
        if (eyes == null) return;
        
        // 기준 방향 결정 (타겟이 있으면 타겟 방향, 없으면 기본 방향)
        Vector2 baseDirection;
        if (CurrentTarget != null)
        {
            baseDirection = ((Vector2)CurrentTarget.position - (Vector2)transform.position).normalized;
        }
        else
        {
            baseDirection = Vector2.right;
        }
        
        float angleStep = FieldOfViewAngle / config.visionRayCount;
        
        for (int i = 0; i < config.visionRayCount; i++)
        {
            float angle = -FieldOfViewAngle * 0.5f + angleStep * i;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection; // 타겟 방향 기준 회전
            
            RaycastHit2D hit = Physics2D.Raycast(eyes.position, direction, DetectionRange, config.obstacleLayer);
            if (hit.collider != null)
            {
                OnObstacleDetected?.Invoke(hit.point);
                break; // 첫 번째 장애물만 처리
            }
        }
    }
    
    // ========== Gizmos ==========
    
   private void OnDrawGizmos()
    {
        if (!showDetectionRange && !showVisionCone) return;
        
        // 탐지 범위 표시 (2D 원)
        if (showDetectionRange)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
        }
        
        // 시야각 표시 (타겟 방향 기준)
        if (showVisionCone)
        {
            Gizmos.color = HasLineOfSight ? Color.green : Color.yellow;
            
            // 기준 방향 결정
            Vector2 baseDirection;
            if (CurrentTarget != null)
            {
                baseDirection = ((Vector2)CurrentTarget.position - (Vector2)transform.position).normalized;
            }
            else
            {
                baseDirection = Vector2.right;
            }
            
            int segments = 16;
            float angleStep = FieldOfViewAngle / segments;
            
            Vector3 previousPoint = transform.position;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = -FieldOfViewAngle * 0.5f + angleStep * i;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection; // 타겟 방향 기준 회전
                Vector3 point = transform.position + (Vector3)direction * DetectionRange;
                
                if (i > 0)
                {
                    Gizmos.DrawLine(previousPoint, point);
                }
                
                previousPoint = point;
            }
            
            // 시야각의 양 끝점을 시작점과 연결
            Vector2 leftDirection = Quaternion.Euler(0, 0, -FieldOfViewAngle * 0.5f) * baseDirection;
            Vector2 rightDirection = Quaternion.Euler(0, 0, FieldOfViewAngle * 0.5f) * baseDirection;
            
            Vector3 leftPoint = transform.position + (Vector3)leftDirection * DetectionRange;
            Vector3 rightPoint = transform.position + (Vector3)rightDirection * DetectionRange;
            
            Gizmos.DrawLine(transform.position, leftPoint);
            Gizmos.DrawLine(transform.position, rightPoint);
        }
        
        // 시야 레이 표시
        if (showVisionRays && eyes != null)
        {
            Gizmos.color = Color.red;
            
            if (CurrentTarget != null)
            {
                Vector2 direction = ((Vector2)CurrentTarget.position - (Vector2)eyes.position).normalized;
                float distance = Vector2.Distance(eyes.position, CurrentTarget.position);
                
                Gizmos.DrawRay(eyes.position, (Vector3)direction * distance);
            }
        }
        
        // 마지막으로 본 위치 표시
        if (LastSeenPosition != Vector3.zero)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(LastSeenPosition, 0.5f);
            Gizmos.DrawLine(transform.position, LastSeenPosition);
        }
    }
    
    // ========== Validation ==========
    
    private void OnValidate()
    {
        // 시야 시작점 자동 할당
        if (eyes == null)
        {
            eyes = transform.Find("Eyes");
        }

        if (config)
            return;
        // 설정값 검증
        if (DetectionRange < 1f) config.detectionRange = 10f;
        if (FieldOfViewAngle < 10f) config.fieldOfViewAngle = 90f;
        if (FieldOfViewAngle > 360f) config.fieldOfViewAngle = 360f;
        if (config.visionCheckInterval < 0.05f) config.visionCheckInterval = 0.1f;
        if (config.visionRayCount < 3) config.visionRayCount = 8;
        if (config.visionRayCount > 20) config.visionRayCount = 20;
    }
} 