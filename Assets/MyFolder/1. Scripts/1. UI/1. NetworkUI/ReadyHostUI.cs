using System;
using UnityEngine;
using FishNet;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._4._Network;
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
    void Start()
    {
        if (!IsHost())
        {
            gameObject.SetActive(false);
            return;
        }

        Invoke("FindGameManager",0.5f);
        InitialzeUI();
    }
    
    void InitialzeUI()
    {
        startGameButton.onClick.AddListener(OnStartGameClicked);

        UpdateUIFromSettings();
    }

    void FindGameManager()
    {
        if(GameSettingManager.Instance)
        {
            GameSettingManager.Instance.OnSettingsChanged += UpdateUIFromSettings;
            GameSettingManager.Instance.OnPlayerCountChanged += UpdatePlayerCount;
        }
    }
    
    // 호스트 여부 확인
    bool IsHost()
    {
        return FishNetConnector.Instance.IsHost();
    }
    
    private void OnStartGameClicked()
    {
        if (GameSettingManager.Instance)
            GameSettingManager.Instance.RequestStartGame();
    }

    
    private void UpdateUIFromSettings()
    {
        if (GameSettingManager.Instance)
        {
            GameSettings settings = GameSettingManager.Instance.GetCurrentSettings();
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
        if (GameSettingManager.Instance)
        {
            GameSettingManager.Instance.OnSettingsChanged -= UpdateUIFromSettings;
            GameSettingManager.Instance.OnPlayerCountChanged -= UpdatePlayerCount;
        }
    }
}
