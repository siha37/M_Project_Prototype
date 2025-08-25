using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._3._QuestAgent;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    public abstract class QuestSpawner
    {
        // 섬멸 : 딱히 필요없음
        // 방어 : 방어 오브젝트 생성 /
        // 생존 : 생존 유지 오브젝트 생성
        
        // 절차
        // 1. 생성 오브젝트 갯수 설정 / 생성 프리펩 설정
        // 2. 생성 위치 선정
        // 3. 전체 생성 or 순차적 생성

        protected abstract int createAmount { get; }
        private NetworkObject questPrefab;
        public List<Transform> spawnPoints;
        private List<NetworkObject> spawnedObjects = new List<NetworkObject>();
        
        
        private int createStep;
        
        
        public event Action<GameObject> OnSpawned;
        public event Action<GameObject> OnDespawned;

        protected void RaiseSpawned(GameObject go)   => OnSpawned?.Invoke(go);
        protected void RaiseDespawned(GameObject go) => OnDespawned?.Invoke(go);

        private int currentCreateAmount => spawnedObjects.Count;
        public int CurrentCreateAmount=> currentCreateAmount;
        public void SetSpawnPoints(List<Transform> spawnPoints)
        {
            this.spawnPoints = spawnPoints.OrderBy(o=>Random.Range(0, spawnPoints.Count)).ToList();
        }

        public void SetSpawnPrefab(NetworkObject spawnPrefab)
        {
            this.questPrefab = spawnPrefab;
        }

        #region Spawn

        public abstract void SpawnStart();
        protected void AllSpawn()
        {
            if(spawnPoints==null)
                return;
            for (int i = spawnedObjects.Count; i < createAmount; i++)
            {           
                NetworkObject nob = Object.Instantiate(questPrefab, spawnPoints[i].position, Quaternion.identity);
                //obj.transform.SetParent(transform);
                if (nob.TryGetComponent(out QuestAgentControll controll))
                    controll.SetMyMother(this);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
                spawnedObjects.Add(nob);
            }
        }

        protected void StepSpawn()
        {
            if (spawnPoints == null || questPrefab == null)
                return;
            if (createStep < 0 || createStep >= spawnPoints.Count)
                return;

            NetworkObject nob = Object.Instantiate(questPrefab, spawnPoints[createStep++].position, Quaternion.identity);
            InstanceFinder.ServerManager.Spawn(nob);
            nob.gameObject.SetActive(true);
            if (nob.TryGetComponent(out QuestAgentControll controll))
                controll.SetMyMother(this);
            spawnedObjects.Add(nob);
        }

        public void AllRemove()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                NetworkObject obj = spawnedObjects[i];
                if (obj != null)
                {
                    if (obj.IsSpawned)
                        InstanceFinder.ServerManager.Despawn(obj);
                    Object.Destroy(obj.gameObject);
                }
            }
            spawnedObjects.Clear();
        }

        #endregion

        #region Controll

        public void DestroyObject(NetworkObject obj)
        {
            spawnedObjects.Remove(obj);
        }

        #endregion
    }
}