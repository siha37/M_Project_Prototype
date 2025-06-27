using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AgentUI : MonoBehaviour
{
    [Header("체력바")]
    [SerializeField] private Image frontHealthBar;
    [SerializeField] private Image secondaryHealthBar;
    [SerializeField] private float healthBarLerpSpeed = 5f;
    private float targetHealthFill;

    [Header("재장전")]
    [SerializeField] private Image reloadBar;

    [Header("부활")]
    [SerializeField] private Image reviveBar;

    [Header("탄창")]
    [SerializeField] private TextMeshProUGUI ammoText;

    private void Start()
    {
        if (reloadBar != null)
            reloadBar.fillAmount = 0f;
            
        if (frontHealthBar != null)
            frontHealthBar.fillAmount = 1f;
            
        if (secondaryHealthBar != null)
            secondaryHealthBar.fillAmount = 1f;

        if (reviveBar != null)
            reviveBar.fillAmount = 0f;
    }

    private void Update()
    {
        // Secondary 체력바가 Front 체력바를 따라가도록 Lerp 적용
        if (secondaryHealthBar != null && frontHealthBar != null)
        {
            secondaryHealthBar.fillAmount = Mathf.Lerp(secondaryHealthBar.fillAmount, frontHealthBar.fillAmount, Time.deltaTime * healthBarLerpSpeed);
        }
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (frontHealthBar != null)
        {
            targetHealthFill = Mathf.Clamp01(currentHealth / maxHealth);
            frontHealthBar.fillAmount = targetHealthFill;
        }
    }

    public void StartReloadUI()
    {
        if (reloadBar != null)
            reloadBar.fillAmount = 0f;
    }

    public void UpdateReloadProgress(float progress)
    {
        if (reloadBar != null)
            reloadBar.fillAmount = Mathf.Clamp01(progress);
    }

    public void EndReloadUI()
    {            
        if (reloadBar != null)
            reloadBar.fillAmount = 0f;
    }

    public void StartReviveUI()
    {
        if (reviveBar != null)
            reviveBar.fillAmount = 0f;
    }

    public void UpdateReviveProgress(float progress)
    {
        if (reviveBar != null)
            reviveBar.fillAmount = Mathf.Clamp01(progress);
    }

    public void EndReviveUI()
    {
        if (reviveBar != null)
            reviveBar.fillAmount = 0f;
    }

    public void UpdateAmmoUI(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null)
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
    }

    // UI 초기화 함수
    public void InitializeUI(float initialHealth, float maxHealth, int initialAmmo, int maxAmmo)
    {
        UpdateHealthUI(initialHealth, maxHealth);
        UpdateAmmoUI(initialAmmo, maxAmmo);
    }
}
