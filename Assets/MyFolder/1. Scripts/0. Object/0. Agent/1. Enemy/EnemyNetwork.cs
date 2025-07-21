using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkObject))]
public class EnemyNetwork : NetworkBehaviour
{
    [SerializeField] private EnemyControll enemyControll;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private EnemyState enemyState;
    
    [Header("Network Settings")]
    [SerializeField] private float syncInterval = 0.1f; // 동기화 주기 (초)
    [SerializeField] private float positionThreshold = 0.1f; // 위치 변화 임계값
    [SerializeField] private float rotationThreshold = 5f; // 회전 변화 임계값 (도)
    
    private float lastSyncTime;
    private Vector3 lastSyncedPosition;
    private Quaternion lastSyncedRotation;
    private bool isInitialized = false;
    
    // 네트워크 동기화를 위한 상태 변수들
    private float lastSyncedHp;
    private float lastSyncedBulletCount;
    private bool lastSyncedIsDead;
    private bool lastSyncedIsReloading;
    private Vector3 lastShotPosition;
    private Quaternion lastShotRotation;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeComponents();
        
        if (enemyControll != null)
            enemyControll.enabled = true;
        if (navAgent != null)
            navAgent.enabled = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        InitializeComponents();
        
        if (!IsServer)
        {
            // 클라이언트에서는 AI 로직을 비활성화하되, NavMeshAgent는 유지
            if (enemyControll != null)
                enemyControll.enabled = false;
            
            // NavMeshAgent는 비활성화하지 않고 대신 업데이트만 중단
            if (navAgent != null)
            {
                navAgent.updatePosition = false;
                navAgent.updateRotation = false;
            }
        }
    }

    private void InitializeComponents()
    {
        if (isInitialized) return;
        
        if (enemyControll == null)
            enemyControll = GetComponent<EnemyControll>();
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();
        if (enemyState == null)
            enemyState = GetComponent<EnemyState>();
            
        if (enemyControll == null)
            Debug.LogError($"[{gameObject.name}] EnemyControll 컴포넌트를 찾을 수 없습니다.");
        if (navAgent == null)
            Debug.LogError($"[{gameObject.name}] NavMeshAgent 컴포넌트를 찾을 수 없습니다.");
        if (enemyState == null)
            Debug.LogError($"[{gameObject.name}] EnemyState 컴포넌트를 찾을 수 없습니다.");
            
        isInitialized = true;
    }

    private void Update()
    {
        if (IsServer && isInitialized)
        {
            // 동기화 주기 체크
            if (Time.time - lastSyncTime >= syncInterval)
            {
                SyncAllStates();
                lastSyncTime = Time.time;
            }
        }
    }

    private void SyncAllStates()
    {
        // 위치/회전 동기화
        float positionDelta = Vector3.Distance(transform.position, lastSyncedPosition);
        float rotationDelta = Quaternion.Angle(transform.rotation, lastSyncedRotation);
        
        if (positionDelta > positionThreshold || rotationDelta > rotationThreshold)
        {
            SyncTransform(transform.position, transform.rotation);
            lastSyncedPosition = transform.position;
            lastSyncedRotation = transform.rotation;
        }

        // HP 동기화
        if (Mathf.Abs(enemyState.currentHp - lastSyncedHp) > 0.1f)
        {
            SyncHealth(enemyState.currentHp);
            lastSyncedHp = enemyState.currentHp;
        }

        // 탄약 수 동기화
        if (Mathf.Abs(enemyState.bulletCurrentCount - lastSyncedBulletCount) > 0.1f)
        {
            SyncBulletCount(enemyState.bulletCurrentCount);
            lastSyncedBulletCount = enemyState.bulletCurrentCount;
        }

        // 사망 상태 동기화
        if (enemyState.IsDead != lastSyncedIsDead)
        {
            SyncDeathState(enemyState.IsDead);
            lastSyncedIsDead = enemyState.IsDead;
        }

        // 재장전 상태 동기화 (EnemyControll에서 확인)
        bool isReloading = false;
        if (enemyControll != null)
        {
            // EnemyControll의 isReloading 필드를 리플렉션으로 확인
            var reloadingField = enemyControll.GetType().GetField("isReloading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (reloadingField != null)
            {
                isReloading = (bool)reloadingField.GetValue(enemyControll);
            }
        }
        
        if (isReloading != lastSyncedIsReloading)
        {
            SyncReloadState(isReloading);
            lastSyncedIsReloading = isReloading;
        }
    }

    // 슈팅 이벤트를 위한 public 메서드 (EnemyControll에서 호출)
    public void OnShoot(Vector3 shotPosition, Quaternion shotRotation)
    {
        if (IsServer)
        {
            SyncShoot(shotPosition, shotRotation);
        }
    }

    [ObserversRpc]
    private void SyncTransform(Vector3 position, Quaternion rotation)
    {
        if (IsServer)
            return;
            
        // 클라이언트에서 부드러운 보간 적용
        if (navAgent != null)
        {
            navAgent.Warp(position);
            transform.rotation = rotation;
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }

    [ObserversRpc]
    private void SyncHealth(float health)
    {
        if (IsServer)
            return;
            
        if (enemyState != null)
        {
            enemyState.currentHp = health;
            // UI 업데이트
            var agentUI = GetComponent<AgentUI>();
            if (agentUI != null)
            {
                agentUI.UpdateHealthUI(health, State.maxHp);
            }
        }
    }

    [ObserversRpc]
    private void SyncBulletCount(float bulletCount)
    {
        if (IsServer)
            return;
            
        if (enemyState != null)
        {
            enemyState.bulletCurrentCount = bulletCount;
            // UI 업데이트
            var agentUI = GetComponent<AgentUI>();
            if (agentUI != null)
            {
                agentUI.UpdateAmmoUI((int)bulletCount, (int)AgentState.bulletMaxCount);
            }
        }
    }

    [ObserversRpc]
    private void SyncDeathState(bool isDead)
    {
        if (IsServer)
            return;
            
        if (enemyState != null && isDead)
        {
            // 클라이언트에서도 사망 처리
            if (enemyControll != null)
                enemyControll.enabled = false;
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.enabled = false;
            }
            
            // 사망 시퀀스 시작 (public 메서드 사용)
            enemyState.StartCoroutine(enemyState.DeathSequence());
        }
    }

    [ObserversRpc]
    private void SyncReloadState(bool isReloading)
    {
        if (IsServer)
            return;
            
        // 클라이언트에서 재장전 UI 업데이트
        var agentUI = GetComponent<AgentUI>();
        if (agentUI != null)
        {
            if (isReloading)
            {
                agentUI.StartReloadUI();
            }
            else
            {
                agentUI.EndReloadUI();
            }
        }
    }

    [ObserversRpc]
    private void SyncShoot(Vector3 shotPosition, Quaternion shotRotation)
    {
        if (IsServer)
            return;
            
        // 클라이언트에서 슈팅 효과 재생
        if (enemyControll != null)
        {
            // 슈팅 이펙트나 사운드 재생
            Debug.Log($"[{gameObject.name}] 클라이언트에서 슈팅 이펙트 재생");
            
            // 실제 총알은 서버에서만 생성되므로 클라이언트에서는 이펙트만 재생
            // 필요시 슈팅 이펙트 프리팹을 인스턴스화
        }
    }

    // 디버깅을 위한 Gizmos
    private void OnDrawGizmosSelected()
    {
        if (IsServer)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, positionThreshold);
            
            // HP 상태 표시
            if (enemyState != null)
            {
                Gizmos.color = enemyState.IsDead ? Color.red : Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            }
        }
    }
}
