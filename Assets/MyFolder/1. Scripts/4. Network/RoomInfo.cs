using System;

[Serializable]
public class RoomInfo
{
    public string roomId;
    public string hostAddress;
    public int hostPort;
    public int maxPlayers;
    public int currentPlayers;
    public string status;
    public string roomName;
    public string gameType;
    public long createdTime;
    public long lastHeartbeat;
    public long lastActivity;
    public bool isPrivate;
}

[Serializable]
public class ApiResponse
{
    public bool success;
    public long timestamp;
    public string version;
    public object data;
    public ErrorInfo error;
}

[Serializable]
public class ErrorInfo
{
    public int code;
    public string message;
} 