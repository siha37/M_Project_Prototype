using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class DeviceIdentifier
{
    private static string deviceId = null;
    
    public static string GetDeviceId()
    {
        if (deviceId == null)
        {
            // 하드웨어 기반 고유 ID 생성
            string hardwareId = GetHardwareId();
            
            // 현재 프로세스 ID 추가
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            
            // 하드웨어 ID + 프로세스 ID 조합
            deviceId = $"{hardwareId}_{processId}";
            
            Debug.Log($"[DeviceIdentifier] 생성된 디바이스 ID: {deviceId} (프로세스 ID: {processId})");
        }
        
        return deviceId;
    }
    
    private static string GetHardwareId()
    {
        try
        {
            // 기존 하드웨어 ID 생성 로직
            string systemInfo = SystemInfo.deviceUniqueIdentifier;
            
            // 추가 하드웨어 정보 조합
            string cpuInfo = SystemInfo.processorType;
            string gpuInfo = SystemInfo.graphicsDeviceName;
            string ramInfo = SystemInfo.systemMemorySize.ToString();
            
            // 모든 정보를 조합하여 해시 생성
            string combinedInfo = $"{systemInfo}_{cpuInfo}_{gpuInfo}_{ramInfo}";
            
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedInfo));
                return Convert.ToBase64String(hashBytes).Substring(0, 16);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DeviceIdentifier] 하드웨어 ID 생성 실패: {ex.Message}");
            // 폴백: 랜덤 ID 생성
            return Guid.NewGuid().ToString().Substring(0, 16);
        }
    }
    
    // 디버깅용: 현재 프로세스 정보 출력
    public static void LogProcessInfo()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            Debug.Log($"[DeviceIdentifier] 프로세스 정보:");
            Debug.Log($"  - 프로세스 ID: {process.Id}");
            Debug.Log($"  - 프로세스 이름: {process.ProcessName}");
            Debug.Log($"  - 시작 시간: {process.StartTime}");
            Debug.Log($"  - 디바이스 ID: {GetDeviceId()}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DeviceIdentifier] 프로세스 정보 조회 실패: {ex.Message}");
        }
    }
} 