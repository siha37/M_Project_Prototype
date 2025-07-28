using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class NetworkPlayerManager : NetworkBehaviour
    {
        private readonly SyncVar<int> syncPlayerCount = new SyncVar<int>();
    
        private static NetworkPlayerManager instance;
        public static NetworkPlayerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NetworkPlayerManager>();
                }
                return instance;
            }
        }

        // 모든 클라이언트에서 접근 가능한 플레이어 리스트
        private List<NetworkObject> allPlayers = new List<NetworkObject>();

        public int PlayerCount => syncPlayerCount.Value;
        public List<NetworkObject> AllPlayers => new List<NetworkObject>(allPlayers);

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                LogManager.Log(LogCategory.Network, "NetworkPlayerManager 인스턴스 생성 완료", this);
            }
            else if (instance != this)
            {
                LogManager.LogWarning(LogCategory.Network, "NetworkPlayerManager 중복 인스턴스 제거", this);
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            syncPlayerCount.Value = 0;
            LogManager.Log(LogCategory.Network, "NetworkPlayerManager 서버 초기화 완료", this);
        }

        public override void OnStartClient()
        {
            syncPlayerCount.OnChange += OnPlayerCount_Changed;
            LogManager.Log(LogCategory.Network, "NetworkPlayerManager 클라이언트 초기화 완료", this);
        }

        public override void OnStopClient()
        {
            if (syncPlayerCount != null)
                syncPlayerCount.OnChange -= OnPlayerCount_Changed;
        }

        // ✅ SceneBasedPlayerSpawner에서 호출할 등록 메서드
        public void RegisterPlayer(NetworkObject player)
        {
            if (player == null) return;

            // 서버에서는 직접 등록하고 클라이언트에 알림
            if (IsServer)
            {
                RegisterPlayerInternal(player);
                NotifyPlayerAddedClientRpc(player);
            }
        }

        // ✅ SceneBasedPlayerSpawner에서 호출할 해제 메서드
        public void UnregisterPlayer(NetworkObject player)
        {
            if (player == null) return;

            // 서버에서는 직접 해제하고 클라이언트에 알림
            if (IsServer)
            {
                UnregisterPlayerInternal(player);
                NotifyPlayerRemovedClientRpc(player);
            }
        }

        // 서버 내부 등록 처리
        private void RegisterPlayerInternal(NetworkObject player)
        {
            if (!allPlayers.Contains(player))
            {
                allPlayers.Add(player);
                syncPlayerCount.Value = allPlayers.Count;
            
                LogManager.Log(LogCategory.Network, 
                    $"NetworkPlayerManager 플레이어 등록: {player.Owner?.ClientId}, 총 플레이어: {allPlayers.Count}명", this);
            
                OnPlayerAdded?.Invoke(player);
            }
        }

        // 서버 내부 해제 처리
        private void UnregisterPlayerInternal(NetworkObject player)
        {
            if (allPlayers.Contains(player))
            {
                allPlayers.Remove(player);
                syncPlayerCount.Value = allPlayers.Count;
            
                LogManager.Log(LogCategory.Network, 
                    $"NetworkPlayerManager 플레이어 해제: {player.Owner?.ClientId}, 총 플레이어: {allPlayers.Count}명", this);
            
                OnPlayerRemoved?.Invoke(player);
            }
        }

        // 클라이언트에 플레이어 추가 알림
        [ObserversRpc]
        private void NotifyPlayerAddedClientRpc(NetworkObject player)
        {
            if (IsServer) return; // 서버는 이미 처리했으므로 건너뜀
        
            if (player != null && !allPlayers.Contains(player))
            {
                allPlayers.Add(player);
                LogManager.Log(LogCategory.Network, $"NetworkPlayerManager 클라이언트에 플레이어 추가: {player.name}", this);
                OnPlayerAdded?.Invoke(player);
            }
        }

        // 클라이언트에 플레이어 제거 알림
        [ObserversRpc]
        private void NotifyPlayerRemovedClientRpc(NetworkObject player)
        {
            if (IsServer) return; // 서버는 이미 처리했으므로 건너뜀
        
            if (player != null && allPlayers.Contains(player))
            {
                allPlayers.Remove(player);
                LogManager.Log(LogCategory.Network, $"NetworkPlayerManager 클라이언트에서 플레이어 제거: {player.name}", this);
                OnPlayerRemoved?.Invoke(player);
            }
        }

        // ✅ 플레이어 조회 메서드들
        public NetworkObject GetPlayerByClientId(int clientId)
        {
            return allPlayers.Find(player => player.Owner != null && player.Owner.ClientId == clientId);
        }

        public NetworkObject GetRandomPlayer()
        {
            if (allPlayers.Count == 0) return null;
            return allPlayers[Random.Range(0, allPlayers.Count)];
        }

        public List<NetworkObject> GetAlivePlayers()
        {
            List<NetworkObject> alivePlayers = new List<NetworkObject>();
            foreach (var player in allPlayers)
            {
                PlayerNetworkSync playerSync = player.GetComponent<PlayerNetworkSync>();
                if (playerSync != null && !playerSync.IsDead())
                {
                    alivePlayers.Add(player);
                }
            }
            return alivePlayers;
        }

        public List<NetworkObject> GetDeadPlayers()
        {
            List<NetworkObject> deadPlayers = new List<NetworkObject>();
            foreach (var player in allPlayers)
            {
                PlayerNetworkSync playerSync = player.GetComponent<PlayerNetworkSync>();
                if (playerSync && playerSync.IsDead())
                {
                    deadPlayers.Add(player);
                }
            }
            return deadPlayers;
        }

        // ✅ 이벤트 시스템
        public System.Action<NetworkObject> OnPlayerAdded;
        public System.Action<NetworkObject> OnPlayerRemoved;
        public System.Action<int> OnPlayerCountChanged;

        private void OnPlayerCount_Changed(int previousValue, int newValue, bool asServer)
        {
            LogManager.Log(LogCategory.Network, $"NetworkPlayerManager 플레이어 수 변경: {previousValue} → {newValue}", this);
            OnPlayerCountChanged?.Invoke(newValue);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
} 