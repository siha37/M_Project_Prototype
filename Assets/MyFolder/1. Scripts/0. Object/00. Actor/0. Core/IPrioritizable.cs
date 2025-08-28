namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 우선순위를 제공하는 인터페이스
    /// </summary>
    public interface IPrioritizable
    {
        /// <summary>
        /// 실행 우선순위 (낮을수록 먼저 실행)
        /// 0: Highest (Input)
        /// 5: AI Controller
        /// 10: Movement
        /// 20: Combat/Shooter
        /// 90: UI/Animation
        /// </summary>
        int Priority { get; }
    }
}
