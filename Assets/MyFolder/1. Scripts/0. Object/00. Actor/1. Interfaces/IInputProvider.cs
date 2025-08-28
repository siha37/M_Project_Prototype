using System;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces
{
    /// <summary>
    /// 입력 이벤트를 제공하는 인터페이스
    /// </summary>
    public interface IInputProvider
    {
        // 이동 입력
        event Action<Vector2> MoveRequested;

        // 조준 입력
        event Action<Vector2> LookRequested;

        // 전투 입력
        event Action FireStarted;
        event Action FireCanceled;
        event Action ReloadRequested;

        // 상호작용 입력
        event Action InteractStarted;
        event Action InteractCanceled;

        // 스킬 입력
        event Action Skill1Started;
        event Action Skill1Canceled;
    }
}
