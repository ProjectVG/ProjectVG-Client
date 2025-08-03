# 채팅 입력 컴포넌트 사용법

## 개요

채팅 입력 기능이 `TextInputView`와 `VoiceInputView`로 분리되어 있습니다.

## TextInputView

텍스트 입력 전용 컴포넌트입니다.

### 설정 방법

1. **GameObject 생성**
   ```
   ChatInputContainer
   ├── TextInputView (TextInputView 컴포넌트 추가)
   ├── InputField (TMP_InputField)
   └── BtnSend (Button)
   ```

2. **코드에서 사용**
   ```csharp
   public class ChatController : MonoBehaviour
   {
       [SerializeField] private TextInputView _textInputView;
       [SerializeField] private VoiceInputView _voiceInputView;
       
       private ChatManager _chatManager;
       
       private void Start()
       {
           // ChatManager 설정
           _textInputView.SetChatManager(_chatManager);
           _voiceInputView.SetChatManager(_chatManager);
           
           // 이벤트 구독
           _textInputView.OnTextMessageSent += OnTextMessageSent;
           _textInputView.OnError += OnError;
       }
       
       private void OnTextMessageSent(string message)
       {
           Debug.Log($"텍스트 메시지 전송: {message}");
       }
       
       private void OnError(string error)
       {
           Debug.LogError($"오류: {error}");
       }
   }
   ```

## VoiceInputView

음성 입력 전용 컴포넌트입니다.

### 설정 방법

1. **GameObject 생성**
   ```
   ChatInputContainer
   ├── VoiceInputView (VoiceInputView 컴포넌트 추가)
   ├── BtnVoice (Button)
   ├── BtnVoiceStop (Button)
   └── TxtVoiceStatus (TextMeshProUGUI)
   ```

2. **코드에서 사용**
   ```csharp
   // VoiceInputView 이벤트 구독
   _voiceInputView.OnVoiceMessageSent += OnVoiceMessageSent;
   _voiceInputView.OnError += OnError;
   
   private void OnVoiceMessageSent(string transcribedText)
   {
       Debug.Log($"음성 메시지 변환: {transcribedText}");
   }
   ```

## 독립적 사용

각 컴포넌트는 독립적으로 사용할 수 있습니다:

### 텍스트만 사용
```csharp
// TextInputView만 사용 (자동 설정)
_textInputView.Initialize();

// 또는 수동 설정
_textInputView.SetChatManager(_chatManager);
```

### 음성만 사용
```csharp
// VoiceInputView만 사용 (자동 설정)
_voiceInputView.Initialize();

// 또는 수동 설정
_voiceInputView.SetChatManager(_chatManager);
```

### 둘 다 사용
```csharp
// 두 컴포넌트 모두 사용 (자동 설정)
_textInputView.Initialize();
_voiceInputView.Initialize();

// 또는 수동 설정
_textInputView.SetChatManager(_chatManager);
_voiceInputView.SetChatManager(_chatManager);
```

## 자동 설정 기능

### ChatManager 자동 검색
- `Initialize()` 호출 시 자동으로 `ChatManager`를 찾아서 설정
- 씬에 `ChatManager`가 있으면 자동으로 연결

### AudioRecorder 자동 생성
- `VoiceInputView`에서 `AudioRecorder`가 없으면 자동으로 컴포넌트 추가
- `AudioRecorder.Instance`가 없으면 현재 GameObject에 추가

## 주요 이벤트

### TextInputView
- `OnTextMessageSent(string message)`: 텍스트 메시지 전송 시
- `OnError(string error)`: 오류 발생 시

### VoiceInputView
- `OnVoiceMessageSent(string transcribedText)`: 음성 변환 완료 시
- `OnError(string error)`: 오류 발생 시

## 주요 메서드

### TextInputView
- `SetChatManager(ChatManager chatManager)`: ChatManager 설정
- `SendTextMessage(string message)`: 텍스트 메시지 전송
- `ClearInput()`: 입력 필드 초기화

### VoiceInputView
- `SetChatManager(ChatManager chatManager)`: ChatManager 설정
- `StartVoiceRecording()`: 음성 녹음 시작
- `StopVoiceRecording()`: 음성 녹음 중지
- `SendVoiceMessage(byte[] audioData)`: 음성 데이터 전송 