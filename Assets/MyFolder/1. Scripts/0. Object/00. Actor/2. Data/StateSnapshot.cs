using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._2._Data
{
    /// <summary>
    /// 네트워크를 통해 전송되는 상태 스냅샷
    /// </summary>
    [System.Serializable]
    public struct StateSnapshot
    {
        /// <summary>
        /// 틱 번호
        /// </summary>
        public uint Tick;

        /// <summary>
        /// 위치
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 회전각 (Z축)
        /// </summary>
        public float Angle;

        /// <summary>
        /// 속도
        /// </summary>
        public Vector2 Velocity;

        /// <summary>
        /// 체력
        /// </summary>
        public int Health;

        /// <summary>
        /// 최대 체력
        /// </summary>
        public int MaxHealth;

        public StateSnapshot(uint tick, Vector2 position, float angle)
        {
            Tick = tick;
            Position = position;
            Angle = angle;
            Velocity = Vector2.zero;
            Health = 0;
            MaxHealth = 0;
        }

        public override string ToString()
        {
            return $"StateSnapshot[{Tick}]: Pos={Position}, Angle={Angle:F1}°, HP={Health}/{MaxHealth}";
        }
    }
}
