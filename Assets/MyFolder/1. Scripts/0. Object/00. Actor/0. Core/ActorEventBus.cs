using System;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 액터 내부 컴포넌트 간 이벤트 통신을 위한 이벤트 버스
    /// </summary>
    public sealed class ActorEventBus
    {
        // 이동 관련
        public event Action<Vector2> MoveRequested;
        public event Action<Vector3> MoveToRequested;

        // 조준 관련
        public event Action<Vector2> LookRequested;

        // 전투 관련
        public event Action FireStarted;
        public event Action FireCanceled;
        public event Action ReloadRequested;

        // 상호작용
        public event Action InteractStarted;
        public event Action InteractCanceled;

        // 상태 변화
        public event Action<int, int> HpChanged; // current, max
        public event Action<int, int> AmmoChanged; // current, reserve

        // 스킬/특수능력
        public event Action<bool> CamouflageToggled;

        // === 발행 메서드들 ===

        public void PublishMove(Vector2 direction)
        {
            MoveRequested?.Invoke(direction);
        }

        public void PublishMoveTo(Vector3 worldPosition)
        {
            MoveToRequested?.Invoke(worldPosition);
        }

        public void PublishLook(Vector2 lookInput)
        {
            LookRequested?.Invoke(lookInput);
        }

        public void PublishFireStart()
        {
            FireStarted?.Invoke();
        }

        public void PublishFireCancel()
        {
            FireCanceled?.Invoke();
        }

        public void PublishReload()
        {
            ReloadRequested?.Invoke();
        }

        public void PublishInteractStart()
        {
            InteractStarted?.Invoke();
        }

        public void PublishInteractCancel()
        {
            InteractCanceled?.Invoke();
        }

        public void PublishHpChanged(int current, int max)
        {
            HpChanged?.Invoke(current, max);
        }

        public void PublishAmmoChanged(int current, int reserve)
        {
            AmmoChanged?.Invoke(current, reserve);
        }

        public void PublishCamouflageToggle(bool isActive)
        {
            CamouflageToggled?.Invoke(isActive);
        }

        /// <summary>
        /// 모든 이벤트 구독 해제 (디버깅용)
        /// </summary>
        public void ClearAllSubscriptions()
        {
            MoveRequested = null;
            MoveToRequested = null;
            LookRequested = null;
            FireStarted = null;
            FireCanceled = null;
            ReloadRequested = null;
            InteractStarted = null;
            InteractCanceled = null;
            HpChanged = null;
            AmmoChanged = null;
            CamouflageToggled = null;
        }
    }
}
