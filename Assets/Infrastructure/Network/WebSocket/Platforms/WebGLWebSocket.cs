using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using NativeWebSocket;

namespace ProjectVG.Infrastructure.Network.WebSocket.Platforms
{
    /// <summary>
    /// WebGL 플랫폼용 WebSocket 구현체
    /// NativeWebSocket 패키지를 사용합니다.
    /// </summary>
    public class WebGLWebSocket : INativeWebSocket
    {
        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        public bool IsConnecting => _webSocket?.State == WebSocketState.Connecting;
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<string> OnMessageReceived;

        private NativeWebSocket.WebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed = false;

        public WebGLWebSocket()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async UniTask<bool> ConnectAsync(string url, CancellationToken cancellationToken = default)
        {
            if (IsConnected || IsConnecting)
            {
                return IsConnected;
            }

            if (_isDisposed)
            {
                Debug.LogError("[WebGL WebSocket] Cannot connect: WebSocket is disposed");
                return false;
            }

            try
            {
                var wsUrl = url.Replace("http://", "ws://").Replace("https://", "wss://");
                Debug.Log($"[WebGL WebSocket] 연결 시도: {wsUrl}");

                _webSocket = new NativeWebSocket.WebSocket(wsUrl);

                _webSocket.OnOpen += OnNativeConnected;
                _webSocket.OnMessage += OnNativeMessageReceived;
                _webSocket.OnError += OnNativeError;
                _webSocket.OnClose += OnNativeDisconnected;

                await _webSocket.Connect();

                Debug.Log("[WebGL WebSocket] 연결 성공");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[WebGL WebSocket] 연결이 취소되었습니다.");
                return false;
            }
            catch (Exception ex)
            {
                var error = $"WebGL WebSocket 연결 중 예외 발생: {ex.Message}";
                Debug.LogError(error);
                OnError?.Invoke(error);
                return false;
            }
        }

        public async UniTask DisconnectAsync()
        {
            if (!IsConnected && !IsConnecting)
            {
                return;
            }

            try
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.Close();
                }
                
                Debug.Log("[WebGL WebSocket] 연결 해제됨");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGL WebSocket] 연결 해제 중 오류: {ex.Message}");
            }
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WebGL WebSocket] 연결되지 않은 상태에서 메시지 전송 시도");
                return false;
            }

            if (_isDisposed)
            {
                Debug.LogError("[WebGL WebSocket] WebSocket이 해제된 상태입니다.");
                return false;
            }

            try
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.SendText(message);
                    Debug.Log($"[WebGL WebSocket] 메시지 전송 성공: {message.Substring(0, Math.Min(message.Length, 50))}...");
                    return true;
                }
                else
                {
                    Debug.LogWarning("[WebGL WebSocket] WebSocket이 연결되지 않았습니다.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGL WebSocket] 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        private void OnNativeConnected()
        {
            if (_isDisposed) return;

            Debug.Log($"[WebGL WebSocket] 연결 성공! Platform: {Application.platform}, IsWebGL: {Application.platform == RuntimePlatform.WebGLPlayer}");
            OnConnected?.Invoke();
        }

        private void OnNativeMessageReceived(byte[] data)
        {
            if (_isDisposed) return;

            try
            {
                var message = System.Text.Encoding.UTF8.GetString(data);
                Debug.Log($"[WebGL WebSocket] 메시지 수신! Platform: {Application.platform}");
                Debug.Log($"[WebGL WebSocket] 데이터 크기: {data.Length} bytes");
                Debug.Log($"[WebGL WebSocket] 메시지 내용: {message.Substring(0, Math.Min(message.Length, 200))}...");
                OnMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGL WebSocket] 메시지 처리 중 오류: {ex.Message}");
            }
        }

        private void OnNativeError(string error)
        {
            if (_isDisposed) return;

            Debug.LogError($"[WebGL WebSocket] 오류 발생: {error}");
            OnError?.Invoke(error);
        }

        private void OnNativeDisconnected(WebSocketCloseCode closeCode)
        {
            if (_isDisposed) return;

            Debug.Log($"[WebGL WebSocket] 연결 해제됨 - Code: {closeCode}");
            OnDisconnected?.Invoke();
        }

        public void DispatchMessageQueue()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _webSocket?.DispatchMessageQueue();
            Debug.Log($"[WebGL WebSocket] DispatchMessageQueue 호출됨 - Platform: {Application.platform}");
#else
            Debug.Log($"[WebGL WebSocket] WebGL에서는 DispatchMessageQueue 생략 - Platform: {Application.platform}");
#endif
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _isDisposed = true;

            if (_webSocket != null)
            {
                _webSocket.OnOpen -= OnNativeConnected;
                _webSocket.OnMessage -= OnNativeMessageReceived;
                _webSocket.OnError -= OnNativeError;
                _webSocket.OnClose -= OnNativeDisconnected;

                if (_webSocket.State == WebSocketState.Open)
                {
                    _webSocket.Close();
                }
                _webSocket = null;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
} 