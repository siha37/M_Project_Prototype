using System.Collections.Generic;
using UnityEngine;

public class RoomInfo
{
    public string RoomId;
    public bool IsOpen;

    public RoomInfo(string id)
    {
        RoomId = id;
        IsOpen = true;
    }
}

public class RoomManager : SingleTone<RoomManager>
{
    private readonly List<RoomInfo> rooms = new List<RoomInfo>();
    public IReadOnlyList<RoomInfo> Rooms => rooms;

    public RoomInfo CreateRoom()
    {
        string id = Random.Range(1000, 10000).ToString();
        RoomInfo room = new RoomInfo(id);
        rooms.Add(room);
        return room;
    }

    public bool JoinRoom(string id)
    {
        RoomInfo room = rooms.Find(r => r.RoomId == id && r.IsOpen);
        return room != null;
    }
}
