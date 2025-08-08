using System;
using System.Collections;
using FishNet.Object;
using UnityEngine;
using MyFolder._1._Scripts._0._Object._2._Projectile;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy
{
    /// <summary>
    /// 적 네트워크 동기화 컴포넌트 (리팩토링 버전)
    /// 기존의 여러 개별 SyncVar을 하나의 EnemyStateData로 통합
    /// 새로운 컴포넌트 기반 구조와 연동
    /// </summary>
    public class EnemyNetworkSync : AgentNetworkSync
    {
        public Action ReloadCompleteEvent;

        // AI 전용 재장전 처리
        [ServerRpc]
        public void RequestReload()
        {
            if (syncIsReloading.Value) return;
        
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 서버에서 재장전 시작", this);
            RequestSetReloadingState(true);
            StartCoroutine(ServerReloadProcess());
        }
    
        private IEnumerator ServerReloadProcess()
        {
            float reloadTimer = 0f;
        
            while (reloadTimer < AgentStatus.bulletReloadTime)
            {
                reloadTimer += Time.deltaTime;
                float progress = reloadTimer / AgentStatus.bulletReloadTime;
                OnReloadProgress(progress);
                yield return null;
            }
        
            // 재장전 완료
            RequestUpdateBulletCount(AgentStatus.bulletMaxCount);
            RequestSetReloadingState(false);
            OnReloadComplete();
            ReloadCompleteEvent?.Invoke();
        }
    
        [ObserversRpc]
        private void OnReloadProgress(float progress)
        {
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 재장전 진행률: {progress * 100:F1}%", this);
        }
    
        [ObserversRpc]
        private void OnReloadComplete()
        {
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 재장전 완료", this);
        }

        // 적 전용 발사 처리
        [Server]
        public void ShootEnemyBullet(float angle, Vector3 shotPosition)
        {
            if (!BulletManager.Instance)
            {
                StartCoroutine(WaitForBulletManagerAndShootEnemy(angle, shotPosition));
                return;
            }

            BulletManager.Instance.FireBulletForEnemy(
                shotPosition,
                angle,
                AgentStatus.bulletSpeed,
                AgentStatus.bulletDamage,
                AgentStatus.bulletRange,
                gameObject
            );

            RequestUpdateBulletCount(-1);
            OnShootEffect(angle, shotPosition);
        }

        private IEnumerator WaitForBulletManagerAndShootEnemy(float angle, Vector3 shotPosition)
        {
            float waitTime = 0f;
            const float maxWaitTime = 5f;

            while (BulletManager.Instance == null && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            if (BulletManager.Instance != null)
                ShootEnemyBullet(angle, shotPosition);
            else
                LogManager.LogError(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 타임아웃! 발사 취소", this);
        }
    }
}