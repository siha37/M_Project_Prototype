using System;

[Serializable]
public class JoinRoomResult
{
    public bool success;           // 성공/실패 여부
    public string hostAddress;     // 호스트 IP 주소
    public int hostPort;          // 호스트 포트 번호
    public RoomInfo roomInfo;     // 방 정보
    public string errorMessage;   // 오류 메시지
    public string joinCode;       // Relay 참가 코드
}

[Serializable]
public class JoinRoomData
{
    public string hostAddress;
    public int hostPort;
    public RoomInfo roomInfo;
    public string joinCode;
} 