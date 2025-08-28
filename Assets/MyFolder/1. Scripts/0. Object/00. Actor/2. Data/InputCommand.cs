using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._2._Data
{
    /// <summary>
    /// 네트워크를 통해 전송되는 입력 커맨드
    /// </summary>
    [System.Serializable]
    public struct InputCommand
    {
        /// <summary>
        /// 틱 번호
        /// </summary>
        public uint Tick;

        /// <summary>
        /// 이동 입력
        /// </summary>
        public Vector2 Move;

        /// <summary>
        /// 조준 입력
        /// </summary>
        public Vector2 Look;

        /// <summary>
        /// 발사 입력
        /// </summary>
        public bool Fire;

        /// <summary>
        /// 재장전 입력
        /// </summary>
        public bool Reload;

        /// <summary>
        /// 상호작용 입력
        /// </summary>
        public bool Interact;

        /// <summary>
        /// 스킬 입력
        /// </summary>
        public bool Skill1;

        public InputCommand(uint tick)
        {
            Tick = tick;
            Move = Vector2.zero;
            Look = Vector2.zero;
            Fire = false;
            Reload = false;
            Interact = false;
            Skill1 = false;
        }

        public override string ToString()
        {
            return $"InputCmd[{Tick}]: Move={Move}, Look={Look}, Fire={Fire}";
        }
    }
}
