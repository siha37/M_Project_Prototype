using UnityEngine;

namespace MyFolder._1._Scripts._00._Actor._6._Config
{
    /// <summary>
    /// 이동 관련 설정
    /// </summary>
    [CreateAssetMenu(fileName = "New Movement Config", menuName = "Actor/Movement Config", order = 2)]
    public class MovementConfig : ScriptableObject
    {
        [Header("Speed Settings")]
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float deceleration = 30f;
        [SerializeField] private float maxAcceleration = 50f;
        
        [Header("AI Navigation (NavMesh)")]
        [SerializeField] private float stoppingDistance = 0.1f;
        [SerializeField] private float pathUpdateInterval = 0.1f;
        [SerializeField] private float avoidancePriority = 50f;
        
        [Header("Physics Settings")]
        [SerializeField] private float drag = 5f;
        [SerializeField] private float angularDrag = 5f;
        [SerializeField] private bool freezeRotation = true;

        // 속성
        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float MaxAcceleration => maxAcceleration;
        public float StoppingDistance => stoppingDistance;
        public float PathUpdateInterval => pathUpdateInterval;
        public float AvoidancePriority => avoidancePriority;
        public float Drag => drag;
        public float AngularDrag => angularDrag;
        public bool FreezeRotation => freezeRotation;

        private void OnValidate()
        {
            // 유효성 검사
            maxSpeed = Mathf.Max(0.1f, maxSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            maxAcceleration = Mathf.Max(0.1f, maxAcceleration);
            stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            pathUpdateInterval = Mathf.Max(0.01f, pathUpdateInterval);
            avoidancePriority = Mathf.Clamp(avoidancePriority, 0f, 99f);
            drag = Mathf.Max(0f, drag);
            angularDrag = Mathf.Max(0f, angularDrag);
        }

        /// <summary>
        /// 속도 배율 적용
        /// </summary>
        public float GetScaledSpeed(float multiplier)
        {
            return maxSpeed * multiplier;
        }

        /// <summary>
        /// 상태에 따른 속도 가져오기
        /// </summary>
        public float GetSpeedByState(MovementState state)
        {
            return state switch
            {
                MovementState.Walk => maxSpeed * 0.5f,
                MovementState.Run => maxSpeed,
                MovementState.Sprint => maxSpeed * 1.5f,
                MovementState.Sneak => maxSpeed * 0.25f,
                _ => maxSpeed
            };
        }

        public override string ToString()
        {
            return $"MovementConfig: {maxSpeed}u/s (Accel: {acceleration})";
        }
    }

    /// <summary>
    /// 이동 상태
    /// </summary>
    public enum MovementState
    {
        Idle,
        Walk,
        Run,
        Sprint,
        Sneak
    }
}
