using FishNet;
using FishNet.Object;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class NetworkSingletonBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkObject singletonPrefab; // GameSettingManager + KeepAlive + NetworkObject가 붙은 프리팹
        [SerializeField] private NetworkObject playerRoleManager;
        private void Awake()
        {
            // (선택) 자신도 살려두고 싶다면
            DontDestroyOnLoad(gameObject);
            SpawnOnce();
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnServerConnectionState -= OnServerState;
        }

        private void OnServerState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
                SpawnOnce();
        }

        private void SpawnOnce()
        {
            if (!GameSettingManager.Instance)
            {
                var nob = Instantiate(singletonPrefab);
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }
            
            if (!PlayerRoleManager.Instance)
            {
                var nob = Instantiate(playerRoleManager);
                nob.SetIsGlobal(true);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
            }
            
        }
    }
}