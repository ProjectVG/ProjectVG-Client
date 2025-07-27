# ProjectVG Network Module

Unity 클라이언트와 서버 간의 통신을 위한 네트워크 모듈입니다.

## 📦 설치

### 1. UniTask 설치
```json
// Packages/manifest.json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```

### 2. WebSocket 시뮬레이션
현재는 시뮬레이션 구현체를 사용합니다. 개발/테스트에 최적화되어 있습니다.

**시뮬레이션 구현체 장점:**
- 🟩 패키지 의존성 없음
- 🟩 즉시 사용 가능
- 🟩 개발/테스트에 적합
- 🟩 크로스 플랫폼 지원

## 🏗️ 구조

```
Assets/Infrastructure/Network/
├── Configs/                 # 설정 파일들
│   ├── ApiConfig.cs        # HTTP API 설정
│   └── WebSocketConfig.cs  # WebSocket 설정
├── DTOs/                   # 데이터 전송 객체들
│   ├── BaseApiResponse.cs  # 기본 API 응답
│   ├── Chat/              # 채팅 관련 DTO
│   ├── Character/         # 캐릭터 관련 DTO
│   └── WebSocket/         # WebSocket 메시지 DTO
├── Http/                  # HTTP 클라이언트
│   └── HttpApiClient.cs   # HTTP API 클라이언트
├── Services/              # API 서비스들
│   ├── ApiServiceManager.cs  # API 서비스 매니저
│   ├── ChatApiService.cs     # 채팅 API 서비스
│   └── CharacterApiService.cs # 캐릭터 API 서비스
└── WebSocket/             # WebSocket 관련
    ├── WebSocketManager.cs    # WebSocket 매니저
    ├── IWebSocketHandler.cs   # WebSocket 핸들러 인터페이스
    ├── DefaultWebSocketHandler.cs # 기본 핸들러
    └── Platforms/         # 플랫폼별 WebSocket 구현
        ├── UnityWebSocket.cs       # WebSocket 시뮬레이션
        ├── MobileWebSocket.cs      # 모바일 시뮬레이션
        └── WebSocketSharpFallback.cs # 폴백 시뮬레이션
```

## 🚀 사용법

### HTTP API 사용

```csharp
// API 서비스 매니저 사용
var apiManager = ApiServiceManager.Instance;

// 채팅 API 사용
var chatResponse = await apiManager.Chat.SendChatAsync(
    sessionId: "session-123",
    actor: "user",
    message: "안녕하세요!",
    characterId: "char-456",
    userId: "user-789"
);

// 캐릭터 API 사용
var characters = await apiManager.Character.GetCharactersAsync();
var character = await apiManager.Character.GetCharacterAsync("char-456");
```

### WebSocket 사용

```csharp
// WebSocket 매니저 사용
var wsManager = WebSocketManager.Instance;

// 핸들러 등록
var handler = gameObject.AddComponent<DefaultWebSocketHandler>();
wsManager.RegisterHandler(handler);

// 연결
await wsManager.ConnectAsync("session-123");

// 메시지 전송
await wsManager.SendChatMessageAsync(
    message: "안녕하세요!",
    characterId: "char-456",
    userId: "user-789"
);
```

## ⚙️ 설정

### ApiConfig 설정
```csharp
// ScriptableObject로 생성
var apiConfig = ApiConfig.CreateProductionConfig();
apiConfig.BaseUrl = "http://122.153.130.223:7900/api/v1/";
```

### WebSocketConfig 설정
```csharp
// ScriptableObject로 생성
var wsConfig = WebSocketConfig.CreateProductionConfig();
wsConfig.BaseUrl = "ws://122.153.130.223:7900/ws";
```

## 🔧 WebSocket 시뮬레이션 특별 기능

### 1. 패키지 의존성 없음
- 외부 패키지 설치 없이 즉시 사용 가능
- 개발/테스트에 최적화

### 2. 크로스 플랫폼 지원
- ✅ Unity Desktop
- ✅ Unity WebGL
- ✅ Unity Android
- ✅ Unity iOS

### 3. 시뮬레이션 모드
```csharp
// 실제 WebSocket 연결 대신 시뮬레이션
// 개발/테스트 단계에서 안전하게 사용
Debug.Log("WebSocket 시뮬레이션 연결");
```

## 🐛 문제 해결

### 시뮬레이션 모드
```
WebSocket 시뮬레이션 연결: ws://...
WebSocket 시뮬레이션 메시지: ...
```
**설명:** 실제 WebSocket 연결 대신 시뮬레이션으로 동작합니다.

### 개발/테스트
- 실제 서버 연결 없이도 개발 가능
- 로그를 통해 메시지 흐름 확인
- 안전한 테스트 환경 제공

## 📝 로그

모든 로그는 한국어로 출력됩니다:
- `Debug.Log("WebSocket 연결 성공")`
- `Debug.LogError("연결 실패")`
- `Debug.LogWarning("재연결 시도")` 