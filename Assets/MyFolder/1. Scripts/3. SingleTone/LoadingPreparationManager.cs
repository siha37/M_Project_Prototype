using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    /// <summary>
    /// 로딩 씬에서 게임 시작 전 전처리를 담당하는 매니저
    /// </summary>
    public class LoadingPreparationManager : NetworkBehaviour
    {
        public static LoadingPreparationManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        
        [Header("Loading Settings")]
        [SerializeField] private float minLoadingTime = 3f;
        
        // 로딩 상태 동기화
        private readonly SyncVar<LoadingPreparationState> syncLoadingState = new SyncVar<LoadingPreparationState>();
        
        // 로딩 전처리 이벤트
        public event System.Action<LoadingPreparationState> OnLoadingStateChanged;
        public event System.Action OnLoadingPreparationCompleted;
        public event System.Action OnGameReadyToStart;
        
        
        /// <summary>
        /// 로딩 씬에서 전처리 시작 (자동 시작)
        /// </summary>
        public override void OnStartServer()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            
            // 로딩 상태 초기화
            syncLoadingState.Value = LoadingPreparationState.WaitingForPlayers;
            
            LogManager.Log(LogCategory.System, "LoadingPreparationManager 서버 시작", this);
            
            // 자동으로 전처리 시작
            StartCoroutine(AutoStartPreparationCoroutine());
        }
        
        /// <summary>
        /// 자동 전처리 시작 코루틴
        /// </summary>
        private IEnumerator AutoStartPreparationCoroutine()
        {
            // 잠시 대기 (씬 로딩 완료 대기)
            yield return new WaitForSeconds(1f);
            
            // 전처리 시작
            StartCoroutine(LoadingPreparationCoroutine());
        }
        
        /// <summary>
        /// 로딩 전처리 코루틴
        /// </summary>
        private IEnumerator LoadingPreparationCoroutine()
        {
            // 1단계: 역할 배정
            syncLoadingState.Value = LoadingPreparationState.AssigningRoles;
            LogManager.Log(LogCategory.System, "1단계: 역할 배정 시작", this);
            
            // PlayerRoleManager에 역할 배정 요청
            PlayerRoleManager.Instance.AssignRolesToAllPlayersServerRpc();
            
            // 역할 배정 완료 대기
            yield return new WaitUntil(() => PlayerRoleManager.Instance.AreRolesAssigned());
            
            LogManager.Log(LogCategory.System, "1단계: 역할 배정 완료", this);
            
            // 2단계: 리소스 로딩
            syncLoadingState.Value = LoadingPreparationState.LoadingResources;
            LogManager.Log(LogCategory.System, "2단계: 리소스 로딩 시작", this);
            
            // 실제 리소스 로딩 (Addressables 또는 Resources)
            yield return StartCoroutine(LoadGameResourcesCoroutine());
            
            LogManager.Log(LogCategory.System, "2단계: 리소스 로딩 완료", this);
            
            // 3단계: 스테이지 씬 로딩
            syncLoadingState.Value = LoadingPreparationState.LoadingStage;
            LogManager.Log(LogCategory.System, "3단계: 스테이지 씬 로딩 시작", this);
            
            // 스테이지 씬 로딩 및 완료 대기
            yield return StartCoroutine(LoadStageSceneCoroutine());
            
            LogManager.Log(LogCategory.System, "3단계: 스테이지 씬 로딩 완료", this);
            
            // 4단계: 전처리 완료
            syncLoadingState.Value = LoadingPreparationState.Completed;
            LogManager.Log(LogCategory.System, "로딩 전처리 완료", this);
            
            // 전처리 완료 알림
            NotifyLoadingPreparationCompletedClientRpc();
            
            // 잠시 대기 후 게임 시작
            yield return new WaitForSeconds(1f);
            
            // 게임 시작 이벤트 발생
            OnGameReadyToStart?.Invoke();
        }
        
    /// <summary>
    /// 게임 리소스 로딩 코루틴
    /// </summary>
    private IEnumerator LoadGameResourcesCoroutine()
    {
        // Addressables 사용 시
        #if UNITY_ADDRESSABLES
        var loadOperation = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync("MainScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        
        while (!loadOperation.IsDone)
        {
            // 로딩 진행률 업데이트 (UI에 표시 가능)
            float progress = loadOperation.PercentComplete;
            LogManager.Log(LogCategory.System, $"리소스 로딩 진행률: {progress:P0}", this);
            yield return null;
        }
        
        // Addressables 정리
        UnityEngine.AddressableAssets.Addressables.Release(loadOperation);
        
        #else
        // Resources 폴더 사용 시
        //var resourceRequest = Resources.LoadAsync<GameObject>("Prefabs/GameResources");
        
        //while (!resourceRequest.isDone)
        //{
        //    float progress = resourceRequest.progress;
        //    LogManager.Log(LogCategory.System, $"리소스 로딩 진행률: {progress:P0}", this);
        //    yield return null;
        //}
        
        // 추가 리소스 로딩 (필요한 경우)
        yield return new WaitForSeconds(0.5f);
        #endif
        
        LogManager.Log(LogCategory.System, "게임 리소스 로딩 완료", this);
    }

    /// <summary>
    /// 스테이지 씬 로딩 코루틴
    /// </summary>
    private IEnumerator LoadStageSceneCoroutine()
    {
        if (!NetworkManager?.SceneManager)
        {
            LogManager.LogError(LogCategory.System, "SceneManager가 null입니다", this);
            yield break;
        }
        
        // 스테이지 씬 로딩 시작
        SceneLoadData sceneData = new SceneLoadData(new List<string> {"MainScene"})
        {
            ReplaceScenes = ReplaceOption.All
        };
        
        // 씬 로딩 시작
        NetworkManager.SceneManager.LoadGlobalScenes(sceneData);
    
        LogManager.Log(LogCategory.System, "스테이지 씬 로딩 시작됨", this);
    
        // 씬 로딩 완료까지 대기 (FishNet 이벤트 사용)
        bool sceneLoaded = false;
    
        // 씬 로딩 완료 이벤트 구독
        void OnSceneLoadEnd(SceneLoadEndEventArgs args)
        {        // 배열에서 "MainScene" 찾기
            for (int i = 0; i < args.LoadedScenes.Length; i++)
            {
                if (args.LoadedScenes[i].name == "MainScene")
                {
                    sceneLoaded = true;
                    break;
                }
            }
        }
    
        NetworkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
    
        // 씬 로딩 완료까지 대기
        yield return new WaitUntil(() => sceneLoaded);
        
    
        // 이벤트 구독 해제
        NetworkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
    
        // 씬 로딩 완료 확인
        yield return new WaitForSeconds(0.5f); // 추가 안정화 시간
    
    }
        /// <summary>
        /// 로딩 전처리 완료 알림
        /// </summary>
        [ObserversRpc]
        private void NotifyLoadingPreparationCompletedClientRpc()
        {
            OnLoadingPreparationCompleted?.Invoke();
            LogManager.Log(LogCategory.System, "로딩 전처리 완료 알림", this);
        }
        
        /// <summary>
        /// 로딩 상태 변경 콜백
        /// </summary>
        private void OnLoadingStateChangedCallback(LoadingPreparationState prev, LoadingPreparationState next, bool asserver)
        {
            OnLoadingStateChanged?.Invoke(next);
            LogManager.Log(LogCategory.System, $"로딩 상태 변경: {prev} → {next}", this);
        }
        
        /// <summary>
        /// 현재 로딩 상태 가져오기
        /// </summary>
        public LoadingPreparationState GetLoadingState()
        {
            return syncLoadingState.Value;
        }
        
        /// <summary>
        /// 로딩 진행률 가져오기 (0~1)
        /// </summary>
        public float GetLoadingProgress()
        {
            var state = GetLoadingState();
            switch (state)
            {
                case LoadingPreparationState.WaitingForPlayers:
                    return 0f;
                case LoadingPreparationState.AssigningRoles:
                    return 0.25f;
                case LoadingPreparationState.LoadingResources:
                    return 0.5f;
                case LoadingPreparationState.LoadingStage:
                    return 0.75f;
                case LoadingPreparationState.Completed:
                    return 1f;
                default:
                    return 0f;
            }
        }
        
        public override void OnStopClient()
        {
            syncLoadingState.OnChange -= OnLoadingStateChangedCallback;
            LogManager.Log(LogCategory.System, "LoadingPreparationManager 클라이언트 정지", this);
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                LogManager.Log(LogCategory.System, "LoadingPreparationManager 인스턴스 해제됨", this);
            }
        }
    }
    
    /// <summary>
    /// 로딩 씬 전처리 상태
    /// </summary>
    public enum LoadingPreparationState
    {
        WaitingForPlayers,      // 플레이어 대기
        AssigningRoles,         // 역할 배정 중
        LoadingResources,       // 리소스 로딩 중
        LoadingStage,           // 스테이지 씬 로딩 중
        Completed               // 전처리 완료
    }

}
