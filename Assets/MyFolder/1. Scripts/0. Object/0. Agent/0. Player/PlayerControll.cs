using UnityEngine;
using System.Collections;
using FishNet.Object;

public class PlayerControll : NetworkBehaviour
{
    PlayerInputControll playerInputControll;
    PlayerState state;
    Transform tf;
    Rigidbody2D rd2D;
    // ✅ AgentUI 참조 제거 (NetworkSync에서 처리)
    private PlayerNetworkSync networkSync;

    [Header("Look Settings")]
    [SerializeField] private float gamepadDeadzone = 0.15f;
    [SerializeField] private float lookSensitivity = 1f;
    [SerializeField] private float lookSmoothing = 10f;
    [SerializeField] private bool enableLookSmoothing = false;
    
    private float lookAngle;
    private float targetLookAngle;
    private float currentLookAngle;
    
    public float LookAngle => lookAngle;
    public Transform shotPivot;
    public Transform shotPoint;
    public GameObject bulletPrefab;
    public Camera mainCamera;

    private bool canShoot = true;
    private bool isReloading = false;
    private float lastNetworkSyncTime = 0f;
    
    public override void OnStartClient()
    {
        playerInputControll = GetComponent<PlayerInputControll>();
        state = GetComponent<PlayerState>();
        // ✅ AgentUI 참조 제거
        networkSync = GetComponent<PlayerNetworkSync>();
        tf = transform;
        rd2D = GetComponent<Rigidbody2D>();

        if(!IsOwner){
            playerInputControll.enabled = false;
            // ✅ AgentUI는 NetworkSync에서 관리하므로 여기서 제어하지 않음
        }
        else
        {
            playerInputControll.movePerformedCallback += Move;
            playerInputControll.moveStopCallback += MoveStop;
            playerInputControll.lookPerformedCallback += Look;
            playerInputControll.attackCallback += AttackTrigger;
            playerInputControll.reloadCallback += ReloadTrigger;
            InitializeCamera();
        }
    }
    
    private void InitializeCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            if (mainCamera == null)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} 카메라를 찾을 수 없습니다!", this);
            }
        }
    }
    
    private Vector2 GetWorldMousePosition(Vector2 screenPosition)
    {
        if (mainCamera == null) return Vector2.zero;
        
        // 2D 게임에서 정확한 월드 좌표 계산
        // 카메라에서 게임 월드(Z=0)까지의 거리 사용
        float distanceToGameWorld = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceToGameWorld));
        return new Vector2(worldPos.x, worldPos.y);
    }
    
    private void UpdateLookAngle(Vector2 targetVector)
    {
        targetLookAngle = Mathf.Atan2(targetVector.y, targetVector.x) * Mathf.Rad2Deg * lookSensitivity;
        
        if (enableLookSmoothing)
        {
            currentLookAngle = Mathf.LerpAngle(currentLookAngle, targetLookAngle, 
                Time.deltaTime * lookSmoothing);
            lookAngle = currentLookAngle;
        }
        else
        {
            lookAngle = targetLookAngle;
        }
    }

    void Move(Vector2 direction)
    {
        rd2D.linearVelocity = new Vector2(direction.x, direction.y) * PlayerState.speed;
    }
    
    void MoveStop()
    {
        if(rd2D == null || !rd2D) return;
        rd2D.linearVelocity = Vector2.zero;
    }

    void Look(Vector2 position)
    {
        if (!mainCamera)
        {
            mainCamera = Camera.main;
            return;
        }
        if (!IsOwner) return;
        
        Vector2 targetVector = Vector2.zero;
        
        switch (playerInputControll.controllerType)
        {
            case PlayerInputControll.ControllerType.Keyboard:
                // 개선된 마우스 위치 계산
                Vector2 worldMousePos = GetWorldMousePosition(position);
                targetVector = worldMousePos - (Vector2)transform.position;
                break;
                
            case PlayerInputControll.ControllerType.Gamepad:
                // 개선된 데드존 처리
                float inputMagnitude = position.sqrMagnitude;
                if (inputMagnitude > gamepadDeadzone * gamepadDeadzone)
                {
                    // 데드존 보정 적용
                    float correctedMagnitude = (Mathf.Sqrt(inputMagnitude) - gamepadDeadzone) / (1f - gamepadDeadzone);
                    targetVector = position.normalized * correctedMagnitude;
                }
                else
                {
                    return; // 데드존 내부면 무시
                }
                break;
        }

        // 개선된 각도 계산
        UpdateLookAngle(targetVector);
        
        // 로컬에서 즉시 반영 (입력 지연 최소화)
        shotPivot.rotation = Quaternion.Euler(new Vector3(0, 0, lookAngle));
        
        // 네트워크 동기화는 덜 자주 (성능 최적화)
        if (Time.time - lastNetworkSyncTime > 0.05f) // 20fps로 동기화
        {
            if (networkSync != null)
            {
                networkSync.RequestUpdateLookAngle(lookAngle);
            }
            lastNetworkSyncTime = Time.time;
        }
    }

    private void AttackTrigger()
    {
        if (!canShoot || isReloading) return;

        // 네트워크 동기화된 발사 처리
        if (networkSync != null)
        {
            // 서버에 발사 요청 (네트워크 동기화)
            networkSync.RequestShoot(lookAngle, shotPoint.position);
        }

        StartCoroutine(ShootDelay());
    }

    private IEnumerator ShootDelay()
    {
        canShoot = false;
        yield return new WaitForSeconds(AgentState.bulletDelay);
        canShoot = true;
    }

    // ✅ 로컬 Reload 메서드 제거 - NetworkSync에서 처리
    
    private void ReloadTrigger()
    {
        if (!isReloading && state.bulletCurrentCount < AgentState.bulletMaxCount)
        {
            // ✅ 네트워크 동기화만 사용 (폴백 방식 제거)
            if (networkSync != null)
            {
                networkSync.RequestReload();
            }
            else
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} PlayerNetworkSync 컴포넌트를 찾을 수 없습니다!", this);
            }
        }
    }
    // 콜백 해제를 위한 메서드 추가
    public override void OnStopClient()
    {
        if (playerInputControll != null && IsOwner)
        {
            playerInputControll.movePerformedCallback -= Move;
            playerInputControll.moveStopCallback -= MoveStop;
            playerInputControll.lookPerformedCallback -= Look;
            playerInputControll.attackCallback -= AttackTrigger;
            playerInputControll.reloadCallback -= ReloadTrigger;
        }
    }

    private void OnDestroy()
    {
        // 추가 안전 장치로 OnDestroy에서도 콜백 해제
        if (playerInputControll != null && IsOwner)
        {
            playerInputControll.movePerformedCallback -= Move;
            playerInputControll.moveStopCallback -= MoveStop;
            playerInputControll.lookPerformedCallback -= Look;
            playerInputControll.attackCallback -= AttackTrigger;
            playerInputControll.reloadCallback -= ReloadTrigger;
        }
    }
}
