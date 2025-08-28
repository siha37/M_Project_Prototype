using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 플레이어 액터 구현체
    /// </summary>
    public class PlayerActor : Actor
    {
        [Header("Player Settings")]
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int playerId = -1;

        public string PlayerName => playerName;
        public int PlayerId => playerId;

        protected override void Awake()
        {
            base.Awake();
            
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player_{GetInstanceID()}";
            }
        }

        /// <summary>
        /// 플레이어 ID 설정
        /// </summary>
        public void SetPlayerId(int id)
        {
            playerId = id;
            name = $"PlayerActor_{id}";
        }

        /// <summary>
        /// 플레이어 이름 설정
        /// </summary>
        public void SetPlayerName(string newName)
        {
            playerName = newName;
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug Player Info")]
        private void DebugPlayerInfo()
        {
            Debug.Log($"=== Player Actor Info ===");
            Debug.Log($"Name: {playerName}");
            Debug.Log($"ID: {playerId}");
            Debug.Log($"IsServer: {IsServer}");
            Debug.Log($"IsOwner: {IsOwner}");
            Debug.Log($"NetworkObjectId: {NetworkObject?.ObjectId}");
        }
        #endif
    }
}
