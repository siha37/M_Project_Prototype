using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using FishNet.Object;

public class EnemyControll : NetworkBehaviour
{
    private AgentUI agentUI;
    [SerializeField] private GameObject target;
    private NavMeshAgent navAgent;
    private EnemyState state;
    private EnemyNetworkSync networkSync;
    
    // AI 상태 관리
    private string currentAIState = "Patrol";
    private bool canShoot = true;
    private bool isReloading = false;
    private bool isFindingPosition = false;
    private Vector3 lastKnownTargetPosition;
    private float lastAttackTime;
    private float pathUpdateInterval = 0.5f;
    private float lastPathUpdateTime;
    private float strafeDirection = 1f; // 회피 기동 방향
    private float strafeDistance = 3f; // 회피 기동 거리
    private float strafeChangeInterval = 2f; // 회피 방향 변경 간격
    private float lastStrafeChangeTime; // 마지막 회피 방향 변경 시간

    [SerializeField] public Transform shotPivot;
    [SerializeField] private Transform shotPoint;
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] private float aimPrecision = 0.1f; // 목표 조준 정밀도 (0~1)
    [SerializeField] private float searchRadius = 5f; // 사격 가능 위치 탐색 반경
    [SerializeField] private float searchAngle = 45f; // 사격 가능 위치 탐색 각도
    [SerializeField] private LayerMask obstacleLayer; // Wall과 Agent 레이어만 체크
    [SerializeField] private float minDistanceToTarget = 2f; // 최소 거리

    public override void OnStartServer()
    {
        InitializeComponents();
        LogManager.Log(LogCategory.Enemy, $"{gameObject.name} 서버에서 EnemyControll 시작", this);
    }

    public override void OnStartClient()
    {
        InitializeComponents();
        
        // 클라이언트에서는 AI 로직 비활성화
        if (!IsServer)
        {
            enabled = false;
            LogManager.Log(LogCategory.Enemy, $"{gameObject.name} 클라이언트에서 EnemyControll 비활성화", this);
        }
    }

    private void InitializeComponents()
    {
        // 컴포넌트 초기화
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = false;
        }

        agentUI = GetComponent<AgentUI>();
        state = GetComponent<EnemyState>();
        networkSync = GetComponent<EnemyNetworkSync>();

        if (navAgent == null)
        {
            LogManager.LogError(LogCategory.Enemy, $"{gameObject.name} NavMeshAgent 컴포넌트가 없습니다.", this);
            return;
        }

        if (state == null)
        {
            LogManager.LogError(LogCategory.Enemy, $"{gameObject.name} EnemyState 컴포넌트가 없습니다.", this);
            return;
        }

        if (networkSync == null)
        {
            LogManager.LogError(LogCategory.Enemy, $"{gameObject.name} EnemyNetworkSync 컴포넌트가 없습니다.", this);
            return;
        }

        // Wall과 Agent 레이어 마스크 설정
        obstacleLayer = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("Agent"));
    }

    public void Init(GameObject target, Vector2 startPos)
    {
        if (!IsServer) return; // 서버에서만 초기화

        if (!target)
        {
            LogManager.LogError(LogCategory.Enemy, $"{gameObject.name} 초기화할 타겟이 없습니다.", this);
            return;
        }

        this.target = target;
        lastKnownTargetPosition = target.transform.position;

        // NavMeshAgent 초기 설정
        if (navAgent != null)
        {
            navAgent.destination = startPos;
            navAgent.speed = AgentState.speed;
            navAgent.stoppingDistance = EnemyState.targetDistance;
            navAgent.updateRotation = false;
        }

        // 네트워크 동기화 초기화
        if (networkSync != null)
        {
            NetworkObject targetNetworkObject = target.GetComponent<NetworkObject>();
            networkSync.RequestUpdateAIState("Patrol", startPos, targetNetworkObject);
        }

        LogManager.Log(LogCategory.Enemy, $"{gameObject.name} 서버에서 초기화 완료 - 타겟: {target.name}", this);
    }

    private void Update()
    {
        if (!IsServer) return; // 서버에서만 실행

        if (!target || state.IsDead)
        {
            return;
        }

        // 경로 업데이트 간격 체크
        if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
        {
            UpdatePath();
            lastPathUpdateTime = Time.time;
        }

        // 회피 방향 변경 체크
        if (Time.time - lastStrafeChangeTime >= strafeChangeInterval)
        {
            strafeDirection *= -1f;
            lastStrafeChangeTime = Time.time;
            
            // 네트워크 동기화
            if (networkSync != null)
            {
                networkSync.RequestUpdateStrafeState(true, strafeDirection);
            }
        }

        // 목표와의 거리 체크
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        
        if (distanceToTarget <= EnemyState.targetDistance)
        {
            // 시야 확인
            if (CheckLineOfSight())
            {
                isFindingPosition = false;
                UpdateAIState("Attack");
                LookAtTarget();
                
                // 공격 가능 상태이고 재장전 중이 아닐 때
                if (canShoot && !isReloading)
                {
                    if (state.bulletCurrentCount > 0)
                    {
                        Attack();
                    }
                    else
                    {
                        StartCoroutine(Reload());
                    }
                }
            }
            else if (!isFindingPosition)
            {
                UpdateAIState("Chase");
                LogManager.Log(LogCategory.Enemy, $"{gameObject.name} 시야가 막혀 사격 위치 탐색 시작", this);
                StartCoroutine(FindShootingPosition());
            }
        }
        else if (distanceToTarget < minDistanceToTarget)
        {
            // 너무 가까이 있을 경우 후퇴
            UpdateAIState("Return");
            Vector3 retreatDirection = (transform.position - target.transform.position).normalized;
            Vector3 retreatPosition = transform.position + retreatDirection * minDistanceToTarget;
            navAgent.SetDestination(retreatPosition);
            
            // 네트워크 동기화
            if (networkSync != null)
            {
                NetworkObject targetNetworkObject = target.GetComponent<NetworkObject>();
                networkSync.RequestUpdateAIState("Return", retreatPosition, targetNetworkObject);
            }
        }
        else
        {
            UpdateAIState("Chase");
        }
    }

    private void UpdateAIState(string newState)
    {
        if (currentAIState != newState)
        {
            currentAIState = newState;
            
            // 네트워크 동기화
            if (networkSync != null && target != null)
            {
                NetworkObject targetNetworkObject = target.GetComponent<NetworkObject>();
                networkSync.RequestUpdateAIState(newState, target.transform.position, targetNetworkObject);
            }
        }
    }

    private void UpdatePath()
    {
        if (target != null && !isFindingPosition)
        {
            lastKnownTargetPosition = target.transform.position;
            
            // 타겟을 향한 방향 벡터
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            
            // 회피 기동을 위한 수직 방향 벡터
            Vector3 perpendicularDirection = new Vector3(-directionToTarget.y, directionToTarget.x, 0);
            
            // 회피 기동 위치 계산
            Vector3 strafePosition = target.transform.position + perpendicularDirection * strafeDistance * strafeDirection;
            
            // NavMesh 위의 유효한 위치 찾기
            NavMeshHit hit;
            if (NavMesh.SamplePosition(strafePosition, out hit, strafeDistance, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
                
                // 네트워크 동기화
                if (networkSync != null)
                {
                    networkSync.RequestUpdateTargetPosition(hit.position);
                }
            }
            else
            {
                // 유효한 위치를 찾지 못한 경우 타겟 위치로 이동
                navAgent.SetDestination(lastKnownTargetPosition);
                
                // 네트워크 동기화
                if (networkSync != null)
                {
                    networkSync.RequestUpdateTargetPosition(lastKnownTargetPosition);
                }
            }
        }
    }

    private IEnumerator FindShootingPosition()
    {
        isFindingPosition = true;
        Vector3 bestPosition = transform.position;
        float bestAngle = float.MaxValue;
        int attempts = 0;
        const int maxAttempts = 10;

        while (attempts < maxAttempts)
        {
            // NavMeshAgent가 비활성화되어 있다면 코루틴 종료
            if (!navAgent.enabled || state.IsDead)
            {
                isFindingPosition = false;
                yield break;
            }

            for (float angle = -searchAngle; angle <= searchAngle; angle += 10f)
            {
                Vector3 direction = Quaternion.Euler(0, 0, angle) * (target.transform.position - transform.position).normalized;
                Vector3 testPosition = target.transform.position + direction * searchRadius;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(testPosition, out hit, searchRadius, NavMesh.AllAreas))
                {
                    Vector3 directionToTarget = target.transform.position - hit.position;
                    float distance = Vector3.Distance(hit.position, target.transform.position);
                    
                    // Wall과 Agent 레이어만 체크
                    RaycastHit2D rayHit = Physics2D.Raycast(hit.position, directionToTarget, distance, obstacleLayer);
                    
                    if (rayHit.collider != null)
                    {
                        // Agent 레이어인 경우 자기 자신은 무시
                        if (rayHit.collider.gameObject.layer == LayerMask.NameToLayer("Agent") && 
                            rayHit.collider.gameObject == gameObject)
                        {
                            float currentAngle = Vector3.Angle(directionToTarget, transform.position - target.transform.position);
                            if (currentAngle < bestAngle)
                            {
                                bestAngle = currentAngle;
                                bestPosition = hit.position;
                            }
                            continue;
                        }

                        if (rayHit.collider.gameObject == target)
                        {
                            float currentAngle = Vector3.Angle(directionToTarget, transform.position - target.transform.position);
                            if (currentAngle < bestAngle)
                            {
                                bestAngle = currentAngle;
                                bestPosition = hit.position;
                            }
                        }
                    }
                }
                yield return null;
            }

            // 가장 좋은 위치로 이동
            if (bestPosition != transform.position)
            {
                // NavMeshAgent가 비활성화되어 있다면 코루틴 종료
                if (!navAgent.enabled || state.IsDead)
                {
                    isFindingPosition = false;
                    yield break;
                }

                navAgent.SetDestination(bestPosition);
                
                // 네트워크 동기화
                if (networkSync != null)
                {
                    networkSync.RequestUpdateTargetPosition(bestPosition);
                }

                float moveTimeout = 5f; // 이동 타임아웃
                float startTime = Time.time;

                while (Vector3.Distance(transform.position, bestPosition) > 0.5f)
                {
                    // NavMeshAgent가 비활성화되어 있다면 코루틴 종료
                    if (!navAgent.enabled || state.IsDead)
                    {
                        isFindingPosition = false;
                        yield break;
                    }

                    if (Time.time - startTime > moveTimeout)
                    {
                        LogManager.LogWarning(LogCategory.Enemy, $"{gameObject.name} 위치 이동 타임아웃", this);
                        break;
                    }
                    yield return null;
                }
                break;
            }

            attempts++;
            yield return new WaitForSeconds(0.5f);
        }

        isFindingPosition = false;
                            LogManager.Log(LogCategory.Enemy, $"{gameObject.name} 사격 위치 탐색 완료", this);
    }

    private bool CheckLineOfSight()
    {
        if (target == null) return false;

        Vector3 directionToTarget = target.transform.position - transform.position;
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        // Wall과 Agent 레이어만 체크
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, distance, obstacleLayer);
        
        if (hit.collider != null)
        {
            // Agent 레이어인 경우 자기 자신은 무시
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Agent") && 
                hit.collider.gameObject == gameObject)
            {
                return true;
            }
            return hit.collider.gameObject == target;
        }
        return false;
    }

    private void LookAtTarget()
    {
        if (target == null) return;

        Vector2 direction = target.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 정밀도에 따른 각도 조정
        float randomOffset = Random.Range(-aimPrecision * 10f, aimPrecision * 10f);
        angle += randomOffset;
        
        shotPivot.rotation = Quaternion.Euler(0, 0, angle);
        
        // 네트워크 동기화
        if (networkSync != null)
        {
            networkSync.RequestUpdateLookAngle(angle);
        }
    }

    private void Attack()
    {
        if (Time.time - lastAttackTime < AgentState.bulletDelay) return;

        // 네트워크 동기화된 발사 처리
        if (networkSync != null)
        {
            float angle = shotPivot.rotation.eulerAngles.z;
            networkSync.RequestShoot(angle, shotPoint.position);
            lastAttackTime = Time.time;
        }

        StartCoroutine(ShootDelay());
    }

    private IEnumerator ShootDelay()
    {
        canShoot = false;
        yield return new WaitForSeconds(AgentState.bulletDelay);
        canShoot = true;
    }

    private IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        
        // 네트워크 동기화된 재장전 처리
        if (networkSync != null)
        {
            networkSync.RequestEnemyReload();
        }
        
        // 서버에서 실제 재장전 처리
        float reloadTimer = 0f;
        while (reloadTimer < AgentState.bulletReloadTime)
        {
            reloadTimer += Time.deltaTime;
            yield return null;
        }

        isReloading = false;
                    LogManager.Log(LogCategory.Enemy, $"{gameObject.name} 서버에서 재장전 완료", this);
    }

    // 네트워크 동기화에서 호출할 공개 메서드들
    public void SetAIState(string newState)
    {
        currentAIState = newState;
    }

    public void SetTargetPosition(Vector3 position)
    {
        lastKnownTargetPosition = position;
    }

    public void SetStrafeState(bool isStrafing, float direction)
    {
        strafeDirection = direction;
    }

    public bool CanShoot => canShoot;
    public bool IsReloading => isReloading;
    public string CurrentAIState => currentAIState;

    private void OnDrawGizmosSelected()
    {
        // 디버그용 시각화
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, EnemyState.targetDistance);
        }
    }
}
