using System.Collections;
using UnityEngine;

/// <summary>
/// 적 전투 컴포넌트
/// 조준, 발사, 재장전, 탄약 관리 등을 담당
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    [Header("=== 전투 설정 ===")]
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private float aimPrecision = 0.1f;
    [SerializeField] private float fireRate = 0.5f;
    
    [Header("=== 발사 설정 ===")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform shotPivot;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileDamage = 10f;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showAimLine = true;
    [SerializeField] private bool showFirePoint = true;
    
    // 전투 상태
    [SerializeField] private int currentAmmo;
    private bool isReloading;
    private float lastFireTime;
    private float reloadStartTime;
    private float finalAngle;
    private Vector3 currentAimTarget;
    private bool canShoot;
    
    // 컴포넌트 참조
    private EnemyNetworkSync networkSync;
    
    // 이벤트
    public System.Action OnShoot;
    public System.Action OnReloadStarted;
    public System.Action OnReloadCompleted;
    public System.Action<float> OnReloadProgress;
    
    // ========== Properties ==========
    
    /// <summary>
    /// 현재 탄약 수
    /// </summary>
    public int CurrentAmmo => currentAmmo;
    
    /// <summary>
    /// 최대 탄약 수
    /// </summary>
    public int MaxAmmo => maxAmmo;
    
    /// <summary>
    /// 재장전 중인지 여부
    /// </summary>
    public bool IsReloading => isReloading;
    
    /// <summary>
    /// 발사 가능한지 여부
    /// </summary>
    public bool CanShoot => canShoot && !isReloading && currentAmmo > 0;
    
    /// <summary>
    /// 현재 조준 각도
    /// </summary>
    public float LookAngle => finalAngle;
    
    /// <summary>
    /// 재장전 진행률 (0.0 ~ 1.0)
    /// </summary>
    public float ReloadProgress
    {
        get
        {
            if (!isReloading) return 0f;
            return Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime);
        }
    }
    
    // ========== Unity Lifecycle ==========
    
    private void Awake()
    {
        // 컴포넌트 참조 초기화
        networkSync = GetComponent<EnemyNetworkSync>();
        
        // 발사점 자동 할당
        if (firePoint == null)
        {
            firePoint = transform.Find("FirePoint");
            if (firePoint == null)
            {
                // FirePoint가 없으면 자식으로 생성
                GameObject firePointObj = new GameObject("FirePoint");
                firePointObj.transform.SetParent(transform);
                firePointObj.transform.localPosition = Vector3.forward * 0.5f;
                firePoint = firePointObj.transform;
            }
        }
        
        // 초기 탄약 설정
        currentAmmo = maxAmmo;
        
        LogManager.Log(LogCategory.Enemy, "EnemyCombat 컴포넌트 초기화 완료", this);
    }
    
    private void Start()
    {
        // 전투 로직 초기화
        canShoot = true;
        LogManager.Log(LogCategory.Enemy, "EnemyCombat 초기화 완료", this);
    }
    
    private void Update()
    {
        // 재장전 업데이트
        UpdateReload();
        
        // 발사 쿨다운 업데이트
        UpdateFireCooldown();
    }
    
    // ========== Public Methods ==========
    
    /// <summary>
    /// 특정 위치를 조준
    /// </summary>
    public void AimAt(Vector3 targetPosition)
    {
        currentAimTarget = targetPosition;
        
        // 조준 각도 계산
        Vector2 direction = (targetPosition - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 조준 적용
        if (shotPivot)
        {
            shotPivot.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
        
        // 조준 정밀도 적용 (약간의 오차 추가)
        float aimError = Random.Range(-aimPrecision * 90f, aimPrecision * 90f);
        finalAngle = targetAngle + aimError;
        
        // 각도 정규화
        finalAngle = Mathf.Repeat(finalAngle, 360f);
        
        LogManager.Log(LogCategory.Enemy, $"조준: {targetPosition} (각도: {finalAngle:F1}°)", this);
    }
    
    
    /// <summary>
    /// 발사 시도
    /// </summary>
    public bool TryShoot()
    {
        if (!CanShoot) return false;
        
        // 발사 쿨다운 체크
        if (Time.time - lastFireTime < fireRate) return false;
        
        // 발사 실행
        FireProjectile();
        
        // 탄약 감소
        currentAmmo--;
        
        // 발사 시간 업데이트
        lastFireTime = Time.time;
        
        LogManager.Log(LogCategory.Enemy, $"발사 성공 - 남은 탄약: {currentAmmo}", this);
        
        // 이벤트 발생
        OnShoot?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// 재장전 시작
    /// </summary>
    public void StartReload()
    {
        if (isReloading) return;
        
        isReloading = true;
        reloadStartTime = Time.time;
        
        LogManager.Log(LogCategory.Enemy, "재장전 시작", this);
        
        // 이벤트 발생
        OnReloadStarted?.Invoke();
    }
    
    /// <summary>
    /// 재장전 강제 완료
    /// </summary>
    public void ForceCompleteReload()
    {
        CompleteReload();
    }
    
    /// <summary>
    /// 탄약 추가
    /// </summary>
    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        
        LogManager.Log(LogCategory.Enemy, $"탄약 추가: +{amount} (총 {currentAmmo})", this);
    }
    
    /// <summary>
    /// 탄약 설정
    /// </summary>
    public void SetAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(amount, 0, maxAmmo);
        
        LogManager.Log(LogCategory.Enemy, $"탄약 설정: {currentAmmo}", this);
    }
    
    /// <summary>
    /// 발사 가능 여부 설정
    /// </summary>
    public void SetCanShoot(bool _canShoot)
    {
        this.canShoot = _canShoot;
        
        LogManager.Log(LogCategory.Enemy, $"발사 가능 설정: {_canShoot}", this);
    }
    
    /// <summary>
    /// 발사 속도 설정
    /// </summary>
    public void SetFireRate(float newFireRate)
    {
        fireRate = Mathf.Max(0.1f, newFireRate);
        
        LogManager.Log(LogCategory.Enemy, $"발사 속도 변경: {fireRate}", this);
    }
    
    /// <summary>
    /// 재장전 시간 설정
    /// </summary>
    public void SetAimPrecision(float precision)
    {
        aimPrecision = precision;
        
        LogManager.Log(LogCategory.Enemy, $"정확도 변경: {aimPrecision}", this);
    }

    public void SetReloadTime(float _reloadTime)
    {
        reloadTime = _reloadTime;
    }
    
    // ========== Private Methods ==========
    
    /// <summary>
    /// 발사체 생성 및 발사
    /// </summary>
    private void FireProjectile()
    {
        if (BulletManager.Instance)
        {
            // ✅ 적군 AI는 NetworkConnection 대신 GameObject 참조로 식별
            Vector3 firePos = firePoint.position;
            float fireAngle = finalAngle;
            
            // ✅ 적군 AI용 별도 발사 메서드 사용
            BulletManager.Instance.FireBulletForEnemy(firePos, fireAngle,AgentState.bulletSpeed , AgentState.bulletDamage, AgentState.bulletRange, gameObject);
        }
    }
    
    
    // ✅ BulletManager 초기화 대기 코루틴
    private IEnumerator WaitForBulletManagerAndShoot()
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
            FireProjectile();
        }
        else
        {
            LogManager.LogError(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 타임아웃! 발사 취소", this);
        }
    }


    
    /// <summary>
    /// 재장전 업데이트
    /// </summary>
    private void UpdateReload()
    {
        if (!isReloading) return;
        
        // 재장전 진행률 업데이트
        float progress = ReloadProgress;
        OnReloadProgress?.Invoke(progress);
        
        // 재장전 완료 체크
        if (progress >= 1f)
        {
            CompleteReload();
        }
    }
    
    /// <summary>
    /// 재장전 완료
    /// </summary>
    private void CompleteReload()
    {
        isReloading = false;
        currentAmmo = maxAmmo;
        
        LogManager.Log(LogCategory.Enemy, "재장전 완료", this);
        
        // 이벤트 발생
        OnReloadCompleted?.Invoke();
    }
    
    /// <summary>
    /// 발사 쿨다운 업데이트
    /// </summary>
    private void UpdateFireCooldown()
    {
        // 발사 가능 여부 업데이트
        bool newCanShoot = !isReloading && currentAmmo > 0 && (Time.time - lastFireTime >= fireRate);
        canShoot = newCanShoot;
    }
    
    // ========== Gizmos ==========
    
    private void OnDrawGizmos()
    {
        if (!showAimLine) return;
        
        // 조준선 표시
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.right * 3f);
        }
        
        // 발사점 표시
        if (showFirePoint && firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
        
        // 조준 대상 표시
        if (currentAimTarget != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(currentAimTarget, 0.3f);
            Gizmos.DrawLine(transform.position, currentAimTarget);
        }
    }
    
    // ========== Validation ==========
    
    private void OnValidate()
    {
        // 발사점 자동 할당
        if (firePoint == null)
        {
            firePoint = transform.Find("FirePoint");
        }
        
        // 설정값 검증
        if (reloadTime < 0.1f) reloadTime = 2f;
        if (maxAmmo < 1) maxAmmo = 30;
        if (aimPrecision < 0f) aimPrecision = 0.1f;
        if (fireRate < 0.1f) fireRate = 0.5f;
        if (projectileSpeed < 1f) projectileSpeed = 10f;
        if (projectileDamage < 0f) projectileDamage = 10f;
    }
} 