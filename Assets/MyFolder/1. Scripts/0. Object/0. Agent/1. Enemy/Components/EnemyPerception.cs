using UnityEngine;
using FishNet.Object;

/// <summary>
/// 적 인지 시스템 컴포넌트
/// 시야 확인, 장애물 감지, 타겟 탐지 등을 담당
/// </summary>
public class EnemyPerception : NetworkBehaviour
{
    [Header("=== 인지 설정 ===")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float fieldOfViewAngle = 90f;
    [SerializeField] private LayerMask targetLayer = -1;
    [SerializeField] private LayerMask obstacleLayer = -1;
    
    [Header("=== 시야 설정 ===")]
    [SerializeField] private Transform eyes; // 시야 시작점
    [SerializeField] private float visionCheckInterval = 0.1f;
    [SerializeField] private int visionRayCount = 8; // 시야 확인용 레이 개수
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showVisionCone = true;
    [SerializeField] private bool showDetectionRange = true;
    [SerializeField] private bool showVisionRays = false;
    
    // 인지 상태
    private float lastVisionCheckTime;
    private bool hasLineOfSight;
    private Vector3 lastSeenPosition;
    private Transform currentTarget;
    
    // 성능 최적화
    private Collider[] detectedTargets;
    private int maxDetectedTargets = 10;
    
    // 이벤트
    public System.Action<Transform> OnTargetInSight;
    public System.Action<Transform> OnTargetOutOfSight;
    public System.Action<Vector3> OnObstacleDetected;
    
    // ========== Properties ==========
    
    /// <summary>
    /// 탐지 범위
    /// </summary>
    public float DetectionRange => detectionRange;
    
    /// <summary>
    /// 시야각
    /// </summary>
    public float FieldOfViewAngle => fieldOfViewAngle;
    
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
                eyesObj.transform.localPosition = Vector3.up * 0.5f; // 약간 위쪽
                eyes = eyesObj.transform;
            }
        }
        
        // 탐지 배열 초기화
        detectedTargets = new Collider[maxDetectedTargets];
        
        LogManager.Log(LogCategory.Enemy, "EnemyPerception 컴포넌트 초기화 완료", this);
    }
    
    public override void OnStartServer()
    {
        // 서버에서만 인지 로직 실행
        LogManager.Log(LogCategory.Enemy, "EnemyPerception 서버 초기화 완료", this);
    }
    
    public override void OnStartClient()
    {
        // 클라이언트에서는 시각화만
        LogManager.Log(LogCategory.Enemy, "EnemyPerception 클라이언트 초기화 완료", this);
    }
    
    private void Update()
    {
        // 서버에서만 인지 업데이트
        if (!IsServer) return;
        
        // 시야 확인 간격 체크
        if (Time.time - lastVisionCheckTime >= visionCheckInterval)
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
        if (target == null) return false;
        
        // 거리 확인
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > detectionRange) return false;
        
        // 시야각 확인
        if (!IsInFieldOfView(target.position)) return false;
        
        // 장애물 확인
        return !IsObstructed(target.position);
    }
    
    /// <summary>
    /// 범위 내 모든 타겟 탐지
    /// </summary>
    public Transform[] DetectTargetsInRange()
    {
        if (!IsServerOnlyInitialized) return new Transform[0];
        
        // 범위 내 콜라이더 탐지
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, detectedTargets, targetLayer);
        
        // 유효한 타겟만 필터링
        System.Collections.Generic.List<Transform> validTargets = new System.Collections.Generic.List<Transform>();
        
        for (int i = 0; i < hitCount; i++)
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
            float distance = Vector3.Distance(transform.position, target.position);
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
        Vector3 directionToTarget = (position - transform.position).normalized;
        Vector3 forward = transform.forward;
        
        float angle = Vector3.Angle(forward, directionToTarget);
        
        return angle <= fieldOfViewAngle * 0.5f;
    }
    
    /// <summary>
    /// 특정 위치가 장애물에 가려져 있는지 확인
    /// </summary>
    public bool IsObstructed(Vector3 position)
    {
        if (eyes == null) return false;
        
        Vector3 direction = (position - eyes.position).normalized;
        float distance = Vector3.Distance(eyes.position, position);
        
        // 레이캐스트로 장애물 확인
        if (Physics.Raycast(eyes.position, direction, out RaycastHit hit, distance, obstacleLayer))
        {
            return true; // 장애물이 있음
        }
        
        return false; // 장애물이 없음
    }
    
    /// <summary>
    /// 탐지 범위 설정
    /// </summary>
    public void SetDetectionRange(float range)
    {
        if (!IsServer) return;
        
        detectionRange = Mathf.Max(1f, range);
        
        LogManager.Log(LogCategory.Enemy, $"탐지 범위 변경: {detectionRange}", this);
    }
    
    /// <summary>
    /// 시야각 설정
    /// </summary>
    public void SetFieldOfViewAngle(float angle)
    {
        if (!IsServer) return;
        
        fieldOfViewAngle = Mathf.Clamp(angle, 10f, 360f);
        
        LogManager.Log(LogCategory.Enemy, $"시야각 변경: {fieldOfViewAngle}", this);
    }
    
    /// <summary>
    /// 타겟 레이어 설정
    /// </summary>
    public void SetTargetLayer(LayerMask layer)
    {
        if (!IsServer) return;
        
        targetLayer = layer;
        
        LogManager.Log(LogCategory.Enemy, $"타겟 레이어 변경: {layer}", this);
    }
    
    /// <summary>
    /// 장애물 레이어 설정
    /// </summary>
    public void SetObstacleLayer(LayerMask layer)
    {
        if (!IsServer) return;
        
        obstacleLayer = layer;
        
        LogManager.Log(LogCategory.Enemy, $"장애물 레이어 변경: {layer}", this);
    }
    
    /// <summary>
    /// 시야 확인 간격 설정
    /// </summary>
    public void SetVisionCheckInterval(float interval)
    {
        if (!IsServer) return;
        
        visionCheckInterval = Mathf.Max(0.05f, interval);
        
        LogManager.Log(LogCategory.Enemy, $"시야 확인 간격 변경: {visionCheckInterval}", this);
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 인지 시스템 업데이트
    /// </summary>
    private void UpdatePerception()
    {
        // 현재 타겟에 대한 시야 확인
        bool currentLineOfSight = false;
        
        if (currentTarget != null)
        {
            currentLineOfSight = LineOfSight(currentTarget);
            
            if (currentLineOfSight)
            {
                lastSeenPosition = currentTarget.position;
            }
        }
        
        // 시야 상태 변경 확인
        if (hasLineOfSight != currentLineOfSight)
        {
            hasLineOfSight = currentLineOfSight;
            
            if (hasLineOfSight)
            {
                OnTargetInSight?.Invoke(currentTarget);
                LogManager.Log(LogCategory.Enemy, $"타겟 시야 확보: {currentTarget?.name}", this);
            }
            else
            {
                OnTargetOutOfSight?.Invoke(currentTarget);
                LogManager.Log(LogCategory.Enemy, $"타겟 시야 상실: {currentTarget?.name}", this);
            }
        }
    }
    
    /// <summary>
    /// 시야각 내 장애물 탐지
    /// </summary>
    private void DetectObstaclesInFieldOfView()
    {
        if (eyes == null) return;
        
        float angleStep = fieldOfViewAngle / visionRayCount;
        
        for (int i = 0; i < visionRayCount; i++)
        {
            float angle = -fieldOfViewAngle * 0.5f + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            
            if (Physics.Raycast(eyes.position, direction, out RaycastHit hit, detectionRange, obstacleLayer))
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
        
        // 탐지 범위 표시
        if (showDetectionRange)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
        
        // 시야각 표시
        if (showVisionCone)
        {
            Gizmos.color = hasLineOfSight ? Color.green : Color.yellow;
            
            int segments = 16;
            float angleStep = fieldOfViewAngle / segments;
            
            Vector3 previousPoint = transform.position;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = -fieldOfViewAngle * 0.5f + angleStep * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                Vector3 point = transform.position + direction * detectionRange;
                
                if (i > 0)
                {
                    Gizmos.DrawLine(previousPoint, point);
                }
                
                previousPoint = point;
            }
            
            // 시야각의 양 끝점을 시작점과 연결
            Vector3 leftPoint = transform.position + Quaternion.Euler(0, -fieldOfViewAngle * 0.5f, 0) * transform.forward * detectionRange;
            Vector3 rightPoint = transform.position + Quaternion.Euler(0, fieldOfViewAngle * 0.5f, 0) * transform.forward * detectionRange;
            
            Gizmos.DrawLine(transform.position, leftPoint);
            Gizmos.DrawLine(transform.position, rightPoint);
        }
        
        // 시야 레이 표시
        if (showVisionRays && eyes != null)
        {
            Gizmos.color = Color.red;
            
            if (currentTarget != null)
            {
                Vector3 direction = (currentTarget.position - eyes.position).normalized;
                float distance = Vector3.Distance(eyes.position, currentTarget.position);
                
                Gizmos.DrawRay(eyes.position, direction * distance);
            }
        }
        
        // 마지막으로 본 위치 표시
        if (lastSeenPosition != Vector3.zero)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(lastSeenPosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastSeenPosition);
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
        
        // 설정값 검증
        if (detectionRange < 1f) detectionRange = 10f;
        if (fieldOfViewAngle < 10f) fieldOfViewAngle = 90f;
        if (fieldOfViewAngle > 360f) fieldOfViewAngle = 360f;
        if (visionCheckInterval < 0.05f) visionCheckInterval = 0.1f;
        if (visionRayCount < 3) visionRayCount = 8;
        if (visionRayCount > 20) visionRayCount = 20;
    }
} 