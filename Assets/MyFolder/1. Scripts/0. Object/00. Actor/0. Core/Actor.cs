using System;
using System.Collections.Generic;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._00._Actor._3._Network;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 액터의 기본 클래스. 컴포넌트들을 소유하고 관리한다.
    /// </summary>
    public abstract class Actor : NetworkBehaviour
    {
        [Header("Actor Components")]
        [SerializeField] private bool debugMode = false;

        // 컴포넌트 관리
        private readonly List<IActorComponent> _components = new List<IActorComponent>();
        private readonly Dictionary<Type, object> _typeMap = new Dictionary<Type, object>();

        // 인프라
        private ActorEventBus _eventBus;
        private UpdateScheduler _scheduler;
        private ActorNetworkSync _networkSync;

        public ActorEventBus EventBus => _eventBus;
        public UpdateScheduler Scheduler => _scheduler;
        public ActorNetworkSync NetworkSync => _networkSync;

        protected virtual void Awake()
        {
            InitializeInfrastructure();
        }

        private void InitializeInfrastructure()
        {
            _eventBus = new ActorEventBus();
            _scheduler = new UpdateScheduler();
            
            // NetworkSync는 이미 컴포넌트로 붙어있을 수 있음
            TryGetComponent(out _networkSync);
            if (_networkSync == null)
            {
                _networkSync = gameObject.AddComponent<ActorNetworkSync>();
            }
        }

        /// <summary>
        /// 컴포넌트 추가
        /// </summary>
        public T AddComponent<T>(T component) where T : IActorComponent
        {
            if (component == null) return default(T);

            // 초기화
            component.Init(this);
            component.OnEnable();
            
            // 등록
            _components.Add(component);
            _typeMap[typeof(T)] = component;

            // 인터페이스 매핑
            var interfaces = component.GetType().GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (!_typeMap.ContainsKey(interfaceType))
                {
                    _typeMap[interfaceType] = component;
                }
            }

            // 스케줄러에 등록
            if (component is IActorUpdatable updatable)
            {
                _scheduler.Add(updatable);
            }

            if (debugMode)
            {
                Debug.Log($"[Actor] Added component: {typeof(T).Name}");
            }

            return component;
        }

        /// <summary>
        /// 컴포넌트 제거
        /// </summary>
        public void RemoveComponent<T>(T component) where T : IActorComponent
        {
            if (component == null) return;

            // 스케줄러에서 제거
            if (component is IActorUpdatable updatable)
            {
                _scheduler.Remove(updatable);
            }

            // 등록 해제
            _components.Remove(component);
            _typeMap.Remove(typeof(T));

            // 정리
            component.OnDisable();
            component.Dispose();

            if (debugMode)
            {
                Debug.Log($"[Actor] Removed component: {typeof(T).Name}");
            }
        }

        /// <summary>
        /// 타입으로 컴포넌트 찾기
        /// </summary>
        public bool TryResolve<T>(out T component) where T : class
        {
            if (_typeMap.TryGetValue(typeof(T), out var obj))
            {
                component = obj as T;
                return component != null;
            }
            component = null;
            return false;
        }

        /// <summary>
        /// 컴포넌트 존재 확인
        /// </summary>
        public bool HasComponent<T>() where T : class
        {
            return _typeMap.ContainsKey(typeof(T));
        }

        private void Update()
        {
            _scheduler.RunUpdate();
        }

        private void FixedUpdate()
        {
            _scheduler.RunFixedUpdate();
        }

        private void LateUpdate()
        {
            _scheduler.RunLateUpdate();
        }

        protected virtual void OnDestroy()
        {
            // 모든 컴포넌트 정리
            for (int i = _components.Count - 1; i >= 0; i--)
            {
                var component = _components[i];
                component.OnDisable();
                component.Dispose();
            }
            
            _components.Clear();
            _typeMap.Clear();
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug Components")]
        private void DebugComponents()
        {
            Debug.Log($"[Actor] Components ({_components.Count}):");
            foreach (var component in _components)
            {
                Debug.Log($"  - {component.GetType().Name}");
            }
        }
        #endif
    }
}
