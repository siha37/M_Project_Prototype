using System;
using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces;
using MyFolder._1._Scripts._0._Object._00._Actor._2._Data;
using UnityEngine;

namespace MyFolder._1._Scripts._00._Actor._4._Components.Health
{
    /// <summary>
    /// 체력 관리 컴포넌트
    /// </summary>
    public class HealthComponent : IActorComponent, IActorUpdatable, IDamageable, IConfigurable<HealthSettings>
    {
        public int Priority => 50; // 중간 우선순위

        // 체력 상태
        private int _currentHp;
        private int _maxHp;
        private bool _isDirty = false;
        private bool _isInvincible = false;
        private float _invincibilityEndTime = 0f;

        // 참조
        private Actor _owner;
        private ActorEventBus _eventBus;

        // 이벤트
        public event Action<int, int> HpChanged; // current, max
        public event Action<DamageInfo> DamageTaken;
        public event Action Death;

        // 속성
        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public bool IsAlive => _currentHp > 0;
        public bool IsDirty => _isDirty;
        public float HealthPercentage => _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;

        public void Init(Actor actor)
        {
            _owner = actor;
            _eventBus = actor.EventBus;
        }

        public void OnEnable()
        {
            // 필요한 경우 이벤트 구독
        }

        public void OnDisable()
        {
            // 이벤트 구독 해제
        }

        public void ApplyConfig(in HealthSettings settings)
        {
            _maxHp = settings.MaxHp;
            _currentHp = settings.StartHp;
            
            // 이벤트 발행
            NotifyHpChanged();
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            // 서버에서만 체력 변경
            if (!_owner.IsServer) return;
            
            // 무적 상태 확인
            if (_isInvincible && Time.time < _invincibilityEndTime)
                return;

            // 이미 죽어있으면 무시
            if (!IsAlive) return;

            int previousHp = _currentHp;
            _currentHp = Mathf.Max(0, _currentHp - damageInfo.Amount);
            
            if (previousHp != _currentHp)
            {
                _isDirty = true;
                
                // 이벤트 발행
                DamageTaken?.Invoke(damageInfo);
                NotifyHpChanged();
                
                Debug.Log($"[HealthComponent] {_owner.name} took {damageInfo.Amount} damage. HP: {previousHp} → {_currentHp}");
                
                // 사망 처리
                if (_currentHp <= 0)
                {
                    OnDeath();
                }
                // 무적 시간 적용 (크리티컬이 아닌 경우)
                else if (!damageInfo.IsCritical)
                {
                    SetInvincible(1f); // 1초 무적
                }
            }
        }

        public void Heal(int amount)
        {
            if (!_owner.IsServer || !IsAlive) return;

            int previousHp = _currentHp;
            _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
            
            if (previousHp != _currentHp)
            {
                _isDirty = true;
                NotifyHpChanged();
                
                Debug.Log($"[HealthComponent] {_owner.name} healed {amount}. HP: {previousHp} → {_currentHp}");
            }
        }

        public void SetMaxHp(int newMaxHp)
        {
            if (!_owner.IsServer) return;

            int previousMax = _maxHp;
            _maxHp = Mathf.Max(1, newMaxHp);
            
            // 현재 체력이 최대 체력을 초과하면 조정
            if (_currentHp > _maxHp)
            {
                _currentHp = _maxHp;
            }
            
            if (previousMax != _maxHp)
            {
                _isDirty = true;
                NotifyHpChanged();
            }
        }

        public void SetInvincible(float duration)
        {
            _isInvincible = true;
            _invincibilityEndTime = Time.time + duration;
        }

        public void FullHeal()
        {
            if (!_owner.IsServer) return;
            
            if (_currentHp != _maxHp)
            {
                _currentHp = _maxHp;
                _isDirty = true;
                NotifyHpChanged();
            }
        }

        private void OnDeath()
        {
            Death?.Invoke();
            _eventBus?.PublishHpChanged(_currentHp, _maxHp);
            
            Debug.Log($"[HealthComponent] {_owner.name} died");
            
            // 사망 처리는 상위 시스템에서 담당
        }

        private void NotifyHpChanged()
        {
            HpChanged?.Invoke(_currentHp, _maxHp);
            _eventBus?.PublishHpChanged(_currentHp, _maxHp);
        }

        public void ClearDirty()
        {
            _isDirty = false;
        }

        public void Update()
        {
            // 무적 시간 체크
            if (_isInvincible && Time.time >= _invincibilityEndTime)
            {
                _isInvincible = false;
            }
        }

        public void FixedUpdate() { }
        public void LateUpdate() { }
        public void Dispose() { }

        #if UNITY_EDITOR
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugInfo()
        {
            Debug.Log($"[HealthComponent] HP: {_currentHp}/{_maxHp} ({HealthPercentage:P0}), Invincible: {_isInvincible}");
        }
        #endif
    }

    /// <summary>
    /// 체력 설정
    /// </summary>
    [System.Serializable]
    public struct HealthSettings
    {
        public int MaxHp;
        public int StartHp;

        public static HealthSettings Default => new HealthSettings
        {
            MaxHp = 100,
            StartHp = 100
        };
    }
}
