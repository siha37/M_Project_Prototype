using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class RoomInfo
{
    public string RoomId { get; set; }
    public bool IsOpen { get; set; }
    public float CreatedTime { get; set; }
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public string HostName { get; set; }
    public string GameMode { get; set; }

    public RoomInfo(string id, int maxPlayers = 4, string hostName = "Host", string gameMode = "Default")
    {
        RoomId = id;
        IsOpen = true;
        CreatedTime = Time.time;
        MaxPlayers = maxPlayers;
        CurrentPlayers = 0;
        HostName = hostName;
        GameMode = gameMode;
    }

    public bool CanJoin => IsOpen && CurrentPlayers < MaxPlayers;
    public float AgeInMinutes => (Time.time - CreatedTime) / 60f;
}

public class NetworkRoomManager : NetworkBehaviour
{
    [SyncVar(OnChange = nameof(OnRoomListChanged))]
    private string serializedRooms = "";
    
    private readonly List<RoomInfo> rooms = new List<RoomInfo>();
    private readonly System.Random random = new System.Random();
    
    [Header("Room Settings")]
    [SerializeField] private float roomCleanupInterval = 30f; // 30분
    [SerializeField] private int maxRooms = 100;
    
    public IReadOnlyList<RoomInfo> Rooms => rooms.AsReadOnly();
    public int TotalRooms => rooms.Count;
    public int AvailableRooms => rooms.Count(r => r.CanJoin);

    // 방 목록 업데이트 이벤트
    public System.Action OnRoomListUpdated;

    // 네트워크 변수 변경 시 호출되는 콜백
    private void OnRoomListChanged(string previousValue, string newValue, bool asServer)
    {
        if (!asServer) // 클라이언트에서만 실행
        {
            DeserializeRooms(newValue);
            // UI 업데이트 이벤트 발생
            OnRoomListUpdated?.Invoke();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 서버 시작 시 주기적 정리 시작
        InvokeRepeating(nameof(CleanupOldRooms), roomCleanupInterval * 60f, roomCleanupInterval * 60f);
    }

    [ServerRpc]
    public void CreateRoomServerRpc(int maxPlayers = 4, string hostName = "Host", string gameMode = "Default")
    {
        // 방 개수 제한 확인
        if (rooms.Count >= maxRooms)
        {
            Debug.LogWarning("최대 방 개수에 도달했습니다.");
            return;
        }

        string id = GenerateUniqueRoomId();
        RoomInfo room = new RoomInfo(id, maxPlayers, hostName, gameMode);
        rooms.Add(room);
        
        Debug.Log($"방 생성됨: {id} (호스트: {hostName}, 최대플레이어: {maxPlayers})");
        
        // 모든 클라이언트에게 방 목록 동기화
        SerializeAndSyncRooms();
    }

    [ServerRpc]
    public void JoinRoomServerRpc(string id)
    {
        RoomInfo room = rooms.FirstOrDefault(r => r.RoomId == id && r.CanJoin);
        if (room != null)
        {
            room.CurrentPlayers++;
            Debug.Log($"방 참가: {id}, 현재 플레이어: {room.CurrentPlayers}/{room.MaxPlayers}");
            
            // 모든 클라이언트에게 방 목록 동기화
            SerializeAndSyncRooms();
        }
        else
        {
            Debug.LogWarning($"방 {id}에 참가할 수 없습니다.");
        }
    }

    [ServerRpc]
    public void LeaveRoomServerRpc(string id)
    {
        RoomInfo room = rooms.FirstOrDefault(r => r.RoomId == id);
        if (room != null)
        {
            room.CurrentPlayers = Mathf.Max(0, room.CurrentPlayers - 1);
            Debug.Log($"방 나감: {id}, 현재 플레이어: {room.CurrentPlayers}/{room.MaxPlayers}");
            
            // 플레이어가 없으면 방 제거
            if (room.CurrentPlayers == 0)
            {
                rooms.Remove(room);
                Debug.Log($"빈 방 제거됨: {id}");
            }
            
            // 모든 클라이언트에게 방 목록 동기화
            SerializeAndSyncRooms();
        }
    }

    [ServerRpc]
    public void CloseRoomServerRpc(string id)
    {
        RoomInfo room = rooms.FirstOrDefault(r => r.RoomId == id);
        if (room != null)
        {
            room.IsOpen = false;
            rooms.Remove(room);
            Debug.Log($"방 닫힘: {id}");
            
            // 모든 클라이언트에게 방 목록 동기화
            SerializeAndSyncRooms();
        }
    }

    public RoomInfo GetRoom(string id)
    {
        return rooms.FirstOrDefault(r => r.RoomId == id);
    }

    public List<RoomInfo> GetAvailableRooms()
    {
        return rooms.Where(r => r.CanJoin).ToList();
    }

    private string GenerateUniqueRoomId()
    {
        string id;
        int attempts = 0;
        const int maxAttempts = 100;
        
        do
        {
            id = random.Next(1000, 100000).ToString("D5"); // 5자리 숫자
            attempts++;
        } while (rooms.Any(r => r.RoomId == id) && attempts < maxAttempts);
        
        if (attempts >= maxAttempts)
        {
            Debug.LogError("고유한 방 ID를 생성할 수 없습니다.");
            return null;
        }
        
        return id;
    }

    private void CleanupOldRooms()
    {
        if (!IsServer) return;
        
        float currentTime = Time.time;
        int removedCount = rooms.RemoveAll(r => currentTime - r.CreatedTime > roomCleanupInterval * 60f);
        
        if (removedCount > 0)
        {
            Debug.Log($"{removedCount}개의 오래된 방이 정리되었습니다.");
            SerializeAndSyncRooms();
        }
    }

    [ServerRpc]
    public void ClearAllRoomsServerRpc()
    {
        rooms.Clear();
        Debug.Log("모든 방이 정리되었습니다.");
        SerializeAndSyncRooms();
    }

    // 방 목록을 JSON으로 직렬화
    private void SerializeAndSyncRooms()
    {
        if (!IsServer) return;
        
        var roomDataList = rooms.Select(r => new RoomData
        {
            RoomId = r.RoomId,
            IsOpen = r.IsOpen,
            CreatedTime = r.CreatedTime,
            MaxPlayers = r.MaxPlayers,
            CurrentPlayers = r.CurrentPlayers,
            HostName = r.HostName,
            GameMode = r.GameMode
        }).ToList();
        
        var wrapper = new RoomListWrapper { rooms = roomDataList };
        serializedRooms = JsonUtility.ToJson(wrapper);
    }

    // JSON에서 방 목록을 역직렬화
    private void DeserializeRooms(string json)
    {
        rooms.Clear();
        
        if (string.IsNullOrEmpty(json)) return;
        
        try
        {
            var wrapper = JsonUtility.FromJson<RoomListWrapper>(json);
            foreach (var roomData in wrapper.rooms)
            {
                RoomInfo room = new RoomInfo(roomData.RoomId, roomData.MaxPlayers, roomData.HostName, roomData.GameMode)
                {
                    IsOpen = roomData.IsOpen,
                    CreatedTime = roomData.CreatedTime,
                    CurrentPlayers = roomData.CurrentPlayers
                };
                rooms.Add(room);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"방 목록 역직렬화 실패: {e.Message}");
        }
    }

    // JSON 직렬화를 위한 래퍼 클래스
    [System.Serializable]
    private class RoomListWrapper
    {
        public List<RoomData> rooms;
    }

    [System.Serializable]
    private class RoomData
    {
        public string RoomId;
        public bool IsOpen;
        public float CreatedTime;
        public int MaxPlayers;
        public int CurrentPlayers;
        public string HostName;
        public string GameMode;
    }
} 