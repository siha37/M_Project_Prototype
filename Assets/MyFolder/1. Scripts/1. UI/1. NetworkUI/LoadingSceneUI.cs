using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyFolder._1._Scripts._3._SingleTone;

namespace MyFolder._1._Scripts._1._UI.NetworkUI
{
    /// <summary>
    /// 로딩 씬에서 전처리 진행도를 표시하는 UI 클래스
    /// </summary>
    public class LoadingSceneUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private GameObject loadingAnimation;
        
        [Header("Loading Messages")]
        [SerializeField] private string[] loadingMessages = {
            "플레이어 연결 대기 중...",
            "역할 배정 중...",
            "리소스 로딩 중...",
            "스테이지 씬 로딩 중...",
            "게임 준비 완료!"
        };
        
        [Header("Animation Settings")]
        [SerializeField] private float progressUpdateSpeed = 5f; // 진행률 업데이트 속도
        [SerializeField] private float messageDisplayTime = 2f; // 메시지 표시 시간
        
        private float currentProgress = 0f;
        private float targetProgress = 0f;
        private LoadingPreparationState currentState;
        
        private void Start()
        {
            // 이벤트 구독
            if (LoadingPreparationManager.Instance != null)
            {
                LoadingPreparationManager.Instance.OnLoadingStateChanged += OnLoadingStateChanged;
                LoadingPreparationManager.Instance.OnLoadingPreparationCompleted += OnLoadingPreparationCompleted;
                
                // 초기 상태 설정
                UpdateUI(LoadingPreparationManager.Instance.GetLoadingState());
            }
            else
            {
                LogManager.LogWarning(LogCategory.UI, "LoadingPreparationManager 인스턴스를 찾을 수 없습니다", this);
            }
            
            // 로딩 애니메이션 시작
            loadingAnimation.SetActive(true);
            
            // 초기 UI 설정
            UpdateProgressDisplay(0f);
        }
        
        private void Update()
        {
            // 진행률 부드러운 업데이트
            if (Mathf.Abs(currentProgress - targetProgress) > 0.01f)
            {
                currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressUpdateSpeed);
                UpdateProgressDisplay(currentProgress);
            }
        }
        
        private void OnDestroy()
        {
            if (LoadingPreparationManager.Instance != null)
            {
                LoadingPreparationManager.Instance.OnLoadingStateChanged -= OnLoadingStateChanged;
                LoadingPreparationManager.Instance.OnLoadingPreparationCompleted -= OnLoadingPreparationCompleted;
            }
        }
        
        private void OnLoadingStateChanged(LoadingPreparationState state)
        {
            currentState = state;
            UpdateUI(state);
        }
        
        private void OnLoadingPreparationCompleted()
        {
            UpdateUI(LoadingPreparationState.Completed);
            loadingAnimation.SetActive(false);
            
            // "게임 시작!" 메시지 표시
            StartCoroutine(ShowGameStartMessage());
        }
        
        private System.Collections.IEnumerator ShowGameStartMessage()
        {
            statusText.text = "게임 시작!";
            progressText.text = "100%";
            targetProgress = 1f;
            
            // 잠시 대기 후 씬 전환
            yield return new WaitForSeconds(messageDisplayTime);
            
            // UI 숨기기 (씬 전환 준비)
            if (loadingPanel)
            {
                loadingPanel.SetActive(false);
            }
        }
        
        private void UpdateUI(LoadingPreparationState state)
        {
            switch (state)
            {
                case LoadingPreparationState.WaitingForPlayers:
                    statusText.text = loadingMessages[0];
                    targetProgress = 0f;
                    break;
                    
                case LoadingPreparationState.AssigningRoles:
                    statusText.text = loadingMessages[1];
                    targetProgress = 0.25f;
                    break;
                    
                case LoadingPreparationState.LoadingResources:
                    statusText.text = loadingMessages[2];
                    targetProgress = 0.5f;
                    break;
                    
                case LoadingPreparationState.LoadingStage:
                    statusText.text = loadingMessages[3];
                    targetProgress = 0.75f;
                    break;
                    
                case LoadingPreparationState.Completed:
                    statusText.text = loadingMessages[4];
                    targetProgress = 1f;
                    break;
            }
            
            // 진행률 텍스트 업데이트
            progressText.text = $"{targetProgress:P0}";
        }
        
        private void UpdateProgressDisplay(float progress)
        {
            if (progressBar)
            {
                progressBar.value = progress;
            }
            
            if (progressText)
            {
                progressText.text = $"{progress:P0}";
            }
        }
        
        /// <summary>
        /// 로딩 진행률을 수동으로 설정 (외부에서 호출 가능)
        /// </summary>
        public void SetProgress(float progress)
        {
            targetProgress = Mathf.Clamp01(progress);
        }
        
        /// <summary>
        /// 로딩 상태 메시지를 수동으로 설정
        /// </summary>
        public void SetStatusMessage(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
        
        /// <summary>
        /// 로딩 애니메이션 활성화/비활성화
        /// </summary>
        public void SetLoadingAnimation(bool active)
        {
            if (loadingAnimation != null)
            {
                loadingAnimation.SetActive(active);
            }
        }
    }
}