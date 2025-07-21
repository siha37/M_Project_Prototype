using System.Collections.Generic;
using UnityEngine;

public class NetworkExample : MonoBehaviour
{
    [Header("Example Settings")]
    public bool autoConnect = true;
    public string testRoomName = "테스트 방";
    
    void Start()
    {
        if (autoConnect)
        {
            TestConnection();
        }
    }
    
    async void TestConnection()
    {
        Debug.Log("=== 네트워크 연결 테스트 시작 ===");
        
        // 1. 서버 연결 테스트
        var connected = await TCPNetworkManager.Instance.TestConnectionAsync();
        if (!connected)
        {
            Debug.LogError("서버 연결 실패!");
            return;
        }
        
        Debug.Log("서버 연결 성공!");
        
        // 2. 방 목록 조회
        var rooms = await TCPNetworkManager.Instance.GetRoomListAsync();
        Debug.Log($"현재 활성 방: {rooms.Count}개");
        
        // 3. 테스트 방 생성
        var roomCreated = await TCPNetworkManager.Instance.CreateRoomAsync(testRoomName, 4);
        if (roomCreated)
        {
            Debug.Log($"테스트 방 생성 성공! Room ID: {TCPNetworkManager.Instance.GetCurrentRoomId()}");
            
            // 4. 하트비트 시작
            HeartbeatManager.Instance.StartHeartbeat(TCPNetworkManager.Instance.GetCurrentRoomId());
            
            // 5. 10초 후 방 삭제
            Invoke(nameof(DeleteTestRoom), 10f);
        }
        else
        {
            Debug.LogError("테스트 방 생성 실패!");
        }
    }
    
    async void DeleteTestRoom()
    {
        var roomId = TCPNetworkManager.Instance.GetCurrentRoomId();
        if (!string.IsNullOrEmpty(roomId))
        {
            HeartbeatManager.Instance.StopHeartbeat();
            var deleted = await TCPNetworkManager.Instance.DeleteRoomAsync(roomId);
            Debug.Log(deleted ? "테스트 방 삭제 성공!" : "테스트 방 삭제 실패!");
        }
    }
    
    // 수동 테스트 메서드들
    [ContextMenu("서버 연결 테스트")]
    public async void ManualTestConnection()
    {
        var success = await TCPNetworkManager.Instance.TestConnectionAsync();
        Debug.Log(success ? "수동 연결 성공!" : "수동 연결 실패!");
    }
    
    [ContextMenu("방 목록 조회")]
    public async void ManualGetRoomList()
    {
        var rooms = await TCPNetworkManager.Instance.GetRoomListAsync();
        Debug.Log($"수동 조회 결과: {rooms.Count}개 방");
        
        foreach (var room in rooms)
        {
            Debug.Log($"방: {room.roomName} ({room.currentPlayers}/{room.maxPlayers}) - {room.hostAddress}:{room.hostPort}");
        }
    }
    
    [ContextMenu("테스트 방 생성")]
    public async void ManualCreateRoom()
    {
        var success = await TCPNetworkManager.Instance.CreateRoomAsync("수동 테스트 방", 6);
        if (success)
        {
            Debug.Log($"수동 방 생성 성공! ID: {TCPNetworkManager.Instance.GetCurrentRoomId()}");
            HeartbeatManager.Instance.StartHeartbeat(TCPNetworkManager.Instance.GetCurrentRoomId());
        }
        else
        {
            Debug.LogError("수동 방 생성 실패!");
        }
    }
    
    [ContextMenu("현재 방 삭제")]
    public async void ManualDeleteRoom()
    {
        var roomId = TCPNetworkManager.Instance.GetCurrentRoomId();
        if (!string.IsNullOrEmpty(roomId))
        {
            HeartbeatManager.Instance.StopHeartbeat();
            var success = await TCPNetworkManager.Instance.DeleteRoomAsync(roomId);
            Debug.Log(success ? "수동 방 삭제 성공!" : "수동 방 삭제 실패!");
        }
        else
        {
            Debug.LogWarning("삭제할 방이 없습니다!");
        }
    }
    
    [ContextMenu("방 참가 테스트")]
    public async void ManualJoinRoomTest()
    {
        Debug.Log("=== 방 참가 테스트 시작 ===");
        
        // 1. 방 목록 조회
        var rooms = await TCPNetworkManager.Instance.GetRoomListAsync();
        if (rooms.Count == 0)
        {
            Debug.LogWarning("참가할 방이 없습니다! 먼저 방을 생성해주세요.");
            return;
        }
        
        // 2. 첫 번째 방에 참가
        var targetRoom = rooms[0];
        Debug.Log($"방 참가 시도: {targetRoom.roomName} (ID: {targetRoom.roomId})");
        
        var joinResult = await TCPNetworkManager.Instance.JoinRoomAsync(targetRoom.roomId, "테스트 플레이어");
        
        if (joinResult.success)
        {
            Debug.Log($"방 참가 성공!");
            Debug.Log($"호스트 주소: {joinResult.hostAddress}:{joinResult.hostPort}");
            Debug.Log($"현재 플레이어: {joinResult.roomInfo.currentPlayers}/{joinResult.roomInfo.maxPlayers}");
            
            // 3. 다음 단계 준비 (FishNet 연결)
            Debug.Log("다음 단계: FishNet 연결 준비 완료");
        }
        else
        {
            Debug.LogError($"방 참가 실패: {joinResult.errorMessage}");
        }
    }

    [ContextMenu("방 나가기 테스트")]
    public async void ManualLeaveRoomTest()
    {
        var roomId = TCPNetworkManager.Instance.GetCurrentRoomId();
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogWarning("나갈 방이 없습니다!");
            return;
        }
        
        var success = await TCPNetworkManager.Instance.LeaveRoomAsync(roomId);
        Debug.Log(success ? "방 나가기 성공!" : "방 나가기 실패!");
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("=== 네트워크 테스트 패널 ===");
        
        GUILayout.Space(10);
        GUILayout.Label($"연결 상태: {TCPNetworkManager.Instance.connectionStatus}");
        GUILayout.Label($"현재 방: {TCPNetworkManager.Instance.GetCurrentRoomId() ?? "없음"}");
        
        if (HeartbeatManager.Instance.IsHeartbeating())
        {
            GUILayout.Label($"하트비트: {HeartbeatManager.Instance.GetTimeSinceLastHeartbeat():F1}초 전");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("서버 연결"))
        {
            ManualTestConnection();
        }
        
        if (GUILayout.Button("방 목록 조회"))
        {
            ManualGetRoomList();
        }
        
        if (GUILayout.Button("테스트 방 생성"))
        {
            ManualCreateRoom();
        }
        
        if (GUILayout.Button("방 참가 테스트"))
        {
            ManualJoinRoomTest();
        }
        
        if (GUILayout.Button("방 나가기 테스트"))
        {
            ManualLeaveRoomTest();
        }
        
        if (GUILayout.Button("현재 방 삭제"))
        {
            ManualDeleteRoom();
        }
        
        GUILayout.EndArea();
    }
} 