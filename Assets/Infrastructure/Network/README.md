# ProjectVG Network Module

Unity 클라이언트와 서버 간의 통신을 위한 네트워크 모듈입니다.

## 📦 설치

### 1. UniTask 설치
```json
// Packages/manifest.json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```

### 2. 플랫폼별 WebSocket 구현
플랫폼에 따라 최적화된 WebSocket 구현을 사용합니다.

**플랫폼별 구현:**
- 🟩 Desktop: System.Net.WebSockets.ClientWebSocket
- 🟩 WebGL: UnityWebRequest.WebSocket
- 🟩 Mobile: 네이티브 WebSocket 라이브러리

## 🏗️ 구조

```
Assets/Infrastructure/Network/
├── Configs/                    # 설정 파일들
│   └── NetworkConfig.cs        # Unity 표준 ScriptableObject 기반 설정
├── DTOs/                      # 데이터 전송 객체들
│   ├── BaseApiResponse.cs     # 기본 API 응답
│   ├── Chat/                 # 채팅 관련 DTO
│   ├── Character/            # 캐릭터 관련 DTO
│   └── WebSocket/            # WebSocket 메시지 DTO
├── Http/                     # HTTP 클라이언트
│   └── HttpApiClient.cs      # HTTP API 클라이언트
├── Services/                 # API 서비스들
│   ├── ApiServiceManager.cs  # API 서비스 매니저
│   ├── ChatApiService.cs     # 채팅 API 서비스
│   └── CharacterApiService.cs # 캐릭터 API 서비스
└── WebSocket/                # WebSocket 관련
    ├── WebSocketManager.cs       # WebSocket 매니저
    ├── WebSocketFactory.cs       # 플랫폼별 WebSocket 팩토리
    ├── IWebSocketHandler.cs      # WebSocket 핸들러 인터페이스
    ├── DefaultWebSocketHandler.cs # 기본 핸들러
    └── Platforms/            # 플랫폼별 WebSocket 구현
        ├── DesktopWebSocket.cs    # 데스크톱용 (.NET ClientWebSocket)
        ├── WebGLWebSocket.cs      # WebGL용 (UnityWebRequest)
        └── MobileWebSocket.cs     # 모바일용 (네이티브 라이브러리)
```

## 🚀 사용법

### 1. Unity 표준 ScriptableObject 기반 설정 관리

#### 설정 파일 생성
```csharp
// Unity Editor에서 ScriptableObject 생성
// Assets/Infrastructure/Network/Configs/ 폴더에서 우클릭
// Create > ProjectVG > Network > NetworkConfig
// Resources 폴더에 NetworkConfig.asset 파일 생성
```

#### 앱 시작 시 환경 설정
```csharp
// 앱 시작 시 (Editor에서만 가능)
NetworkConfig.SetDevelopmentEnvironment();  // localhost:7900
NetworkConfig.SetTestEnvironment();         // localhost:7900
NetworkConfig.SetProductionEnvironment();   // 122.153.130.223:7900
```

#### 런타임 중 설정 사용
```csharp
// 어디서든 동일한 설정 접근 (강제로 NetworkConfig 사용)
var currentEnv = NetworkConfig.CurrentEnvironment;
var apiUrl = NetworkConfig.GetFullApiUrl("chat");
var wsUrl = NetworkConfig.GetWebSocketUrl();

// API URL 생성
var userUrl = NetworkConfig.GetUserApiUrl();
var characterUrl = NetworkConfig.GetCharacterApiUrl();
var conversationUrl = NetworkConfig.GetConversationApiUrl();
var authUrl = NetworkConfig.GetAuthApiUrl();

// WebSocket URL 생성
var wsUrl = NetworkConfig.GetWebSocketUrl();
var wsUrlWithVersion = NetworkConfig.GetWebSocketUrlWithVersion();
var wsUrlWithSession = NetworkConfig.GetWebSocketUrlWithSession("session-123");
```

### 2. 전체 흐름 테스트 (권장)
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

### 3. 개별 모듈 사용

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

### Unity 표준 ScriptableObject 설정 관리

#### 1. 설정 파일 생성
1. **NetworkConfig.asset** 생성:
   - `Assets/Resources/` 폴더에 `NetworkConfig.asset` 파일 생성
   - Unity Editor에서 우클릭 > Create > ProjectVG > Network > NetworkConfig

#### 2. 환경별 서버 주소
- **개발 환경**: `localhost:7900`
- **테스트 환경**: `localhost:7900`  
- **프로덕션 환경**: `122.153.130.223:7900`

#### 3. 설정값들 (Editor에서 설정 가능)
- **HTTP 타임아웃**: 30초
- **최대 재시도**: 3회
- **WebSocket 타임아웃**: 30초
- **자동 재연결**: 활성화
- **하트비트**: 활성화 (30초 간격)

#### 4. 런타임 보안
- ✅ 앱 시작 시 한 번 설정
- ✅ 런타임 중 설정 변경 불가
- ✅ 어디서든 동일한 설정 접근
- ✅ ScriptableObject로 일관성 보장
- ✅ Editor에서 설정 가능
- ✅ 팀 협업 용이
- ✅ NetworkConfig 강제 사용으로 일관성 보장

## 🔧 플랫폼별 WebSocket 구현

### 1. DesktopWebSocket (데스크톱)
- System.Net.WebSockets.ClientWebSocket 사용
- Windows/Mac/Linux 지원
- 최고 성능

### 2. WebGLWebSocket (브라우저)
- UnityWebRequest.WebSocket 사용
- WebGL 플랫폼 지원
- 브라우저 제약사항 대응

### 3. MobileWebSocket (모바일)
- 네이티브 WebSocket 라이브러리 사용
- iOS/Android 지원
- 네이티브 성능

### 4. WebSocketFactory
- 플랫폼별 WebSocket 구현 생성
- 컴파일 타임에 적절한 구현체 선택

## 🐛 문제 해결

### 플랫폼별 WebSocket
```
데스크톱 플랫폼용 WebSocket 생성
WebSocket 연결 시도: ws://localhost:7900/ws
WebSocket 연결 성공
```
**설명:** 플랫폼에 따라 적절한 WebSocket 구현체가 자동으로 선택됩니다.

### 플랫폼별 특징
- **Desktop**: .NET ClientWebSocket으로 최고 성능
- **WebGL**: 브라우저 WebSocket API 사용
- **Mobile**: 네이티브 라이브러리로 최적화

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