using System;
using System.Threading.Tasks;
using UnityEngine;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEngine.Serialization;

public class FishNetConnector : MonoBehaviour
{
    [FormerlySerializedAs("NetworkManager")] [FormerlySerializedAs("tcpNetworkManager")] [FormerlySerializedAs("networkManager")] [Header("FishNet Settings")]
    public NetworkManager fishNetworkManager;
    public Tugboat tugboat;
    
    [Header("Connection Status")]
    public bool isFishNetConnected = false;
    public string fishNetStatus = "연결 안됨";
    
    // 이벤트
    public event Action<bool> OnFishNetConnectionChanged;
    public event Action<string> OnFishNetError;
    
    private static FishNetConnector instance;
    public static FishNetConnector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FishNetConnector>();
                if (instance == null)
                {
                    GameObject go = new GameObject("FishNetConnector");
                    instance = go.AddComponent<FishNetConnector>();
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
            InitializeFishNet();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeFishNet()
    {
        // NetworkManager 찾기
        if (fishNetworkManager == null)
        {
            fishNetworkManager = FindAnyObjectByType<NetworkManager>();
        }
        
        // Tugboat 찾기
        if (tugboat == null && fishNetworkManager != null)
        {
            tugboat = fishNetworkManager.GetComponent<Tugboat>();
        }
        
        if (fishNetworkManager == null || tugboat == null)
        {
            LogManager.LogError(LogCategory.Network, "FishNet NetworkManager 또는 Tugboat를 찾을 수 없습니다!");
            return;
        }
        
        // 이벤트 구독
        fishNetworkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        fishNetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        
        LogManager.Log(LogCategory.Network, "FishNetConnector 초기화 완료");
    }
    
    // 호스트로 서버 시작
    public async Task<bool> StartHostAsync()
    {
        try
        {
            UpdateFishNetStatus("호스트 서버 시작 중...");
            
            if (fishNetworkManager == null || tugboat == null)
            {
                UpdateFishNetStatus("FishNet 컴포넌트가 없습니다");
                return false;
            }
            
            // 포트 설정
            tugboat.SetPort(ServerConfig.FISHNET_PORT);
            
            // 서버 시작
            fishNetworkManager.ServerManager.StartConnection();
            
            // 연결 대기
            float timeout = 10f;
            float elapsed = 0f;
            
            while (!fishNetworkManager.ServerManager.Started && elapsed < timeout)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }
            
            if (fishNetworkManager.ServerManager.Started)
            {
                UpdateFishNetStatus("호스트 서버 시작 성공");
                fishNetworkManager.ClientManager.StartConnection();
                elapsed = 0;
                while (!fishNetworkManager.ClientManager.Started && elapsed < timeout)
                {
                    await Task.Delay(100);
                    elapsed += 0.1f;
                }

                if (fishNetworkManager.ClientManager.Started)
                {
                    isFishNetConnected = true;
                    OnFishNetConnectionChanged?.Invoke(true);
                    return true;   
                }
                else
                {
                    UpdateFishNetStatus("호스트 클라 활성화 실패");
                    return false;
                }
            }
            else
            {
                UpdateFishNetStatus("호스트 서버 시작 실패");
                return false;
            }
        }
        catch (Exception ex)
        {
            UpdateFishNetStatus($"호스트 시작 오류: {ex.Message}");
            OnFishNetError?.Invoke(ex.Message);
            return false;
        }
    }
    
    // 게스트로 클라이언트 연결
    public async Task<bool> ConnectAsClientAsync(string hostAddress, int hostPort)
    {
        try
        {
            UpdateFishNetStatus($"게스트로 연결 중... {hostAddress}:{hostPort}");
            
            if (fishNetworkManager == null || tugboat == null)
            {
                UpdateFishNetStatus("FishNet 컴포넌트가 없습니다");
                return false;
            }
            
            // 서버 주소와 포트 설정
            tugboat.SetClientAddress(hostAddress);
            tugboat.SetPort((ushort)hostPort);
            
            // 클라이언트 연결
            fishNetworkManager.ClientManager.StartConnection();
            
            // 연결 대기
            float timeout = 10f;
            float elapsed = 0f;
            
            while (!fishNetworkManager.ClientManager.Started && elapsed < timeout)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }
            
            if (fishNetworkManager.ClientManager.Started)
            {
                UpdateFishNetStatus("게스트 연결 성공");
                isFishNetConnected = true;
                OnFishNetConnectionChanged?.Invoke(true);
                return true;
            }
            else
            {
                UpdateFishNetStatus("게스트 연결 실패");
                return false;
            }
        }
        catch (Exception ex)
        {
            UpdateFishNetStatus($"게스트 연결 오류: {ex.Message}");
            OnFishNetError?.Invoke(ex.Message);
            return false;
        }
    }
    
    // 연결 해제
    public void Disconnect()
    {
        try
        {
            if (fishNetworkManager != null)
            {
                fishNetworkManager.ServerManager.StopConnection(true);
                fishNetworkManager.ClientManager.StopConnection();
            }
            
            UpdateFishNetStatus("연결 해제됨");
            isFishNetConnected = false;
            OnFishNetConnectionChanged?.Invoke(false);
        }
        catch (Exception ex)
        {
            LogManager.LogError(LogCategory.Network, $"연결 해제 오류: {ex.Message}");
        }
    }
    
    // 서버 연결 상태 콜백
    private void OnServerConnectionState(ServerConnectionStateArgs stateArgs)
    {
        switch (stateArgs.ConnectionState)
        {
            case LocalConnectionState.Started:
                UpdateFishNetStatus("서버 시작됨");
                break;
            case LocalConnectionState.Stopped:
                UpdateFishNetStatus("서버 중지됨");
                isFishNetConnected = false;
                OnFishNetConnectionChanged?.Invoke(false);
                break;
            case LocalConnectionState.Starting:
                UpdateFishNetStatus("서버 시작 중...");
                break;
            case LocalConnectionState.Stopping:
                UpdateFishNetStatus("서버 중지 중...");
                break;
        }
    }
    
    // 클라이언트 연결 상태 콜백
    private void OnClientConnectionState(ClientConnectionStateArgs stateArgs)
    {
        switch (stateArgs.ConnectionState)
        {
            case LocalConnectionState.Started:
                UpdateFishNetStatus("클라이언트 연결됨");
                break;
            case LocalConnectionState.Stopped:
                UpdateFishNetStatus("클라이언트 연결 해제됨");
                isFishNetConnected = false;
                OnFishNetConnectionChanged?.Invoke(false);
                break;
            case LocalConnectionState.Starting:
                UpdateFishNetStatus("클라이언트 연결 중...");
                break;
            case LocalConnectionState.Stopping:
                UpdateFishNetStatus("클라이언트 연결 해제 중...");
                break;
        }
    }
    
    private void UpdateFishNetStatus(string status)
    {
        fishNetStatus = status;
        LogManager.Log(LogCategory.Network, $"FishNetConnector {status}");
    }
    
    // 현재 연결 상태 확인
    public bool IsHost()
    {
        return fishNetworkManager != null && fishNetworkManager.ServerManager.Started;
    }
    
    public bool IsClient()
    {
        return fishNetworkManager != null && fishNetworkManager.ClientManager.Started;
    }
    
    public bool IsConnected()
    {
        return isFishNetConnected && (IsHost() || IsClient());
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            if (fishNetworkManager != null)
            {
                fishNetworkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                fishNetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            }
            instance = null;
        }
    }
} 