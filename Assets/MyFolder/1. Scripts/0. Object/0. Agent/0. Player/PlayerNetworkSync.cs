using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
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
        private PlayerStatus playerStatus;
        private PlayerControll playerControll;
        private PlayerInputControll playerInputControll;
        private PlayerInteractController playerInteractController;
    
        // 플레이어 전용 이벤트
        public delegate void OnPlayerRevivedHandler();
        public event OnPlayerRevivedHandler OnPlayerRevived;
    
        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            playerStatus = GetComponent<PlayerStatus>();
            playerControll = GetComponent<PlayerControll>();
            playerInputControll = GetComponent<PlayerInputControll>();
            playerInteractController = GetComponent<PlayerInteractController>();
        
            if (playerStatus == null)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} PlayerState 컴포넌트를 찾을 수 없습니다.", this);
            }
        
            if (playerControll == null)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} PlayerControll 컴포넌트를 찾을 수 없습니다.", this);
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
            if (playerStatus != null)
            {
                syncReviveCurrentCount.Value = PlayerStatus.reviveCount;
                syncIsReviving.Value = false;
                syncReviveProgress.Value = 0f;
                syncMoveDirection.Value = Vector2.zero;
                syncIsMoving.Value = false;
                syncIsAttacking.Value = false;
            }
        }
        protected override void OnIsDeadChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (playerStatus)
            {
                playerStatus.isDead = newValue;
            
                // ✅ 사망 상태 UI 업데이트 (필요시)
                if (agentUI && newValue)
                {
                    if (syncReviveCurrentCount.Value != 0)
                        playerStatus.OnClientDeathSequence();
                    else
                        playerStatus.OnClientDeath();
                    // TODO: 사망 시 UI 변경 로직 (체력바 숨김, 사망 표시 등)
                } 
#if UNITY_EDITOR
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 사망 상태 동기화: {oldValue} -> {newValue}", this);
#endif
            }
        }
        // 플레이어 전용 부활 처리
        [ServerRpc(RequireOwnership = false)]
        public void RequestRevive()
        {
            if (playerStatus && playerStatus.IsDead)
            {
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 서버에서 부활 처리", this);
            
                playerStatus.Revive();
                syncCurrentHp.Value = playerStatus.currentHp;
                syncReviveCurrentCount.Value = playerStatus.reviveCurrentCount;
                syncIsDead.Value = playerStatus.IsDead;
            
                // 모든 클라이언트에 부활 효과 전송
                OnRevivedEffect();
                OnPlayerRevived?.Invoke();
            }
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
        
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 서버에서 재장전 시작", this);
            RequestSetReloadingState(true);
            StartCoroutine(ServerReloadProcess());
        }
    
        private IEnumerator ServerReloadProcess()
        {
            float reloadTimer = 0f;
        
            while (reloadTimer < AgentStatus.bulletReloadTime)
            {
                reloadTimer += Time.deltaTime;
                float progress = reloadTimer / AgentStatus.bulletReloadTime;
                OnReloadProgress_Local(progress);
                OnReloadProgress_Observer(progress);
                yield return null;
            }
        
            // 재장전 완료
            RequestUpdateBulletCount(AgentStatus.bulletMaxCount);
            RequestSetReloadingState(false);
            OnReloadComplete();
        }
    
        // 플레이어 전용 이벤트들
        [ObserversRpc]
        private void OnReviveProgress(float progress)
        {
            syncReviveProgress.Value = progress;
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 부활 진행률: {progress * 100:F1}%", this);
        }
    
        [ObserversRpc]
        private void OnReviveComplete(NetworkObject targetPlayer)
        {
            Log($"{gameObject.name} 부활 완료", this);
        }
    
        [ObserversRpc]
        private void OnRevivedEffect()
        {
            playerStatus.ClientReviveEffect();
            Log($"{gameObject.name} 부활 효과 재생", this);
        }
    
        // 플레이어 전용 발사 효과
        protected override void OnShootEffect(float angle, Vector3 position)
        {
            // ✅ 플레이어 전용 시각/음향 효과
            // TODO: 실제 시각 효과 구현 (파티클, 사운드, 총구 화염 등
        }

        private void OnReloadProgress_Local(float progress)
        {
            if(agentUI)
                agentUI.UpdateReloadProgress(progress);   
        }
        [ObserversRpc]
        private void OnReloadProgress_Observer(float progress)
        {
            if(agentUI)
                agentUI.UpdateReloadProgress(progress);
        }
    
        [ObserversRpc]
        private void OnReloadComplete()
        {
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 재장전 완료", this);
        }
    
        // SyncVar 변경 시 호출되는 메서드들
        private void OnReviveCountChanged(int oldValue, int newValue, bool asServer)
        {
            if (playerStatus)
            {
                playerStatus.reviveCurrentCount = newValue;
#if UNITY_EDITOR
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 부활 횟수 동기화: {oldValue} -> {newValue}", this);
#endif
            }
        }
    
        private void OnIsRevivingChanged(bool oldValue, bool newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 부활 진행 상태 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        private void OnReviveProgressChanged(float oldValue, float newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 부활 진행률 동기화: {oldValue * 100:F1}% -> {newValue * 100:F1}%", this);
#endif
        }
    
        private void OnMoveDirectionChanged(Vector2 oldValue, Vector2 newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 이동 방향 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        private void OnIsMovingChanged(bool oldValue, bool newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 이동 상태 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        private void OnIsAttackingChanged(bool oldValue, bool newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 공격 상태 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        protected override void ApplyLookRotation(float angle)
        {
            if (playerControll && playerControll.shotPivot)
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
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 플레이어 조준 방향 보간 시작: {oldValue} -> {newValue}", this);
#endif
            }
        }

        protected override void OnIsReloadingChanged(bool oldValue, bool newValue, bool asServer)
        {
            base.OnIsReloadingChanged(oldValue, newValue, asServer);
            if (playerControll)
            {
                playerControll.SetReloadingstate(newValue);
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
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 플레이어 사망 처리 (킬러: {killer?.ClientId})", this);
        
            // TODO: 플레이어 전용 사망 로직
            // - 리스폰 타이머 시작
            // - 사망 통계 업데이트
            // - 플레이어 UI 처리
        }
    }
} 