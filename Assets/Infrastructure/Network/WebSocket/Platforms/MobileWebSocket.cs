using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ProjectVG.Infrastructure.Network.WebSocket.Platforms
{
    /**
     * 모바일 플랫폼용 WebSocket 구현체
     * 
     * iOS/Android 네이티브 WebSocket 라이브러리를 사용합니다.
     * 네이티브 플러그인을 통해 각 플랫폼의 최적화된 WebSocket 구현을 호출합니다.
     */
    public class MobileWebSocket : INativeWebSocket
    {
        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<string> OnMessageReceived;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed = false;
        private string _currentUrl;
        private int _nativeWebSocketId = -1;

        public MobileWebSocket()
        {
            _cancellationTokenSource = new CancellationTokenSource();
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
                
                Debug.Log($"[MobileWebSocket] 연결 시도: {url}");
                
                // 네이티브 WebSocket 연결
                bool success = await ConnectNativeWebSocketAsync(url, combinedCancellationToken);
                
                if (success)
                {
                    IsConnected = true;
                    IsConnecting = false;
                    Debug.Log("[MobileWebSocket] 연결 성공");
                    OnConnected?.Invoke();
                    
                    // 메시지 수신 모니터링 시작
                    _ = MonitorNativeWebSocketAsync();
                    
                    return true;
                }
                else
                {
                    IsConnecting = false;
                    Debug.LogError("[MobileWebSocket] 네이티브 연결 실패");
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
                
                // 네이티브 WebSocket 연결 해제
                await DisconnectNativeWebSocketAsync();
                
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 연결 해제 중 오류: {ex.Message}");
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
                
                // 네이티브 WebSocket 메시지 전송
                bool success = await SendNativeMessageAsync(message, cancellationToken);
                
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
         * 네이티브 WebSocket 연결
         */
        private async UniTask<bool> ConnectNativeWebSocketAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    return await ConnectAndroidWebSocketAsync(url, cancellationToken);
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    return await ConnectIOSWebSocketAsync(url, cancellationToken);
                }
                else if (Application.isEditor)
                {
                    // Unity 에디터에서는 DesktopWebSocket과 동일한 방식으로 동작
                    Debug.Log($"[MobileWebSocket] 에디터에서 테스트 모드로 동작");
                    return await ConnectEditorWebSocketAsync(url, cancellationToken);
                }
                else
                {
                    Debug.LogWarning($"[MobileWebSocket] 지원되지 않는 플랫폼입니다: {Application.platform}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 네이티브 연결 실패: {ex.Message}");
                return false;
            }
        }

        /**
         * Android WebSocket 연결
         */
        private async UniTask<bool> ConnectAndroidWebSocketAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                // Android 네이티브 플러그인 호출
                _nativeWebSocketId = AndroidWebSocket_Connect(url);
                
                if (_nativeWebSocketId >= 0)
                {
                    Debug.Log($"[MobileWebSocket] Android WebSocket 연결 성공 (ID: {_nativeWebSocketId})");
                    return true;
                }
                else
                {
                    Debug.LogError("[MobileWebSocket] Android WebSocket 연결 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] Android 연결 오류: {ex.Message}");
                return false;
            }
        }

        /**
         * iOS WebSocket 연결
         */
        private async UniTask<bool> ConnectIOSWebSocketAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                // iOS 네이티브 플러그인 호출
                _nativeWebSocketId = IOSWebSocket_Connect(url);
                
                if (_nativeWebSocketId >= 0)
                {
                    Debug.Log($"[MobileWebSocket] iOS WebSocket 연결 성공 (ID: {_nativeWebSocketId})");
                    return true;
                }
                else
                {
                    Debug.LogError("[MobileWebSocket] iOS WebSocket 연결 실패");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] iOS 연결 오류: {ex.Message}");
                return false;
            }
        }

        /**
         * 에디터용 WebSocket 연결 (테스트용)
         */
        private async UniTask<bool> ConnectEditorWebSocketAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                // 에디터에서는 성공 시뮬레이션
                await UniTask.Delay(100, cancellationToken: cancellationToken);
                
                _nativeWebSocketId = 1; // 에디터용 ID
                Debug.Log($"[MobileWebSocket] 에디터 WebSocket 연결 성공 (ID: {_nativeWebSocketId})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 에디터 연결 오류: {ex.Message}");
                return false;
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
                    else if (Application.isEditor)
                    {
                        Debug.Log($"[MobileWebSocket] 에디터 WebSocket 연결 해제 (ID: {_nativeWebSocketId})");
                    }
                    
                    _nativeWebSocketId = -1;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 네이티브 연결 해제 오류: {ex.Message}");
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
                    else if (Application.isEditor)
                    {
                        // 에디터에서는 성공 시뮬레이션
                        Debug.Log($"[MobileWebSocket] 에디터 메시지 전송: {message.Length} bytes");
                        return true;
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
         * 네이티브 WebSocket 모니터링
         */
        private async UniTask MonitorNativeWebSocketAsync()
        {
            try
            {
                while (IsConnected && !_isDisposed)
                {
                    // 네이티브에서 메시지 수신 확인
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
                        else if (Application.isEditor)
                        {
                            // 에디터에서는 테스트 메시지 시뮬레이션
                            if (UnityEngine.Random.Range(0, 100) < 5) // 5% 확률로 메시지 수신
                            {
                                receivedMessage = $"{{\"type\":\"test\",\"data\":\"Editor test message at {DateTime.Now:HH:mm:ss}\"}}";
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(receivedMessage))
                        {
                            Debug.Log($"[MobileWebSocket] 메시지 수신: {receivedMessage.Length} bytes");
                            OnMessageReceived?.Invoke(receivedMessage);
                        }
                    }
                    
                    await UniTask.Delay(50); // 50ms 간격으로 체크
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"[MobileWebSocket] 모니터링 오류: {ex.Message}");
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
            
            // 네이티브 WebSocket 정리
            if (_nativeWebSocketId >= 0)
            {
                DisconnectNativeWebSocketAsync().Forget();
            }
        }

        // ===== 네이티브 플러그인 인터페이스 =====

        #if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int AndroidWebSocket_Connect(string url);
        
        [DllImport("__Internal")]
        private static extern void AndroidWebSocket_Disconnect(int webSocketId);
        
        [DllImport("__Internal")]
        private static extern bool AndroidWebSocket_SendMessage(int webSocketId, string message);
        
        [DllImport("__Internal")]
        private static extern string AndroidWebSocket_ReceiveMessage(int webSocketId);
        #else
        private static int AndroidWebSocket_Connect(string url) => -1;
        private static void AndroidWebSocket_Disconnect(int webSocketId) { }
        private static bool AndroidWebSocket_SendMessage(int webSocketId, string message) => false;
        private static string AndroidWebSocket_ReceiveMessage(int webSocketId) => null;
        #endif

        #if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int IOSWebSocket_Connect(string url);
        
        [DllImport("__Internal")]
        private static extern void IOSWebSocket_Disconnect(int webSocketId);
        
        [DllImport("__Internal")]
        private static extern bool IOSWebSocket_SendMessage(int webSocketId, string message);
        
        [DllImport("__Internal")]
        private static extern string IOSWebSocket_ReceiveMessage(int webSocketId);
        #else
        private static int IOSWebSocket_Connect(string url) => -1;
        private static void IOSWebSocket_Disconnect(int webSocketId) { }
        private static bool IOSWebSocket_SendMessage(int webSocketId, string message) => false;
        private static string IOSWebSocket_ReceiveMessage(int webSocketId) => null;
        #endif
    }
} 