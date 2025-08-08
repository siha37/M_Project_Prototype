using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._6._Quest;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class NetworkQuestManager : NetworkBehaviour
    {
        private readonly SyncVar<QuestType> syncQuestType = new SyncVar<QuestType>();
        private readonly SyncVar<int> syncQuestTarget = new SyncVar<int>();
        private readonly SyncVar<int> syncQuestProgress = new SyncVar<int>();

        private QuestBase currentQuest;

        public QuestType CurrentQuestType => syncQuestType.Value;
        public int CurrentQuestTarget => syncQuestTarget.Value;
        public int CurrentQuestProgress => syncQuestProgress.Value;

        public bool HasQuest => currentQuest != null;
        public bool IsQuestComplete => HasQuest && currentQuest.IsComplete;

        private static NetworkQuestManager instance;
        public static NetworkQuestManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<NetworkQuestManager>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                LogManager.Log(LogCategory.System, "NetworkQuestManager 인스턴스 생성", this);
            }
            else if (instance != this)
            {
                LogManager.LogWarning(LogCategory.System, "NetworkQuestManager 중복 인스턴스 제거", this);
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            currentQuest = null;
            syncQuestType.Value = QuestType.None;
            syncQuestTarget.Value = 0;
            syncQuestProgress.Value = 0;
            LogManager.Log(LogCategory.System, "NetworkQuestManager 서버 초기화", this);
        }

        /// <summary>서버에서 퀘스트를 발급.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void IssueQuestServerRpc(QuestType type, int target)
        {
            if (!IsServerInitialized) return;

            switch (type)
            {
                case QuestType.Extermination:
                    currentQuest = new ExterminationQuest(target);
                    break;
                case QuestType.Defense:
                    currentQuest = new DefenseQuest(target);
                    break;
                case QuestType.Survival:
                    currentQuest = new SurvivalQuest(target);
                    break;
                default:
                    currentQuest = null;
                    break;
            }

            if (currentQuest == null) return;

            syncQuestType.Value = currentQuest.Type;
            syncQuestTarget.Value = currentQuest.Target;
            syncQuestProgress.Value = currentQuest.Progress;

            NotifyQuestUpdatedObserversRpc(currentQuest.Type, currentQuest.Progress, currentQuest.Target);
            LogManager.Log(LogCategory.System, $"퀘스트 발급: {type} (목표 {target})", this);
        }

        /// <summary>클라이언트에서 퀘스트 진행 상황 보고.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportProgressServerRpc(int amount)
        {
            if (!IsServerInitialized || currentQuest == null) return;

            currentQuest.ReportProgress(amount);
            syncQuestProgress.Value = currentQuest.Progress;
            NotifyQuestUpdatedObserversRpc(currentQuest.Type, currentQuest.Progress, currentQuest.Target);

            if (currentQuest.IsComplete)
                CompleteQuest();
        }

        /// <summary>클라이언트에서 수동으로 완료 요청.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void CompleteQuestServerRpc()
        {
            if (!IsServerInitialized || currentQuest == null) return;
            CompleteQuest();
        }

        private void CompleteQuest()
        {
            LogManager.Log(LogCategory.System, $"퀘스트 완료: {currentQuest.Type}", this);
            NotifyQuestCompletedObserversRpc(currentQuest.Type);
            currentQuest.Complete();

            currentQuest = null;
            syncQuestType.Value = QuestType.None;
            syncQuestTarget.Value = 0;
            syncQuestProgress.Value = 0;
        }

        [ObserversRpc]
        private void NotifyQuestUpdatedObserversRpc(QuestType type, int progress, int target)
        {
            // 클라이언트 퀘스트 UI 갱신용
        }

        [ObserversRpc]
        private void NotifyQuestCompletedObserversRpc(QuestType type)
        {
            // 클라이언트 퀘스트 완료 처리용
        }
    }
}
