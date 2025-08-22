using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    public abstract class QuestSpawner : MonoBehaviour
    {
        // 섬멸 : 딱히 필요없음
        // 방어 : 방어 오브젝트 생성 /
        // 생존 : 생존 유지 오브젝트 생성
        
        // 절차
        // 1. 생성 오브젝트 갯수 설정 / 생성 프리펩 설정
        // 2. 생성 위치 선정
        // 3. 전체 생성 or 순차적 생성

        [SerializeField] protected int createAmount;
        [SerializeField] protected GameObject questPrefab;
        [SerializeField] protected List<List<Transform>> spawnPointsCollection = new List<List<Transform>>();
        protected List<Transform> spawnPoints;
        protected int currentCreateAmount;


        public void SetCreateAmount(int amount)
        {
            createAmount = Mathf.Max(0,6,amount);
        }
        protected void AllSpawn()
        {
            if(spawnPoints==null)
                return;
            spawnPoints = spawnPoints.OrderBy(o=>Random.Range(0, spawnPoints.Count)).ToList();
            for (int i = 0; i < createAmount; i++)
            {
                GameObject obj = Instantiate(questPrefab, spawnPoints[i].position, Quaternion.identity);
                obj.transform.SetParent(transform);
            }
        }

        protected void StepSpawn()
        {
            if(spawnPoints==null)
                return;
            spawnPoints = spawnPoints.OrderBy(o=>Random.Range(0, spawnPoints.Count)).ToList();
            
            GameObject obj = Instantiate(questPrefab, spawnPoints[currentCreateAmount++].position, Quaternion.identity);
            obj.transform.SetParent(transform);
        }
    }
}