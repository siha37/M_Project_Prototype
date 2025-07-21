using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

public class RoomManager : SingleTone<RoomManager>
{
    private readonly List<RoomInfo> rooms = new List<RoomInfo>();
    private readonly System.Random random = new System.Random();
    
    [Header("Room Settings")]
    [SerializeField] private int maxRooms = 100;
    
    public IReadOnlyList<RoomInfo> Rooms => rooms.AsReadOnly();
    public int TotalRooms => rooms.Count;
    public int AvailableRooms => rooms.Count(r => r.CanJoin);


    public RoomInfo CreateRoom(int maxPlayers = 4, string hostName = "Host", string gameMode = "Default")
    {
        // 방 개수 제한 확인
        if (rooms.Count >= maxRooms)
        {
            Debug.LogWarning("최대 방 개수에 도달했습니다.");
            return null;
        }

        string id = GenerateUniqueRoomId();
        RoomInfo room = new RoomInfo(id, maxPlayers, hostName, gameMode);
        rooms.Add(room);
        
        Debug.Log($"방 생성됨: {id} (호스트: {hostName}, 최대플레이어: {maxPlayers})");
        return room;
    }

    public bool JoinRoom(string id)
    {
        RoomInfo room = rooms.FirstOrDefault(r => r.RoomId == id && r.CanJoin);
        if (room != null)
        {
            room.CurrentPlayers++;
            Debug.Log($"방 참가: {id}, 현재 플레이어: {room.CurrentPlayers}/{room.MaxPlayers}");
            return true;
        }
        
        Debug.LogWarning($"방 {id}에 참가할 수 없습니다.");
        return false;
    }

    public void LeaveRoom(string id)
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
        }
    }

    public void CloseRoom(string id)
    {
        RoomInfo room = rooms.FirstOrDefault(r => r.RoomId == id);
        if (room != null)
        {
            room.IsOpen = false;
            rooms.Remove(room);
            Debug.Log($"방 닫힘: {id}");
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


    public void ClearAllRooms()
    {
        rooms.Clear();
        Debug.Log("모든 방이 정리되었습니다.");
    }
}