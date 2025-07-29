using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ProjectVG.Infrastructure.Network.WebSocket.Platforms
{
    /// <summary>
    /// 데스크톱 플랫폼용 WebSocket 구현체
    /// System.Net.WebSockets.ClientWebSocket을 사용합니다.
    /// JSON 메시지만 처리합니다.
    /// </summary>
    public class DesktopWebSocket : INativeWebSocket
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

        public DesktopWebSocket()
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
                var wsUrl = url.Replace("http://", "wss://").Replace("https://", "wss://");
                Debug.Log($"Desktop WebSocket 연결: {wsUrl}");

                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                await _webSocket.ConnectAsync(new Uri(wsUrl), combinedCancellationToken);
                
                IsConnected = true;
                IsConnecting = false;
                OnConnected?.Invoke();
                
                // 메시지 수신 루프 시작
                _ = ReceiveLoopAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                IsConnecting = false;
                var error = $"Desktop WebSocket 연결 실패: {ex.Message}";
                Debug.LogError(error);
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
                Debug.LogError($"Desktop WebSocket 연결 해제 중 오류: {ex.Message}");
            }
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("Desktop WebSocket이 연결되지 않았습니다.");
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
                Debug.LogError($"Desktop WebSocket 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[4096];
            
            try
            {
                while (IsConnected && _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("Desktop WebSocket: 서버에서 연결 종료 요청");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        OnMessageReceived?.Invoke(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // 바이너리 메시지는 무시 (JSON만 처리)
                        Debug.LogWarning("Desktop WebSocket: 바이너리 메시지 수신됨 (무시됨)");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"Desktop WebSocket 수신 루프 오류: {ex.Message}");
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