using FishNet.Serializing;
using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces;
using MyFolder._1._Scripts._0._Object._00._Actor._3._Network;
using MyFolder._1._Scripts._00._Actor._4._Components.Health;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._7._NetworkModules
{
    /// <summary>
    /// 체력 동기화 모듈
    /// </summary>
    public class HealthNetSyncModule : IActorNetSync
    {
        public int ComponentId => 0;
        public int Priority => 10;
        
        private bool _isDirty = false;
        private int _lastSentHp = 0;
        private int _lastSentMaxHp = 0;
        private int _lastReceivedHp = 0;
        
        private ActorNetworkSync _context;
        private HealthComponent _healthComponent;

        public bool IsDirty => _isDirty;

        public void OnRegister(ActorNetworkSync context)
        {
            _context = context;
            
            // HealthComponent 찾기
            if (_context.TryResolve<HealthComponent>(out _healthComponent))
            {
                // 체력 변화 이벤트 구독
                _healthComponent.HpChanged += OnHpChanged;
                
                Debug.Log("[HealthNetSyncModule] Registered with HealthComponent");
            }
            else
            {
                Debug.LogWarning("[HealthNetSyncModule] HealthComponent not found");
            }
        }

        private void OnHpChanged(int current, int max)
        {
            _isDirty = true;
        }

        public void CaptureState()
        {
            if (_healthComponent == null) return;
            
            // 서버에서 현재 상태 캡처
            _lastSentHp = _healthComponent.CurrentHp;
            _lastSentMaxHp = _healthComponent.MaxHp;
        }

        public void Write(PooledWriter writer)
        {
            // 서버 → 클라이언트 직렬화
            writer.WriteInt32(_lastSentHp);
            writer.WriteInt32(_lastSentMaxHp);
            
            Debug.Log($"[HealthNetSyncModule] Sent HP: {_lastSentHp}/{_lastSentMaxHp}");
        }

        public void Read(PooledReader reader)
        {
            // 클라이언트에서 수신
            int receivedHp = reader.ReadInt32();
            int receivedMaxHp = reader.ReadInt32();
            
            Debug.Log($"[HealthNetSyncModule] Received HP: {receivedHp}/{receivedMaxHp}");
            
            // HUD 갱신 (직접 처리)
            UpdateClientHUD(receivedHp, receivedMaxHp);
            
            // 피격 이펙트 (HP 감소 시)
            if (receivedHp < _lastReceivedHp && _lastReceivedHp > 0)
            {
                PlayHitEffect();
            }
            
            // 사망 이펙트 (HP가 0이 된 경우)
            if (receivedHp <= 0 && _lastReceivedHp > 0)
            {
                PlayDeathEffect();
            }
            
            _lastReceivedHp = receivedHp;
        }

        public void ClearDirty()
        {
            _isDirty = false;
        }

        private void UpdateClientHUD(int currentHp, int maxHp)
        {
            // HUD 컴포넌트가 있다면 직접 갱신
            if (_context.TryResolve<HUDComponent>(out var hud))
            {
                hud.UpdateHealthBar(currentHp, maxHp);
            }
            
            // EventBus를 통한 알림도 가능
            if (_context.TryResolve<Actor>(out var actor))
            {
                actor.EventBus?.PublishHpChanged(currentHp, maxHp);
            }
        }

        private void PlayHitEffect()
        {
            // 피격 이펙트 재생
            // TODO: 파티클, 사운드, 화면 흔들림 등
            Debug.Log("[HealthNetSyncModule] Playing hit effect");
        }

        private void PlayDeathEffect()
        {
            // 사망 이펙트 재생
            // TODO: 사망 애니메이션, 사운드, 파티클 등
            Debug.Log("[HealthNetSyncModule] Playing death effect");
        }
    }

    /// <summary>
    /// HUD 컴포넌트 인터페이스 (임시)
    /// </summary>
    public interface HUDComponent
    {
        void UpdateHealthBar(int current, int max);
    }
}
