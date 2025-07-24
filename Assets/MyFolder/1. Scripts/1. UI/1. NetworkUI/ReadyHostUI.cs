using System;
using UnityEngine;
using FishNet;
using MyFolder._1._Scripts._5._Manager;
using TMPro;
using UnityEngine.UI;

public class ReadyHostUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Image maxPlayerSlider;
    [SerializeField] private TextMeshProUGUI playerCountText;

    [Header("Setting")] 
    [SerializeField] private GameSettings currentSettings;
    
    [SerializeField] private GameSettingManager gameManager; 
    void Start()
    {
        if (!IsHost())
        {
            gameObject.SetActive(false);
            return;
        }

        FindGameManager();
        InitialzeUI();
    }
    
    void InitialzeUI()
    {
        startGameButton.onClick.AddListener(OnStartGameClicked);

        UpdateUIFromSettings();
    }

    void FindGameManager()
    {
        if (gameManager != null)
        {
            var settings = gameManager.GetCurrentSettings();
            gameManager.OnSettingsChanged += UpdateUIFromSettings;
            gameManager.OnPlayerCountChanged += UpdatePlayerCount;
        }
    }
    
    // 호스트 여부 확인
    bool IsHost()
    {
        return FishNetConnector.Instance.IsHost();
    }
    
    private void OnStartGameClicked()
    {
        if (gameManager)
            gameManager.RequestStartGame();
    }

    
    private void UpdateUIFromSettings()
    {
        if (gameManager)
        {
            var settings = gameManager.GetCurrentSettings();
        }
    }

    private void UpdatePlayerCount(int count,int max)
    {
        playerCountText.text = $"플레이어: {count}/{max}";
        maxPlayerSlider.fillAmount = (float)count/max;
        startGameButton.interactable = count >= 2;
    }

    private void OnDestroy()
    {
        if (gameManager)
        {
            gameManager.OnSettingsChanged -= UpdateUIFromSettings;
            gameManager.OnPlayerCountChanged -= UpdatePlayerCount;
        }
    }
}
