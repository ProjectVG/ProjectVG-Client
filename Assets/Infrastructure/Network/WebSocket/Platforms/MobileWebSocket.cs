using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Configs;

namespace ProjectVG.Infrastructure.Network.WebSocket.Platforms
{
    /**
     * Unity 6 모바일 플랫폼용 WebSocket 구현체
     * 
     * Unity 6의 .NET Standard 2.1 WebSocket을 우선 사용하고,
     * 네이티브 플러그인을 폴백으로 사용합니다.
     */
    public class MobileWebSocket : INativeWebSocket
    {
        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<string> OnMessageReceived;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed = false;
        private bool _useNativePlugin = false;
        private int _nativeWebSocketId = -1;
        private string _currentUrl;

        public MobileWebSocket()
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Unity 6에서 네이티브 플러그인 사용 여부 결정
            _useNativePlugin = ShouldUseNativePlugin();
        }

        /**
         * 네이티브 플러그인 사용 여부 결정
         */
        private bool ShouldUseNativePlugin()
        {
            // Unity 6에서는 기본적으로 .NET WebSocket 사용
            // 특별한 요구사항이 있을 때만 네이티브 플러그인 사용
            return false;
        }

        public async UniTask<bool> ConnectAsync(string url, CancellationToken cancellationToken = default)
        {
            if (IsConnected || IsConnecting)
            {
                return IsConnected;
            }

            IsConnecting = true;
            _currentUrl = url;

            try
            {
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                Debug.Log($"[MobileWebSocket] 연결 시도: {url} (플랫폼: {Application.platform})");
                
                bool success;
                if (_useNativePlugin)
                {
                    success = await ConnectWithNativePluginAsync(url, combinedCancellationToken);
                }
                else
                {
                    success = await ConnectWithUnityWebSocketAsync(url, combinedCancellationToken);
                }
                
                if (success)
                {
                    IsConnected = true;
                    IsConnecting = false;
                    Debug.Log("[MobileWebSocket] 연결 성공");
                    OnConnected?.Invoke();
                    return true;
                }
                else
                {
                    IsConnecting = false;
                    Debug.LogError("[MobileWebSocket] 연결 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                IsConnecting = false;
                var error = $"모바일 WebSocket 연결 중 예외 발생: {ex.Message}";
                Debug.LogError($"[MobileWebSocket] {error}");
                OnError?.Invoke(error);
                return false;
            }
        }

        /**
         * Unity 6 .NET WebSocket을 사용한 연결
         */
        private async UniTask<bool> ConnectWithUnityWebSocketAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                var wsUrl = url.Replace("http://", "wss://").Replace("https://", "wss://");
                Debug.Log($"[MobileWebSocket] Unity WebSocket 연결: {wsUrl}");

                await _webSocket.ConnectAsync(new Uri(wsUrl), cancellationToken);
                
                // 메시지 수신 루프 시작
                _ = ReceiveLoopAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] Unity WebSocket 연결 실패: {ex.Message}");
                return false;
            }
        }

        /**
         * 네이티브 플러그인을 사용한 연결 (폴백)
         */
        private async UniTask<bool> ConnectWithNativePluginAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    _nativeWebSocketId = AndroidWebSocket_Connect(url);
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    _nativeWebSocketId = IOSWebSocket_Connect(url);
                }
                else
                {
                    Debug.LogWarning($"[MobileWebSocket] 지원되지 않는 플랫폼: {Application.platform}");
                    return false;
                }
                
                if (_nativeWebSocketId >= 0)
                {
                    Debug.Log($"[MobileWebSocket] 네이티브 WebSocket 연결 성공 (ID: {_nativeWebSocketId})");
                    
                    // 네이티브 메시지 수신 모니터링 시작
                    _ = MonitorNativeWebSocketAsync();
                    
                    return true;
                }
                else
                {
                    Debug.LogError("[MobileWebSocket] 네이티브 WebSocket 연결 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 네이티브 연결 실패: {ex.Message}");
                return false;
            }
        }

        public async UniTask DisconnectAsync()
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                Debug.Log("[MobileWebSocket] 연결 해제 중...");
                
                IsConnected = false;
                IsConnecting = false;
                
                if (_useNativePlugin)
                {
                    await DisconnectNativeWebSocketAsync();
                }
                else
                {
                    await DisconnectUnityWebSocketAsync();
                }
                
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 연결 해제 중 오류: {ex.Message}");
            }
        }

        /**
         * Unity WebSocket 연결 해제
         */
        private async UniTask DisconnectUnityWebSocketAsync()
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] Unity WebSocket 연결 해제 오류: {ex.Message}");
            }
        }

        /**
         * 네이티브 WebSocket 연결 해제
         */
        private async UniTask DisconnectNativeWebSocketAsync()
        {
            try
            {
                if (_nativeWebSocketId >= 0)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        AndroidWebSocket_Disconnect(_nativeWebSocketId);
                    }
                    else if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        IOSWebSocket_Disconnect(_nativeWebSocketId);
                    }
                    
                    _nativeWebSocketId = -1;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 네이티브 연결 해제 오류: {ex.Message}");
            }
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[MobileWebSocket] 연결되지 않았습니다.");
                return false;
            }

            try
            {
                Debug.Log($"[MobileWebSocket] 메시지 전송: {message.Length} bytes");
                
                bool success;
                if (_useNativePlugin)
                {
                    success = await SendNativeMessageAsync(message, cancellationToken);
                }
                else
                {
                    success = await SendUnityMessageAsync(message, cancellationToken);
                }
                
                if (success)
                {
                    Debug.Log("[MobileWebSocket] 메시지 전송 성공");
                }
                else
                {
                    Debug.LogError("[MobileWebSocket] 메시지 전송 실패");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        /**
         * Unity WebSocket 메시지 전송
         */
        private async UniTask<bool> SendUnityMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    Debug.LogWarning("[MobileWebSocket] Unity WebSocket이 연결되지 않았습니다.");
                    return false;
                }

                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] Unity WebSocket 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        /**
         * 네이티브 메시지 전송
         */
        private async UniTask<bool> SendNativeMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                if (_nativeWebSocketId >= 0)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        return AndroidWebSocket_SendMessage(_nativeWebSocketId, message);
                    }
                    else if (Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        return IOSWebSocket_SendMessage(_nativeWebSocketId, message);
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 네이티브 메시지 전송 오류: {ex.Message}");
                return false;
            }
        }

        /**
         * Unity WebSocket 메시지 수신 루프
         */
        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[NetworkConfig.ReceiveBufferSize];
            
            try
            {
                while (IsConnected && _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("[MobileWebSocket] Unity WebSocket: 서버에서 연결 종료 요청");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        OnMessageReceived?.Invoke(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        Debug.LogWarning("[MobileWebSocket] Unity WebSocket: 바이너리 메시지 수신됨 (무시됨)");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"[MobileWebSocket] Unity WebSocket 수신 루프 오류: {ex.Message}");
                    OnError?.Invoke(ex.Message);
                }
            }
            finally
            {
                IsConnected = false;
                if (!_isDisposed)
                {
                    OnDisconnected?.Invoke();
                }
            }
        }

        /**
         * 네이티브 WebSocket 모니터링
         */
        private async UniTask MonitorNativeWebSocketAsync()
        {
            try
            {
                while (IsConnected && !_isDisposed)
                {
                    if (_nativeWebSocketId >= 0)
                    {
                        string receivedMessage = null;
                        
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            receivedMessage = AndroidWebSocket_ReceiveMessage(_nativeWebSocketId);
                        }
                        else if (Application.platform == RuntimePlatform.IPhonePlayer)
                        {
                            receivedMessage = IOSWebSocket_ReceiveMessage(_nativeWebSocketId);
                        }
                        
                        if (!string.IsNullOrEmpty(receivedMessage))
                        {
                            Debug.Log($"[MobileWebSocket] 네이티브 메시지 수신: {receivedMessage.Length} bytes");
                            OnMessageReceived?.Invoke(receivedMessage);
                        }
                    }
                    
                    await UniTask.Delay(50);
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"[MobileWebSocket] 네이티브 모니터링 오류: {ex.Message}");
                    OnError?.Invoke(ex.Message);
                }
            }
            finally
            {
                IsConnected = false;
                if (!_isDisposed)
                {
                    OnDisconnected?.Invoke();
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _isDisposed = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            if (_useNativePlugin)
            {
                DisconnectNativeWebSocketAsync().Forget();
            }
            else
            {
                _webSocket?.Dispose();
            }
        }

        // ===== 네이티브 플러그인 인터페이스 (폴백용) =====

        #if UNITY_ANDROID && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int AndroidWebSocket_Connect(string url);
        
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void AndroidWebSocket_Disconnect(int webSocketId);
        
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern bool AndroidWebSocket_SendMessage(int webSocketId, string message);
        
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string AndroidWebSocket_ReceiveMessage(int webSocketId);
        #else
        private static int AndroidWebSocket_Connect(string url) => -1;
        private static void AndroidWebSocket_Disconnect(int webSocketId) { }
        private static bool AndroidWebSocket_SendMessage(int webSocketId, string message) => false;
        private static string AndroidWebSocket_ReceiveMessage(int webSocketId) => null;
        #endif

        #if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int IOSWebSocket_Connect(string url);
        
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void IOSWebSocket_Disconnect(int webSocketId);
        
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern bool IOSWebSocket_SendMessage(int webSocketId, string message);
        
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string IOSWebSocket_ReceiveMessage(int webSocketId);
        #else
        private static int IOSWebSocket_Connect(string url) => -1;
        private static void IOSWebSocket_Disconnect(int webSocketId) { }
        private static bool IOSWebSocket_SendMessage(int webSocketId, string message) => false;
        private static string IOSWebSocket_ReceiveMessage(int webSocketId) => null;
        #endif
    }
} 