using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._3._Network
{
    /// <summary>
    /// 액터의 네트워크 동기화를 관리하는 허브
    /// </summary>
    public class ActorNetworkSync : NetworkBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private float syncRate = 20f; // Hz

        private readonly List<IActorNetSync> _modules = new List<IActorNetSync>();
        private uint _dirtyMask = 0u;
        private float _lastSyncTime = 0f;
        private Actor _actor;

        protected virtual void Awake()
        {
            _actor = GetComponent<Actor>();
        }

        public override void OnStartServer()
        {
            if (debugMode)
                Debug.Log($"[ActorNetworkSync] Server started for {gameObject.name}");
        }

        public override void OnStartClient()
        {
            if (debugMode)
                Debug.Log($"[ActorNetworkSync] Client started for {gameObject.name}");
        }

        /// <summary>
        /// 동기화 모듈 등록
        /// </summary>
        public void RegisterModule(IActorNetSync module)
        {
            if (module == null) return;

            module.OnRegister(this);
            _modules.Add(module);
            
            // 우선순위로 정렬
            _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            if (debugMode)
                Debug.Log($"[ActorNetworkSync] Registered module: {module.GetType().Name} (ID: {module.ComponentId})");
        }

        /// <summary>
        /// 동기화 모듈 제거
        /// </summary>
        public void UnregisterModule(IActorNetSync module)
        {
            if (module == null) return;

            _modules.Remove(module);

            if (debugMode)
                Debug.Log($"[ActorNetworkSync] Unregistered module: {module.GetType().Name}");
        }

        /// <summary>
        /// Actor 컴포넌트 해석
        /// </summary>
        public bool TryResolve<T>(out T component) where T : class
        {
            if (_actor != null)
                return _actor.TryResolve(out component);
            
            component = null;
            return false;
        }

        private void LateUpdate()
        {
            if (!IsServer) return;

            // 동기화 레이트 체크
            if (Time.time - _lastSyncTime < 1f / syncRate)
                return;

            SynchronizeState();
            _lastSyncTime = Time.time;
        }

        private void SynchronizeState()
        {
            // 더티 모듈 체크
            _dirtyMask = 0u;
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].CaptureState();
                if (_modules[i].IsDirty)
                {
                    _dirtyMask |= (1u << _modules[i].ComponentId);
                }
            }

            // 변경사항이 없으면 전송하지 않음
            if (_dirtyMask == 0u) return;

            // 스냅샷 생성 및 전송
            PooledWriter writer  = WriterPool.Retrieve();
            {
                writer.WriteUInt32(_dirtyMask);

                for (int i = 0; i < _modules.Count; i++)
                {
                    var module = _modules[i];
                    if ((_dirtyMask >> module.ComponentId & 1) != 0)
                    {
                        module.Write(writer);
                    }
                }

                BroadcastState(writer.GetArraySegment());
            }

            // 더티 플래그 초기화
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].ClearDirty();
            }

            if (debugMode)
                Debug.Log($"[ActorNetworkSync] Broadcasted state (mask: {_dirtyMask:X})");
        }

        [ObserversRpc(ExcludeOwner = false)]
        private void BroadcastState(ArraySegment<byte> data)
        {
            PooledReader reader = ReaderPool.Retrieve(data,NetworkManager);
            ApplySnapshot(reader);
        }

        private void ApplySnapshot(PooledReader reader)
        {
            uint mask = reader.ReadUInt32();

            for (int i = 0; i < _modules.Count; i++)
            {
                var module = _modules[i];
                if ((mask >> module.ComponentId & 1) != 0)
                {
                    module.Read(reader);
                }
            }

            if (debugMode)
                Debug.Log($"[ActorNetworkSync] Applied snapshot (mask: {mask:X})");
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug Modules")]
        private void DebugModules()
        {
            Debug.Log($"[ActorNetworkSync] Modules ({_modules.Count}):");
            foreach (var module in _modules)
            {
                Debug.Log($"  - {module.GetType().Name} (ID: {module.ComponentId}, Priority: {module.Priority}, Dirty: {module.IsDirty})");
            }
        }
        #endif
    }
}
