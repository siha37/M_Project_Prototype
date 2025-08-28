using UnityEngine;

namespace MyFolder._1._Scripts._00._Actor._6._Config
{
    /// <summary>
    /// 액터 타입
    /// </summary>
    public enum ActorType
    {
        Player,
        AI,
        NPC,
        Boss
    }

    /// <summary>
    /// 액터 구성을 정의하는 프리셋
    /// </summary>
    [CreateAssetMenu(fileName = "New Actor Preset", menuName = "Actor/Actor Preset", order = 0)]
    public class ActorPreset : ScriptableObject
    {
        [Header("Actor Type")]
        public ActorType ActorType = ActorType.Player;
        
        [Header("Component Configs")]
        public HealthConfig HealthConfig;
        public MovementConfig MovementConfig;
        public ShooterConfig ShooterConfig;
        
        [Header("Visual Configs")]
        public AnimationProfile AnimationProfile;
        
        [Header("AI Configs (AI Only)")]
        public PerceptionConfig PerceptionConfig;
        
        [Header("Description")]
        [TextArea(3, 5)]
        public string Description;

        private void OnValidate()
        {
            // 유효성 검사
            if (ActorType == ActorType.AI)
            {
                if (MovementConfig == null)
                    Debug.LogWarning($"[ActorPreset] AI 액터 '{name}'에 MovementConfig가 없습니다.");
            }
            else if (ActorType == ActorType.Player)
            {
                if (HealthConfig == null)
                    Debug.LogWarning($"[ActorPreset] 플레이어 액터 '{name}'에 HealthConfig가 없습니다.");
            }
        }

        /// <summary>
        /// 프리셋 복사본 생성
        /// </summary>
        public ActorPreset Clone()
        {
            return Instantiate(this);
        }

        public override string ToString()
        {
            return $"ActorPreset[{ActorType}]: {name}";
        }
    }
}
