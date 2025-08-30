namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 업데이트 파이프라인에 참여하는 컴포넌트를 위한 인터페이스
    /// </summary>
    public interface IActorUpdatable : IPrioritizable
    {
        /// <summary>
        /// 매 프레임 업데이트
        /// </summary>
        public void Update();

        /// <summary>
        /// 물리 업데이트
        /// </summary>
        public void FixedUpdate();

        /// <summary>
        /// 후처리 업데이트
        /// </summary>
        public void LateUpdate();
    }
}
