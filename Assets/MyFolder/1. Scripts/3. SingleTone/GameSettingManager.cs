using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace MyFolder._1._Scripts._5._Manager
{
    [System.Serializable]
    public class GameSettings
    {
        public int maxPlayers = 8;
        public bool friendlyFire = false;
        public float roundTime = 300f; // 5분
    }
    public class GameSettingManager : NetworkBehaviour
    {
        // ✅ 싱글톤 인스턴스
        public static GameSettingManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private GameSettings defaultSettings;
        
        // 동기화된 게임 설정
        private readonly SyncVar<GameSettings> syncSettings = new SyncVar<GameSettings>();
        private readonly SyncVar<int> syncPlayerCount = new SyncVar<int>();

        // Debouncing용 코루틴
        private Coroutine updatePlayerCountCoroutine;
        
        // 이벤트
        public event System.Action OnSettingsChanged;
        public event System.Action<int, int> OnPlayerCountChanged; // current, max
        public event System.Action OnGameStarted;
        public event System.Action OnGameEnded; // ✅ 게임 종료 이벤트 추가

        private void Awake()
        {
            // ✅ 싱글톤 패턴 구현 (DontDestroyOnLoad 제거)
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[GameSettingManager] 싱글톤 인스턴스 생성");
            }
            else if (Instance != this)
            {
                Debug.LogWarning("[GameSettingManager] 이미 인스턴스가 존재합니다. 중복 오브젝트를 제거합니다.");
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            // ✅ 인스턴스가 설정되어 있는지 확인 (DontDestroyOnLoad 제거)
            if (Instance == null)
            {
                Instance = this;
            }
            
            syncSettings.Value = defaultSettings;
            NetworkManager.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
            
            // ✅ FishNet 씬 매니저 이벤트 구독 (오브젝트 유지를 위해)
            if (NetworkManager?.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadStart += OnSceneLoadStart;
                NetworkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
            }
            
            UpdatePlayerCount();
            Debug.Log("[GameSettingManager] 서버 시작 - 기본 설정 적용됨");
        }

        public override void OnStartClient()
        {
            // ✅ 인스턴스가 설정되어 있는지 확인 (DontDestroyOnLoad 제거)
            if (Instance == null)
            {
                Instance = this;
            }
            
            syncSettings.OnChange += OnSettingChangedCallback;
            syncPlayerCount.OnChange += OnPlayerCountChangedCallback;
            Debug.Log("[GameSettingManager] 클라이언트 시작 - 동기화 콜백 등록됨");
        }
        
        // ✅ FishNet 씬 전환 이벤트 핸들러 추가
        private void OnSceneLoadStart(FishNet.Managing.Scened.SceneLoadStartEventArgs args)
        {
            Debug.Log($"[GameSettingManager] 씬 로딩 시작: {string.Join(", ", args.QueueData.GlobalScenes)}");
        }
        
        private void OnSceneLoadEnd(FishNet.Managing.Scened.SceneLoadEndEventArgs args)
        {
            Debug.Log($"[GameSettingManager] 씬 로딩 완료: {string.Join(", ", args.LoadedScenes)}");
            
            // 씬 전환 후에도 인스턴스 유지 확인
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[GameSettingManager] 씬 전환 후 인스턴스 복구됨");
            }
        }
        
        // 세팅값 요청 리퀘스트
        public void RequestUpdateSettings(GameSettings gameSettings)
        {
            if (!IsHostInitialized) return;
            UpdateGameSettingsServerRpc(gameSettings);
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateGameSettingsServerRpc(GameSettings gameSettings)
        {
            syncSettings.Value = gameSettings;
            Debug.Log("게임 설정 업데이트됨");
        }

        public void RequestStartGame()
        {
            if(!IsHostInitialized) return;
            StartGameServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void StartGameServerRpc()
        {
            if (syncPlayerCount.Value < 2)
            {
                Debug.LogWarning("최소 4명의 플레이어가 필요합니다");
                return;
            }
            
            NotifyGameStartClientRpc();
            
            Debug.Log("게임시작");
        }
        
        // ✅ 게임 종료 요청 메서드 추가
        public void RequestEndGame()
        {
            if (!IsHostInitialized) return;
            EndGameServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void EndGameServerRpc()
        {
            Debug.Log("[GameSettingManager] 게임 종료 요청됨");
            NotifyGameEndClientRpc();
        }

        [ObserversRpc]
        private void NotifyGameEndClientRpc()
        {
            Debug.Log("[GameSettingManager] 게임 종료 알림");
            OnGameEnded?.Invoke();
        }

        [ObserversRpc]
        private void NotifyGameStartClientRpc()
        {
            OnGameStarted?.Invoke();
            
            if(IsHostInitialized)
                LoadMainStageScene();
        }

        void OnPlayerConnectionChanged(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
        {
            if (!IsHostInitialized) return;
            
            // 기존 코루틴 취소하고 새로 시작 (Debouncing)
            if (updatePlayerCountCoroutine != null)
            {
                StopCoroutine(updatePlayerCountCoroutine);
            }
            
            updatePlayerCountCoroutine = StartCoroutine(UpdatePlayerCountDelayed());
        }
        
        private IEnumerator UpdatePlayerCountDelayed()
        {
            // 0.1초 대기 후 업데이트 (연속 호출 방지)
            yield return new WaitForSeconds(0.1f);
            
            UpdatePlayerCount();
            updatePlayerCountCoroutine = null;
        }
        
        /// <summary>
        /// Player 인원 업데이트
        /// </summary>
        private void UpdatePlayerCount()
        {
            if (!IsServerInitialized) return;
            
            int currentCount = base.NetworkManager.ServerManager.Clients.Count;
            
            // ✅ 값이 실제로 변했을 때만 업데이트
            if (currentCount == 1 || syncPlayerCount.Value != currentCount)
            {
                Debug.Log($"플레이어 수 변경: {syncPlayerCount.Value} → {currentCount}");
                syncPlayerCount.Value = currentCount;
            }
        }
        

        private void OnSettingChangedCallback(GameSettings prev, GameSettings next, bool asserver)
        {
            OnSettingsChanged?.Invoke();
        }

        private void OnPlayerCountChangedCallback(int prev, int next, bool asserver)
        {
            var settings = syncSettings.Value ?? defaultSettings;
            OnPlayerCountChanged?.Invoke(next,settings.maxPlayers);
        }

        public GameSettings GetCurrentSettings()
        {
            return syncSettings.Value ?? defaultSettings;
        }

        public int GetCurrentPlayerCount()
        {
            return syncPlayerCount.Value;
        }

        // ✅ 메인 스테이지에서 사용할 수 있는 안전한 접근 메서드들 추가
        public bool HasValidSettings()
        {
            return syncSettings.Value != null;
        }

        public float GetRoundTime()
        {
            var settings = GetCurrentSettings();
            return settings?.roundTime ?? defaultSettings.roundTime;
        }

        public bool IsFriendlyFireEnabled()
        {
            var settings = GetCurrentSettings();
            return settings?.friendlyFire ?? defaultSettings.friendlyFire;
        }

        public int GetMaxPlayers()
        {
            var settings = GetCurrentSettings();
            return settings?.maxPlayers ?? defaultSettings.maxPlayers;
        }

        // ✅ 싱글톤 수동 삭제 메서드 추가
        public static void DestroyInstance()
        {
            if (Instance != null)
            {
                Debug.Log("[GameSettingManager] 싱글톤 인스턴스 수동 삭제 시작");
                
                // 이벤트 정리
                Instance.OnSettingsChanged = null;
                Instance.OnPlayerCountChanged = null;
                Instance.OnGameStarted = null;
                Instance.OnGameEnded = null;
                
                // NetworkBehaviour 정리
                if (Instance.IsServerInitialized)
                {
                    Instance.OnStopServer();
                }
                if (Instance.IsClientInitialized)
                {
                    Instance.OnStopClient();
                }
                
                // GameObject 삭제
                if (Instance.gameObject != null)
                {
                    Destroy(Instance.gameObject);
                }
                
                Instance = null;
                Debug.Log("[GameSettingManager] 싱글톤 인스턴스 삭제 완료");
            }
        }

        // ✅ 씬 기반 자동 정리 메서드
        public static void DestroyInstanceIfInScene(string sceneName)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene.Contains(sceneName))
            {
                Debug.Log($"[GameSettingManager] {sceneName} 씬 감지 - 자동 정리 수행");
                DestroyInstance();
            }
        }

        // ✅ 게임 상태 리셋 메서드 (삭제하지 않고 상태만 리셋)
        public void ResetGameState()
        {
            if (!IsHostInitialized) return;
            
            Debug.Log("[GameSettingManager] 게임 상태 리셋");
            
            // 설정을 기본값으로 되돌림
            if (IsServerInitialized)
            {
                syncSettings.Value = defaultSettings;
            }
            
            // 이벤트 발생
            OnGameEnded?.Invoke();
        }

        void LoadMainStageScene()
        {
            if (NetworkManager?.SceneManager != null)
            {
                SceneLoadData data = new SceneLoadData(new List<string> {"MainScene" })
                {
                    ReplaceScenes =ReplaceOption.All
                };
                InstanceFinder.SceneManager.LoadGlobalScenes(data);
            }
        }
        
        public override void OnStopClient()
        {
            syncSettings.OnChange -= OnSettingChangedCallback;
            syncPlayerCount.OnChange -= OnPlayerCountChangedCallback;
            Debug.Log("[GameSettingManager] 클라이언트 정지 - 콜백 해제됨");
        }

        public override void OnStopServer()
        {
            if (base.NetworkManager?.ServerManager != null)
            {
                base.NetworkManager.ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
            }
            
            // ✅ FishNet 씬 매니저 이벤트 구독 해제
            if (base.NetworkManager?.SceneManager != null)
            {
                base.NetworkManager.SceneManager.OnLoadStart -= OnSceneLoadStart;
                base.NetworkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
            }
            
            // 코루틴 정리
            if (updatePlayerCountCoroutine != null)
            {
                StopCoroutine(updatePlayerCountCoroutine);
                updatePlayerCountCoroutine = null;
            }
            Debug.Log("[GameSettingManager] 서버 정지 - 리소스 정리 완료");
        }

        private void OnDestroy()
        {
            // ✅ 인스턴스 정리
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("[GameSettingManager] 인스턴스 해제됨");
            }
        }
    }
}