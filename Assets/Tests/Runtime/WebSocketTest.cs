using UnityEngine;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.WebSocket.Platforms;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Tests
{
    /**
     * WebSocket 기능 테스트 스크립트
     * 
     * 모바일과 데스크톱 WebSocket 구현을 테스트합니다.
     */
    public class WebSocketTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private string testWebSocketUrl = "wss://echo.websocket.org";
        [SerializeField] private bool autoConnectOnStart = false;
        [SerializeField] private bool enableDebugLogs = true;
        
        private INativeWebSocket _webSocket;
        private bool _isConnected = false;
        
        private void Start()
        {
            if (autoConnectOnStart)
            {
                TestWebSocketConnection().Forget();
            }
        }
        
        private void OnDestroy()
        {
            DisconnectWebSocket().Forget();
        }
        
        /**
         * WebSocket 연결 테스트
         */
        public async UniTaskVoid TestWebSocketConnection()
        {
            try
            {
                LogDebug("WebSocket 연결 테스트 시작");
                
                // WebSocket 생성
                _webSocket = WebSocketFactory.Create();
                LogDebug($"생성된 WebSocket 타입: {_webSocket.GetType().Name}");
                
                // 이벤트 구독
                _webSocket.OnConnected += OnWebSocketConnected;
                _webSocket.OnDisconnected += OnWebSocketDisconnected;
                _webSocket.OnError += OnWebSocketError;
                _webSocket.OnMessageReceived += OnWebSocketMessageReceived;
                
                // 연결 시도
                LogDebug($"WebSocket 연결 시도: {testWebSocketUrl}");
                bool success = await _webSocket.ConnectAsync(testWebSocketUrl);
                
                if (success)
                {
                    LogDebug("WebSocket 연결 성공");
                    _isConnected = true;
                    
                    // 테스트 메시지 전송
                    await UniTask.Delay(1000);
                    await TestMessageSending();
                }
                else
                {
                    LogDebug("WebSocket 연결 실패");
                }
            }
            catch (System.Exception ex)
            {
                LogDebug($"WebSocket 테스트 중 오류: {ex.Message}");
            }
        }
        
        /**
         * 메시지 전송 테스트
         */
        private async UniTask TestMessageSending()
        {
            if (!_isConnected || _webSocket == null)
            {
                LogDebug("WebSocket이 연결되지 않았습니다.");
                return;
            }
            
            try
            {
                string testMessage = "{\"type\":\"test\",\"data\":\"Hello WebSocket!\"}";
                LogDebug($"테스트 메시지 전송: {testMessage}");
                
                bool success = await _webSocket.SendMessageAsync(testMessage);
                
                if (success)
                {
                    LogDebug("메시지 전송 성공");
                }
                else
                {
                    LogDebug("메시지 전송 실패");
                }
            }
            catch (System.Exception ex)
            {
                LogDebug($"메시지 전송 중 오류: {ex.Message}");
            }
        }
        
        /**
         * WebSocket 연결 해제
         */
        public async UniTaskVoid DisconnectWebSocket()
        {
            if (_webSocket != null && _isConnected)
            {
                try
                {
                    LogDebug("WebSocket 연결 해제");
                    await _webSocket.DisconnectAsync();
                    _isConnected = false;
                }
                catch (System.Exception ex)
                {
                    LogDebug($"WebSocket 연결 해제 중 오류: {ex.Message}");
                }
            }
        }
        
        // ===== 이벤트 핸들러 =====
        
        private void OnWebSocketConnected()
        {
            LogDebug("WebSocket 연결됨");
            _isConnected = true;
        }
        
        private void OnWebSocketDisconnected()
        {
            LogDebug("WebSocket 연결 해제됨");
            _isConnected = false;
        }
        
        private void OnWebSocketError(string error)
        {
            LogDebug($"WebSocket 오류: {error}");
        }
        
        private void OnWebSocketMessageReceived(string message)
        {
            LogDebug($"WebSocket 메시지 수신: {message.Length} bytes");
            LogDebug($"메시지 내용: {message}");
        }
        
        // ===== 유틸리티 =====
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[WebSocketTest] {message}");
            }
        }
        
        // ===== UI 버튼용 메서드 =====
        
        [ContextMenu("WebSocket 연결 테스트")]
        public void TestConnection()
        {
            TestWebSocketConnection().Forget();
        }
        
        [ContextMenu("WebSocket 연결 해제")]
        public void TestDisconnection()
        {
            DisconnectWebSocket().Forget();
        }
        
        [ContextMenu("테스트 메시지 전송")]
        public void TestSendMessage()
        {
            TestMessageSending().Forget();
        }
    }
} 