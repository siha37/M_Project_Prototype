using UnityEngine;
using UnityEngine.SceneManagement;

public class GuestReadyFlow : MonoBehaviour
{
    private bool isSceneLoading = false;

    [SerializeField] private string SceneName;
    void Start()
    {
        FishNetConnector.Instance.OnFishNetConnectionChanged += OnFishNetConnectionChanged;
    }
    private void OnFishNetConnectionChanged(bool connected)
    {
        if (connected && !FishNetConnector.Instance.IsHost() && FishNetConnector.Instance.IsClient() && !isSceneLoading)
        {
            isSceneLoading = true;
            LogManager.Log(LogCategory.Network, "게스트 FishNet 연결 완료! Ready 씬으로 이동");
            SceneManager.LoadSceneAsync(SceneName);
        }
    }

    void OnDestroy()
    {
        if (FishNetConnector.Instance != null)
            FishNetConnector.Instance.OnFishNetConnectionChanged -= OnFishNetConnectionChanged;
    }
} 