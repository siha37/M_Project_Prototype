using UnityEngine;

namespace MyFolder._1._Scripts._7._PlayerRole
{
    [CreateAssetMenu(fileName = "RoleDefinition", menuName = "Game/Role Definition")]
    public class RoleDefinition : ScriptableObject
    {
        public PlayerRoleType Role;

        // 스탯
        public float MoveSpeed = 5f;
        public float MaxHp = 100f;
        public float BulletDamage = 45f;
        public float BulletDelay = 0.3f;
        public float BulletRange = 10f;
        public float BulletReloadTime = 2f;
        public float BulletMaxCount = 10f;

        // 능력 게이트
        public bool CanAttack = true;
        public bool CanInteract = true;
        public bool CanReviveAlly = true;
        public bool CanUseSkill1 = false;
        public bool CanUseSkill2 = false;
    }
}