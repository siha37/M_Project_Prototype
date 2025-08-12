using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

namespace MyFolder._1._Scripts._7._PlayerRole
{
    public class PlayerRoleManager : NetworkBehaviour
    {
        public static PlayerRoleManager Instance { get; private set; }

        [SerializeField] private List<RoleDefinition> roleDefinitions;

        private readonly Dictionary<int, PlayerRoleType> assigned = new(); // key: ClientId

        private void Awake()
        {
            Instance = this;
        }

        public RoleDefinition GetDefinition(PlayerRoleType type)
            => roleDefinitions.FirstOrDefault(r => r.Role == type);

        // 서버에서 호출: 플레이어에게 역할 배정(중복 허용/비허용 정책 선택 가능)
        public PlayerRoleType AssignRoleForClient(int clientId)
        {
            if (!IsServerInitialized) return PlayerRoleType.Normal;

            if (assigned.TryGetValue(clientId, out var exists))
                return exists;

            // 예시: 라운드로빈으로 배정
            var pool = roleDefinitions.Select(r => r.Role).ToList();
            var role = pool[(assigned.Count) % pool.Count];
            assigned[clientId] = role;
            return role;
        }
    }
}