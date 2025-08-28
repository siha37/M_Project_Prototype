using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces
{
    /// <summary>
    /// 이동 가능한 객체를 나타내는 인터페이스
    /// </summary>
    public interface IMovable
    {
        /// <summary>
        /// 방향으로 이동 설정 (플레이어용)
        /// </summary>
        /// <param name="direction">이동 방향 벡터</param>
        void SetDirection(Vector2 direction);

        /// <summary>
        /// 목적지로 이동 설정 (AI용)
        /// </summary>
        /// <param name="destination">목적지 월드 좌표</param>
        void SetDestination(Vector3 destination);

        /// <summary>
        /// 현재 이동 속도
        /// </summary>
        float CurrentSpeed { get; }

        /// <summary>
        /// 이동 중인지 여부
        /// </summary>
        bool IsMoving { get; }

        /// <summary>
        /// 이동 정지
        /// </summary>
        void Stop();
    }
}
