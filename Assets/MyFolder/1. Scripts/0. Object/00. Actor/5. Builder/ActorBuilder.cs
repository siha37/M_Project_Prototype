using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;
using MyFolder._1._Scripts._0._Object._00._Actor._1._Interfaces;
using MyFolder._1._Scripts._0._Object._00._Actor._4._Components._2._Movement;
using MyFolder._1._Scripts._0._Object._00._Actor._4._Components.Input;
using MyFolder._1._Scripts._0._Object._00._Actor._7._NetworkModules;
using MyFolder._1._Scripts._00._Actor._4._Components.Health;
using MyFolder._1._Scripts._00._Actor._4._Components.Movement;
using MyFolder._1._Scripts._00._Actor._6._Config;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._5._Builder
{
    /// <summary>
    /// 액터 컴포지션을 조립하는 빌더 클래스
    /// </summary>
    public static class ActorBuilder
    {
        /// <summary>
        /// 서버용 게임플레이 컴포넌트 구성
        /// </summary>
        public static void BuildServer(Actor actor, ActorPreset preset)
        {
            if (actor == null || preset == null) return;

            Debug.Log($"[ActorBuilder] Building server components for {actor.name}");

            // 체력 컴포넌트
            if (preset.HealthConfig != null)
            {
                var health = actor.AddComponent(new HealthComponent());
                health.ApplyConfig(new HealthSettings
                {
                    MaxHp = preset.HealthConfig.MaxHp,
                    StartHp = preset.HealthConfig.StartHp
                });

                // 네트워크 동기화 모듈 등록
                var healthSyncModule = new HealthNetSyncModule();
                actor.NetworkSync.RegisterModule(healthSyncModule);
            }

            // 이동 컴포넌트 (플레이어 vs AI)
            if (preset.MovementConfig != null)
            {
                var movementSettings = new MovementSettings
                {
                    MaxSpeed = preset.MovementConfig.MaxSpeed,
                    Acceleration = preset.MovementConfig.Acceleration,
                    Deceleration = preset.MovementConfig.Deceleration,
                    MaxAcceleration = preset.MovementConfig.MaxAcceleration,
                    StoppingDistance = preset.MovementConfig.StoppingDistance
                };

                if (preset.ActorType == ActorType.Player)
                {
                    var playerMove = actor.AddComponent(new PlayerMoveComponent());
                    playerMove.ApplyConfig(movementSettings);
                }
                else if (preset.ActorType == ActorType.AI)
                {
                    var aiMove = actor.AddComponent(new AiMoveComponent());
                    aiMove.ApplyConfig(movementSettings);
                }
            }

            Debug.Log($"[ActorBuilder] Server build complete for {actor.name}");
        }

        /// <summary>
        /// 클라이언트용 뷰 컴포넌트 구성
        /// </summary>
        public static void BuildClientView(Actor actor, ActorPreset preset)
        {
            if (actor == null || preset == null) return;

            Debug.Log($"[ActorBuilder] Building client view components for {actor.name}");

            // HUD 컴포넌트 (필요한 경우)
            // TODO: HUDComponent 구현 후 추가

            // 애니메이션 컴포넌트 (필요한 경우)
            // TODO: AnimationComponent 구현 후 추가

            Debug.Log($"[ActorBuilder] Client view build complete for {actor.name}");
        }

        /// <summary>
        /// Owner용 입력 컴포넌트 활성화
        /// </summary>
        public static void EnableOwnerInput(Actor actor)
        {
            if (actor == null || !actor.IsOwner) return;

            Debug.Log($"[ActorBuilder] Enabling owner input for {actor.name}");

            // 플레이어 입력 컴포넌트 추가
            var input = actor.AddComponent(new PlayerInputComponent());

            Debug.Log($"[ActorBuilder] Owner input enabled for {actor.name}");
        }

        /// <summary>
        /// 전체 액터 초기화 (서버 + 클라이언트)
        /// </summary>
        public static void BuildComplete(Actor actor, ActorPreset preset)
        {
            if (actor == null || preset == null) return;

            // 서버 컴포넌트
            if (actor.IsServerInitialized)
            {
                BuildServer(actor, preset);
            }

            // 클라이언트 뷰
            BuildClientView(actor, preset);

            // Owner 입력
            if (actor.IsOwner)
            {
                EnableOwnerInput(actor);
            }

            Debug.Log($"[ActorBuilder] Complete build finished for {actor.name} (Server: {actor.IsServer}, Owner: {actor.IsOwner})");
        }

        /// <summary>
        /// 디버그용 컴포넌트 정보 출력
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DebugComponents(Actor actor)
        {
            if (actor == null) return;

            Debug.Log($"[ActorBuilder] Components for {actor.name}:");
            Debug.Log($"  - EventBus: {(actor.EventBus != null ? "✓" : "✗")}");
            Debug.Log($"  - Scheduler: {(actor.Scheduler != null ? "✓" : "✗")}");
            Debug.Log($"  - NetworkSync: {(actor.NetworkSync != null ? "✓" : "✗")}");
            
            // 각 인터페이스별 컴포넌트 확인
            Debug.Log($"  - IMovable: {(actor.HasComponent<IMovable>() ? "✓" : "✗")}");
            Debug.Log($"  - IDamageable: {(actor.HasComponent<IDamageable>() ? "✓" : "✗")}");
            Debug.Log($"  - IInputProvider: {(actor.HasComponent<IInputProvider>() ? "✓" : "✗")}");
        }
    }
}
