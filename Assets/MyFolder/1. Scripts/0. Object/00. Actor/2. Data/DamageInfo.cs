using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._2._Data
{
    /// <summary>
    /// 피해 정보를 담는 구조체
    /// </summary>
    [System.Serializable]
    public struct DamageInfo
    {
        /// <summary>
        /// 피해를 가한 액터
        /// </summary>
        public Actor Source;

        /// <summary>
        /// 피해량
        /// </summary>
        public int Amount;

        /// <summary>
        /// 피격 지점
        /// </summary>
        public Vector2 HitPoint;

        /// <summary>
        /// 피해 타입 (총알, 폭발, 근접 등)
        /// </summary>
        public string DamageType;

        /// <summary>
        /// 관통 여부
        /// </summary>
        public bool IsPenetrating;

        /// <summary>
        /// 크리티컬 히트 여부
        /// </summary>
        public bool IsCritical;

        /// <summary>
        /// 추가 상태 효과 (독, 화상 등)
        /// </summary>
        public string StatusEffect;

        public DamageInfo(Actor source, int amount, Vector2 hitPoint, string damageType = "Default")
        {
            Source = source;
            Amount = amount;
            HitPoint = hitPoint;
            DamageType = damageType;
            IsPenetrating = false;
            IsCritical = false;
            StatusEffect = null;
        }

        public override string ToString()
        {
            return $"Damage: {Amount} ({DamageType}) from {Source?.name} at {HitPoint}";
        }
    }
}
