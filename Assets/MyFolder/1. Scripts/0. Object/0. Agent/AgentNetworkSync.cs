using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._0._Object._2._Projectile;
using MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent
{
    public class AgentNetworkSync : NetworkBehaviour
    {
        // 공통 상태 동기화
        protected readonly SyncVar<float> syncCurrentHp = new SyncVar<float>();
        protected readonly SyncVar<bool> syncIsDead = new SyncVar<bool>();
        protected readonly SyncVar<bool> syncIsCanSee = new SyncVar<bool>();
        protected readonly SyncVar<float> syncBulletCurrentCount = new SyncVar<float>();
        protected readonly SyncVar<bool> syncIsReloading = new SyncVar<bool>();
        protected readonly SyncVar<float> syncLookAngle = new SyncVar<float>();
    
        [Header("Shooting Settings")]
        [SerializeField] protected GameObject bulletPrefab;
        [SerializeField] protected float bulletSpeed = 15f;
        [SerializeField] protected float bulletLifetime = 5f;
    
        [Header("Interpolation Settings")]
        [SerializeField] protected float rotationLerpSpeed = 20f; // 회전 보간 속도
    
        // 보간용 타겟 값들
        protected float targetLookAngle;
        protected float currentLookAngle;
        protected bool shouldInterpolateRotation = false;
    
        // 공통 컴포넌트 참조
        protected AgentStatus AgentStatus;
        protected AgentUI agentUI;
        protected Transform agentTransform;
    
        // 공통 이벤트
        public delegate void OnAgentDamagedHandler(float damage, Vector2 hitDirection, NetworkConnection attacker);
        public delegate void OnAgentDeathHandler(NetworkConnection killer);
    
        public event OnAgentDamagedHandler OnAgentDamaged;
        public event OnAgentDeathHandler OnAgentDeath;
    
        public override void OnStartServer()
        {
            InitializeComponents();
            InitializeSyncVars();
        }
    
        public override void OnStartClient()
        {
            InitializeComponents();
            RegisterSyncVarCallbacks();
        }
    
        protected virtual void Update()
        {
            if (!IsOwner && shouldInterpolateRotation)
            {
                // 부드러운 각도 보간
                float angleDifference = Mathf.DeltaAngle(currentLookAngle, targetLookAngle);
            
                if (Mathf.Abs(angleDifference) > 0.5f) // 0.5도 이상 차이날 때만 보간
                {
                    currentLookAngle = Mathf.LerpAngle(currentLookAngle, targetLookAngle, 
                        Time.deltaTime * rotationLerpSpeed);
                
                    // 실제 회전 적용
                    ApplyLookRotation(currentLookAngle);
                }
                else
                {
                    // 거의 도달했으면 정확한 값으로 설정
                    currentLookAngle = targetLookAngle;
                    ApplyLookRotation(currentLookAngle);
                    shouldInterpolateRotation = false;
                }
            }
        }
    
        protected virtual void ApplyLookRotation(float angle)
        {
            // 자식 클래스에서 구현
        }
    
        protected virtual void RegisterSyncVarCallbacks()
        {
            syncCurrentHp.OnChange += OnCurrentHpChanged;
            syncIsDead.OnChange += OnIsDeadChanged;
            syncBulletCurrentCount.OnChange += OnBulletCountChanged;
            syncIsReloading.OnChange += OnIsReloadingChanged;
            syncLookAngle.OnChange += OnLookAngleChanged;
        }
    
        protected virtual void InitializeComponents()
        {
            AgentStatus = GetComponent<AgentStatus>();
            agentUI = GetComponent<AgentUI>();
            agentTransform = transform;
            if (AgentStatus == null)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} AgentState 컴포넌트를 찾을 수 없습니다.", this);
            }
        
            if (agentUI == null)
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} AgentUI 컴포넌트를 찾을 수 없습니다.", this);
            }
            else
            {
                agentUI.InitializeUI(AgentStatus.maxHp, AgentStatus.maxHp, (int)AgentStatus.bulletMaxCount, (int)AgentStatus.bulletMaxCount,IsOwner);
            }
        }
    
        protected virtual void InitializeSyncVars()
        {
            if (AgentStatus != null)
            {
                syncCurrentHp.Value = AgentStatus.maxHp;
                syncBulletCurrentCount.Value = AgentStatus.bulletMaxCount;
                syncIsDead.Value = false;
                syncIsCanSee.Value = true;
                syncIsReloading.Value = false;
                syncLookAngle.Value = 0f;
            
            }
        }
    
        // ✅ NetworkConnection 정보를 포함한 데미지 처리
        public virtual void RequestTakeDamage(float damage, Vector2 hitDirection, NetworkConnection attacker = null)
        {
            if (AgentStatus && !syncIsDead.Value)
            {
                Log($"{gameObject.name} 서버에서 데미지 처리: {damage} (공격자: {attacker?.ClientId})", this);
            
                AgentStatus.TakeDamage(damage, hitDirection);
                UpdateDamageSyncVars();
            
                // 모든 클라이언트에 데미지 효과 전송
                OnDamagedEffect(damage, hitDirection);
            
                // 별도 콜백 - 암것도 없음 지금은
                OnAgentDamaged?.Invoke(damage, hitDirection, attacker);
            
                // 사망 시 이벤트 호출
                if (syncIsDead.Value)
                {
                    HandleAgentDeath(attacker);
                    OnDeathEffect();
                    OnAgentDeath?.Invoke(attacker);
                }
            }
        }
    
        protected virtual void UpdateDamageSyncVars()
        {
            if (AgentStatus)
            {
                syncCurrentHp.Value = AgentStatus.currentHp;
                syncIsDead.Value = AgentStatus.IsDead;
            }
        }
    
        public void RequestUpdateBulletCount(float count)
        {
            if (AgentStatus)
            {
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 서버에서 탄약 업데이트: {count}", this);
            
                AgentStatus.UpdateBulletCount(count);
                syncBulletCurrentCount.Value = AgentStatus.bulletCurrentCount;
            }
        }
    
        // 공통 조준 방향 처리
        [ServerRpc]
        public virtual void RequestUpdateLookAngle(float angle)
        {
            syncLookAngle.Value = angle;
        }
    
        // 공통 재장전 상태 처리
        [ServerRpc(RequireOwnership = false)]
        public virtual void RequestSetReloadingState(bool isReloading)
        {
            syncIsReloading.Value = isReloading;
        }
    
        // ✅ FishNet 공식 권장: Pool 시스템 + NetworkConnection 결합
        [ServerRpc]
        public virtual void RequestShoot(float angle, Vector3 shotPosition)
        {
            LogManager.Log(LogCategory.Projectile, $"{gameObject.name} 서버에서 발사 처리: {angle} : {shotPosition}", this);
        
            // ✅ BulletManager 초기화 확인 및 대기
            if (!BulletManager.Instance)
            {
                LogManager.LogWarning(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 대기 중...", this);
                StartCoroutine(WaitForBulletManagerAndShoot(angle, shotPosition));
                return;
            }
        
            // ✅ BulletManager Pool 시스템 활용 (성능 최적화)
            BulletManager.Instance.FireBulletWithConnection(
                shotPosition,
                angle, 
                AgentStatus.bulletSpeed,
                AgentStatus.bulletDamage,
                AgentStatus.bulletRange,
                base.Owner  // ✅ NetworkConnection 전달
            );
        
            // 탄약 감소
            RequestUpdateBulletCount(-1);
        
            // 발사 효과
            OnShootEffect(angle, shotPosition);
        }
    
        // ✅ BulletManager 초기화 대기 코루틴
        private IEnumerator WaitForBulletManagerAndShoot(float angle, Vector3 shotPosition)
        {
            float waitTime = 0f;
            const float maxWaitTime = 5f; // 최대 5초 대기
        
            while (!BulletManager.Instance && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
        
            if (BulletManager.Instance)
            {
                // ✅ 초기화 완료 후 정상 발사 처리
                LogManager.Log(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 완료 - 발사 재시도", this);
                RequestShoot(angle, shotPosition);
            }
            else
            {
                LogManager.LogError(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 타임아웃! 발사 취소", this);
            }
        }
    
        // ✅ 기존 CreateBullet 메서드 제거 (Pool 시스템 사용으로 불필요)
        
        [ObserversRpc]
        protected virtual void OnShootEffect(float angle, Vector3 position)
        {
#if UNITY_EDITOR
            Log($"{gameObject.name} 발사 효과 재생: {angle}", this);
#endif
            // ✅ 모든 클라이언트에서 추가 시각/음향 효과 처리
            // 자식 클래스에서 구체적인 효과 구현 (총구 화염, 사운드 등)
        }
    
        // SyncVar 변경 시 호출되는 메서드들
        protected void OnCurrentHpChanged(float oldValue, float newValue, bool asServer)
        {
            if (AgentStatus)
            {
                AgentStatus.currentHp = newValue;
            
                // ✅ AgentUI로 직접 업데이트
                if (agentUI)
                {
                    agentUI.UpdateHealthUI(newValue, AgentStatus.maxHp);
                }
            
#if UNITY_EDITOR
                Log($"{gameObject.name} 체력 동기화: {oldValue} -> {newValue}", this);
#endif
            }
        }
    
        protected void OnBulletCountChanged(float oldValue, float newValue, bool asServer)
        {
            if (AgentStatus)
            {
                AgentStatus.bulletCurrentCount = newValue;
            
                // ✅ AgentUI로 직접 업데이트
                if (agentUI)
                {
                    agentUI.UpdateAmmoUI((int) AgentStatus.bulletCurrentCount, (int)AgentStatus.bulletMaxCount);
                }
            
#if UNITY_EDITOR
                Log($"{gameObject.name} 탄약 동기화: {oldValue} -> {newValue}", this);
#endif
            }
        }
    
        protected virtual void OnIsDeadChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (AgentStatus)
            {
                AgentStatus.isDead = newValue;
            
                // ✅ 사망 상태 UI 업데이트 (필요시)
                if (agentUI && newValue)
                {
                    // TODO: 사망 시 UI 변경 로직 (체력바 숨김, 사망 표시 등)
                }
#if UNITY_EDITOR
                Log($"{gameObject.name} 사망 상태 동기화: {oldValue} -> {newValue}", this);
#endif
            }
        }
    
        protected virtual void OnIsReloadingChanged(bool oldValue, bool newValue, bool asServer)
        {
            // ✅ 재장전 UI 업데이트
            if (agentUI)
            {
                if (newValue)
                {
                    agentUI.StartReloadUI();
                }
                else
                {
                    agentUI.EndReloadUI();
                }
            }
        
#if UNITY_EDITOR
            Log($"{gameObject.name} 재장전 상태 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        protected virtual void OnLookAngleChanged(float oldValue, float newValue, bool asServer)
        {
            if (!IsOwner)
            {
                targetLookAngle = newValue;
            
                // 첫 번째 값이면 즉시 설정
                if (Mathf.Abs(oldValue) < 0.01f)
                {
                    currentLookAngle = newValue;
                    ApplyLookRotation(currentLookAngle);
                    shouldInterpolateRotation = false;
                }
                else
                {
                    // 일반적인 경우: 보간 처리 시작
                    shouldInterpolateRotation = true;
                }
                
        
#if UNITY_EDITOR
                Log($"{gameObject.name} 조준 방향 동기화: {oldValue} -> {newValue}", this);
#endif
            }
        }
    
        // 공통 이벤트 전송
        [ObserversRpc]
        protected virtual void OnDamagedEffect(float damage, Vector2 hitDirection)
        {
            // 데미지 효과, 사운드 등
            Log($"{gameObject.name} 데미지 효과 재생: {damage}", this);
        }
    
        [ObserversRpc]
        protected virtual void OnDeathEffect()
        {
            // 사망 효과, 파티클 등
            Log($"{gameObject.name} 사망 효과 재생", this);
        }
    
        // 공통 유틸리티 메서드들
        public float GetCurrentHp()
        {
            return syncCurrentHp.Value;
        }
    
        public bool IsDead()
        {
            return syncIsDead.Value;
        }

        public bool IsCanSee()
        {
            return syncIsCanSee.Value;
        }

        [ServerRpc]
        public void SetCanSee(bool canSee)
        {
            syncIsCanSee.Value = canSee;
        }
        public float GetBulletCount()
        {
            return syncBulletCurrentCount.Value;
        }
    
        public bool IsReloading()
        {
            return syncIsReloading.Value;
        }
    
        public float GetLookAngle()
        {
            return syncLookAngle.Value;
        }
    
        // ✅ 공격자 정보를 포함한 사망 처리 (상속 클래스에서 오버라이드 가능)
        protected virtual void HandleAgentDeath(NetworkConnection killer)
        {
            // 기본 사망 처리 - 상속 클래스에서 필요에 따라 오버라이드
            Log($"{gameObject.name} 사망 처리 (킬러: {killer?.ClientId})", this);
        
            // TODO: 킬/데스 통계, 리스폰 로직 등 구현
        }

        // 디버그 정보 출력
        protected virtual void OnValidate()
        {
            if (Application.isPlaying && syncCurrentHp != null)
            {
#if UNITY_EDITOR
                Log($"{gameObject.name} AgentNetworkSync 상태 - HP: {syncCurrentHp.Value}, Dead: {syncIsDead.Value}, Bullets: {syncBulletCurrentCount.Value}, Reloading: {syncIsReloading.Value}", this);
#endif
            }
        }

        protected virtual void Log(string message,Object obj)
        {
            LogManager.Log(LogCategory.Player,message,obj);
        }
        protected virtual void LogError(string message,Object obj)
        {
            LogManager.LogError(LogCategory.Player,message,obj);
        }
    }
} 