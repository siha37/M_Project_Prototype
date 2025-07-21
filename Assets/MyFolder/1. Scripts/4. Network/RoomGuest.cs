using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class PlayerInfo
{
    public string name;
    public string deviceId;
    public bool isHost;
    public bool isReady;
    public long joinTime;
}

[Serializable]
public class PlayerListData
{
    public List<PlayerInfo> players;
    public int totalCount;
    public int readyCount;
}

public class RoomGuest
{
    public async Task<List<RoomInfo>> GetRoomListAsync(bool includePrivate = false)
    {
        try
        {
            var payload = new
            {
                type = "list",
                includePrivate = includePrivate
            };

            var response = await TcpClientHelper.SendAuthedJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP,
                ServerConfig.SERVER_PORT,
                payload,
                DeviceIdentifier.GetDeviceId(),
                TCPNetworkManager.Instance.GetSessionToken()
            );
            
            if (!response.success)
            {
                Debug.LogError($"방 리스트 조회 실패: {response.error?.message}");
                return new List<RoomInfo>();
            }
            
            if (response.data != null)
            {
                try
                {
                    Debug.Log($"서버 응답 데이터 타입: {response.data.GetType()}");
                    Debug.Log($"서버 응답 데이터: {JsonConvert.SerializeObject(response.data)}");

                    // rooms 필드만 추출
                    var outer = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.data.ToString());
                    if (outer != null && outer.ContainsKey("rooms"))
                    {
                        var roomsJson = outer["rooms"].ToString();
                        var roomList = JsonConvert.DeserializeObject<List<RoomInfo>>(roomsJson);
                        return roomList;
                    }
                    else
                    {
                        Debug.LogError("방 리스트 응답에 rooms 필드가 없음");
                        return new List<RoomInfo>();
                    }
                }
                catch (JsonException jsonEx)
                {
                    Debug.LogError($"JSON 파싱 오류: {jsonEx.Message}");
                    Debug.LogError($"파싱 시도한 데이터: {response.data}");
                    return new List<RoomInfo>();
                }
                catch (Exception parseEx)
                {
                    Debug.LogError($"데이터 파싱 오류: {parseEx.Message}");
                    return new List<RoomInfo>();
                }
            }
            
            return new List<RoomInfo>();
        }
        catch (NetworkException ex)
        {
            Debug.LogError($"네트워크 오류: {ex.Message}");
            return new List<RoomInfo>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"방 리스트 조회 중 오류: {ex.Message}");
            return new List<RoomInfo>();
        }
    }
    
    public async Task<JoinRoomResult> JoinRoomAsync(string roomId, string playerName = "")
    {
        try
        {
            var payload = new
            {
                type = "join",
                roomId = roomId,
                playerName = playerName,
                deviceId = DeviceIdentifier.GetDeviceId()
            };

            var response = await TcpClientHelper.SendAuthedJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP, 
                ServerConfig.SERVER_PORT, 
                payload,
                DeviceIdentifier.GetDeviceId(),
                TCPNetworkManager.Instance.GetSessionToken()
            );
            
            if (!response.success)
            {
                Debug.LogError($"방 참가 실패: {response.error?.message}");
                return new JoinRoomResult 
                { 
                    success = false, 
                    errorMessage = response.error?.message ?? "알 수 없는 오류" 
                };
            }
            
            if (response.data != null)
            {
                try
                {
                    // 서버 응답의 data를 바로 JoinRoomData로 파싱
                    var joinData = JsonConvert.DeserializeObject<JoinRoomData>(response.data.ToString());
                    if (joinData != null)
                    {
                        return new JoinRoomResult
                        {
                            success = true,
                            hostAddress = joinData.hostAddress,
                            hostPort = joinData.hostPort,
                            roomInfo = joinData.roomInfo
                        };
                    }
                    else
                    {
                        Debug.LogError("서버 응답의 data 파싱 실패");
                        return new JoinRoomResult 
                        { 
                            success = false, 
                            errorMessage = "서버 응답 형식 오류" 
                        };
                    }
                }
                catch (JsonException jsonEx)
                {
                    Debug.LogError($"JSON 파싱 오류: {jsonEx.Message}");
                    Debug.LogError($"파싱 시도한 데이터: {response.data}");
                    return new JoinRoomResult 
                    { 
                        success = false, 
                        errorMessage = "서버 응답 형식 오류" 
                    };
                }
            }
            
            return new JoinRoomResult { success = false, errorMessage = "서버 응답이 비어있습니다" };
        }
        catch (NetworkException ex)
        {
            Debug.LogError($"네트워크 오류: {ex.Message}");
            return new JoinRoomResult { success = false, errorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            Debug.LogError($"방 참가 중 오류: {ex.Message}");
            return new JoinRoomResult { success = false, errorMessage = ex.Message };
        }
    }
    
    public async Task<List<PlayerInfo>> GetPlayerListAsync(string roomId)
    {
        try
        {
            var payload = new
            {
                type = "getPlayerList",
                roomId = roomId,
                deviceId = DeviceIdentifier.GetDeviceId()
            };

            var response = await TcpClientHelper.SendAuthedJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP, 
                ServerConfig.SERVER_PORT, 
                payload,
                DeviceIdentifier.GetDeviceId(),
                TCPNetworkManager.Instance.GetSessionToken()
            );
            
            if (!response.success)
            {
                Debug.LogError($"플레이어 목록 조회 실패: {response.error?.message}");
                return new List<PlayerInfo>();
            }
            
            if (response.data != null)
            {
                var playerListData = JsonConvert.DeserializeObject<PlayerListData>(response.data.ToString());
                return playerListData.players;
            }
            
            return new List<PlayerInfo>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"플레이어 목록 조회 중 오류: {ex.Message}");
            return new List<PlayerInfo>();
        }
    }
    
    public async Task<bool> LeaveRoomAsync(string roomId)
    {
        try
        {
            var payload = new
            {
                type = "leave",
                roomId = roomId,
                deviceId = DeviceIdentifier.GetDeviceId()
            };

            var response = await TcpClientHelper.SendAuthedJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP, 
                ServerConfig.SERVER_PORT, 
                payload,
                DeviceIdentifier.GetDeviceId(),
                TCPNetworkManager.Instance.GetSessionToken()
            );
            
            if (!response.success)
            {
                Debug.LogError($"방 퇴장 실패: {response.error?.message}");
                return false;
            }
            
            Debug.Log($"방 퇴장 성공: {roomId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"방 퇴장 중 오류: {ex.Message}");
            return false;
        }
    }
    
    public async Task<RoomInfo> GetRoomInfoAsync(string roomId)
    {
        try
        {
            var rooms = await GetRoomListAsync();
            return rooms.Find(room => room.roomId == roomId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"방 정보 조회 중 오류: {ex.Message}");
            return null;
        }
    }
    
    public async Task<List<RoomInfo>> SearchRoomsAsync(string gameType = null, int minPlayers = 0, int maxPlayers = 0)
    {
        try
        {
            var allRooms = await GetRoomListAsync();
            var filteredRooms = new List<RoomInfo>();
            
            foreach (var room in allRooms)
            {
                bool match = true;
                
                // 게임 타입 필터
                if (!string.IsNullOrEmpty(gameType) && room.gameType != gameType)
                {
                    match = false;
                }
                
                // 플레이어 수 필터
                if (minPlayers > 0 && room.currentPlayers < minPlayers)
                {
                    match = false;
                }
                
                if (maxPlayers > 0 && room.currentPlayers > maxPlayers)
                {
                    match = false;
                }
                
                if (match)
                {
                    filteredRooms.Add(room);
                }
            }
            
            Debug.Log($"방 검색 완료! 결과: {filteredRooms.Count}개");
            return filteredRooms;
        }
        catch (Exception ex)
        {
            Debug.LogError($"방 검색 중 오류: {ex.Message}");
            return new List<RoomInfo>();
        }
    }
} 