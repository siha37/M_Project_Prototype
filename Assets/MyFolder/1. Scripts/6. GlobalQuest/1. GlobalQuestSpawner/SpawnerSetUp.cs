using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    [Serializable]
    public class SpawnPoint
    {
       public List<Transform> spawnPoints = new List<Transform>();
    }
    // 생성 포인트 셋업 클래스
    public class SpawnerSetUp : MonoBehaviour
    {
        [SerializeField] protected List<SpawnPoint> spawnPointsCollection = new List<SpawnPoint>();        

        public List<Transform> GetRandomSpawnPoints()
        {
            return spawnPointsCollection[Random.Range(0, spawnPointsCollection.Count)].spawnPoints;
        }

    }
}