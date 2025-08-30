using System.Collections.Generic;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._5._Builder;
using MyFolder._1._Scripts._00._Actor._6._Config;
using MyFolder._1._Scripts._00._Actor._8._Authoring;
using UnityEngine;


namespace MyFolder._1._Scripts._0._Object._00._Actor._8._Authoring
{
    /// <summary>
    /// Unity에서 Actor 초기화를 담당하는 Authoring 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public class ActorAuthoring : NetworkBehaviour
    {
        [Header("Actor Configuration")]
        [SerializeField] private ActorPreset actorPreset;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool debugMode = false;

        [Header("Manual Components")]
        [SerializeField] private bool useManualComponents = true;
        [SerializeField] private List<ComponentTypeSelector> manualComponents = new List<ComponentTypeSelector>();

        [Header("Runtime Overrides")]
        [SerializeField] private HealthConfig healthOverride;
        [SerializeField] private MovementConfig movementOverride;
        [SerializeField] private ShooterConfig shooterOverride;

        private Actor _actor;

        private void Awake()
        {
            _actor = GetComponent<Actor>();
            
            if (_actor == null)
            {
                Debug.LogError($"[ActorAuthoring] Actor component not found on {gameObject.name}");
                enabled = false;
                return;
            }
        }

        public override void OnStartServer()
        {
            if (buildOnStart)
            {
                BuildServerComponents();
            }
            
            if (debugMode)
            {
                Debug.Log($"[ActorAuthoring] Server started for {gameObject.name}");
            }
        }

        public override void OnStartClient()
        {
            if (buildOnStart)
            {
                BuildClientComponents();
            }
            
            if (debugMode)
            {
                Debug.Log($"[ActorAuthoring] Client started for {gameObject.name} (IsOwner: {IsOwner})");
            }
        }

        /// <summary>
        /// 서버용 컴포넌트 구성
        /// </summary>
        private void BuildServerComponents()
        {
            if (actorPreset == null)
            {
                Debug.LogWarning($"[ActorAuthoring] ActorPreset is null for {gameObject.name}");
                return;
            }

            // 오버라이드 적용된 프리셋 생성
            var runtimePreset = CreateRuntimePreset();
            
            // 서버 컴포넌트 빌드
            ActorBuilder.BuildServer(_actor, runtimePreset);

            // 수동 컴포넌트 추가
            BuildManualComponents();
            
            if (debugMode)
            {
                ActorBuilder.DebugComponents(_actor);
            }
        }

        /// <summary>
        /// 클라이언트용 컴포넌트 구성
        /// </summary>
        private void BuildClientComponents()
        {
            if (actorPreset == null) return;

            var runtimePreset = CreateRuntimePreset();
            
            // 클라이언트 뷰 컴포넌트
            ActorBuilder.BuildClientView(_actor, runtimePreset);
            
            // Owner 전용 입력
            if (IsOwner)
            {
                ActorBuilder.EnableOwnerInput(_actor);
            }

            // 수동 컴포넌트 추가
            BuildManualComponents();
            
            if (debugMode)
            {
                Debug.Log($"[ActorAuthoring] Client build complete (Owner: {IsOwner})");
            }
        }

        /// <summary>
        /// 런타임 오버라이드가 적용된 프리셋 생성
        /// </summary>
        private ActorPreset CreateRuntimePreset()
        {
            var runtimePreset = actorPreset.Clone();
            
            // 오버라이드 적용
            if (healthOverride != null)
                runtimePreset.HealthConfig = healthOverride;
            if (movementOverride != null)
                runtimePreset.MovementConfig = movementOverride;
            if (shooterOverride != null)
                runtimePreset.ShooterConfig = shooterOverride;
            
            return runtimePreset;
        }

        /// <summary>
        /// 수동으로 컴포넌트 빌드
        /// </summary>
        [ContextMenu("Build Components")]
        public void ManualBuild()
        {
            if (_actor == null || actorPreset == null) return;
            
            var runtimePreset = CreateRuntimePreset();
            ActorBuilder.BuildComplete(_actor, runtimePreset);

            // 수동 컴포넌트 추가
            BuildManualComponents();
            
            Debug.Log($"[ActorAuthoring] Manual build complete for {gameObject.name}");
        }

        /// <summary>
        /// 액터 프리셋 설정
        /// </summary>
        public void SetActorPreset(ActorPreset preset)
        {
            actorPreset = preset;
            
            if (debugMode)
            {
                Debug.Log($"[ActorAuthoring] ActorPreset changed to {preset?.name}");
            }
        }

        /// <summary>
        /// 오버라이드 설정
        /// </summary>
        public void SetHealthOverride(HealthConfig config) => healthOverride = config;
        public void SetMovementOverride(MovementConfig config) => movementOverride = config;
        public void SetShooterOverride(ShooterConfig config) => shooterOverride = config;

        private void OnValidate()
        {
            // 에디터에서 유효성 검사
            if (actorPreset == null)
            {
                Debug.LogWarning($"[ActorAuthoring] ActorPreset is not assigned on {gameObject.name}");
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug Actor Info")]
        private void DebugActorInfo()
        {
            if (_actor == null) return;
            
            Debug.Log($"=== Actor Info: {gameObject.name} ===");
            Debug.Log($"Preset: {actorPreset?.name ?? "None"}");
            Debug.Log($"IsServer: {IsServer}");
            Debug.Log($"IsClient: {IsClient}");
            Debug.Log($"IsOwner: {IsOwner}");
            
            ActorBuilder.DebugComponents(_actor);
        }
        #endif

        /// <summary>
        /// 수동으로 선택된 컴포넌트들을 인스턴스화하여 등록합니다.
        /// </summary>
        private void BuildManualComponents()
        {
            if (!useManualComponents) return;
            if (manualComponents == null || manualComponents.Count == 0) return;

            foreach (var selector in manualComponents)
            {
                if (selector == null) continue;
                
                var componentType = selector.GetSelectedType();
                if (componentType == null) continue;

                var instance = ActorComponentReflection.CreateInstance(componentType);
                if (instance != null)
                {
                    _actor.AddComponent(instance);
                    if (debugMode)
                    {
                        Debug.Log($"[ActorAuthoring] Added manual component: {componentType.Name}");
                    }
                }
                else if (debugMode)
                {
                    Debug.LogWarning($"[ActorAuthoring] Failed to create instance of {componentType.Name}");
                }
            }
        }
    }
}
