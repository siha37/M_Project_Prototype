using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using MyFolder._1._Scripts._3._SingleTone; // Added for Dictionary

public static class TcpClientHelper
{
    private static TcpClient persistentClient = null;
    private static NetworkStream persistentStream = null;
    private static readonly object lockObject = new object();
    private static string currentServerIP = "";
    private static int currentServerPort = 0;
    
    public static async Task<string> SendJsonAsync(string ip, int port, object payload)
    {
        try
        {
            // 기존 연결이 없거나 끊어진 경우 새로 연결
            if (persistentClient == null || !persistentClient.Connected || 
                currentServerIP != ip || currentServerPort != port)
            {
                await ConnectAsync(ip, port);
            }
            
            var json = JsonConvert.SerializeObject(payload);
            var buffer = Encoding.UTF8.GetBytes(json);
            
            // 요청 전송
            await persistentStream.WriteAsync(buffer, 0, buffer.Length);
            
            // 응답 수신
            var responseBuffer = new byte[4096];
            int bytesRead = await persistentStream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            var responseData = new byte[bytesRead];
            Array.Copy(responseBuffer, responseData, bytesRead);
            
            // 압축된 데이터인지 확인하고 해제
            string response;
            if (IsCompressedResponse(responseData))
            {
                response = DecompressResponse(responseData);
            }
            else
            {
                response = Encoding.UTF8.GetString(responseData, 0, responseData.Length);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            // 연결 오류 시 재연결 시도
                            LogManager.LogWarning(LogCategory.Network, $"TcpClientHelper 연결 오류, 재연결 시도: {ex.Message}");
            await DisconnectAsync();
            
            try
            {
                await ConnectAsync(ip, port);
                return await SendJsonAsync(ip, port, payload); // 재시도
            }
            catch (Exception retryEx)
            {
                throw new NetworkException($"네트워크 오류: {retryEx.Message}", retryEx);
            }
        }
    }
    
    public static async Task<T> SendJsonAsync<T>(string ip, int port, object payload)
    {
        string response = null;
        try
        {
            response = await SendJsonAsync(ip, port, payload);
            
            if (string.IsNullOrEmpty(response))
            {
                throw new NetworkException("서버에서 빈 응답을 받았습니다");
            }
            
            return JsonConvert.DeserializeObject<T>(response);
        }
        catch (JsonException ex)
        {
                            LogManager.LogError(LogCategory.Network, $"TcpClientHelper JSON 파싱 오류: {ex.Message}");
                LogManager.LogError(LogCategory.Network, $"TcpClientHelper 파싱 시도한 응답: {response}");
            throw new NetworkException($"JSON 파싱 오류: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            LogManager.LogError(LogCategory.Network, $"TcpClientHelper 응답 처리 오류: {ex.Message}");
            throw;
        }
    }
    
    public static async Task<T> SendAuthedJsonAsync<T>(string ip, int port, object payload, string deviceId, string sessionToken)
    {
        // payload를 Dictionary로 변환
        var dict = payload as IDictionary<string, object> ?? new Dictionary<string, object>();
        foreach (var prop in payload.GetType().GetProperties())
        {
            dict[prop.Name] = prop.GetValue(payload, null);
        }
        dict["deviceId"] = deviceId;
        dict["sessionToken"] = sessionToken;
        return await SendJsonAsync<T>(ip, port, dict);
    }
    
    private static async Task ConnectAsync(string ip, int port)
    {
        lock (lockObject)
        {
            if (persistentClient != null)
            {
                persistentClient.Close();
                persistentClient = null;
            }
            persistentClient = new TcpClient();
        }
        
        var connectTask = persistentClient.ConnectAsync(ip, port);
        if (await Task.WhenAny(connectTask, Task.Delay(ServerConfig.CONNECTION_TIMEOUT)) != connectTask)
        {
            throw new NetworkException("서버 연결 타임아웃");
        }
        
        persistentStream = persistentClient.GetStream();
        currentServerIP = ip;
        currentServerPort = port;
        
        LogManager.Log(LogCategory.Network, $"TcpClientHelper 서버에 연결됨: {ip}:{port}");
    }
    
    public static async Task DisconnectAsync()
    {
        lock (lockObject)
        {
            if (persistentStream != null)
            {
                persistentStream.Close();
                persistentStream = null;
            }
            if (persistentClient != null)
            {
                persistentClient.Close();
                persistentClient = null;
            }
            currentServerIP = "";
            currentServerPort = 0;
        }
        LogManager.Log(LogCategory.Network, "TcpClientHelper 서버 연결 해제");
    }
    
    public static bool IsConnected()
    {
        return persistentClient != null && persistentClient.Connected;
    }
    
    public static bool IsServerReachable(string ip, int port)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(ip, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                client.EndConnect(result);
                return success;
            }
        }
        catch
        {
            return false;
        }
    }
    
    private static bool IsCompressedResponse(byte[] data)
    {
        if (data.Length < 5) return false;
        
        // GZIP: 헤더 확인
        return data[0] == (byte)'G' && 
               data[1] == (byte)'Z' && 
               data[2] == (byte)'I' && 
               data[3] == (byte)'P' && 
               data[4] == (byte)':';
    }
    
    private static string DecompressResponse(byte[] compressedData)
    {
        try
        {
            // GZIP: 헤더 제거 (5바이트)
            var gzipData = new byte[compressedData.Length - 5];
            Array.Copy(compressedData, 5, gzipData, 0, gzipData.Length);
            
            using (var compressedStream = new MemoryStream(gzipData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                var decompressedData = resultStream.ToArray();
                return Encoding.UTF8.GetString(decompressedData);
            }
        }
        catch (Exception ex)
        {
            LogManager.LogError(LogCategory.Network, $"압축 해제 실패: {ex.Message}");
            throw new NetworkException($"압축 해제 실패: {ex.Message}", ex);
        }
    }
} 