using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace MyFolder._1._Scripts._6._GlobalQuest
{
    public sealed class GlobalQuestReplicator : NetworkBehaviour
    {
        public static event Action<GlobalQuestReplicator> OnReplicatorSpawned;
        public static event Action<GlobalQuestReplicator> OnReplicatorDespawned;
        // 여러 퀘스트 동시 관리용 식별자
        public readonly SyncVar<int> QuestId = new SyncVar<int>();

        // 메타/진행도
        public readonly SyncVar<GlobalQuestType> QuestType = new SyncVar<GlobalQuestType>();
        public readonly SyncVar<string> QuestName = new SyncVar<string>();
        public readonly SyncVar<float> Progress = new SyncVar<float>();
        public readonly SyncVar<float> Target = new SyncVar<float>();
        public readonly SyncVar<float> LimitTime = new SyncVar<float>();
        public readonly SyncVar<float> ElapsedTime = new SyncVar<float>();
        public readonly SyncVar<bool> IsActive = new SyncVar<bool>();
        public readonly SyncVar<bool> IsEnd = new SyncVar<bool>();

        public override void OnStartClient()
        {
            OnReplicatorSpawned?.Invoke(this);
        }

        public override void OnStopClient()
        {
            OnReplicatorDespawned?.Invoke(this);
        }
    }
}


