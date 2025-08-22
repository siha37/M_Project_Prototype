using System.Collections.Generic;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    // 생성 포인트 셋업 클래스
    public class SpawnerSetUp : MonoBehaviour
    {
        [SerializeField] protected List<List<Transform>> spawnPointsCollection = new List<List<Transform>>();
        

        protected List<Transform> GetRandomSpawnPoints()
        {
            return spawnPointsCollection[Random.Range(0, spawnPointsCollection.Count)];
        }

    }
}