# Unity/C# 네이밍 컨벤션 가이드

이 문서는 ProjectVG Unity 클라이언트의 코드 네이밍 규칙과 컨벤션을 정의합니다.

---

## 기본 C# 컨벤션

### 네이밍 스타일
- **클래스명, 메서드명, 프로퍼티명**: `PascalCase`
- **변수명, 매개변수명**: `camelCase`
- **상수명**: `UPPER_SNAKE_CASE`
- **인터페이스명**: `IPascalCase` (I 접두사)

### 접근 제한자 규칙
- **private 필드**: `_camelCase` (언더스코어 접두사)
- **public 필드**: `camelCase`
- **프로퍼티**: `PascalCase`
- **메서드**: `PascalCase`

### 예시
```csharp
public class ChatManager
{
    private string _sessionId;           // private 필드
    public string characterId;           // public 필드
    public string SessionId { get; set; } // 프로퍼티
    
    private void InitializeSession()     // private 메서드
    {
        // 구현
    }
    
    public void SendMessage(string message) // public 메서드
    {
        // 구현
    }
}
```

---

## Unity 클래스 네이밍 컨벤션

| 역할 | 접미어 또는 접두어 | 예시 | 설명 |
|------|-------------------|------|------|
| **MonoBehaviour** | Controller, Manager, Behaviour, System 등 | PlayerController, GameManager, CameraBehaviour | 씬에 붙는 실행 스크립트 |
| **데이터 객체** (Plain C# Class) | Data, Info, Model, Config, State 등 | PlayerData, LevelInfo, GameConfig | 직렬화 또는 로직 없는 순수 데이터 |
| **싱글톤 서비스** | Service, Manager, System | AudioService, InputManager, SaveSystem | 전역 기능 담당 클래스 |
| **인터페이스** | I 접두어 | IMoveable, IDamageable | 일반 C# 인터페이스 명명 규칙 동일 |
| **UI 컴포넌트** | UI, Panel, View, Dialog | MainMenuUI, SettingsPanel, GameOverDialog | UI Prefab용 MonoBehaviour |
| **이벤트/메시지 객체** | Event, Message | GameStartEvent, PlayerDeathMessage | EventBus 또는 Observer 용 메시지 구조체 |
| **스크립터블 오브젝트** | SO, Config, Asset, Definition | WeaponConfig, LevelDefinition | ScriptableObject 파생 클래스 |
| **테스트/디버깅** | Debug, Tester, Sample, Fake | PlayerDebug, SoundTester, FakeEnemyAI | 테스트, 샘플 용도 클래스 |

---

## Unity 특화 컨벤션

### 클래스명 접미사 규칙

| 역할 | 접미사 | 예시 | 설명 |
|------|--------|------|------|
| 매니저/컨트롤러 | `Manager` | `ChatManager`, `AudioManager` | 전체 시스템 관리 |
| 서비스 | `Service` | `ChatApiService`, `DataService` | 외부 서비스 연동 |
| 컨트롤러 | `Controller` | `PlayerController`, `UIController` | 특정 기능 제어 |
| 핸들러 | `Handler` | `WebSocketHandler`, `EventHandler` | 이벤트/메시지 처리 |
| 팩토리 | `Factory` | `UIFactory`, `ObjectFactory` | 객체 생성 |
| 풀 | `Pool` | `ObjectPool`, `AudioPool` | 객체 재사용 |
| 뷰 | `View` | `ChatView`, `CharacterView` | UI 표시 담당 |
| 모델 | `Model` | `ChatModel`, `UserModel` | 데이터 구조 |
| DTO | `Request`/`Response` | `ChatRequest`, `LoginResponse` | 데이터 전송 객체 |
| 인터페이스 | `I` + 기능명 | `INetworkClient`, `IAudioPlayer` | 인터페이스 |

### 메서드명 접두사 규칙

| 기능 | 접두사 | 예시 | 설명 |
|------|--------|------|------|
| 초기화 | `Initialize` | `InitializeChat()` | 초기 설정 |
| 설정 | `Setup` | `SetupUI()` | 구성 설정 |
| 시작 | `Start` | `StartSession()` | 프로세스 시작 |
| 중지 | `Stop` | `StopAudio()` | 프로세스 중지 |
| 정리 | `Cleanup` | `CleanupResources()` | 리소스 정리 |
| 업데이트 | `Update` | `UpdatePosition()` | 상태 업데이트 |
| 처리 | `Process` | `ProcessMessage()` | 데이터 처리 |
| 검증 | `Validate` | `ValidateInput()` | 입력 검증 |
| 변환 | `Convert` | `ConvertToJson()` | 형식 변환 |
| 로드 | `Load` | `LoadConfig()` | 데이터 로드 |
| 저장 | `Save` | `SaveData()` | 데이터 저장 |
| 전송 | `Send` | `SendMessage()` | 네트워크 전송 |
| 수신 | `Receive` | `ReceiveResponse()` | 네트워크 수신 |

### 변수명 접두사 규칙

| 타입 | 접두사 | 예시 | 설명 |
|------|--------|------|------|
| GameObject | `go` | `goPlayer` | 게임 오브젝트 |
| Transform | `tr` | `trPlayer` | 트랜스폼 |
| Component | `comp` | `compAudio` | 컴포넌트 |
| UI 요소 | `ui` | `uiButton` | UI 오브젝트 |
| Text | `txt` | `txtMessage` | 텍스트 컴포넌트 |
| Image | `img` | `imgAvatar` | 이미지 컴포넌트 |
| Button | `btn` | `btnSend` | 버튼 컴포넌트 |
| InputField | `input` | `inputMessage` | 입력 필드 |
| AudioSource | `audio` | `audioBGM` | 오디오 소스 |
| Camera | `cam` | `camMain` | 카메라 |
| Rigidbody | `rb` | `rbPlayer` | 리지드바디 |
| Collider | `col` | `colPlayer` | 콜라이더 |

### UI 오브젝트 네이밍

| UI 요소 | 접두사 | 예시 | 설명 |
|---------|--------|------|------|
| Panel | `Panel` | `PanelChat`, `PanelMain` | 패널 |
| Button | `Btn` | `BtnSend`, `BtnClose` | 버튼 |
| Text | `Txt` | `TxtMessage`, `TxtTitle` | 텍스트 |
| Image | `Img` | `ImgAvatar`, `ImgBackground` | 이미지 |
| InputField | `Input` | `InputMessage`, `InputName` | 입력 필드 |
| ScrollView | `Scroll` | `ScrollChat`, `ScrollList` | 스크롤 뷰 |
| Toggle | `Toggle` | `ToggleSound`, `ToggleMusic` | 토글 |
| Slider | `Slider` | `SliderVolume`, `SliderProgress` | 슬라이더 |
| Dropdown | `Dropdown` | `DropdownLanguage` | 드롭다운 |

---

## 프로젝트별 특화 규칙

### Domain 클래스 네이밍
```
Domain/Character/
├── Script/
│   ├── CharacterController.cs      # 캐릭터 제어
│   ├── CharacterAnimation.cs       # 애니메이션 관리
│   └── CharacterState.cs          # 상태 관리
├── View/
│   ├── CharacterView.cs           # 뷰 로직
│   └── CharacterUI.cs             # UI 관련
└── Model/
    └── CharacterData.cs           # 데이터 모델
```

### Infrastructure 클래스 네이밍
```
Infrastructure/Network/
├── Services/
│   ├── ChatApiService.cs          # API 서비스
│   └── WebSocketService.cs        # 웹소켓 서비스
├── DTOs/
│   ├── ChatRequest.cs             # 요청 DTO
│   └── ChatResponse.cs            # 응답 DTO
└── Handlers/
    └── MessageHandler.cs          # 메시지 처리
```

### Core 클래스 네이밍
```
Core/
├── Audio/
│   ├── AudioManager.cs            # 오디오 매니저
│   └── VoicePlayer.cs             # 음성 재생
├── Input/
│   ├── InputManager.cs            # 입력 매니저
│   └── TouchHandler.cs            # 터치 처리
└── Utils/
    ├── JsonHelper.cs              # JSON 유틸
    └── TimeHelper.cs              # 시간 유틸
```

---

## 코딩 스타일 가이드

### 메서드 구조
```csharp
public class ChatManager : MonoBehaviour
{
    // 1. SerializeField (Inspector 노출)
    [SerializeField] private ChatUI _chatUI;
    [SerializeField] private AudioManager _audioManager;
    
    // 2. private 필드
    private string _sessionId;
    private bool _isInitialized;
    
    // 3. public 프로퍼티
    public bool IsConnected { get; private set; }
    
    // 4. Unity 생명주기 메서드
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        InitializeChat();
    }
    
    // 5. public 메서드
    public void SendMessage(string message)
    {
        ValidateInput(message);
        ProcessMessage(message);
    }
    
    // 6. private 메서드
    private void InitializeComponents()
    {
        // 구현
    }
    
    // 7. 이벤트 핸들러
    private void OnMessageReceived(ChatResponse response)
    {
        // 구현
    }
}
```

### 네임스페이스 규칙
```csharp
namespace ProjectVG.Domain.Chat
{
    public class ChatManager { }
}

namespace ProjectVG.Infrastructure.Network
{
    public class ChatApiService { }
}

namespace ProjectVG.Core.Audio
{
    public class AudioManager { }
}
``` 