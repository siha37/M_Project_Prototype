using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private LobbyNetworkManager lobbyNetworkManager;

    [Header("UI References")]
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private RectTransform roomContent;
    [SerializeField] private TMP_Text hostRoomIdText;

    private readonly List<GameObject> spawnedEntries = new();

    private void Awake()
    {
        if (roomListPanel == null || roomContent == null || hostRoomIdText == null)
            BuildDefaultUI();
    }

    private void Start()
    {
        if (roomListPanel != null)
            roomListPanel.SetActive(false);
    }

    private void BuildDefaultUI()
    {
        Canvas canvas = new GameObject("LobbyCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        GameObject createBtn = CreateButton(canvas.transform, "Create Room", new Vector2(-100, 100));
        createBtn.GetComponent<Button>().onClick.AddListener(CreateRoom);

        GameObject joinBtn = CreateButton(canvas.transform, "Join Room", new Vector2(100, 100));
        joinBtn.GetComponent<Button>().onClick.AddListener(ShowRoomList);

        GameObject idTextObj = new GameObject("RoomIdText", typeof(RectTransform), typeof(TMP_Text));
        idTextObj.transform.SetParent(canvas.transform, false);
        hostRoomIdText = idTextObj.GetComponent<TMP_Text>();
        hostRoomIdText.alignment = TextAlignmentOptions.Center;
        hostRoomIdText.rectTransform.anchoredPosition = new Vector2(0, 50);

        roomListPanel = new GameObject("RoomListPanel", typeof(RectTransform));
        roomListPanel.transform.SetParent(canvas.transform, false);
        roomListPanel.SetActive(false);
        roomContent = roomListPanel.GetComponent<RectTransform>();
        VerticalLayoutGroup vlg = roomListPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
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
            hostRoomIdText.text = $"Room ID: {lobbyNetworkManager.CurrentRoomId}";
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

        if (roomContent == null)
            return;

        foreach (var room in RoomManager.Instance.Rooms)
        {
            GameObject entry = new GameObject("RoomEntry", typeof(RectTransform));
            entry.transform.SetParent(roomContent, false);

            HorizontalLayoutGroup layout = entry.AddComponent<HorizontalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            GameObject textObj = new GameObject("RoomId", typeof(TMP_Text));
            textObj.transform.SetParent(entry.transform, false);
            TMP_Text text = textObj.GetComponent<TMP_Text>();
            text.text = room.RoomId;
            text.fontSize = 24;

            GameObject buttonObj = new GameObject("JoinButton", typeof(Button), typeof(TMP_Text));
            buttonObj.transform.SetParent(entry.transform, false);
            TMP_Text btnText = buttonObj.GetComponent<TMP_Text>();
            btnText.text = "Join";
            btnText.fontSize = 24;
            Button button = buttonObj.GetComponent<Button>();
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
