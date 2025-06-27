using UnityEngine;

public class NetworkRoomManagerSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupNetworkRoomManager()
    {
        // NetworkRoomManager가 씬에 없으면 생성
        if (FindObjectOfType<NetworkRoomManager>() == null)
        {
            GameObject roomManagerObj = new GameObject("NetworkRoomManager");
            roomManagerObj.AddComponent<NetworkRoomManager>();
            DontDestroyOnLoad(roomManagerObj);
            Debug.Log("NetworkRoomManager가 자동으로 생성되었습니다.");
        }
    }
} 