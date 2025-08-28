# Actor 컴포지션 시스템

Unity/FishNet 기반의 컴포지션 아키텍처로 구현된 액터 시스템입니다.

## 📁 폴더 구조

```
@00. Actor/
├── 0. Core/                 # 핵심 인터페이스와 베이스 클래스
├── 1. Interfaces/           # 기능별 인터페이스
├── 2. Data/                 # 데이터 구조체
├── 3. Network/              # 네트워크 동기화 허브
├── 4. Components/           # 게임플레이 컴포넌트
│   ├── Input/               # 입력 처리
│   ├── Movement/            # 이동 관련
│   └── Health/              # 체력 관리
├── 5. Builder/              # 액터 조립 빌더
├── 6. Config/               # 설정 ScriptableObject
├── 7. NetworkModules/       # 네트워크 동기화 모듈
└── 8. Authoring/            # Unity 진입점
```

## 🎯 핵심 개념

### 1. 컴포지션 기반 설계
- **Actor**: 컴포넌트들을 소유하고 수명주기를 관리
- **IActorComponent**: 모든 컴포넌트가 구현하는 기본 인터페이스
- **IActorUpdatable**: 업데이트 파이프라인에 참여하는 컴포넌트

### 2. 이벤트 기반 통신
- **ActorEventBus**: 컴포넌트 간 느슨한 결합을 위한 이벤트 허브
- 직접 참조 대신 이벤트 발행/구독으로 통신

### 3. 우선순위 기반 실행
- **UpdateScheduler**: 결정적인 실행 순서 보장
- Priority 값이 낮을수록 먼저 실행 (Input 0 → AI 5 → Move 10 → Combat 20 → UI 90)

### 4. 네트워크 권한 분리
- **서버**: 게임플레이 로직 (Health, Movement, Combat)
- **클라이언트**: 뷰/프리젠테이션 (Animation, HUD, Effects)
- **Owner**: 입력 처리 (PlayerInput)

### 5. 데이터 주도 구성
- **ActorPreset**: 어떤 컴포넌트를 사용할지 정의
- **Config 파일들**: 각 컴포넌트의 초기값과 설정
- **ActorBuilder**: 프리셋을 기반으로 액터 조립

## 🚀 사용 방법

### 1. 기본 액터 생성

```csharp
// GameObject에 Actor와 ActorAuthoring 컴포넌트 추가
var actorGO = new GameObject("Player");
var actor = actorGO.AddComponent<PlayerActor>();
var authoring = actorGO.AddComponent<ActorAuthoring>();

// 프리셋 설정
authoring.SetActorPreset(playerPreset);
```

### 2. 커스텀 컴포넌트 추가

```csharp
public class CustomComponent : IActorComponent, IActorUpdatable
{
    public int Priority => 30; // 실행 우선순위

    public void Init(Actor actor)
    {
        // 초기화 로직
        actor.EventBus.FireStarted += OnFireStarted;
    }

    public void OnEnable() { /* 활성화 시 */ }
    public void OnDisable() { /* 비활성화 시 */ }
    public void Update() { /* 매 프레임 */ }
    public void FixedUpdate() { /* 물리 업데이트 */ }
    public void LateUpdate() { /* 후처리 */ }
    public void Dispose() { /* 정리 */ }

    private void OnFireStarted()
    {
        // 발사 이벤트 처리
    }
}

// 액터에 추가
actor.AddComponent(new CustomComponent());
```

### 3. 네트워크 동기화 모듈

```csharp
public class CustomNetSyncModule : IActorNetSync
{
    public int ComponentId => 10; // 고유 ID
    public int Priority => 20;    // 동기화 우선순위
    public bool IsDirty { get; private set; }

    public void Write(PooledWriter writer)
    {
        // 서버 → 클라이언트 직렬화
        writer.WriteInt32(someValue);
    }

    public void Read(PooledReader reader)
    {
        // 클라이언트에서 역직렬화
        int value = reader.ReadInt32();
        // UI 갱신 등
    }

    // 기타 인터페이스 구현...
}

// 네트워크 동기화에 등록
actor.NetworkSync.RegisterModule(new CustomNetSyncModule());
```

## ⚙️ 설정 파일 생성

### 1. ActorPreset 생성
- `Assets/Create/Actor/Actor Preset`
- 사용할 컴포넌트들의 Config 파일 연결

### 2. Config 파일들 생성
- `HealthConfig`: 체력 관련 설정
- `MovementConfig`: 이동 관련 설정
- `ShooterConfig`: 사격 관련 설정
- `AnimationProfile`: 애니메이션 설정

## 🔧 확장 가이드

### 새로운 컴포넌트 추가
1. `IActorComponent` (필수) 및 `IActorUpdatable` (선택) 구현
2. 필요한 경우 전용 인터페이스 정의 (예: `IShootable`)
3. Config 파일과 Settings 구조체 생성
4. 네트워크 동기화가 필요하면 NetSyncModule 구현
5. ActorBuilder에 조립 로직 추가

### 새로운 액터 타입 추가
1. `Actor`를 상속받은 전용 클래스 생성
2. `ActorPreset`에서 새 액터 타입 정의
3. `ActorBuilder`에 타입별 조립 로직 추가

## 🎮 예제 액터 구성

### 플레이어
- **서버**: `HealthComponent`, `PlayerMoveComponent`, `ShooterComponent`
- **클라이언트**: `AnimationComponent`, `HUDComponent`
- **Owner**: `PlayerInputComponent`

### AI
- **서버**: `HealthComponent`, `AiMoveComponent`, `ShooterComponent`, `AiController`, `PerceptionComponent`
- **클라이언트**: `AnimationComponent`

### NPC (상호작용만)
- **서버**: `HealthComponent`, `InteractComponent`
- **클라이언트**: `AnimationComponent`, `DialogueComponent`

## 📊 성능 고려사항

1. **업데이트 최적화**: Priority로 필요한 컴포넌트만 실행
2. **네트워크 최적화**: 더티 마스크로 변경된 것만 전송
3. **메모리 최적화**: POCO 컴포넌트로 GC 압박 최소화
4. **캐싱**: 자주 사용하는 참조는 Init에서 캐싱

## 🐛 디버깅

- `Actor.DebugComponents()`: 등록된 컴포넌트 목록 출력
- `ActorBuilder.DebugComponents()`: 빌드된 컴포넌트 상태 확인
- `ActorNetworkSync.DebugModules()`: 네트워크 모듈 상태 확인

## 📝 주의사항

1. **네트워크 권한**: 서버에서만 게임플레이 상태 변경
2. **이벤트 구독**: OnEnable/OnDisable에서 구독/해제 필수
3. **컴포지션 순서**: 의존성이 있는 컴포넌트는 순서 고려
4. **FishNet 규칙**: DontDestroyOnLoad 대신 GlobalObject 사용 [[memory:4238833]]
5. **SyncVar 사용**: 제네릭 스타일 `[SyncVar<T>]`와 `.Value` 접근 선호 [[memory:4129845]]

---

이 시스템을 통해 확장 가능하고 유지보수가 용이한 멀티플레이어 게임 아키텍처를 구축할 수 있습니다.
