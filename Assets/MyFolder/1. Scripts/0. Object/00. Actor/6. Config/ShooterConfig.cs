using UnityEngine;

namespace MyFolder._1._Scripts._00._Actor._6._Config
{
    /// <summary>
    /// 사격 관련 설정
    /// </summary>
    [CreateAssetMenu(fileName = "New Shooter Config", menuName = "Actor/Shooter Config", order = 3)]
    public class ShooterConfig : ScriptableObject
    {
        [Header("Fire Settings")]
        [SerializeField] private float fireRate = 10f; // 초당 발사 횟수
        [SerializeField] private int damage = 25;
        [SerializeField] private float range = 20f;
        [SerializeField] private float spread = 2f; // 탄착군 각도
        
        [Header("Ammo Settings")]
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private int reserveAmmo = 90;
        [SerializeField] private float reloadTime = 2f;
        [SerializeField] private bool autoReload = true;
        
        [Header("Projectile Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 50f;
        [SerializeField] private float projectileLifetime = 5f;
        [SerializeField] private bool isPenetrating = false;
        
        [Header("Audio/Visual")]
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private GameObject muzzleFlashEffect;

        // 속성
        public float FireRate => fireRate;
        public float FireCooldown => 1f / fireRate;
        public int Damage => damage;
        public float Range => range;
        public float Spread => spread;
        public int MagazineSize => magazineSize;
        public int ReserveAmmo => reserveAmmo;
        public float ReloadTime => reloadTime;
        public bool AutoReload => autoReload;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public bool IsPenetrating => isPenetrating;
        public AudioClip FireSound => fireSound;
        public AudioClip ReloadSound => reloadSound;
        public GameObject MuzzleFlashEffect => muzzleFlashEffect;

        private void OnValidate()
        {
            // 유효성 검사
            fireRate = Mathf.Max(0.1f, fireRate);
            damage = Mathf.Max(1, damage);
            range = Mathf.Max(1f, range);
            spread = Mathf.Max(0f, spread);
            magazineSize = Mathf.Max(1, magazineSize);
            reserveAmmo = Mathf.Max(0, reserveAmmo);
            reloadTime = Mathf.Max(0.1f, reloadTime);
            projectileSpeed = Mathf.Max(1f, projectileSpeed);
            projectileLifetime = Mathf.Max(0.1f, projectileLifetime);
        }

        /// <summary>
        /// 거리에 따른 데미지 계산
        /// </summary>
        public int GetDamageAtDistance(float distance)
        {
            if (distance >= range) return 0;
            
            // 거리에 따른 데미지 감소 (선형)
            float damageMultiplier = 1f - (distance / range) * 0.5f;
            return Mathf.RoundToInt(damage * damageMultiplier);
        }

        /// <summary>
        /// 탄착군 적용된 발사 방향 계산
        /// </summary>
        public Vector2 GetFireDirection(Vector2 aimDirection)
        {
            if (spread <= 0f) return aimDirection.normalized;
            
            // 스프레드 각도 내에서 랜덤 방향
            float spreadAngle = Random.Range(-spread * 0.5f, spread * 0.5f);
            float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            float finalAngle = (aimAngle + spreadAngle) * Mathf.Deg2Rad;
            
            return new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
        }

        /// <summary>
        /// DPS 계산
        /// </summary>
        public float GetDPS()
        {
            return damage * fireRate;
        }

        public override string ToString()
        {
            return $"ShooterConfig: {damage}dmg @ {fireRate}rpm (DPS: {GetDPS():F1})";
        }
    }
}
