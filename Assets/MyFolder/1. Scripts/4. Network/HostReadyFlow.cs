using UnityEngine;
using UnityEngine.SceneManagement;

public class HostReadyFlow : MonoBehaviour
{
    private bool isSceneLoading = false;
    [SerializeField] private string SceneName;
    void Start()
    {
        FishNetConnector.Instance.OnFishNetConnectionChanged += OnFishNetConnectionChanged;
    }


    private void OnFishNetConnectionChanged(bool connected)
    {
        if (connected && FishNetConnector.Instance.IsHost() && !isSceneLoading)
        {
            isSceneLoading = true;
            Debug.Log("호스트 FishNet 서버 시작 완료! Ready 씬으로 이동");
            SceneManager.LoadSceneAsync(SceneName);
        }
    }

    void OnDestroy()
    {
        if (FishNetConnector.Instance != null)
            FishNetConnector.Instance.OnFishNetConnectionChanged -= OnFishNetConnectionChanged;
    }
} 