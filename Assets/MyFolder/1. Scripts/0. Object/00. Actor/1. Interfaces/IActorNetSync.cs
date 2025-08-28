using FishNet.Serializing;
using MyFolder._1._Scripts._0._Object._00._Actor._3._Network;

namespace MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces
{
    /// <summary>
    /// 액터 네트워크 동기화 모듈을 위한 인터페이스
    /// </summary>
    public interface IActorNetSync
    {
        /// <summary>
        /// 컴포넌트 ID (더티 마스크용)
        /// </summary>
        int ComponentId { get; }

        /// <summary>
        /// 동기화 우선순위
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 동기화가 필요한지 여부
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// 네트워크 동기화 컨텍스트에 등록될 때 호출
        /// </summary>
        /// <param name="context">네트워크 동기화 컨텍스트</param>
        void OnRegister(ActorNetworkSync context);

        /// <summary>
        /// 현재 상태를 캡처 (서버에서 호출)
        /// </summary>
        void CaptureState();

        /// <summary>
        /// 상태를 직렬화 (서버 → 클라이언트)
        /// </summary>
        /// <param name="writer">직렬화 Writer</param>
        void Write(PooledWriter writer);

        /// <summary>
        /// 상태를 역직렬화하여 적용 (클라이언트에서 호출)
        /// </summary>
        /// <param name="reader">역직렬화 Reader</param>
        void Read(PooledReader reader);

        /// <summary>
        /// 더티 플래그 초기화
        /// </summary>
        void ClearDirty();
    }
}
