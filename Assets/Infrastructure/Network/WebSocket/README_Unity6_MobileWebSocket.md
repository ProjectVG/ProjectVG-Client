# Unity 6 모바일 WebSocket 구현 가이드

## 개요

Unity 6에서 모바일 플랫폼(iOS/Android)에서 WebSocket을 사용하기 위한 구현 가이드입니다.

## Unity 6의 WebSocket 지원 현황

### .NET Standard 2.1 지원
- Unity 6는 **.NET Standard 2.1**을 지원하므로 `System.Net.WebSockets.ClientWebSocket`을 사용할 수 있습니다.
- 모바일 플랫폼에서도 기본적으로 WebSocket이 지원됩니다.

### 플랫폼별 제약사항
- **iOS**: AOT 컴파일 제약으로 인해 일부 고급 기능이 제한될 수 있음
- **Android**: .NET Standard 2.1 WebSocket이 완전히 지원됨
- **성능**: 네이티브 구현보다 성능이 떨어질 수 있음

## 구현 방식

### 1. Unity 6 .NET WebSocket 우선 사용
```csharp
// MobileWebSocket.cs에서 기본적으로 .NET WebSocket 사용
private bool ShouldUseNativePlugin()
{
    // Unity 6에서는 기본적으로 .NET WebSocket 사용
    return false;
}
```

### 2. 네이티브 플러그인 폴백
특별한 요구사항이 있을 때만 네이티브 플러그인을 사용합니다.

## 사용 방법

### 1. WebSocket 매니저 생성
```csharp
// 자동으로 플랫폼에 맞는 WebSocket 구현체 생성
var webSocketManager = gameObject.AddComponent<WebSocketManager>();
```

### 2. 연결 설정
```csharp
// 이벤트 구독
webSocketManager.OnConnected += OnConnected;
webSocketManager.OnDisconnected += OnDisconnected;
webSocketManager.OnError += OnError;
webSocketManager.OnMessageReceived += OnMessageReceived;

// 연결
bool success = await webSocketManager.ConnectAsync();
```

### 3. 메시지 전송/수신
```csharp
// 메시지 전송
bool sent = await webSocketManager.SendMessageAsync("Hello, WebSocket!");

// 메시지 수신 (이벤트로 처리)
private void OnMessageReceived(string message)
{
    Debug.Log($"수신된 메시지: {message}");
}
```

### 4. 연결 해제
```csharp
await webSocketManager.DisconnectAsync();
```

## 설정

### NetworkConfig 설정
```csharp
// WebSocket 관련 설정
[Header("WebSocket Settings")]
[SerializeField] private float wsTimeout = 30f;
[SerializeField] private float reconnectDelay = 5f;
[SerializeField] private int maxReconnectAttempts = 3;
[SerializeField] private bool autoReconnect = true;
[SerializeField] private float heartbeatInterval = 30f;
[SerializeField] private bool enableHeartbeat = true;
```

### 플랫폼별 설정
```csharp
// WebSocketFactory에서 플랫폼별 구현체 선택
public static INativeWebSocket CreateWebSocket()
{
    switch (Application.platform)
    {
        case RuntimePlatform.Android:
        case RuntimePlatform.IPhonePlayer:
            return new MobileWebSocket();
        default:
            return new DesktopWebSocket();
    }
}
```

## 테스트

### WebSocketTest 클래스 사용
```csharp
// 테스트 컴포넌트 추가
var testComponent = gameObject.AddComponent<WebSocketTest>();

// 자동 연결 테스트
await testComponent.StartConnectionTest();

// 수동 테스트
await testComponent.ConnectTestButton();
await testComponent.DisconnectTestButton();
await testComponent.ReconnectTestButton();
```

## 성능 최적화

### 1. 메시지 버퍼링
```csharp
// 대용량 메시지 처리
private async UniTask SendLargeMessageAsync(string message)
{
    const int maxChunkSize = 1024;
    for (int i = 0; i < message.Length; i += maxChunkSize)
    {
        int chunkSize = Math.Min(maxChunkSize, message.Length - i);
        string chunk = message.Substring(i, chunkSize);
        await webSocketManager.SendMessageAsync(chunk);
    }
}
```

### 2. 연결 풀링
```csharp
// 여러 WebSocket 연결 관리
private Dictionary<string, WebSocketManager> _webSocketPool = new Dictionary<string, WebSocketManager>();

public async UniTask<WebSocketManager> GetWebSocketAsync(string url)
{
    if (!_webSocketPool.ContainsKey(url))
    {
        var wsManager = gameObject.AddComponent<WebSocketManager>();
        await wsManager.ConnectAsync(url);
        _webSocketPool[url] = wsManager;
    }
    return _webSocketPool[url];
}
```

## 오류 처리

### 1. 연결 실패 처리
```csharp
webSocketManager.OnError += (error) =>
{
    Debug.LogError($"WebSocket 오류: {error}");
    
    // 재연결 시도
    if (autoReconnect)
    {
        _ = webSocketManager.ConnectAsync();
    }
};
```

### 2. 네트워크 상태 모니터링
```csharp
private void OnApplicationPause(bool pauseStatus)
{
    if (!pauseStatus)
    {
        // 앱 재개 시 연결 상태 확인
        CheckConnectionStatus();
    }
}
```

## 보안 고려사항

### 1. SSL/TLS 사용
```csharp
// WSS 프로토콜 사용
string secureUrl = url.Replace("ws://", "wss://");
```

### 2. 인증 토큰
```csharp
// WebSocket URL에 인증 토큰 추가
string authenticatedUrl = $"{baseUrl}?token={authToken}";
```

## 디버깅

### 1. 로그 활성화
```csharp
// NetworkConfig에서 로깅 설정
[SerializeField] private bool enableMessageLogging = true;
```

### 2. 상태 모니터링
```csharp
// WebSocket 상태 로그 출력
webSocketManager.LogStatus();
```

## 주의사항

### 1. 메모리 관리
- WebSocket 연결 해제 시 `Dispose()` 호출 필수
- 이벤트 구독 해제로 메모리 누수 방지

### 2. 스레드 안전성
- Unity 메인 스레드에서 WebSocket 이벤트 처리
- 백그라운드 스레드에서의 WebSocket 조작 주의

### 3. 네트워크 상태
- 모바일 네트워크 상태 변화 대응
- 앱 포그라운드/백그라운드 전환 시 연결 관리

## 예제 코드

### 완전한 사용 예제
```csharp
public class WebSocketExample : MonoBehaviour
{
    private WebSocketManager _webSocketManager;
    
    private async void Start()
    {
        // WebSocket 매니저 초기화
        _webSocketManager = gameObject.AddComponent<WebSocketManager>();
        
        // 이벤트 구독
        _webSocketManager.OnConnected += OnConnected;
        _webSocketManager.OnDisconnected += OnDisconnected;
        _webSocketManager.OnError += OnError;
        _webSocketManager.OnMessageReceived += OnMessageReceived;
        
        // 연결
        bool connected = await _webSocketManager.ConnectAsync();
        if (connected)
        {
            Debug.Log("WebSocket 연결 성공");
        }
    }
    
    private void OnConnected()
    {
        Debug.Log("WebSocket 연결됨");
    }
    
    private void OnDisconnected()
    {
        Debug.Log("WebSocket 연결 해제됨");
    }
    
    private void OnError(string error)
    {
        Debug.LogError($"WebSocket 오류: {error}");
    }
    
    private void OnMessageReceived(string message)
    {
        Debug.Log($"메시지 수신: {message}");
    }
    
    private async void SendTestMessage()
    {
        if (_webSocketManager.IsConnected)
        {
            bool sent = await _webSocketManager.SendMessageAsync("Test message");
            Debug.Log($"메시지 전송: {(sent ? "성공" : "실패")}");
        }
    }
    
    private void OnDestroy()
    {
        _webSocketManager?.Dispose();
    }
}
```

## 결론

Unity 6에서는 모바일 플랫폼에서도 .NET Standard 2.1 WebSocket을 안정적으로 사용할 수 있습니다. 네이티브 플러그인은 특별한 요구사항이 있을 때만 폴백으로 사용하는 것이 권장됩니다.

이 구현은 다음과 같은 장점을 제공합니다:
- 플랫폼 독립적인 코드
- Unity 6의 최신 .NET 지원 활용
- 자동 재연결 및 하트비트 기능
- 포괄적인 오류 처리
- 성능 최적화된 메시지 처리
