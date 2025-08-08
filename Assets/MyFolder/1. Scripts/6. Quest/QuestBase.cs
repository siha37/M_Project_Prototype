using UnityEngine;

namespace MyFolder._1._Scripts._6._Quest
{
    public abstract class QuestBase
    {
        public QuestType Type { get; }
        public int Target { get; protected set; }
        public int Progress { get; protected set; }

        protected QuestBase(QuestType type, int target)
        {
            Type = type;
            Target = target;
            Progress = 0;
        }

        public virtual void Start() { }

        public virtual void ReportProgress(int amount)
        {
            Progress = Mathf.Min(Progress + amount, Target);
        }

        public bool IsComplete => Progress >= Target;

        public virtual void Complete() { }
    }
}
