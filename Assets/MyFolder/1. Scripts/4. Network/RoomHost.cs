using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;

public class RoomHost
{
    private string sessionToken;
    private string roomId;
    
    public async Task<bool> CreateRoomAsync(string roomName, int maxPlayers = ServerConfig.DEFAULT_MAX_PLAYERS)
    {
        try
        {
            var payload = new
            {
                type = "create",
                roomId = GenerateRoomId(),
                hostAddress = ServerConfig.GetLocalIPAddress(),
                hostPort = ServerConfig.FISHNET_PORT,
                maxPlayers = maxPlayers,
                roomName = roomName,
                gameType = ServerConfig.DEFAULT_GAME_TYPE,
                isPrivate = false,
                deviceId = DeviceIdentifier.GetDeviceId()
            };

            var response = await TcpClientHelper.SendJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP, 
                ServerConfig.SERVER_PORT, 
                payload
            );
            
            if (!response.success)
            {
                Debug.LogError($"방 생성 실패: {response.error?.message}");
                return false;
            }
            
            // 세션 토큰 및 roomId 저장 - 안전한 파싱
            if (response.data != null)
            {
                try
                {
                    Debug.Log($"서버 응답 데이터 타입: {response.data.GetType()}");
                    Debug.Log($"서버 응답 데이터: {JsonConvert.SerializeObject(response.data)}");
                    
                    var outer = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.data.ToString());
                    if (outer != null && outer.ContainsKey("data"))
                    {
                        var inner = JsonConvert.DeserializeObject<Dictionary<string, object>>(outer["data"].ToString());
                        //var inner = outer["data"] as Dictionary<string, object>;
                        if (inner != null && inner.ContainsKey("roomId") && inner.ContainsKey("sessionToken"))
                        {
                            roomId = inner["roomId"].ToString();
                            sessionToken = inner["sessionToken"].ToString();
                            Debug.Log($"방 생성 성공! Room ID: {roomId}, SessionToken: {sessionToken}");
                            return true;
                        }
                    }
                    Debug.LogError("방 생성 응답에 roomId 또는 sessionToken이 없음");
                    return false;
                }
                catch (JsonException jsonEx)
                {
                    Debug.LogError($"JSON 파싱 오류: {jsonEx.Message}");
                    Debug.LogError($"파싱 시도한 데이터: {response.data}");
                    return false;
                }
                catch (Exception parseEx)
                {
                    Debug.LogError($"데이터 파싱 오류: {parseEx.Message}");
                    return false;
                }
            }
            else
            {
                Debug.LogError("서버 응답에 data 필드가 없음");
                return false;
            }
        }
        catch (NetworkException ex)
        {
            Debug.LogError($"네트워크 오류: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"방 생성 중 오류: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> DeleteRoomAsync(string roomId)
    {
        try
        {
            var payload = new
            {
                type = "delete",
                roomId = roomId,
                sessionToken = sessionToken
            };

            var response = await TcpClientHelper.SendJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP, 
                ServerConfig.SERVER_PORT, 
                payload
            );
            
            if (!response.success)
            {
                Debug.LogError($"[RoomHost] 방 삭제 실패: {response.error?.message}");
                return false;
            }
            
            Debug.Log("[RoomHost] 방 삭제 성공!");
            sessionToken = null;
            this.roomId = null;
            return true;
        }
        catch (NetworkException ex)
        {
            Debug.LogError($"[RoomHost] 네트워크 오류: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RoomHost] 방 삭제 중 오류: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> SendHeartbeatAsync(string roomId, int playerCount = 0)
    {
        try
        {
            var payload = new
            {
                type = "heartbeat",
                roomId = roomId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                playerCount = playerCount
            };

            var response = await TcpClientHelper.SendJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP, 
                ServerConfig.SERVER_PORT, 
                payload
            );
            
            if (!response.success)
            {
                Debug.LogWarning($"하트비트 실패: {response.error?.message}");
                return false;
            }
            
            return true;
        }
        catch (NetworkException ex)
        {
            Debug.LogError($"하트비트 네트워크 오류: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"하트비트 중 오류: {ex.Message}");
            return false;
        }
    }
    private string GenerateRoomId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomValue = (int)UnityEngine.Random.Range(0, 0x100000000);
        
        // 16진수 문자열을 수동으로 생성
        var hexChars = "0123456789abcdef";
        var randomHex = "";
        for (int i = 0; i < 8; i++)
        {
            var digit = randomValue % 16;
            randomHex = hexChars[digit] + randomHex;
            randomValue /= 16;
        }
        
        return $"R{timestamp}{randomHex}";
    }
    public string GetRoomId()
    {
        return roomId;
    }
    public string GetSessionToken()
    {
        return sessionToken;
    }
} 