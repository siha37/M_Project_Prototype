# Unity FishNet 방 관리 클라이언트

이 폴더는 Unity FishNet 기반 멀티플레이 게임을 위한 TCP 방 관리 클라이언트 코드를 포함합니다.

## 📁 파일 구조

```
4. Network/
├── RoomInfo.cs              # 방 정보 데이터 구조
├── ServerConfig.cs          # 서버 설정 관리
├── NetworkException.cs      # 네트워크 예외처리
├── TcpClientHelper.cs       # TCP 통신 헬퍼
├── RoomHost.cs             # 호스트용 방 관리
├── RoomGuest.cs            # 게스트용 방 관리
├── NetworkManager.cs        # 네트워크 통합 관리
├── HeartbeatManager.cs      # 하트비트 관리
├── NetworkUIManager.cs      # UI 연동 매니저
├── NetworkExample.cs        # 사용 예제
└── README.md               # 이 파일
```

## 🚀 빠른 시작

### 1. 기본 설정

```csharp
// ServerConfig.cs에서 서버 IP 설정
#if UNITY_EDITOR
    public const string SERVER_IP = "127.0.0.1";
#else
    public const string SERVER_IP = "실제서버IP"; // 배포 시 변경
#endif
```

### 2. NetworkManager 초기화

```csharp
// 씬에 NetworkManager 컴포넌트 추가
// 또는 코드에서 자동 생성
NetworkManager.Instance.TestConnectionAsync();
```

### 3. 방 생성 (호스트)

```csharp
var success = await NetworkManager.Instance.CreateRoomAsync("방 이름", 8);
if (success)
{
    string roomId = NetworkManager.Instance.GetCurrentRoomId();
    HeartbeatManager.Instance.StartHeartbeat(roomId);
}
```

### 4. 방 목록 조회 (게스트)

```csharp
var rooms = await NetworkManager.Instance.GetRoomListAsync();
foreach (var room in rooms)
{
    Debug.Log($"방: {room.roomName} ({room.currentPlayers}/{room.maxPlayers})");
}
```

## 🔧 주요 기능

### NetworkManager
- 서버 연결 관리
- 방 생성/삭제
- 방 목록 조회
- 이벤트 시스템

### RoomHost
- 방 생성
- 방 삭제
- 하트비트 전송

### RoomGuest
- 방 목록 조회
- 방 정보 조회
- 방 검색/필터링

### HeartbeatManager
- 자동 하트비트 전송
- 하트비트 간격 설정
- 하트비트 상태 모니터링

## 🎮 UI 연동

### NetworkUIManager 사용법

1. UI 요소들을 Inspector에서 연결
2. 자동 새로고침 설정
3. 이벤트 기반 UI 업데이트

```csharp
// UI 매니저 초기화
NetworkUIManager uiManager = GetComponent<NetworkUIManager>();

// 이벤트 구독
NetworkManager.Instance.OnRoomListReceived += OnRoomListReceived;
NetworkManager.Instance.OnConnectionError += OnConnectionError;
```

## 📡 서버 통신 프로토콜

### 방 생성 요청
```json
{
    "type": "create",
    "roomId": "R1234567890",
    "hostAddress": "192.168.1.100",
    "hostPort": 7777,
    "maxPlayers": 8,
    "roomName": "테스트 방",
    "gameType": "mafia",
    "isPrivate": false
}
```

### 방 목록 조회 요청
```json
{
    "type": "list",
    "includePrivate": false
}
```

### 하트비트 요청
```json
{
    "type": "heartbeat",
    "roomId": "R1234567890",
    "timestamp": 1234567890,
    "playerCount": 3
}
```

## ⚙️ 설정 옵션

### ServerConfig
- `SERVER_IP`: 서버 IP 주소
- `SERVER_PORT`: TCP 서버 포트 (기본: 9000)
- `FISHNET_PORT`: FishNet 게임 포트 (기본: 7777)
- `CONNECTION_TIMEOUT`: 연결 타임아웃 (기본: 5초)
- `HEARTBEAT_INTERVAL`: 하트비트 간격 (기본: 30초)

### HeartbeatManager
- `heartbeatInterval`: 하트비트 전송 간격
- `autoHeartbeat`: 자동 하트비트 활성화

## 🧪 테스트

### NetworkExample 사용
1. NetworkExample 컴포넌트를 GameObject에 추가
2. Play 모드에서 자동 테스트 실행
3. OnGUI 패널에서 수동 테스트 가능

### 수동 테스트
```csharp
// Context Menu에서 테스트 실행
[ContextMenu("서버 연결 테스트")]
public async void ManualTestConnection()
{
    var success = await NetworkManager.Instance.TestConnectionAsync();
    Debug.Log(success ? "성공!" : "실패!");
}
```

## 🔍 디버깅

### 로그 확인
- 모든 네트워크 작업은 Debug.Log로 로깅
- 오류는 Debug.LogError로 출력
- 하트비트 상태는 Debug.LogWarning으로 출력

### 연결 상태 확인
```csharp
bool isConnected = NetworkManager.Instance.isConnected;
string status = NetworkManager.Instance.connectionStatus;
```

## 🚨 주의사항

1. **Newtonsoft.Json 필요**: 프로젝트에 Newtonsoft.Json 패키지 추가 필요
2. **서버 실행**: Python TCP 서버가 실행 중이어야 함
3. **포트 설정**: 방화벽에서 9000번 포트 허용 필요
4. **FishNet 연동**: 실제 게임 연결은 별도 구현 필요

## 📝 TODO

- [ ] FishNet과의 직접 연동
- [ ] 보안 강화 (암호화, 인증)
- [ ] 재연결 메커니즘
- [ ] 방 비밀번호 기능
- [ ] 플레이어 목록 관리
- [ ] 게임 상태 동기화

## 🤝 기여

버그 리포트나 기능 요청은 이슈로 등록해주세요. 