using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._6._GlobalQuest
{
    public class GlobalQuestManager : NetworkBehaviour
    {        
        public static GlobalQuestManager instance;
        
        //동적 Quest 변수
        private List<GlobalQuestBase> globalQuests = new();
        private Dictionary<GlobalQuestBase, GlobalQuestReplicator> questToReplicator = new();
        private int globalQuestsCount => globalQuests.Count;
        public List<GlobalQuestBase> GlobalQuests => globalQuests;
        private int totalQuestCount => 1;
        
        //스포너
        [SerializeField] private SpawnerSetUp spawnerSetUp;
        [SerializeField] private NetworkObject defencePrefab;
        [SerializeField] private NetworkObject survivalPrefab;
        [SerializeField] private NetworkObject replicatorPrefab;
        
        
        //팩토리
        private static readonly Dictionary<GlobalQuestType, Func<GlobalQuestContext,GlobalQuestBase>> _map =
            new()
            {
                { GlobalQuestType.Extermination, ctx => new ExterminationGlobalQuest(ctx.Spawner,ctx.targetAmount,ctx.limitTime) },
                { GlobalQuestType.Defense,       ctx => new DefenseGlobalQuest(ctx.Spawner,ctx.limitTime) },
                { GlobalQuestType.Survival,      ctx => new SurvivalGlobalQuest(ctx.Spawner,ctx.limitTime) },
            };
        private static readonly Dictionary<GlobalQuestType, Func<QuestSpawner>> _spawner_map =
            new()
            {
                { GlobalQuestType.Extermination, () => new ExterminationGlobalSpawner() },
                { GlobalQuestType.Defense,       () => new DefenseGlobalSpawner() },
                { GlobalQuestType.Survival,      () => new SurvivalGlobalSpawner() },
            };
        
        //초기화 변수
        private int exterminationTargetAmount;
        
        //생성 주기 변수
        [SerializeField][ReadOnly] private float QuestAmount = 1;
        [SerializeField][ReadOnly] private float QuestFirstTime = 30;
        [SerializeField][ReadOnly] private float QuestCreationCycle = 120;
        [SerializeField][ReadOnly] private float QuestLimitTime = 60;
        [SerializeField] private bool QuestCreateAble = false;
        
        //동적 주기 변수
        private float currentTime = 0;
        private float lastTime = 0;
        private GlobalQuestType lastQuestType = GlobalQuestType.None;
        
        //callback
        public delegate void questDel(GlobalQuestBase quest);
        public questDel OnGlobalQuestCreated;
        public questDel OnGlobalQuestRemoved;

        private void Awake()
        {
            if(!instance)
                instance = this;
        }
        public override void OnStartServer()
        {
            currentTime = 0;
            lastTime = QuestFirstTime;
        }
        public override void OnStartClient()
        {
        }

        
        public GlobalQuestBase Create(GlobalQuestType type)
        {
            if (!_map.TryGetValue(type, out var ctor))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported quest type");
            if(!_spawner_map.TryGetValue(type, out var stor))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported quest type");
            QuestSpawner spawner = stor();
            spawner.SetSpawnPoints(spawnerSetUp.GetRandomSpawnPoints());
            spawner.SetSpawnPrefab(GetQuestPrefab(type));
            GlobalQuestContext context = new GlobalQuestContext(spawner,10,80,1);
            return ctor(context);
        }

        private NetworkObject GetQuestPrefab(GlobalQuestType type)
        {
            switch (type)
            {
                case GlobalQuestType.Defense:
                    return defencePrefab;
                case GlobalQuestType.Survival:
                    return survivalPrefab;
                default:
                    Log($"지원되지 않는 퀘스트 타입의 프리팹 요청: {type}");
                    return null;
            }
        }

        private void Update()
        {
            if (!IsServerInitialized)
                return;
            if (globalQuestsCount >= totalQuestCount)
            {
                Log("Global Quest Manager : 현재 활성화된 퀘스트가 충분히 존재합니다");
                foreach (var quest in globalQuests)
                {
                    quest.Update();

                    // 변화 감지 미러링
                    if (questToReplicator.TryGetValue(quest, out var rep))
                        MirrorQuestToReplicatorIfChanged(quest, rep);
                    if (quest.IsEnd)
                        QuestRemove(quest);
                }
                return;
            }

            if (currentTime >= lastTime)
            {
                //목표 시간 달성
                lastQuestType = (GlobalQuestType)Random.Range(1, (int)GlobalQuestType.GlobalQuestTypeAmount);
                GlobalQuestBase quest = Create(lastQuestType);
                OnGlobalQuestCreated?.Invoke(quest);
                globalQuests.Add(quest);
                CreateReplicatorFor(quest);
                // 생성 직후 다음 생성 주기 리셋
                currentTime = 0;
                lastTime = QuestCreationCycle;
            }
            else
            {
                //시간 진행
                currentTime += Time.deltaTime;
            }
        }

        
        private void QuestRemove(GlobalQuestBase quest)
        {
            OnGlobalQuestRemoved?.Invoke(quest);
            globalQuests.Remove(quest);
            RemoveReplicatorFor(quest);
        }

        private void Log(string message, Object obj = null)
        {
            LogManager.Log(LogCategory.Quest, message, obj);
        }

        #region Replicator
        private int _questIdSeed = 1;
        private void CreateReplicatorFor(GlobalQuestBase quest)
        {
            if (!replicatorPrefab)
                return;
            NetworkObject nob = Instantiate(replicatorPrefab, Vector3.zero, Quaternion.identity);
            InstanceFinder.ServerManager.Spawn(nob);
            var rep = nob.GetComponent<GlobalQuestReplicator>();
            if (!rep)
                return;
            rep.QuestId.Value = _questIdSeed++;
            // 초기 미러링
            MirrorQuestToReplicatorIfChanged(quest, rep);
            questToReplicator[quest] = rep;
        }

        private void RemoveReplicatorFor(GlobalQuestBase quest)
        {
            if (!questToReplicator.TryGetValue(quest, out var rep))
                return;
            if (rep && rep.NetworkObject && rep.NetworkObject.IsSpawned)
                InstanceFinder.ServerManager.Despawn(rep.NetworkObject);
            questToReplicator.Remove(quest);
        }

        // 변화분만 반영하는 미러링
        private void MirrorQuestToReplicatorIfChanged(GlobalQuestBase quest, GlobalQuestReplicator rep)
        {
            if (rep.QuestType.Value != quest.Type)              rep.QuestType.Value = quest.Type;
            if (rep.QuestName.Value != quest.QuestName)         rep.QuestName.Value = quest.QuestName;
            if (!Mathf.Approximately(rep.Progress.Value, quest.progress))     rep.Progress.Value = quest.progress;
            if (!Mathf.Approximately(rep.Target.Value, quest.target))         rep.Target.Value = quest.target;
            if (!Mathf.Approximately(rep.LimitTime.Value, quest.limitTime))   rep.LimitTime.Value = quest.limitTime;
            if (!Mathf.Approximately(rep.ElapsedTime.Value, quest.currentTime)) rep.ElapsedTime.Value = quest.currentTime;
            if (rep.IsActive.Value != quest.IsActive)           rep.IsActive.Value = quest.IsActive;
            if (rep.IsEnd.Value != quest.IsEnd)                 rep.IsEnd.Value = quest.IsEnd;
        }
        #endregion
    }
}