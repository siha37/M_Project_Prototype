using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyNetworkManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private NetworkRoomManager networkRoomManager;
    [SerializeField] private string readyScene = "Ready";
    public string CurrentRoomId { get; private set; }

    private void Start()
    {
        if (networkManager == null)
            networkManager = InstanceFinder.NetworkManager;
        
        // NetworkRoomManager 찾기
        if (networkRoomManager == null)
            networkRoomManager = FindObjectOfType<NetworkRoomManager>();
    }

    public void StartHost()
    {
        if (networkManager == null)
            networkManager = InstanceFinder.NetworkManager;
        
        // 이미 연결된 상태인지 확인
        if (networkManager.IsServer || networkManager.IsClient)
        {
            Debug.LogWarning("이미 연결된 상태입니다. 호스트를 시작할 수 없습니다.");
            return;
        }
        
        // 먼저 네트워크 연결 시작
        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
        
        // 연결 완료 후 방 생성 (OnLoadEnd에서 처리)
    }

    public void JoinHost(string roomId)
    {
        if (networkManager == null)
            networkManager = InstanceFinder.NetworkManager;
        
        // 이미 연결된 상태인지 확인
        if (networkManager.IsServer || networkManager.IsClient)
        {
            Debug.LogWarning("이미 연결된 상태입니다. 다른 방에 참가할 수 없습니다.");
            return;
        }
        
        // 먼저 네트워크 연결 시작
        networkManager.ClientManager.StartConnection();
        
        // 연결 완료 후 방 참가 (OnLoadEnd에서 처리)
        CurrentRoomId = roomId;
    }

    private void OnEnable()
    {
        if (networkManager == null)
            networkManager = InstanceFinder.NetworkManager;
        networkManager.SceneManager.OnLoadEnd += OnLoadEnd;
    }

    private void OnDisable()
    {
        if (networkManager != null)
            networkManager.SceneManager.OnLoadEnd -= OnLoadEnd;
    }

    private void OnLoadEnd(SceneLoadEndEventArgs obj)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby")
        {
            if (networkManager.IsServer && networkManager.IsClient)
            {
                // 호스트인 경우 방 생성
                if (networkRoomManager != null)
                {
                    networkRoomManager.CreateRoomServerRpc();
                }
                
                networkManager.SceneManager.LoadGlobalScenes(new SceneLoadData(readyScene));
            }
            else if (networkManager.IsClient && !string.IsNullOrEmpty(CurrentRoomId))
            {
                // 클라이언트인 경우 방 참가
                if (networkRoomManager != null)
                {
                    networkRoomManager.JoinRoomServerRpc(CurrentRoomId);
                }
            }
        }
    }
}
