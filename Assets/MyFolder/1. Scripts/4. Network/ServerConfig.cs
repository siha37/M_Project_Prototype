using UnityEngine;

public static class ServerConfig
{
    // 서버 설정
    #if UNITY_EDITOR
        public const string SERVER_IP = "127.0.0.1";
        public const int SERVER_PORT = 9122;
    #else
        public const string SERVER_IP = "39.120.49.222"; // 배포 시 실제 서버 IP로 변경
        public const int SERVER_PORT = 9222;
    #endif
    
    public const int FISHNET_PORT = 7777;       // FishNet 게임 포트
    
    // 타임아웃 설정
    public const int CONNECTION_TIMEOUT = 5000;  // 5초
    public const int REQUEST_TIMEOUT = 10000;    // 10초
    
    // 하트비트 설정
    public const float HEARTBEAT_INTERVAL = 30f; // 30초
    
    // 방 설정
    public const int DEFAULT_MAX_PLAYERS = 8;
    public const string DEFAULT_GAME_TYPE = "mafia";
    
    // 로컬 IP 주소 가져오기
    public static string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
} 