using UnityEngine;

namespace MyFolder._1._Scripts._00._Actor._6._Config
{
    /// <summary>
    /// AI 인지 관련 설정
    /// </summary>
    [CreateAssetMenu(fileName = "New Perception Config", menuName = "Actor/Perception Config", order = 5)]
    public class PerceptionConfig : ScriptableObject
    {
        [Header("Vision Settings")]
        [SerializeField] private float sightRange = 15f;
        [SerializeField] private float fieldOfViewAngle = 60f;
        [SerializeField] private LayerMask obstacleLayerMask = -1;
        [SerializeField] private LayerMask targetLayerMask = -1;
        
        [Header("Hearing Settings")]
        [SerializeField] private float hearingRange = 10f;
        [SerializeField] private bool canHearFootsteps = true;
        [SerializeField] private bool canHearGunshots = true;
        
        [Header("Update Settings")]
        [SerializeField] private float perceptionUpdateInterval = 0.1f;
        [SerializeField] private float lostTargetTime = 3f; // 타겟을 놓친 후 추적 유지 시간
        
        [Header("Alert Settings")]
        [SerializeField] private float alertRadius = 5f; // 다른 AI에게 알림 반경
        [SerializeField] private bool canAlertOthers = true;
        [SerializeField] private float alertDuration = 10f;

        // 속성
        public float SightRange => sightRange;
        public float FieldOfViewAngle => fieldOfViewAngle;
        public LayerMask ObstacleLayerMask => obstacleLayerMask;
        public LayerMask TargetLayerMask => targetLayerMask;
        public float HearingRange => hearingRange;
        public bool CanHearFootsteps => canHearFootsteps;
        public bool CanHearGunshots => canHearGunshots;
        public float PerceptionUpdateInterval => perceptionUpdateInterval;
        public float LostTargetTime => lostTargetTime;
        public float AlertRadius => alertRadius;
        public bool CanAlertOthers => canAlertOthers;
        public float AlertDuration => alertDuration;

        private void OnValidate()
        {
            // 유효성 검사
            sightRange = Mathf.Max(1f, sightRange);
            fieldOfViewAngle = Mathf.Clamp(fieldOfViewAngle, 10f, 360f);
            hearingRange = Mathf.Max(0f, hearingRange);
            perceptionUpdateInterval = Mathf.Max(0.01f, perceptionUpdateInterval);
            lostTargetTime = Mathf.Max(0f, lostTargetTime);
            alertRadius = Mathf.Max(0f, alertRadius);
            alertDuration = Mathf.Max(0f, alertDuration);
        }

        /// <summary>
        /// 거리에 따른 시야 정확도 계산
        /// </summary>
        public float GetSightAccuracy(float distance)
        {
            if (distance >= sightRange) return 0f;
            
            // 거리에 따른 정확도 감소 (제곱 감소)
            float distanceRatio = distance / sightRange;
            return 1f - (distanceRatio * distanceRatio);
        }

        /// <summary>
        /// 각도가 시야각 내에 있는지 확인
        /// </summary>
        public bool IsWithinFieldOfView(Vector2 directionToTarget, Vector2 forwardDirection)
        {
            float angle = Vector2.Angle(forwardDirection, directionToTarget);
            return angle <= fieldOfViewAngle * 0.5f;
        }

        /// <summary>
        /// 소음 레벨에 따른 청각 감지 거리 계산
        /// </summary>
        public float GetHearingDistance(float noiseLevel)
        {
            // 소음 레벨 (0~1)에 따른 청각 거리 조정
            return hearingRange * Mathf.Clamp01(noiseLevel);
        }

        /// <summary>
        /// 난이도에 따른 인지 능력 조정
        /// </summary>
        public PerceptionConfig GetDifficultyAdjusted(float difficultyMultiplier)
        {
            var adjusted = Instantiate(this);
            adjusted.sightRange *= difficultyMultiplier;
            adjusted.hearingRange *= difficultyMultiplier;
            adjusted.fieldOfViewAngle = Mathf.Min(360f, fieldOfViewAngle * difficultyMultiplier);
            adjusted.perceptionUpdateInterval /= difficultyMultiplier;
            return adjusted;
        }

        public override string ToString()
        {
            return $"PerceptionConfig: Sight {sightRange}u @ {fieldOfViewAngle}°, Hearing {hearingRange}u";
        }
    }
}
