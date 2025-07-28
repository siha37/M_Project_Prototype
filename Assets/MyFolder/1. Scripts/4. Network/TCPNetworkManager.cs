using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class TCPNetworkManager : MonoBehaviour
{
    [Header("Network Components")]
    public RoomHost roomHost;
    public RoomGuest roomGuest;
    
    [Header("Connection Status")]
    public bool isConnected = false;
    public string connectionStatus = "Disconnected";
    
    // 이벤트
    public event Action<List<RoomInfo>> OnRoomListReceived;
    public event Action<NetworkError> OnConnectionError;
    public event Action<string> OnRoomCreated;
    public event Action<string> OnRoomDeleted;
    public event Action<bool> OnConnectionStatusChanged;
    public event Action<JoinRoomResult> OnRoomJoined;
    public event Action<string> OnRoomLeft;
    
    private static TCPNetworkManager instance;
    public static TCPNetworkManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TCPNetworkManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("NetworkManager");
                    instance = go.AddComponent<TCPNetworkManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNetwork();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeNetwork()
    {
        roomHost = new RoomHost();
        roomGuest = new RoomGuest();
        
        LogManager.Log(LogCategory.Network, "NetworkManager 초기화 완료", this);
    }
    
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            UpdateConnectionStatus("서버 연결 테스트 중...");

            if (!TcpClientHelper.IsServerReachable(ServerConfig.SERVER_IP, ServerConfig.SERVER_PORT))
            {
                UpdateConnectionStatus("서버에 연결할 수 없습니다");
                OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.ServerUnreachable, "서버에 연결할 수 없습니다"));
                return false;
            }

            // ★ 최초 인증 요청
            var authSuccess = await AuthAsync("플레이어닉네임"); // 닉네임은 적절히 전달
            if (!authSuccess)
            {
                UpdateConnectionStatus("인증 실패");
                OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.Unknown, "인증 실패"));
                return false;
            }

            var rooms = await roomGuest.GetRoomListAsync();
            UpdateConnectionStatus("서버 연결 성공");
            isConnected = true;
            OnConnectionStatusChanged?.Invoke(true);

            return true;
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"연결 실패: {ex.Message}");
            OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.Unknown, ex.Message));
            isConnected = false;
            OnConnectionStatusChanged?.Invoke(false);
            return false;
        }
    }
    
    public async Task<bool> CreateRoomAsync(string roomName, int maxPlayers = ServerConfig.DEFAULT_MAX_PLAYERS)
    {
        try
        {
            UpdateConnectionStatus("방을 생성하는 중...");
            var success = await roomHost.CreateRoomWithRetryAsync(roomName, maxPlayers);
            
            if (success)
            {
                UpdateConnectionStatus("방 생성 성공");
                // FishNet 호스트 서버 시작
                UpdateConnectionStatus("FishNet 호스트 서버 시작 중...");
                var fishNetSuccess = await FishNetConnector.Instance.StartHostAsync();
                if (fishNetSuccess)
                {
                    UpdateConnectionStatus("FishNet 호스트 서버 시작 성공");
                    OnRoomCreated?.Invoke(roomHost.GetSessionToken());
                    return true;
                }
                else
                {
                    UpdateConnectionStatus("FishNet 호스트 서버 시작 실패");
                    await DeleteRoomAsync(roomHost.GetSessionToken());
                    return false;
                }
            }
            else
            {
                UpdateConnectionStatus("방 생성 실패");
                return false;
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"방 생성 오류: {ex.Message}");
            OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.Unknown, ex.Message));
            return false;
        }
    }
    
    public async Task<bool> DeleteRoomAsync(string roomId)
    {
        try
        {
            UpdateConnectionStatus("방을 삭제하는 중...");
            // FishNet 연결 해제
            FishNetConnector.Instance.Disconnect();
            var success = await roomHost.DeleteRoomAsync(roomId);
            if (success)
            {
                UpdateConnectionStatus("방 삭제 성공");
                OnRoomDeleted?.Invoke(roomId);
                return true;
            }
            else
            {
                UpdateConnectionStatus("방 삭제 실패");
                return false;
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"방 삭제 오류: {ex.Message}");
            OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.Unknown, ex.Message));
            return false;
        }
    }
    
    public async Task<JoinRoomResult> JoinRoomAsync(string roomId, string playerName = "")
    {
        try
        {
            // 중복 참가 방지
            if (!string.IsNullOrEmpty(GetCurrentRoomId()))
            {
                LogManager.LogWarning(LogCategory.Network, "이미 방에 참가 중입니다. 먼저 현재 방을 나가주세요.", this);
                return new JoinRoomResult { success = false, errorMessage = "이미 방에 참가 중입니다" };
            }
            
            UpdateConnectionStatus("방에 참가하는 중...");
            var result = await roomGuest.JoinRoomAsync(roomId, playerName);
            
            if (result.success)
            {
                UpdateConnectionStatus("방 참가 성공");
                // FishNet 클라이언트 연결
                UpdateConnectionStatus("FishNet 클라이언트 연결 중...");
                var fishNetSuccess = await FishNetConnector.Instance.ConnectAsClientAsync(result.hostAddress, result.hostPort);
                if (fishNetSuccess)
                {
                    UpdateConnectionStatus("FishNet 클라이언트 연결 성공");
                    OnRoomJoined?.Invoke(result);
                }
                else
                {
                    UpdateConnectionStatus("FishNet 클라이언트 연결 실패");
                    await LeaveRoomAsync(roomId);
                    result.success = false;
                    result.errorMessage = "FishNet 연결 실패";
                }
                return result;
            }
            else
            {
                UpdateConnectionStatus($"방 참가 실패: {result.errorMessage}");
                OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.Unknown, result.errorMessage));
                return result;
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"방 참가 오류: {ex.Message}");
            OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.Unknown, ex.Message));
            return new JoinRoomResult { success = false, errorMessage = ex.Message };
        }
    }
    
    public async Task<bool> LeaveRoomAsync(string roomId)
    {
        try
        {
            UpdateConnectionStatus("방을 나가는 중...");
            // FishNet 연결 해제
            FishNetConnector.Instance.Disconnect();
            var payload = new
            {
                type = "leave",
                roomId = roomId,
                deviceId = DeviceIdentifier.GetDeviceId()
            };
            var response = await TcpClientHelper.SendJsonAsync<ApiResponse>(
                ServerConfig.SERVER_IP, 
                ServerConfig.SERVER_PORT, 
                payload
            );
            if (response.success)
            {
                UpdateConnectionStatus("방을 나갔습니다");
                OnRoomLeft?.Invoke(roomId);
                return true;
            }
            else
            {
                UpdateConnectionStatus($"방 나가기 실패: {response.error?.message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"방 나가기 오류: {ex.Message}");
            return false;
        }
    }
    
    public async Task<List<RoomInfo>> GetRoomListAsync(bool includePrivate = false)
    {
        try
        {
            UpdateConnectionStatus("방 목록을 불러오는 중...");
            var rooms = await roomGuest.GetRoomListAsync(includePrivate);
            
            UpdateConnectionStatus($"방 {rooms.Count}개 발견");
            OnRoomListReceived?.Invoke(rooms);
            
            return rooms;
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"방 목록 조회 오류: {ex.Message}");
            OnConnectionError?.Invoke(new NetworkError(NetworkErrorType.Unknown, ex.Message));
            return new List<RoomInfo>();
        }
    }
    
    public async Task<bool> SendHeartbeatAsync(string roomId, int playerCount = 0)
    {
        try
        {
            return await roomHost.SendHeartbeatAsync(roomId, playerCount);
        }
        catch (Exception ex)
        {
            LogManager.LogError(LogCategory.Network, $"하트비트 전송 오류: {ex.Message}", this);
            return false;
        }
    }
    
    private void UpdateConnectionStatus(string status)
    {
        connectionStatus = status;
        LogManager.Log(LogCategory.Network, $"NetworkManager {status}", this);
    }
    
    public string GetCurrentRoomId()
    {
        return roomHost?.GetRoomId();
    }

    private string sessionToken = null;

    public string GetSessionToken()
    {
        return sessionToken;
    }

    private void SetSessionToken(string token)
    {
        sessionToken = token;
        LogManager.Log(LogCategory.Network, $"TCPNetworkManager 세션 토큰 저장: {token}", this);
    }

    public async Task<bool> AuthAsync(string nickname)
    {
        var payload = new
        {
            type = "auth",
            deviceId = DeviceIdentifier.GetDeviceId(),
            nickname = nickname
        };

        var response = await TcpClientHelper.SendJsonAsync<ApiResponse>(
            ServerConfig.SERVER_IP,
            ServerConfig.SERVER_PORT,
            payload
        );

        if (response.success && response.data != null)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.data.ToString());
            if (dict != null && dict.ContainsKey("sessionToken"))
            {
                SetSessionToken(dict["sessionToken"].ToString());
                return true;
            }
        }
        LogManager.LogError(LogCategory.Network, "세션 토큰 발급 실패", this);
        return false;
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            // 애플리케이션 종료 시에만 연결 해제
            TcpClientHelper.DisconnectAsync();
            instance = null;
        }
    }
} 