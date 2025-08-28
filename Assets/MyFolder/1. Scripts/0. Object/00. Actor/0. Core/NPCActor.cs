using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// NPC 액터 구현체 (상호작용 가능한 비전투 캐릭터)
    /// </summary>
    public class NPCActor : Actor
    {
        [Header("NPC Settings")]
        [SerializeField] private string npcName = "NPC";
        [SerializeField] private string npcRole = "Villager";
        [SerializeField] private bool isInteractable = true;
        [SerializeField] private float interactionRange = 3f;

        public string NPCName => npcName;
        public string NPCRole => npcRole;
        public bool IsInteractable => isInteractable;
        public float InteractionRange => interactionRange;

        protected override void Awake()
        {
            base.Awake();
            
            if (string.IsNullOrEmpty(npcName))
            {
                npcName = $"NPC_{GetInstanceID()}";
            }
        }

        /// <summary>
        /// NPC 정보 설정
        /// </summary>
        public void SetNPCInfo(string name, string role)
        {
            npcName = name;
            npcRole = role;
            gameObject.name = $"NPCActor_{name}";
        }

        /// <summary>
        /// 상호작용 가능 여부 설정
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
        }

        /// <summary>
        /// 상호작용 범위 설정
        /// </summary>
        public void SetInteractionRange(float range)
        {
            interactionRange = Mathf.Max(0f, range);
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug NPC Info")]
        private void DebugNPCInfo()
        {
            Debug.Log($"=== NPC Actor Info ===");
            Debug.Log($"Name: {npcName}");
            Debug.Log($"Role: {npcRole}");
            Debug.Log($"Interactable: {isInteractable}");
            Debug.Log($"Interaction Range: {interactionRange}");
            Debug.Log($"IsServer: {IsServer}");
            Debug.Log($"NetworkObjectId: {NetworkObject?.ObjectId}");
        }
        #endif
    }
}
