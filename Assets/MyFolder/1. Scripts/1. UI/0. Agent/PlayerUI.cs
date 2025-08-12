using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._Agent
{
    public class PlayerUI : AgentUI
    {
        private PlayerMainHUD playerMainHUD = null;

        public override void InitializeUI(float initialHealth, float maxHealth, int initialAmmo, int maxAmmo,
            bool isOwner)
        {
            base.InitializeUI(initialHealth, maxHealth, initialAmmo, maxAmmo, isOwner);

            if (isOwner)
            {
                playerMainHUD = FindFirstObjectByType<PlayerMainHUD>();
                if (!playerMainHUD)
                    Invoke(nameof(GetHUD),1);
            }
        }

        private void GetHUD()
        {
            playerMainHUD = FindFirstObjectByType<PlayerMainHUD>();
        }

        public override void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            base.UpdateHealthUI(currentHealth, maxHealth);
            if (playerMainHUD)
                playerMainHUD.SetHealth(currentHealth, maxHealth);
        }
        
        public override void UpdateShieldUI(float currentShield, float maxShield)
        {
            base.UpdateHealthUI(currentShield, maxShield);
            if (playerMainHUD)
                playerMainHUD.SetShield(currentShield, maxShield);
        }
        
        public override void UpdateAmmoUI(int currentAmmo, int maxAmmo)
        {
            base.UpdateAmmoUI(currentAmmo, maxAmmo);
            if (playerMainHUD)
                playerMainHUD.SetAmmo(currentAmmo, maxAmmo);
        }
    }
}