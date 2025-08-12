using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._Agent
{
    public class PlayerMainHUD : MonoBehaviour
    {
        [Header("체력바")]
        [SerializeField] private Image frontHealthBar;
        [SerializeField] private Image secondaryHealthBar;
        [SerializeField] private float healthBarLerpSpeed = 5f;
        private float targetHealthFill;
    
        [Header("실드바")]
        [SerializeField] private Image frontShieldBar;
        [SerializeField] private Image secondaryShieldBar;
        [SerializeField] private float shieldBarLerpSpeed = 5f;
        private float targetShieldFill;


        [Header("탄창")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private RectTransform ammoBar;
        private void Start()
        {
            if (frontHealthBar) frontHealthBar.fillAmount = 1f;
            
            if (secondaryHealthBar) secondaryHealthBar.fillAmount = 1f;

            if (frontShieldBar) frontShieldBar.fillAmount = 1f;
            
            if (secondaryShieldBar) secondaryShieldBar.fillAmount = 1f;   
        }
        
        private void Update()
        {
            // Secondary 체력바가 Front 체력바를 따라가도록 Lerp 적용
            if (secondaryHealthBar && frontHealthBar)
            {
                secondaryHealthBar.fillAmount = Mathf.Lerp(secondaryHealthBar.fillAmount, frontHealthBar.fillAmount, Time.deltaTime * healthBarLerpSpeed);
            }
            
            // Secondary 체력바가 Front 체력바를 따라가도록 Lerp 적용
            if (secondaryShieldBar && frontShieldBar)
            {
                secondaryShieldBar.fillAmount = Mathf.Lerp(secondaryShieldBar.fillAmount, frontShieldBar.fillAmount, Time.deltaTime * shieldBarLerpSpeed);
            }
        }
        
        
        public void InitUI()
        {
            
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {   
            if (frontHealthBar)
            {
                targetHealthFill = Mathf.Clamp01(currentHealth / maxHealth);
                frontHealthBar.fillAmount = targetHealthFill;
            }
        }

        public void SetShield(float currentShield, float maxShield)
        {
            if (frontShieldBar)
            {
                targetShieldFill = Mathf.Clamp01(currentShield / maxShield);
                frontShieldBar.fillAmount = targetShieldFill;
            }
        }
        public void SetAmmo(int ammo,int Maxammo)
        {
            ammoText.text = ammo+" / "+Maxammo;
            ammoBar.sizeDelta = new Vector2(ammoBar.sizeDelta.x,ammo * 128);            
        }
    }
}
