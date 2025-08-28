using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._0._Core
{
    /// <summary>
    /// 적 액터 구현체
    /// </summary>
    public class EnemyActor : Actor
    {
        [Header("Enemy Settings")]
        [SerializeField] private string enemyType = "BasicEnemy";
        [SerializeField] private int enemyLevel = 1;
        [SerializeField] private float detectionRange = 10f;

        public string EnemyType => enemyType;
        public int EnemyLevel => enemyLevel;
        public float DetectionRange => detectionRange;

        protected override void Awake()
        {
            base.Awake();
            
            if (string.IsNullOrEmpty(enemyType))
            {
                enemyType = "Enemy";
            }
        }

        /// <summary>
        /// 적 타입 설정
        /// </summary>
        public void SetEnemyType(string type)
        {
            enemyType = type;
            name = $"EnemyActor_{type}_{GetInstanceID()}";
        }

        /// <summary>
        /// 적 레벨 설정
        /// </summary>
        public void SetEnemyLevel(int level)
        {
            enemyLevel = Mathf.Max(1, level);
        }

        /// <summary>
        /// 탐지 범위 설정
        /// </summary>
        public void SetDetectionRange(float range)
        {
            detectionRange = Mathf.Max(0f, range);
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug Enemy Info")]
        private void DebugEnemyInfo()
        {
            Debug.Log($"=== Enemy Actor Info ===");
            Debug.Log($"Type: {enemyType}");
            Debug.Log($"Level: {enemyLevel}");
            Debug.Log($"Detection Range: {detectionRange}");
            Debug.Log($"IsServer: {IsServer}");
            Debug.Log($"NetworkObjectId: {NetworkObject?.ObjectId}");
        }
        #endif
    }
}
