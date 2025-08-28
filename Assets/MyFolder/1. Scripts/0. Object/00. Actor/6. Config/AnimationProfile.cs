using UnityEngine;

namespace MyFolder._1._Scripts._00._Actor._6._Config
{
    /// <summary>
    /// 애니메이션 프로필
    /// </summary>
    [CreateAssetMenu(fileName = "New Animation Profile", menuName = "Actor/Animation Profile", order = 4)]
    public class AnimationProfile : ScriptableObject
    {
        [Header("Animator Controller")]
        [SerializeField] private RuntimeAnimatorController animatorController;
        
        [Header("Animation Parameters")]
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private string isMovingParam = "IsMoving";
        [SerializeField] private string lookAngleParam = "LookAngle";
        [SerializeField] private string isFireParam = "IsFiring";
        [SerializeField] private string reloadTriggerParam = "Reload";
        [SerializeField] private string deathTriggerParam = "Death";
        [SerializeField] private string hurtTriggerParam = "Hurt";
        
        [Header("Sprite Settings")]
        [SerializeField] private bool useFlipX = true; // SpriteRenderer.flipX 사용 여부
        [SerializeField] private bool flipOnNegativeX = true;
        
        [Header("Animation Speeds")]
        [SerializeField] private float idleAnimSpeed = 1f;
        [SerializeField] private float walkAnimSpeed = 1f;
        [SerializeField] private float runAnimSpeed = 1f;
        [SerializeField] private float fireAnimSpeed = 1f;

        // 속성
        public RuntimeAnimatorController AnimatorController => animatorController;
        public string MoveSpeedParam => moveSpeedParam;
        public string IsMovingParam => isMovingParam;
        public string LookAngleParam => lookAngleParam;
        public string IsFireParam => isFireParam;
        public string ReloadTriggerParam => reloadTriggerParam;
        public string DeathTriggerParam => deathTriggerParam;
        public string HurtTriggerParam => hurtTriggerParam;
        public bool UseFlipX => useFlipX;
        public bool FlipOnNegativeX => flipOnNegativeX;
        public float IdleAnimSpeed => idleAnimSpeed;
        public float WalkAnimSpeed => walkAnimSpeed;
        public float RunAnimSpeed => runAnimSpeed;
        public float FireAnimSpeed => fireAnimSpeed;

        private void OnValidate()
        {
            // 유효성 검사
            idleAnimSpeed = Mathf.Max(0.1f, idleAnimSpeed);
            walkAnimSpeed = Mathf.Max(0.1f, walkAnimSpeed);
            runAnimSpeed = Mathf.Max(0.1f, runAnimSpeed);
            fireAnimSpeed = Mathf.Max(0.1f, fireAnimSpeed);
        }

        /// <summary>
        /// 이동 속도에 따른 애니메이션 속도 계산
        /// </summary>
        public float GetAnimationSpeed(float moveSpeed, float maxSpeed)
        {
            if (maxSpeed <= 0f) return idleAnimSpeed;
            
            float speedRatio = moveSpeed / maxSpeed;
            
            if (speedRatio <= 0.1f)
                return idleAnimSpeed;
            else if (speedRatio <= 0.6f)
                return Mathf.Lerp(idleAnimSpeed, walkAnimSpeed, speedRatio / 0.6f);
            else
                return Mathf.Lerp(walkAnimSpeed, runAnimSpeed, (speedRatio - 0.6f) / 0.4f);
        }

        /// <summary>
        /// 각도에 따른 스프라이트 플립 여부 결정
        /// </summary>
        public bool ShouldFlipSprite(float lookAngle)
        {
            if (!useFlipX) return false;
            
            if (flipOnNegativeX)
            {
                // -90도 ~ 90도 범위에서 플립 결정
                return lookAngle > 90f || lookAngle < -90f;
            }
            
            return lookAngle < 0f;
        }

        /// <summary>
        /// 파라미터 존재 여부 확인
        /// </summary>
        public bool HasParameter(Animator animator, string paramName)
        {
            if (animator == null || string.IsNullOrEmpty(paramName)) return false;
            
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            
            return false;
        }

        public override string ToString()
        {
            return $"AnimationProfile: {(animatorController != null ? animatorController.name : "None")}";
        }
    }
}
