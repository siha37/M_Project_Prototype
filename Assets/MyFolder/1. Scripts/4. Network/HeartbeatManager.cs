using System;
using System.Threading.Tasks;
using UnityEngine;

public class HeartbeatManager : MonoBehaviour
{
    [Header("Heartbeat Settings")]
    public float heartbeatInterval = ServerConfig.HEARTBEAT_INTERVAL;
    public bool autoHeartbeat = true;
    
    private string currentRoomId;
    private float lastHeartbeatTime;
    private bool isHeartbeating = false;
    
    private static HeartbeatManager instance;
    public static HeartbeatManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HeartbeatManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("HeartbeatManager");
                    instance = go.AddComponent<HeartbeatManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (autoHeartbeat && isHeartbeating && !string.IsNullOrEmpty(currentRoomId))
        {
            if (Time.time - lastHeartbeatTime > heartbeatInterval)
            {
                SendHeartbeat();
            }
        }
    }
    
    public void StartHeartbeat(string roomId)
    {
        currentRoomId = roomId;
        lastHeartbeatTime = Time.time;
        isHeartbeating = true;
        
        Debug.Log($"하트비트 시작: {roomId}");
    }
    
    public void StopHeartbeat()
    {
        currentRoomId = null;
        isHeartbeating = false;
        
        Debug.Log("하트비트 중지");
    }
    
    public async void SendHeartbeat()
    {
        if (string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogWarning("하트비트 전송 실패: 방 ID가 없습니다");
            return;
        }
        
        try
        {
            var success = await TCPNetworkManager.Instance.SendHeartbeatAsync(currentRoomId);
            
            if (success)
            {
                lastHeartbeatTime = Time.time;
                Debug.Log($"하트비트 전송 성공: {currentRoomId}");
            }
            else
            {
                Debug.LogWarning($"하트비트 전송 실패: {currentRoomId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"하트비트 전송 중 오류: {ex.Message}");
        }
    }
    
    public void SetHeartbeatInterval(float interval)
    {
        heartbeatInterval = interval;
        Debug.Log($"하트비트 간격 변경: {interval}초");
    }
    
    public void SetAutoHeartbeat(bool enabled)
    {
        autoHeartbeat = enabled;
        Debug.Log($"자동 하트비트: {(enabled ? "활성화" : "비활성화")}");
    }
    
    public string GetCurrentRoomId()
    {
        return currentRoomId;
    }
    
    public bool IsHeartbeating()
    {
        return isHeartbeating;
    }
    
    public float GetTimeSinceLastHeartbeat()
    {
        return Time.time - lastHeartbeatTime;
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
} 