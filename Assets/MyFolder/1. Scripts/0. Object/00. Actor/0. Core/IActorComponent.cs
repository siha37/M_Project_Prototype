namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 액터 컴포넌트의 기본 수명주기를 정의하는 인터페이스
    /// </summary>
    public interface IActorComponent
    {
        /// <summary>
        /// 컴포넌트 초기화 (Actor에 등록될 때 호출)
        /// </summary>
        void Init(Actor actor);

        /// <summary>
        /// 컴포넌트 활성화
        /// </summary>
        void OnEnable();

        /// <summary>
        /// 컴포넌트 비활성화
        /// </summary>
        void OnDisable();

        /// <summary>
        /// 컴포넌트 정리 (제거 시 호출)
        /// </summary>
        void Dispose();

        /// <summary>
        /// 매 프레임 업데이트
        /// </summary>
        void Update();

        /// <summary>
        /// 물리 업데이트
        /// </summary>
        void FixedUpdate();

        /// <summary>
        /// 후처리 업데이트
        /// </summary>
        void LateUpdate();
    }
}
