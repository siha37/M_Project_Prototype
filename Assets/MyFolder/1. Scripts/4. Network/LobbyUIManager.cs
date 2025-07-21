using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private LobbyNetworkManager lobbyNetworkManager;
    [SerializeField] private NetworkRoomManager networkRoomManager;

    [Header("UI References")]
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private RectTransform roomContent;
    [SerializeField] private TMP_Text hostRoomIdText;

    private readonly List<GameObject> spawnedEntries = new();

    private void Start()
    {
        if (roomListPanel != null)
            roomListPanel.SetActive(false);
        
        // NetworkRoomManager 찾기
        if (networkRoomManager == null)
            networkRoomManager = FindObjectOfType<NetworkRoomManager>();
        
        // 방 목록 업데이트 이벤트 구독
        if (networkRoomManager != null)
        {
            networkRoomManager.OnRoomListUpdated += RefreshRoomList;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (networkRoomManager != null)
        {
            networkRoomManager.OnRoomListUpdated -= RefreshRoomList;
        }
    }

    private GameObject CreateButton(Transform parent, string text, Vector2 pos)
    {
        GameObject btnObj = new GameObject(text.Replace(" ", "") + "Button", typeof(RectTransform), typeof(Button), typeof(TMP_Text));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        TMP_Text tmp = btnObj.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        return btnObj;
    }

    public void CreateRoom()
    {
        lobbyNetworkManager.StartHost();
        if (hostRoomIdText != null)
            hostRoomIdText.text = "방 생성 중...";
    }

    public void ShowRoomList()
    {
        if (roomListPanel != null)
            roomListPanel.SetActive(true);
        RefreshRoomList();
    }

    private void RefreshRoomList()
    {
        foreach (var go in spawnedEntries)
            Destroy(go);
        spawnedEntries.Clear();

        if (roomContent == null || networkRoomManager == null)
            return;

        foreach (var room in networkRoomManager.Rooms)
        {
            GameObject entry = new GameObject("RoomEntry", typeof(RectTransform));
            entry.transform.SetParent(roomContent, false);

            HorizontalLayoutGroup layout = entry.AddComponent<HorizontalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            GameObject textObj = new GameObject("RoomId");
            textObj.transform.SetParent(entry.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"{room.RoomId} ({room.CurrentPlayers}/{room.MaxPlayers})";
            text.fontSize = 24;

            GameObject buttonObj = new GameObject("JoinButton");
            buttonObj.transform.SetParent(entry.transform, false);
            TextMeshProUGUI btnText = buttonObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "Join";
            btnText.fontSize = 24;
            Button button = buttonObj.AddComponent<Button>();
            string id = room.RoomId;
            button.onClick.AddListener(() => JoinRoom(id));

            spawnedEntries.Add(entry);
        }
    }

    private void JoinRoom(string id)
    {
        lobbyNetworkManager.JoinHost(id);
    }
}
