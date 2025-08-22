using System;
using System.Collections.Generic;
using FishNet.Object;
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
        public List<GlobalQuestBase> globalQuests = new();
        private int globalQuestsCount => globalQuests.Count;
        
        //스포너
        private SpawnerSetUp spawnerSetUp;
        
        
        //팩토리
        private static readonly Dictionary<GlobalQuestType, Func<GlobalQuestContext,GlobalQuestBase>> _map =
            new()
            {
                { GlobalQuestType.Extermination, ctx => new ExterminationGlobalQuest(ctx.targetAmount,ctx.limitTime) },
                { GlobalQuestType.Defense,       ctx => new DefenseGlobalQuest(ctx.limitTime,ctx.defenceAmount) },
                { GlobalQuestType.Survival,      ctx => new SurvivalGlobalQuest(ctx.limitTime) },
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

        
        public static GlobalQuestBase Create(GlobalQuestType type)
        {
            if (!_map.TryGetValue(type, out var ctor))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported quest type");
            if(!_spawner_map.TryGetValue(type, out var stor))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported quest type");
            
            GlobalQuestContext context = new GlobalQuestContext(stor(),10,80,1);
            return ctor(context);
        }
        private void Update()
        {
            if (!IsHostInitialized)
                return;
            if (globalQuestsCount != 0)
            {
                Log("Global Quest Manager : 현재 활성화된 퀘스트가 존재합니다");
                return;
            }

            if (currentTime >= lastTime)
            {
                //목표 시간 달성
                lastQuestType = (GlobalQuestType)Random.Range(1, (int)GlobalQuestType.GlobalQuestTypeAmount);
                globalQuests.Add(Create(lastQuestType));
            }
            else
            {
                //시간 진행
                currentTime += Time.deltaTime;
            }
        }

        private void Log(string message, Object obj = null)
        {
            LogManager.Log(LogCategory.Quest, message, obj);
        }
    }
}