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

### 1. 전체 흐름 테스트 (권장)
```csharp
// NetworkTestManager 사용
var testManager = FindObjectOfType<NetworkTestManager>();

// 1. WebSocket 연결
await testManager.ConnectWebSocket();

// 2. HTTP 요청 전송
await testManager.SendChatRequest();

// 3. WebSocket으로 결과 수신 (자동)
// 서버가 비동기 작업 완료 후 WebSocket으로 결과 전송
```

### 2. 개별 모듈 사용

#### HTTP API 사용
```csharp
// API 서비스 매니저 사용
var apiManager = ApiServiceManager.Instance;

// 채팅 API 사용
var chatResponse = await apiManager.ChatApiService.SendChatAsync(
    new ChatRequest
    {
        message = "안녕하세요!",
        characterId = "char-456",
        userId = "user-789",
        sessionId = "session-123"
    }
);

// 캐릭터 API 사용
var character = await apiManager.CharacterApiService.GetCharacterAsync("char-456");
```

#### WebSocket 사용
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

### 테스트 환경 설정
```csharp
// localhost:7900으로 설정
var apiConfig = ApiConfig.CreateDevelopmentConfig();
var wsConfig = WebSocketConfig.CreateDevelopmentConfig();
```

### 프로덕션 환경 설정
```csharp
// 실제 서버로 설정
var apiConfig = ApiConfig.CreateProductionConfig();
var wsConfig = WebSocketConfig.CreateProductionConfig();
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
WebSocket 시뮬레이션 연결: ws://localhost:7900/ws
WebSocket 시뮬레이션 메시지: ...
```
**설명:** 실제 WebSocket 연결 대신 시뮬레이션으로 동작합니다.

### 개발/테스트
- 실제 서버 연결 없이도 개발 가능
- 로그를 통해 메시지 흐름 확인
- 안전한 테스트 환경 제공

### 테스트 실행 방법
1. **NetworkTestManager** 컴포넌트를 씬에 추가
2. **Context Menu**에서 테스트 실행:
   - `1. WebSocket 연결`
   - `2. HTTP 채팅 요청`
   - `3. HTTP 캐릭터 정보 요청`
   - `4. WebSocket 메시지 전송`
   - `5. WebSocket 연결 해제`
   - `전체 테스트 실행`

## 📝 로그

모든 로그는 한국어로 출력됩니다:
- `Debug.Log("WebSocket 연결 성공")`
- `Debug.LogError("연결 실패")`
- `Debug.LogWarning("재연결 시도")` 