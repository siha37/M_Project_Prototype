using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class PlayerRoleManager : NetworkBehaviour
    {
        public static PlayerRoleManager instance { get; private set; }
        private void Awake()
        {
            instance = this;
        }        
        public static PlayerRoleManager Instance
        {
            get
            {
                if (!instance)
                    instance = FindFirstObjectByType<PlayerRoleManager>();
                return instance;
            }
        }
        
        

        [SerializeField] private List<RoleDefinition> roleDefinitions;

        private readonly Dictionary<int, PlayerRoleType> assignedRoles = new(); // key: ClientId
        
        // 역할 배정 완료 이벤트
        public event System.Action<Dictionary<int, PlayerRoleType>> OnRolesAssigned;

        private bool readyRole = false;


        /// <summary>
        /// 원하는 타입의 Deficition 얻기
        /// </summary>
        public RoleDefinition GetDefinition(PlayerRoleType type)
            => roleDefinitions.FirstOrDefault(r => r.Role == type);

        
        /// <summary>
        /// 모든 플레이어에게 역할 동시 배정 (서버에서 호출)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AssignRolesToAllPlayersServerRpc()
        {
            if (!IsServerInitialized) return;
            
            LogManager.Log(LogCategory.System, "모든 플레이어에게 역할 배정 시작", this);
            
            // 기존 할당 초기화
            assignedRoles.Clear();
            
            // GameSettingManager에서 역할 설정 가져오기
            GameSettings gameSettings = GameSettingManager.Instance.GetCurrentSettings();
            List<PlayerRoleSettings> roleSettings = gameSettings.playerRoleSettings;
            
            // 현재 연결된 모든 클라이언트 가져오기
            var connectedClients = NetworkManager.ServerManager.Clients;
            
            // 역할 풀 생성 (설정된 수량만큼)
            var rolePool = new List<PlayerRoleType>();
            foreach (var roleSetting in roleSettings)
            {
                for (int i = 0; i < roleSetting.RoleAmount; i++)
                {
                    rolePool.Add(roleSetting.RoleType);
                }
            }
            
            // 역할 풀을 랜덤하게 섞기
            rolePool = rolePool.OrderBy(x => Random.Range(0f, 1f)).ToList();
            
            // 각 클라이언트에게 역할 배정
            int roleIndex = 0;
            foreach (var client in connectedClients)
            {
                if (roleIndex < rolePool.Count)
                {
                    var assignedRole = rolePool[roleIndex];
                    assignedRoles.Add(client.Value.ClientId, assignedRole);
                    
                    LogManager.Log(LogCategory.System, 
                        $"클라이언트 {client.Value.ClientId}에게 역할 {assignedRole} 배정됨", this);
                    
                    roleIndex++;
                }
                else
                {
                    // 역할이 부족한 경우 기본 역할 배정
                    var defaultRole = roleSettings[0].RoleType;
                    assignedRoles.Add(client.Value.ClientId, defaultRole);
                    
                    LogManager.Log(LogCategory.System, 
                        $"클라이언트 {client.Value.ClientId}에게 기본 역할 {defaultRole} 배정됨", this);
                }
            }
            
            // 모든 클라이언트에게 역할 배정 결과 전송
            NotifyRolesAssignedClientRpc(assignedRoles);
            
            LogManager.Log(LogCategory.System, 
                $"총 {assignedRoles.Count}명의 플레이어에게 역할 배정 완료", this);
        }     
        
        /// <summary>
        /// 모든 클라이언트에게 역할 배정 결과 알림
        /// </summary>
        [ObserversRpc]
        private void NotifyRolesAssignedClientRpc(Dictionary<int, PlayerRoleType> roles)
        {
            // 서버에서 받은 역할 정보로 로컬 딕셔너리 업데이트
            assignedRoles.Clear();
            foreach (var kvp in roles)
            {
                assignedRoles.Add(kvp.Key, kvp.Value);
            }
            
            // 이벤트 발생
            OnRolesAssigned?.Invoke(assignedRoles);
            readyRole = true;
            LogManager.Log(LogCategory.System, $"역할 배정 완료: {assignedRoles.Count}명", this);
        }
        
        /// <summary>
        /// 특정 클라이언트의 역할 가져오기
        /// </summary>
        public PlayerRoleType GetClientRole(int clientId)
        {
            return assignedRoles.TryGetValue(clientId, out var role) ? role : PlayerRoleType.Normal;
        }

        /// <summary>
        /// 현재 플레이어의 역할 가져오기
        /// </summary>
        public PlayerRoleType GetLocalPlayerRole()
        {
            if (NetworkManager == null || NetworkManager.ClientManager == null) 
                return PlayerRoleType.Normal;
                
            return GetClientRole(NetworkManager.ClientManager.Connection.ClientId);
        }

        /// <summary>
        /// 역할 배정이 완료되었는지 확인
        /// </summary>
        public bool AreRolesAssigned()
        {
            return readyRole;
        }

        /// <summary>
        /// 역할 배정 상태 초기화
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ResetRoleAssignmentsServerRpc()
        {
            if (!IsServerInitialized) return;
            
            assignedRoles.Clear();
            NotifyRolesAssignedClientRpc(assignedRoles);
            
            LogManager.Log(LogCategory.System, "역할 배정 상태 초기화됨", this);
        }
    }
}