using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Button connectButton;
    public Button createRoomButton;
    public Button refreshButton;
    public Button deleteRoomButton;
    
    public TMP_InputField roomNameInput;
    public TMP_InputField maxPlayersInput;
    public TMP_Dropdown gameTypeDropdown;
    
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI roomIdText;
    public TextMeshProUGUI connectionStatusText;
    
    [Header("FishNet UI")]
    public TextMeshProUGUI fishNetStatusText;
    
    public Transform roomListContent;
    public GameObject roomItemPrefab;
    
    [Header("Settings")]
    public bool autoRefresh = true;
    public float refreshInterval = 5f;
    
    private List<GameObject> roomItems = new List<GameObject>();
    private float lastRefreshTime;
    
    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
        OnConnectButtonClicked();
    }
    
    void Update()
    {
        UpdateUI();
        
        if (autoRefresh && TCPNetworkManager.Instance.isConnected)
        {
            if (Time.time - lastRefreshTime > refreshInterval)
            {
                RefreshRoomList();
            }
        }
    }
    
    void InitializeUI()
    {
            
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
            
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
            
        if (deleteRoomButton != null)
            deleteRoomButton.onClick.AddListener(OnDeleteRoomButtonClicked);
        
        // 초기 UI 상태 설정
        SetUIState(false);
        
        // 게임 타입 드롭다운 초기화
        if (gameTypeDropdown != null)
        {
            gameTypeDropdown.ClearOptions();
            gameTypeDropdown.AddOptions(new List<string> { "mafia", "racing", "fps", "rpg" });
        }
        
        // 기본값 설정
        if (maxPlayersInput != null)
            maxPlayersInput.text = ServerConfig.DEFAULT_MAX_PLAYERS.ToString();
    }
    
    void SubscribeToEvents()
    {
        TCPNetworkManager.Instance.OnRoomListReceived += OnRoomListReceived;
        TCPNetworkManager.Instance.OnConnectionError += OnConnectionError;
        TCPNetworkManager.Instance.OnRoomCreated += OnRoomCreated;
        TCPNetworkManager.Instance.OnRoomDeleted += OnRoomDeleted;
        TCPNetworkManager.Instance.OnConnectionStatusChanged += OnConnectionStatusChanged;
        
        // FishNet 이벤트 구독
        FishNetConnector.Instance.OnFishNetConnectionChanged += OnFishNetConnectionChanged;
        FishNetConnector.Instance.OnFishNetError += OnFishNetError;
    }
    
    void UnsubscribeFromEvents()
    {
        if (TCPNetworkManager.Instance != null)
        {
            TCPNetworkManager.Instance.OnRoomListReceived -= OnRoomListReceived;
            TCPNetworkManager.Instance.OnConnectionError -= OnConnectionError;
            TCPNetworkManager.Instance.OnRoomCreated -= OnRoomCreated;
            TCPNetworkManager.Instance.OnRoomDeleted -= OnRoomDeleted;
            TCPNetworkManager.Instance.OnConnectionStatusChanged -= OnConnectionStatusChanged;
        }
        
        // FishNet 이벤트 구독 해제
        if (FishNetConnector.Instance != null)
        {
            FishNetConnector.Instance.OnFishNetConnectionChanged -= OnFishNetConnectionChanged;
            FishNetConnector.Instance.OnFishNetError -= OnFishNetError;
        }
    }
    
    async void OnConnectButtonClicked()
    {
        SetUIState(false);
        UpdateStatusText("서버에 연결하는 중...");
        
        var success = await TCPNetworkManager.Instance.TestConnectionAsync();
        
        if (success)
        {
            SetUIState(true);
            UpdateStatusText("서버 연결 성공!");
            RefreshRoomList();
        }
        else
        {
            SetUIState(false);
            UpdateStatusText("서버 연결 실패");
        }
    }
    
    async void OnCreateRoomButtonClicked()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            UpdateStatusText("방 이름을 입력해주세요");
            return;
        }
        
        int maxPlayers = ServerConfig.DEFAULT_MAX_PLAYERS;
        if (!string.IsNullOrEmpty(maxPlayersInput.text))
        {
            int.TryParse(maxPlayersInput.text, out maxPlayers);
        }
        
        SetUIState(false);
        UpdateStatusText("방을 생성하는 중...");
        
        var success = await TCPNetworkManager.Instance.CreateRoomAsync(roomNameInput.text, maxPlayers);
        
        if (success)
        {
            UpdateStatusText("방 생성 성공!");
            UpdateRoomIdText(TCPNetworkManager.Instance.GetCurrentRoomId());
            HeartbeatManager.Instance.StartHeartbeat(TCPNetworkManager.Instance.GetCurrentRoomId());
        }
        else
        {
            UpdateStatusText("방 생성 실패");
        }
        
        SetUIState(true);
    }
    
    async void OnRefreshButtonClicked()
    {
        if (!TCPNetworkManager.Instance.isConnected)
        {
            UpdateStatusText("서버에 연결되지 않았습니다");
            return;
        }
        
        UpdateStatusText("방 목록을 새로고침하는 중...");
        await TCPNetworkManager.Instance.GetRoomListAsync();
        lastRefreshTime = Time.time;
    }
    
    async void OnDeleteRoomButtonClicked()
    {
        var roomId = TCPNetworkManager.Instance.GetCurrentRoomId();
        if (string.IsNullOrEmpty(roomId))
        {
            UpdateStatusText("삭제할 방이 없습니다");
            return;
        }
        
        SetUIState(false);
        UpdateStatusText("방을 삭제하는 중...");
        
        var success = await TCPNetworkManager.Instance.DeleteRoomAsync(roomId);
        
        if (success)
        {
            UpdateStatusText("방 삭제 성공!");
            UpdateRoomIdText("");
            HeartbeatManager.Instance.StopHeartbeat();
        }
        else
        {
            UpdateStatusText("방 삭제 실패");
        }
        
        SetUIState(true);
    }
    
    public async void OnJoinRoomButtonClicked(string roomId, string playerName ="")
    {
        if (string.IsNullOrEmpty(roomId))
        {
            UpdateStatusText("참가할 방 ID를 입력해주세요");
            return;
        }
        
        SetUIState(false);
        UpdateStatusText("방에 참가하는 중...");
        
        var result = await TCPNetworkManager.Instance.JoinRoomAsync(roomId, playerName);
        
        if (result.success)
        {
            UpdateStatusText($"방 참가 성공! 호스트: {result.hostAddress}:{result.hostPort}");
            UpdateRoomIdText(roomId);
            
            // 게스트도 하트비트 시작
            HeartbeatManager.Instance.StartHeartbeat(roomId);
            
            // 연결 상태 확인
                    LogManager.Log(LogCategory.UI, $"NetworkUIManager 방 참가 후 연결 상태: {TcpClientHelper.IsConnected()}");
        LogManager.Log(LogCategory.UI, $"NetworkUIManager 하트비트 상태: {HeartbeatManager.Instance.IsHeartbeating()}");
        }
        else
        {
            UpdateStatusText($"방 참가 실패: {result.errorMessage}");
        }
        
        SetUIState(true);
    }
    
    async void OnLeaveRoomButtonClicked()
    {
        var roomId = TCPNetworkManager.Instance.GetCurrentRoomId();
        if (string.IsNullOrEmpty(roomId))
        {
            UpdateStatusText("퇴장할 방이 없습니다");
            return;
        }
        
        SetUIState(false);
        UpdateStatusText("방에서 퇴장하는 중...");
        
        var success = await TCPNetworkManager.Instance.LeaveRoomAsync(roomId);
        
        if (success)
        {
            UpdateStatusText("방 퇴장 성공!");
            UpdateRoomIdText("");
        }
        else
        {
            UpdateStatusText("방 퇴장 실패");
        }
        
        SetUIState(true);
    }
    
    void OnRoomListReceived(List<RoomInfo> rooms)
    {
        ClearRoomList();
        
        foreach (var room in rooms)
        {
            CreateRoomItem(room);
        }
        
        UpdateStatusText($"방 {rooms.Count}개 발견");
    }
    
    void OnConnectionError(NetworkError error)
    {
        UpdateStatusText($"오류: {error.message}");
                    LogManager.LogError(LogCategory.UI, $"네트워크 오류: {error.type} - {error.message}");
    }
    
    void OnRoomCreated(string roomId)
    {
        UpdateRoomIdText(roomId);
        UpdateStatusText($"방 생성됨: {roomId}");
    }
    
    void OnRoomDeleted(string roomId)
    {
        UpdateRoomIdText("");
        UpdateStatusText($"방 삭제됨: {roomId}");
    }
    
    void OnConnectionStatusChanged(bool connected)
    {
        SetUIState(connected);
        UpdateConnectionStatusText(connected ? "연결됨" : "연결 끊김");
    }
    
    // FishNet 이벤트 핸들러
    void OnFishNetConnectionChanged(bool connected)
    {
        UpdateFishNetStatusText(connected ? "FishNet 연결됨" : "FishNet 연결 끊김");
    }
    
    void OnFishNetError(string error)
    {
        UpdateStatusText($"FishNet 오류: {error}");
    }
    
    void CreateRoomItem(RoomInfo room)
    {
        if (roomItemPrefab == null || roomListContent == null) return;
        
        var roomItem = Instantiate(roomItemPrefab, roomListContent);
        roomItems.Add(roomItem);
        
        // 방 정보 표시
        RoomItemController roomNameText = roomItem.GetComponent<RoomItemController>();
        if (roomNameText)
        {
            roomNameText.SetRoomData(room,this);
        }
    }
    
    void ClearRoomList()
    {
        foreach (var item in roomItems)
        {
            if (item)
                Destroy(item);
        }
        roomItems.Clear();
    }
    
    void SetUIState(bool connected)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = connected;
            
        if (refreshButton != null)
            refreshButton.interactable = connected;
            
        if (deleteRoomButton != null)
            deleteRoomButton.interactable = connected && !string.IsNullOrEmpty(TCPNetworkManager.Instance.GetCurrentRoomId());
    }
    
    void UpdateStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
    
    void UpdateRoomIdText(string roomId)
    {
        if (roomIdText != null)
            roomIdText.text = $"현재 방: {roomId}";
    }
    
    void UpdateConnectionStatusText(string text)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = text;
    }
    
    void UpdateFishNetStatusText(string text)
    {
        if (fishNetStatusText != null)
            fishNetStatusText.text = text;
    }
    
    void UpdateUI()
    {
        // 연결 상태 업데이트
        if (connectionStatusText != null)
        {
            connectionStatusText.text = TCPNetworkManager.Instance.connectionStatus;
        }
        
        // FishNet 상태 업데이트
        if (fishNetStatusText != null)
        {
            fishNetStatusText.text = FishNetConnector.Instance.fishNetStatus;
        }
        
        // 하트비트 상태 표시
        if (HeartbeatManager.Instance.IsHeartbeating())
        {
            var timeSinceHeartbeat = HeartbeatManager.Instance.GetTimeSinceLastHeartbeat();
            if (statusText != null)
            {
                statusText.text = $"하트비트: {timeSinceHeartbeat:F1}초 전";
            }
        }
    }
    
    async void RefreshRoomList()
    {
        if (TCPNetworkManager.Instance.isConnected)
        {
            await TCPNetworkManager.Instance.GetRoomListAsync();
            lastRefreshTime = Time.time;
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
} 