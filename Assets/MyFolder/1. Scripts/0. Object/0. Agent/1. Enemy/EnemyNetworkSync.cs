using UnityEngine;
using FishNet.Object;
using System.Collections;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine.AI;

public class EnemyNetworkSync : AgentNetworkSync
{
    // 적 전용 동기화
    private readonly SyncVar<Vector3> syncTargetPosition = new SyncVar<Vector3>();
    private readonly SyncVar<bool> syncIsChasing = new SyncVar<bool>();
    private readonly SyncVar<string> syncAIState = new SyncVar<string>(); // "Patrol", "Chase", "Attack", "Return"
    private readonly SyncVar<NetworkObject> syncCurrentTarget = new SyncVar<NetworkObject>();
    private readonly SyncVar<float> syncAttackCooldown = new SyncVar<float>();
    private readonly SyncVar<bool> syncIsStrafing = new SyncVar<bool>();
    private readonly SyncVar<float> syncStrafeDirection = new SyncVar<float>();
    private readonly SyncVar<Vector3> syncLastKnownTargetPosition = new SyncVar<Vector3>();
    
    // 적 전용 컴포넌트
    private EnemyState enemyState;
    private EnemyControll enemyControll;
    private NavMeshAgent navMeshAgent;
    
    // 적 전용 이벤트
    public delegate void OnEnemyStateChangedHandler(string newState);
    public delegate void OnEnemyTargetChangedHandler(NetworkObject target);
    public event OnEnemyStateChangedHandler OnEnemyStateChanged;
    public event OnEnemyTargetChangedHandler OnEnemyTargetChanged;
    
    protected override void InitializeComponents()
    {
        base.InitializeComponents();
        enemyState = GetComponent<EnemyState>();
        enemyControll = GetComponent<EnemyControll>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        
        if (enemyState == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyState 컴포넌트를 찾을 수 없습니다.");
        }
        
        if (enemyControll == null)
        {
            Debug.LogError($"[{gameObject.name}] EnemyControll 컴포넌트를 찾을 수 없습니다.");
        }
        
        if (navMeshAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent 컴포넌트를 찾을 수 없습니다.");
        }
    }
    
    protected override void RegisterSyncVarCallbacks()
    {
        base.RegisterSyncVarCallbacks();
        syncTargetPosition.OnChange += OnTargetPositionChanged;
        syncIsChasing.OnChange += OnIsChasingChanged;
        syncAIState.OnChange += OnAIStateChanged;
        syncCurrentTarget.OnChange += OnCurrentTargetChanged;
        syncAttackCooldown.OnChange += OnAttackCooldownChanged;
        syncIsStrafing.OnChange += OnIsStrafingChanged;
        syncStrafeDirection.OnChange += OnStrafeDirectionChanged;
    }
    
    protected override void InitializeSyncVars()
    {
        base.InitializeSyncVars();
        syncTargetPosition.Value = transform.position;
        syncIsChasing.Value = false;
        syncAIState.Value = "Patrol";
        syncCurrentTarget.Value = null;
        syncAttackCooldown.Value = 0f;
        syncIsStrafing.Value = false;
        syncStrafeDirection.Value = 1f;
        syncLastKnownTargetPosition.Value = transform.position;
    }
    
    // 적 전용 AI 상태 동기화
    [ServerRpc]
    public void RequestUpdateAIState(string newState, Vector3 targetPos, NetworkObject target = null)
    {
        Debug.Log($"[{gameObject.name}] 서버에서 AI 상태 업데이트: {syncAIState.Value} -> {newState}");
        
        syncAIState.Value = newState;
        syncTargetPosition.Value = targetPos;
        syncCurrentTarget.Value = target;
        syncIsChasing.Value = (newState == "Chase");
        
        // 모든 클라이언트에 상태 변경 알림
        OnAIStateChanged(newState, targetPos);
    }
    
    // 적 전용 타겟 위치 업데이트
    [ServerRpc]
    public void RequestUpdateTargetPosition(Vector3 targetPos)
    {
        syncTargetPosition.Value = targetPos;
        syncLastKnownTargetPosition.Value = targetPos;
    }
    
    // 적 전용 회피 기동 동기화
    [ServerRpc]
    public void RequestUpdateStrafeState(bool isStrafing, float direction = 1f)
    {
        syncIsStrafing.Value = isStrafing;
        syncStrafeDirection.Value = direction;
    }
    
    // 적 전용 공격 쿨다운 동기화
    [ServerRpc]
    public void RequestUpdateAttackCooldown(float cooldown)
    {
        syncAttackCooldown.Value = cooldown;
    }
    
    // 적 전용 재장전 처리
    [ServerRpc]
    public void RequestEnemyReload()
    {
        if (syncIsReloading.Value) return;
        
        Debug.Log($"[{gameObject.name}] 서버에서 적 재장전 시작");
        RequestSetReloadingState(true);
        StartCoroutine(ServerEnemyReloadProcess());
    }
    
    private IEnumerator ServerEnemyReloadProcess()
    {
        float reloadTimer = 0f;
        
        while (reloadTimer < AgentState.bulletReloadTime)
        {
            reloadTimer += Time.deltaTime;
            float progress = reloadTimer / AgentState.bulletReloadTime;
            OnEnemyReloadProgress(progress);
            yield return null;
        }
        
        // 재장전 완료
        RequestUpdateBulletCount(AgentState.bulletMaxCount);
        RequestSetReloadingState(false);
        OnEnemyReloadComplete();
    }
    
    // 적 전용 이벤트들
    [ObserversRpc]
    private void OnAIStateChanged(string newState, Vector3 targetPos)
    {
        Debug.Log($"[{gameObject.name}] AI 상태 변경: {newState}, 타겟 위치: {targetPos}");
        OnEnemyStateChanged?.Invoke(newState);
    }
    
    // 적 전용 발사 효과
    protected override void OnShootEffect(float angle, Vector3 position)
    {
        // ✅ 적 전용 시각/음향 효과
        // TODO: 실제 시각 효과 구현 (파티클, 사운드, 총구 화염 등)
    }
    
    [ObserversRpc]
    private void OnEnemyReloadProgress(float progress)
    {
        Debug.Log($"[{gameObject.name}] 적 재장전 진행률: {progress * 100:F1}%");
    }
    
    [ObserversRpc]
    private void OnEnemyReloadComplete()
    {
        Debug.Log($"[{gameObject.name}] 적 재장전 완료");
    }
    
    // SyncVar 변경 시 호출되는 메서드들
    private void OnTargetPositionChanged(Vector3 oldValue, Vector3 newValue, bool asServer)
    {
        if (!asServer && enemyControll != null)
        {
            // 클라이언트에서만 위치 동기화 (서버는 Owner이므로 제외)
            enemyControll.SetTargetPosition(newValue);
        }
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 타겟 위치 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    private void OnIsChasingChanged(bool oldValue, bool newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 추적 상태 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    private void OnAIStateChanged(string oldValue, string newValue, bool asServer)
    {
        if (!asServer && enemyControll != null)
        {
            // 클라이언트에서만 AI 상태 동기화 (서버는 Owner이므로 제외)
            enemyControll.SetAIState(newValue);
        }
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] AI 상태 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    private void OnCurrentTargetChanged(NetworkObject oldValue, NetworkObject newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 현재 타겟 동기화: {oldValue?.name ?? "null"} -> {newValue?.name ?? "null"}");
#endif
        OnEnemyTargetChanged?.Invoke(newValue);
    }
    
    private void OnAttackCooldownChanged(float oldValue, float newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 공격 쿨다운 동기화: {oldValue:F2} -> {newValue:F2}");
#endif
    }
    
    private void OnIsStrafingChanged(bool oldValue, bool newValue, bool asServer)
    {
        if (!asServer && enemyControll != null)
        {
            // 클라이언트에서만 회피 기동 상태 동기화 (서버는 Owner이므로 제외)
            enemyControll.SetStrafeState(newValue, syncStrafeDirection.Value);
        }
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 회피 기동 상태 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    private void OnStrafeDirectionChanged(float oldValue, float newValue, bool asServer)
    {
        if (!asServer && enemyControll != null)
        {
            // 클라이언트에서만 회피 방향 동기화 (서버는 Owner이므로 제외)
            enemyControll.SetStrafeState(syncIsStrafing.Value, newValue);
        }
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 회피 방향 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    protected override void ApplyLookRotation(float angle)
    {
        if (enemyControll != null && enemyControll.shotPivot != null)
        {
            enemyControll.shotPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    protected override void OnLookAngleChanged(float oldValue, float newValue, bool asServer)
    {
        base.OnLookAngleChanged(oldValue, newValue, asServer);
        
        // Enemy 전용 추가 처리 - 클라이언트에서만
        if (!asServer)
        {
#if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] 적 조준 방향 보간 시작: {oldValue} -> {newValue}");
#endif
        }
    }

    // ✅ 적 전용 사망 처리 개선
    protected override void HandleAgentDeath(NetworkConnection killer)
    {
        base.HandleAgentDeath(killer);
        
        // ✅ EnemyState의 HandleDeath 호출 (순환 참조 해결됨)
        if (enemyState != null)
        {
            enemyState.HandleDeath();
        }
        
        Debug.Log($"[{gameObject.name}] 적 사망 처리 (킬러: {killer?.ClientId})");
        
        // TODO: 적 전용 사망 로직
        // - 경험치/아이템 드롭
        // - 킬 점수 지급
        // - 적 리스폰 또는 제거
    }
    
    // 적 전용 유틸리티 메서드들
    public Vector3 GetTargetPosition()
    {
        return syncTargetPosition.Value;
    }
    
    public bool IsChasing()
    {
        return syncIsChasing.Value;
    }
    
    public string GetAIState()
    {
        return syncAIState.Value;
    }
    
    public NetworkObject GetCurrentTarget()
    {
        return syncCurrentTarget.Value;
    }
    
    public float GetAttackCooldown()
    {
        return syncAttackCooldown.Value;
    }
    
    public bool IsStrafing()
    {
        return syncIsStrafing.Value;
    }
    
    public float GetStrafeDirection()
    {
        return syncStrafeDirection.Value;
    }
    
    public Vector3 GetLastKnownTargetPosition()
    {
        return syncLastKnownTargetPosition.Value;
    }
    
    // 적 전용 AI 상태 확인 메서드들
    public bool IsPatrolling()
    {
        return syncAIState.Value == "Patrol";
    }
    
    public bool IsAttacking()
    {
        return syncAIState.Value == "Attack";
    }
    
    public bool IsReturning()
    {
        return syncAIState.Value == "Return";
    }
} 