using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 이동 컴포넌트
/// NavMesh 기반 이동, 회피 기동, 속도 제어 등을 담당
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("=== 이동 설정 ===")]
    [SerializeField] private float defaultSpeed = 3f;
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("=== 회피 기동 설정 ===")]
    [SerializeField] private float strafeDistance = 3f;
    [SerializeField] private float strafeSpeedMultiplier = 1.2f;
    [SerializeField] private float strafeChangeInterval = 2f;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showPath = true;
    [SerializeField] private bool showVelocity = true;
    
    // NavMesh Agent
    private NavMeshAgent agent;
    
    // 현재 이동 상태
    private Vector3 currentDestination;
    private bool isMoving;
    private bool isStrafing;
    private float strafeDirection = 1f;
    private float lastStrafeChangeTime;
    
    // 성능 최적화
    private float lastPathUpdateTime;
    private float pathUpdateInterval = 0.5f;
    
    // 이벤트
    public System.Action<Vector3> OnDestinationReached;
    public System.Action<Vector3> OnMovementStarted;
    public System.Action OnMovementStopped;
    
    // ========== Properties ==========
    
    /// <summary>
    /// 현재 이동 중인지 여부
    /// </summary>
    public bool IsMoving => isMoving;
    
    /// <summary>
    /// 회피 기동 중인지 여부
    /// </summary>
    public bool IsStrafing => isStrafing;
    
    /// <summary>
    /// 현재 목적지
    /// </summary>
    public Vector3 CurrentDestination => currentDestination;
    
    /// <summary>
    /// 목적지에 도달했는지 여부
    /// </summary>
    public bool HasReachedDestination => agent && !agent.pathPending && agent.hasPath && 
                                       agent.remainingDistance <= agent.stoppingDistance;
    
    // ========== Unity Lifecycle ==========
    
    private void Awake()
    {
        // NavMesh Agent 가져오기
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            LogManager.LogError(LogCategory.Enemy, "NavMeshAgent가 없습니다!", this);
            return;
        }
        
        // 기본 설정
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        transform.rotation = Quaternion.identity;
        
        LogManager.Log(LogCategory.Enemy, "EnemyMovement 컴포넌트 초기화 완료", this);
    }
    
    private void Start()
    {
        // NavMesh Agent 초기화
        if (agent == null) return;
        
        // NavMesh Agent 활성화
        agent.enabled = true;
        
        LogManager.Log(LogCategory.Enemy, "EnemyMovement 초기화 완료", this);
    }
    
    private void Update()
    {
        // 이동 업데이트
        if (!agent) return;
        
        // 이동 상태 업데이트
        UpdateMovementState();
        
        // 회피 기동 업데이트
        UpdateStrafeMovement();
    }
    
    // ========== Public Methods ==========
    
    /// <summary>
    /// 목적지로 이동
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (!agent) return;
        
        // NavMesh 위의 유효한 위치인지 확인
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            Vector3 validDestination = hit.position;
            
            // 경로 설정
            agent.SetDestination(validDestination);
            currentDestination = validDestination;
            isMoving = true;
            
            LogManager.Log(LogCategory.Enemy, $"이동 시작: {validDestination}", this);
            
            // 이벤트 발생
            OnMovementStarted?.Invoke(validDestination);
        }
        else
        {
            LogManager.LogWarning(LogCategory.Enemy, $"유효하지 않은 목적지: {destination}", this);
        }
    }
    
    /// <summary>
    /// 이동 정지
    /// </summary>
    public void Stop()
    {
        if (agent == null) return;
        
        agent.isStopped = true;
        isMoving = false;
        isStrafing = false;
        
        LogManager.Log(LogCategory.Enemy, "이동 정지", this);
        
        // 이벤트 발생
        OnMovementStopped?.Invoke();
    }
    
    /// <summary>
    /// 이동 재개
    /// </summary>
    public void Resume()
    {
        if (agent == null) return;
        
        agent.isStopped = false;
        isMoving = true;
        
        LogManager.Log(LogCategory.Enemy, "이동 재개", this);
    }
    
    /// <summary>
    /// 속도 설정
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (agent)
        {
            agent.speed = speed;
            LogManager.Log(LogCategory.Enemy, $"속도 변경: {speed}", this);
        }
    }
    
    /// <summary>
    /// 정지 거리 설정
    /// </summary>
    public void SetStoppingDistance(float distance)
    {
        if (agent)
        {
            agent.stoppingDistance = distance;
        }
    }

    /// <summary>
    /// 회전 속도 설정
    /// </summary>
    public void SetRotationSpeed(float rotation)
    {
        rotationSpeed = rotation;
    }

    public void SetStrafeDistance(float distance)
    {
        this.strafeDistance = distance;
    }

    public void SetStrafeSpeedMultiplier(float _strafeSpeedMultiplier)
    {
        this.strafeSpeedMultiplier = _strafeSpeedMultiplier;
    }

    public void SetStrafeChangeInterval(float _strafeChangeInterval)
    {
        this.strafeChangeInterval = _strafeChangeInterval;
    }
    

    public void SetAgent()
    {
        agent.speed = defaultSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = rotationSpeed;
    }
    
    /// <summary>
    /// 타겟 주변 회피 기동
    /// </summary>
    public void StrafeAroundTarget(Vector3 targetPosition)
    {
        if (!agent) return;
        
        // 회피 기동 방향 변경
        if (Time.time - lastStrafeChangeTime >= strafeChangeInterval)
        {
            strafeDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
            lastStrafeChangeTime = Time.time;
        }
        
        // 타겟으로부터의 방향 계산
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        
        // 회피 기동 방향 (타겟을 중심으로 회전)
        Vector3 strafeDirectionVector = Vector3.Cross(directionToTarget, Vector3.up) * strafeDirection;
        
        // 회피 기동 목적지 계산
        Vector3 strafeDestination = targetPosition + strafeDirectionVector * strafeDistance;
        
        // NavMesh 위의 유효한 위치 찾기
        if (NavMesh.SamplePosition(strafeDestination, out NavMeshHit hit, strafeDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            currentDestination = hit.position;
            isMoving = true;
            isStrafing = true;
            
            LogManager.Log(LogCategory.Enemy, $"회피 기동: {hit.position}", this);
        }
    }
    
    /// <summary>
    /// 회피 기동 정지
    /// </summary>
    public void StopStrafe()
    {
        isStrafing = false;
        LogManager.Log(LogCategory.Enemy, "회피 기동 정지", this);
    }
    
    /// <summary>
    /// 현재 속도 가져오기
    /// </summary>
    public float GetCurrentSpeed()
    {
        return agent != null ? agent.velocity.magnitude : 0f;
    }
    
    /// <summary>
    /// 목적지까지의 거리
    /// </summary>
    public float GetDistanceToDestination()
    {
        return agent != null ? agent.remainingDistance : float.MaxValue;
    }
    
    /// <summary>
    /// 경로가 유효한지 확인
    /// </summary>
    public bool HasValidPath()
    {
        return agent && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 이동 상태 업데이트
    /// </summary>
    private void UpdateMovementState()
    {
        if (!agent) return;
        
        // 목적지 도달 확인
        if (isMoving && HasReachedDestination)
        {
            isMoving = false;
            isStrafing = false;
            
            LogManager.Log(LogCategory.Enemy, $"목적지 도달: {currentDestination}", this);
            
            // 이벤트 발생
            OnDestinationReached?.Invoke(currentDestination);
        }
        
        // 경로 업데이트 (성능 최적화)
        if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
        {
            // 경로가 유효하지 않으면 재계산
            if (isMoving && !HasValidPath())
            {
                LogManager.LogWarning(LogCategory.Enemy, "경로 재계산 필요", this);
                agent.SetDestination(currentDestination);
            }
            
            lastPathUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// 회피 기동 업데이트
    /// </summary>
    private void UpdateStrafeMovement()
    {
        if (!isStrafing) return;
        
        // 회피 기동 중에는 속도 증가
        if (agent && !Mathf.Approximately(agent.speed, defaultSpeed * strafeSpeedMultiplier))
        {
            agent.speed = defaultSpeed * strafeSpeedMultiplier;
        }
    }
    
    // ========== Gizmos ==========
    
    private void OnDrawGizmos()
    {
        if (!showPath || agent == null) return;
        
        // 현재 경로 표시
        if (agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] path = agent.path.corners;
            
            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
            
            // 목적지 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentDestination, 0.5f);
        }
        
        // 속도 벡터 표시
        if (showVelocity && agent != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, agent.velocity);
        }
        
        // 정지 거리 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
    
    // ========== Validation ==========
    
    private void OnValidate()
    {
        // NavMesh Agent 자동 할당
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        // 설정값 검증
        if (defaultSpeed < 0) defaultSpeed = 3f;
        if (stoppingDistance < 0) stoppingDistance = 1f;
        if (strafeDistance < 0) strafeDistance = 3f;
        if (strafeSpeedMultiplier < 0) strafeSpeedMultiplier = 1.2f;
    }
} 