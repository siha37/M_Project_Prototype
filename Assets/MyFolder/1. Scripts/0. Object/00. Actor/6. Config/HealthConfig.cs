using UnityEngine;

namespace MyFolder._1._Scripts._00._Actor._6._Config
{
    /// <summary>
    /// 체력 관련 설정
    /// </summary>
    [CreateAssetMenu(fileName = "New Health Config", menuName = "Actor/Health Config", order = 1)]
    public class HealthConfig : ScriptableObject
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int startHp = 100;
        
        [Header("Damage Settings")]
        [SerializeField] private float invincibilityDuration = 1f;
        [SerializeField] private bool canHeal = true;
        [SerializeField] private int maxHealAmount = 50;
        
        [Header("Death Settings")]
        [SerializeField] private float deathDelay = 2f;
        [SerializeField] private bool respawnEnabled = false;
        [SerializeField] private float respawnTime = 5f;

        // 속성
        public int MaxHp => maxHp;
        public int StartHp => startHp;
        public float InvincibilityDuration => invincibilityDuration;
        public bool CanHeal => canHeal;
        public int MaxHealAmount => maxHealAmount;
        public float DeathDelay => deathDelay;
        public bool RespawnEnabled => respawnEnabled;
        public float RespawnTime => respawnTime;

        private void OnValidate()
        {
            // 유효성 검사
            maxHp = Mathf.Max(1, maxHp);
            startHp = Mathf.Clamp(startHp, 1, maxHp);
            invincibilityDuration = Mathf.Max(0f, invincibilityDuration);
            maxHealAmount = Mathf.Max(1, maxHealAmount);
            deathDelay = Mathf.Max(0f, deathDelay);
            respawnTime = Mathf.Max(0f, respawnTime);
        }

        /// <summary>
        /// 체력 비율로 최대 체력 계산
        /// </summary>
        public int GetScaledMaxHp(float multiplier)
        {
            return Mathf.RoundToInt(maxHp * multiplier);
        }

        /// <summary>
        /// 난이도에 따른 체력 조정
        /// </summary>
        public int GetDifficultyAdjustedHp(float difficultyMultiplier)
        {
            return GetScaledMaxHp(difficultyMultiplier);
        }

        public override string ToString()
        {
            return $"HealthConfig: {maxHp}HP (Start: {startHp})";
        }
    }
}
