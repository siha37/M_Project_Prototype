using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;        // 따라갈 타겟
    [SerializeField] private float smoothSpeed = 5f;  // 카메라 이동 속도
    [SerializeField] private Vector3 offset;         // 카메라와 타겟 사이의 거리

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;
        
        // 부드러운 이동을 위한 Lerp 사용
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // 카메라 위치 업데이트
        transform.position = smoothedPosition;
    }
}
