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
    /// 실제 WebSocket 연결을 수행하는 구현체
    /// </summary>
    public class RealWebSocket : INativeWebSocket
    {
        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<string> OnMessageReceived;
        public event Action<byte[]> OnBinaryDataReceived;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed = false;

        public RealWebSocket()
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
                Debug.Log($"실제 WebSocket 연결 시도: {url}");
                
                // HTTP URL을 WebSocket URL로 변환 (HTTPS 우선 사용)
                var wsUrl = url.Replace("http://", "wss://").Replace("https://", "wss://");
                Debug.Log($"변환된 WebSocket URL: {wsUrl}");

                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                await _webSocket.ConnectAsync(new Uri(wsUrl), combinedCancellationToken);
                
                IsConnected = true;
                IsConnecting = false;
                
                Debug.Log("실제 WebSocket 연결 성공");
                OnConnected?.Invoke();
                
                // 메시지 수신 루프 시작
                _ = ReceiveLoopAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                IsConnecting = false;
                var error = $"WebSocket 연결 실패: {ex.Message}";
                Debug.LogError(error);
                OnError?.Invoke(error);
                return false;
            }
        }

        public async UniTask DisconnectAsync()
        {
            if (!IsConnected)
            {
                Debug.Log("WebSocket이 이미 연결 해제됨");
                return;
            }

            try
            {
                Debug.Log($"WebSocket 연결 해제 시작 - 상태: {_webSocket.State}");
                IsConnected = false;
                IsConnecting = false;
                
                if (_webSocket.State == WebSocketState.Open)
                {
                    Debug.Log("WebSocket 정상 종료 시도");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                }
                else
                {
                    Debug.Log($"WebSocket 상태가 Open이 아님: {_webSocket.State}");
                }
                
                Debug.Log("실제 WebSocket 연결 해제 완료");
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 연결 해제 중 오류: {ex.Message}");
            }
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("WebSocket이 연결되지 않았습니다.");
                return false;
            }

            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
                Debug.Log($"WebSocket 메시지 전송: {message}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> SendBinaryAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("WebSocket이 연결되지 않았습니다.");
                return false;
            }

            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, cancellationToken);
                Debug.Log($"WebSocket 바이너리 전송: {data.Length} bytes");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 바이너리 전송 실패: {ex.Message}");
                return false;
            }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[4096];
            
            try
            {
                Debug.Log("WebSocket 수신 루프 시작");
                while (IsConnected && _webSocket.State == WebSocketState.Open)
                {
                    Debug.Log($"WebSocket 상태: {_webSocket.State}, 메시지 대기 중...");
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    
                    Debug.Log($"WebSocket 메시지 수신: 타입={result.MessageType}, 크기={result.Count}, 종료={result.EndOfMessage}");
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("서버에서 연결 종료 요청");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.Log($"WebSocket 텍스트 메시지 수신: {message}");
                        Debug.Log($"메시지 길이: {message.Length}, 내용: '{message}'");
                        OnMessageReceived?.Invoke(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        var data = new byte[result.Count];
                        Array.Copy(buffer, data, result.Count);
                        Debug.Log($"WebSocket 바이너리 메시지 수신: {result.Count} bytes");
                        OnBinaryDataReceived?.Invoke(data);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"WebSocket 수신 루프 오류: {ex.Message}");
                    Debug.LogError($"스택 트레이스: {ex.StackTrace}");
                    OnError?.Invoke(ex.Message);
                }
            }
            finally
            {
                Debug.Log("WebSocket 수신 루프 종료");
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