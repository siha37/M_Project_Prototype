using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyNetworkManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string readyScene = "Ready";

    public void StartHost()
    {
        if (networkManager == null)
            networkManager = InstanceFinder.NetworkManager;
        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
    }

    public void JoinHost()
    {
        if (networkManager == null)
            networkManager = InstanceFinder.NetworkManager;
        networkManager.ClientManager.StartConnection();
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
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            if (networkManager.IsServer && networkManager.IsClient)
            {
                networkManager.SceneManager.LoadGlobalScenes(new SceneLoadData(readyScene));
            }
        }
    }
}
