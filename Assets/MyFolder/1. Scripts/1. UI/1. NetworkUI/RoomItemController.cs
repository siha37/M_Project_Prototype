using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class RoomItemController : MonoBehaviour
{
    
    
    [Header("UI References")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI gameTypeText;
    [FormerlySerializedAs("hostInfoText")] public TextMeshProUGUI RoomIdText;
    public Button joinButton;
    public Image backgroundImage;
    
    [Header("Room Data")]
    public string roomId;
    public RoomInfo roomInfo;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 1f, 1f);
    public Color selectedColor = new Color(0.7f, 0.7f, 1f, 1f);
    
    
    private NetworkUIManager networkUIManager;
    private bool isHovered = false;
    private bool isSelected = false;
    
    void Start()
    {
        InitializeUI();
    }
    void InitializeUI()
    {
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }
    }
    
    
    public void SetRoomData(RoomInfo info,NetworkUIManager _networkUIManager)
    {
        roomInfo = info;
        roomId = info.roomId;
        networkUIManager = _networkUIManager;
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (roomInfo == null) return;
        
        // 방 이름 업데이트
        if (roomNameText)
        {
            roomNameText.text = $"{roomInfo.roomName} ({roomInfo.currentPlayers}/{roomInfo.maxPlayers})";
        }
        
        // 게임 타입 업데이트
        if (gameTypeText)
        {
            string gameTypeDisplay = GetGameTypeDisplayName(roomInfo.gameType);
            gameTypeText.text = $"게임 타입: {gameTypeDisplay}";
        }
        
        // 호스트 정보 업데이트
        if (RoomIdText)
        {
            RoomIdText.text = $"ID: {roomId}";
        }
        
        // 참가 버튼 상태 업데이트
        if (joinButton)
        {
            bool canJoin = roomInfo.currentPlayers < roomInfo.maxPlayers && roomInfo.status == "active";
            joinButton.interactable = canJoin;
            
            var buttonText = joinButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText)
            {
                buttonText.text = canJoin ? "참가" : "만원";
            }
        }
    }
    
    string GetGameTypeDisplayName(string gameType)
    {
        switch (gameType.ToLower())
        {
            case "mafia": return "마피아";
            case "racing": return "레이싱";
            case "fps": return "FPS";
            case "rpg": return "RPG";
            default: return gameType;
        }
    }
    
    void OnPointerEnter()
    {
        isHovered = true;
        UpdateBackgroundColor();
    }
    
    void OnPointerExit()
    {
        isHovered = false;
        UpdateBackgroundColor();
    }
    
    void UpdateBackgroundColor()
    {
        if (backgroundImage == null) return;
        
        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else if (isHovered)
        {
            backgroundImage.color = hoverColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }
    
    
    async void OnJoinButtonClicked()
    {
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogWarning("방 ID가 없습니다!");
            return;
        }
        
        Debug.Log($"방 참가 시도: {roomId}");
        
        // 여기에 FishNet 연결 로직 추가
        // NetworkManager.Instance.JoinRoom(roomId);
        
        // 임시로 방 정보 출력
        if (roomInfo != null)
        {
            Debug.Log($"방 정보: {roomInfo.roomName} - {roomInfo.hostAddress}:{roomInfo.hostPort}");
        }

        networkUIManager.OnJoinRoomButtonClicked(roomId);
    }
    
    void OnDestroy()
    {
        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(OnJoinButtonClicked);
        }
    }
} 