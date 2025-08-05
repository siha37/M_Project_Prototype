using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemyCombat: IEnemyUpdateComponent
    {
        private EnemyConfig config;
        private EnemyControll agent;
        private bool AttackAble = false;
        private bool IsReloading = false;
        private float lastAttackTime = 0;
        private float shotAngle;
        private float finalAngle;
        
        public bool CanShot => AttackAble && !IsReloading && agent?.Status?.bulletCurrentCount > 0;
        
        public void Init(EnemyControll agent)
        {
            this.agent = agent;
            config = agent.Config;
            agent.NetworkSync.ReloadCompleteEvent = ReloadComplete;
        }

        public void OnEnable()
        {
            if(agent)
                Init(agent);
        }

        public void OnDisable()
        {
        }

        public void ChangedState(IEnemyState oldstate, IEnemyState newstate)
        {
        }

        public void Update()
        {
            if(!agent) return;
            if(!agent.CurrentTarget) return;

            ShotAngleUpdate();
            FireUpdate();
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        public void AttackOn()
        {
            AttackAble = true;
        }

        public void AttackOff()
        {
            AttackAble = false;
        }

        /*==================Private ========================*/

        private void ShotAngleUpdate()
        {
            Vector2 direction = (agent.CurrentTarget.transform.position - agent.transform.position).normalized;
            shotAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // 조준 정밀도 적용 (약간의 오차 추가)
            float aimPrecision = agent.Config.aimPrecision;
            float aimError = Random.Range(-aimPrecision * 90f, aimPrecision * 90f);
            finalAngle = shotAngle + aimError;
            agent.ShotPivot.rotation = Quaternion.Euler(0,0,shotAngle);
            agent.NetworkSync?.RequestUpdateLookAngle(shotAngle);
        }

        private void FireUpdate()
        {
            if (!CanShot) return;
            if (Time.time - lastAttackTime >= agent.Config.attackInterval)
            {
                lastAttackTime = Time.time;
                if (agent.NetworkSync)
                {
                    agent.NetworkSync.RequestShoot(finalAngle,agent.ShotPoint.position);
                    if (agent.Status.bulletCurrentCount <= 0)
                        Reloading();
                }
            }
        }

        private void Reloading()
        {
            IsReloading = true;
            agent.NetworkSync.RequestReload();
        }
        private void ReloadComplete()
        {
            IsReloading = false;
        }
    }
}