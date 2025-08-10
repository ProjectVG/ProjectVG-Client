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
     * Unity 6 모바일 플랫폼용 WebSocket 구현체 (.NET 전용)
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

        public MobileWebSocket()
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async UniTask<bool> ConnectAsync(string url, CancellationToken cancellationToken = default)
        {
            if (IsConnected || IsConnecting)
            {
                return IsConnected;
            }

            IsConnecting = true;

            try
            {
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                var wsUrl = url
                    .Replace("http://", "ws://")
                    .Replace("https://", "wss://");
                
                Debug.Log($"[MobileWebSocket] 연결 시도: {wsUrl} (플랫폼: {Application.platform})");

                await _webSocket.ConnectAsync(new Uri(wsUrl), combinedCancellationToken);
                
                IsConnected = true;
                IsConnecting = false;
                OnConnected?.Invoke();
                
                _ = ReceiveLoopAsync();
                
                return true;
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
                IsConnected = false;
                IsConnecting = false;
                
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                }
                
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 연결 해제 중 오류: {ex.Message}");
            }
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("[MobileWebSocket] 연결되지 않았습니다.");
                return false;
            }

            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileWebSocket] 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

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
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        OnMessageReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"[MobileWebSocket] 수신 루프 오류: {ex.Message}");
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
            _webSocket?.Dispose();
        }
    }
} 