using MyFolder._1._Scripts._0._Object._00._Actor._2._Data;

namespace MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces
{
    /// <summary>
    /// 피해를 받을 수 있는 객체를 나타내는 인터페이스
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 피해 적용
        /// </summary>
        /// <param name="damageInfo">피해 정보</param>
        void ApplyDamage(DamageInfo damageInfo);

        /// <summary>
        /// 현재 체력
        /// </summary>
        int CurrentHp { get; }

        /// <summary>
        /// 최대 체력
        /// </summary>
        int MaxHp { get; }

        /// <summary>
        /// 생존 여부
        /// </summary>
        bool IsAlive { get; }
    }
}
