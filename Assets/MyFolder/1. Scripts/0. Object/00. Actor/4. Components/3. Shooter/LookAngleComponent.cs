using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._4._Components._3._Shooter
{
    public class LookAngleComponent : IActorComponent
    {
        public int Priority => 10;
        public const string shotPivot = "ShotPivot";
        
        
        [Header("Look Settings")]
        private float lookSensitivity = 1f;
        private float lookSmoothing = 10f;
        
        private Actor _owner;
        private ActorEventBus _eventBus;
        private Transform _lookPivot;
        private Camera _camera;
        
        
        private float lookAngle;
        private float targetLookAngle;
        private float currentLookAngle;
        private bool enableLookSmoothing = false;
        
        public void Init(Actor actor)
        {
            _owner = actor;
            _eventBus = actor.EventBus;
            _lookPivot = actor.transform.Find(shotPivot);
            _camera = Camera.main;
        }

        public void OnEnable()
        {
            if (_eventBus == null) return;

            // 이벤트 구독
            _eventBus.LookRequested += OnLookRequested;
        }

        public void OnDisable()
        {
            if (_eventBus == null) return;

            // 이벤트 구독 해제
            _eventBus.MoveRequested -= OnLookRequested;
        }

        private void OnLookRequested(Vector2 obj)
        {
            Vector2 targetVector = Vector2.zero;

            // 개선된 마우스 위치 계산
            Vector2 worldMousePos = GetWorldMousePosition(obj);
            targetVector = worldMousePos - (Vector2)_owner.transform.position;
            
            lookAngle = UpdateLookAngle(targetVector);
            
            // 로컬에서 즉시 반영 (입력 지연 최소화)
            _lookPivot.rotation = Quaternion.Euler(new Vector3(0, 0, lookAngle));
            
            
        }
        private Vector2 GetWorldMousePosition(Vector2 screenPosition)
        {
            if (!_camera) return Vector2.zero;
        
            // 2D 게임에서 정확한 월드 좌표 계산
            // 카메라에서 게임 월드(Z=0)까지의 거리 사용
            float distanceToGameWorld = Mathf.Abs(_camera.transform.position.z);
            Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceToGameWorld));
            return new Vector2(worldPos.x, worldPos.y);
        }        
        private float UpdateLookAngle(Vector2 targetVector)
        {
            targetLookAngle = Mathf.Atan2(targetVector.y, targetVector.x) * Mathf.Rad2Deg * lookSensitivity;
        
            if (enableLookSmoothing)
            {
                currentLookAngle = Mathf.LerpAngle(currentLookAngle, targetLookAngle, 
                    Time.deltaTime * lookSmoothing);
                return currentLookAngle;
            }
            else
            {
                return targetLookAngle;
            }
        }
        public void Dispose() { }
        public void LateUpdate() { }

    }
}