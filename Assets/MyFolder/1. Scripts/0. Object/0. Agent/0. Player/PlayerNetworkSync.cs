using UnityEngine;
using FishNet.Object;
using System.Collections;
using FishNet.Connection;
using FishNet.Object.Synchronizing;

public class PlayerNetworkSync : AgentNetworkSync
{
    // 플레이어 전용 동기화
    private readonly SyncVar<int> syncReviveCurrentCount = new SyncVar<int>();
    private readonly SyncVar<bool> syncIsReviving = new SyncVar<bool>();
    private readonly SyncVar<float> syncReviveProgress = new SyncVar<float>();
    private readonly SyncVar<Vector2> syncMoveDirection = new SyncVar<Vector2>();
    private readonly SyncVar<bool> syncIsMoving = new SyncVar<bool>();
    private readonly SyncVar<bool> syncIsAttacking = new SyncVar<bool>();
    
    // 플레이어 전용 컴포넌트
    private PlayerState playerState;
    private PlayerControll playerControll;
    private PlayerInputControll playerInputControll;
    private PlayerInteractController playerInteractController;
    
    // 플레이어 전용 이벤트
    public delegate void OnPlayerRevivedHandler();
    public event OnPlayerRevivedHandler OnPlayerRevived;
    
    protected override void InitializeComponents()
    {
        base.InitializeComponents();
        playerState = GetComponent<PlayerState>();
        playerControll = GetComponent<PlayerControll>();
        playerInputControll = GetComponent<PlayerInputControll>();
        playerInteractController = GetComponent<PlayerInteractController>();
        
        if (playerState == null)
        {
            Debug.LogError($"[{gameObject.name}] PlayerState 컴포넌트를 찾을 수 없습니다.");
        }
        
        if (playerControll == null)
        {
            Debug.LogError($"[{gameObject.name}] PlayerControll 컴포넌트를 찾을 수 없습니다.");
        }
    }
    
    protected override void RegisterSyncVarCallbacks()
    {
        base.RegisterSyncVarCallbacks();
        syncReviveCurrentCount.OnChange += OnReviveCountChanged;
        syncIsReviving.OnChange += OnIsRevivingChanged;
        syncReviveProgress.OnChange += OnReviveProgressChanged;
        syncMoveDirection.OnChange += OnMoveDirectionChanged;
        syncIsMoving.OnChange += OnIsMovingChanged;
        syncIsAttacking.OnChange += OnIsAttackingChanged;
    }
    
    protected override void InitializeSyncVars()
    {
        base.InitializeSyncVars();
        if (playerState != null)
        {
            syncReviveCurrentCount.Value = PlayerState.reviveCount;
            syncIsReviving.Value = false;
            syncReviveProgress.Value = 0f;
            syncMoveDirection.Value = Vector2.zero;
            syncIsMoving.Value = false;
            syncIsAttacking.Value = false;
        }
    }
    
    // 플레이어 전용 부활 처리
    [ServerRpc]
    public void RequestRevive()
    {
        if (playerState != null && playerState.IsDead)
        {
            Debug.Log($"[{gameObject.name}] 서버에서 부활 처리");
            
            playerState.Revive();
            syncCurrentHp.Value = playerState.currentHp;
            syncReviveCurrentCount.Value = playerState.reviveCurrentCount;
            syncIsDead.Value = playerState.IsDead;
            
            // 모든 클라이언트에 부활 효과 전송
            OnRevivedEffect();
            OnPlayerRevived?.Invoke();
        }
    }
    
    // 플레이어 전용 부활 시작
    [ServerRpc]
    public void RequestStartRevive(NetworkObject targetPlayer)
    {
        if (syncIsReviving.Value) return;
        
        Debug.Log($"[{gameObject.name}] 서버에서 부활 시작");
        syncIsReviving.Value = true;
        StartCoroutine(ServerReviveProcess(targetPlayer));
    }
    
    private IEnumerator ServerReviveProcess(NetworkObject targetPlayer)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < PlayerState.reviveDelay)
        {
            elapsedTime += Time.deltaTime;
            syncReviveProgress.Value = elapsedTime / PlayerState.reviveDelay;
            OnReviveProgress(syncReviveProgress.Value);
            yield return null;
        }
        
        // 부활 완료
        PlayerState targetState = targetPlayer.GetComponent<PlayerState>();
        if (targetState != null)
        {
            targetState.Revive();
            OnReviveComplete(targetPlayer);
        }
        
        syncIsReviving.Value = false;
        syncReviveProgress.Value = 0f;
    }
    
    // 플레이어 전용 이동 동기화
    [ServerRpc]
    public void RequestUpdateMovement(Vector2 direction, bool isMoving)
    {
        syncMoveDirection.Value = direction;
        syncIsMoving.Value = isMoving;
    }
    
    // 플레이어 전용 공격 상태 동기화
    [ServerRpc]
    public void RequestUpdateAttackState(bool isAttacking)
    {
        syncIsAttacking.Value = isAttacking;
    }
        
    // 플레이어 전용 재장전 처리
    [ServerRpc]
    public void RequestReload()
    {
        if (syncIsReloading.Value) return;
        
        Debug.Log($"[{gameObject.name}] 서버에서 재장전 시작");
        RequestSetReloadingState(true);
        StartCoroutine(ServerReloadProcess());
    }
    
    private IEnumerator ServerReloadProcess()
    {
        float reloadTimer = 0f;
        
        while (reloadTimer < AgentState.bulletReloadTime)
        {
            reloadTimer += Time.deltaTime;
            float progress = reloadTimer / AgentState.bulletReloadTime;
            OnReloadProgress(progress);
            yield return null;
        }
        
        // 재장전 완료
        RequestUpdateBulletCount(AgentState.bulletMaxCount);
        RequestSetReloadingState(false);
        OnReloadComplete();
    }
    
    // 플레이어 전용 이벤트들
    [ObserversRpc]
    private void OnReviveProgress(float progress)
    {
        syncReviveProgress.Value = progress;
        Debug.Log($"[{gameObject.name}] 부활 진행률: {progress * 100:F1}%");
    }
    
    [ObserversRpc]
    private void OnReviveComplete(NetworkObject targetPlayer)
    {
        Debug.Log($"[{gameObject.name}] 부활 완료");
    }
    
    [ObserversRpc]
    private void OnRevivedEffect()
    {
        Debug.Log($"[{gameObject.name}] 부활 효과 재생");
    }
    
    // 플레이어 전용 발사 효과
    protected override void OnShootEffect(float angle, Vector3 position)
    {
        // ✅ 플레이어 전용 시각/음향 효과
        // TODO: 실제 시각 효과 구현 (파티클, 사운드, 총구 화염 등
    }
    
    [ObserversRpc]
    private void OnReloadProgress(float progress)
    {
        Debug.Log($"[{gameObject.name}] 재장전 진행률: {progress * 100:F1}%");
    }
    
    [ObserversRpc]
    private void OnReloadComplete()
    {
        Debug.Log($"[{gameObject.name}] 재장전 완료");
    }
    
    // SyncVar 변경 시 호출되는 메서드들
    private void OnReviveCountChanged(int oldValue, int newValue, bool asServer)
    {
        if (playerState != null)
        {
            playerState.reviveCurrentCount = newValue;
#if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] 부활 횟수 동기화: {oldValue} -> {newValue}");
#endif
        }
    }
    
    private void OnIsRevivingChanged(bool oldValue, bool newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 부활 진행 상태 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    private void OnReviveProgressChanged(float oldValue, float newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 부활 진행률 동기화: {oldValue * 100:F1}% -> {newValue * 100:F1}%");
#endif
    }
    
    private void OnMoveDirectionChanged(Vector2 oldValue, Vector2 newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 이동 방향 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    private void OnIsMovingChanged(bool oldValue, bool newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 이동 상태 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    private void OnIsAttackingChanged(bool oldValue, bool newValue, bool asServer)
    {
#if UNITY_EDITOR
        Debug.Log($"[{gameObject.name}] 공격 상태 동기화: {oldValue} -> {newValue}");
#endif
    }
    
    protected override void ApplyLookRotation(float angle)
    {
        if (playerControll != null && playerControll.shotPivot != null)
        {
            playerControll.shotPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    protected override void OnLookAngleChanged(float oldValue, float newValue, bool asServer)
    {
        base.OnLookAngleChanged(oldValue, newValue, asServer);
        
        // 플레이어 전용 추가 처리 - 서버와 클라이언트 모두
        if (!IsOwner)
        {
#if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] 플레이어 조준 방향 보간 시작: {oldValue} -> {newValue}");
#endif
        }
    }
    
    // 플레이어 전용 유틸리티 메서드들
    public int GetReviveCount()
    {
        return syncReviveCurrentCount.Value;
    }
    
    public bool IsReviving()
    {
        return syncIsReviving.Value;
    }
    
    public float GetReviveProgress()
    {
        return syncReviveProgress.Value;
    }
    
    public Vector2 GetMoveDirection()
    {
        return syncMoveDirection.Value;
    }
    
    public bool IsMoving()
    {
        return syncIsMoving.Value;
    }
    
    public bool IsAttacking()
    {
        return syncIsAttacking.Value;
    }

    protected override void HandleAgentDeath(NetworkConnection killer)
    {
        base.HandleAgentDeath(killer);
        
        // ✅ 플레이어 전용 사망 처리
        Debug.Log($"[{gameObject.name}] 플레이어 사망 처리 (킬러: {killer?.ClientId})");
        
        // TODO: 플레이어 전용 사망 로직
        // - 리스폰 타이머 시작
        // - 사망 통계 업데이트
        // - 플레이어 UI 처리
    }
} 