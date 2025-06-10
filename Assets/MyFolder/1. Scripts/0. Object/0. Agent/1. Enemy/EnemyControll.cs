using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyControll : MonoBehaviour
{
    private AgentUI agentUI;
    [SerializeField] private GameObject target;
    private NavMeshAgent navAgent;
    private EnemyState state;
    private bool canShoot = true;
    private bool isReloading = false;
    private bool isFindingPosition = false;
    private Vector3 lastKnownTargetPosition;
    private float lastAttackTime;
    private float pathUpdateInterval = 0.5f;
    private float lastPathUpdateTime;

    [SerializeField] private Transform shotPivot;
    [SerializeField] private Transform shotPoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float aimPrecision = 0.1f; // 목표 조준 정밀도 (0~1)
    [SerializeField] private float searchRadius = 5f; // 사격 가능 위치 탐색 반경
    [SerializeField] private float searchAngle = 45f; // 사격 가능 위치 탐색 각도
    [SerializeField] private LayerMask obstacleLayer; // Wall과 Agent 레이어만 체크
    [SerializeField] private float minDistanceToTarget = 2f; // 최소 거리

    private void Awake()
    {
        // 컴포넌트 초기화
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;

        agentUI = GetComponent<AgentUI>();
        state = GetComponent<EnemyState>();

        if (navAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent 컴포넌트가 없습니다.");
            enabled = false;
            return;
        }

        if (agentUI == null)
        {
            Debug.LogWarning($"[{gameObject.name}] AgentUI 컴포넌트가 없습니다.");
        }

        if (state == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyState 컴포넌트가 없습니다.");
            enabled = false;
            return;
        }

        // Wall과 Agent 레이어 마스크 설정
        obstacleLayer = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("Agent"));
    }

    public void Init(GameObject target)
    {
        if (target == null)
        {
            Debug.LogError($"[{gameObject.name}] 초기화할 타겟이 없습니다.");
            return;
        }

        this.target = target;
        lastKnownTargetPosition = target.transform.position;

        // NavMeshAgent 초기 설정
        navAgent.speed = AgentState.speed;
        navAgent.stoppingDistance = EnemyState.targetDistance;
        navAgent.updateRotation = false; // 회전은 직접 제어

        Debug.Log($"[{gameObject.name}] 초기화 완료 - 타겟: {target.name}");
    }

    private void Update()
    {
        if (target == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 타겟이 없습니다.");
            return;
        }

        // 경로 업데이트 간격 체크
        if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
        {
            UpdatePath();
            lastPathUpdateTime = Time.time;
        }

        // 목표와의 거리 체크
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        
        if (distanceToTarget <= EnemyState.targetDistance)
        {
            // 시야 확인
            if (CheckLineOfSight())
            {
                isFindingPosition = false;
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
                Debug.Log($"[{gameObject.name}] 시야가 막혀 사격 위치 탐색 시작");
                StartCoroutine(FindShootingPosition());
            }
        }
        else if (distanceToTarget < minDistanceToTarget)
        {
            // 너무 가까이 있을 경우 후퇴
            Vector3 retreatDirection = (transform.position - target.transform.position).normalized;
            Vector3 retreatPosition = transform.position + retreatDirection * minDistanceToTarget;
            navAgent.SetDestination(retreatPosition);
        }
    }

    private void UpdatePath()
    {
        if (target != null && !isFindingPosition)
        {
            lastKnownTargetPosition = target.transform.position;
            navAgent.SetDestination(lastKnownTargetPosition);
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
                navAgent.SetDestination(bestPosition);
                float moveTimeout = 5f; // 이동 타임아웃
                float startTime = Time.time;

                while (Vector3.Distance(transform.position, bestPosition) > 0.5f)
                {
                    if (Time.time - startTime > moveTimeout)
                    {
                        Debug.LogWarning($"[{gameObject.name}] 위치 이동 타임아웃");
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
        Debug.Log($"[{gameObject.name}] 사격 위치 탐색 완료");
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
    }

    private void Attack()
    {
        if (Time.time - lastAttackTime < AgentState.bulletDelay) return;

        // 총알 생성 및 발사
        GameObject bullet = Instantiate(bulletPrefab, shotPoint.position, shotPoint.rotation);
        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Init(AgentState.bulletSpeed, AgentState.bulletDamage, AgentState.bulletRange, gameObject);
            state.UpdateBulletCount(-1);
            lastAttackTime = Time.time;

            if (state.bulletCurrentCount <= 0)
            {
                StartCoroutine(Reload());
            }
            else
            {
                StartCoroutine(ShootDelay());
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Projectile 컴포넌트를 찾을 수 없습니다.");
        }
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
        agentUI.StartReloadUI();
        
        float reloadTimer = 0f;
        while (reloadTimer < AgentState.bulletReloadTime)
        {
            reloadTimer += Time.deltaTime;
            float progress = reloadTimer / AgentState.bulletReloadTime;
            agentUI.UpdateReloadProgress(progress);
            yield return null;
        }

        state.UpdateBulletCount(AgentState.bulletMaxCount);
        agentUI.EndReloadUI();
        isReloading = false;
        Debug.Log($"[{gameObject.name}] 재장전 완료");
    }

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
