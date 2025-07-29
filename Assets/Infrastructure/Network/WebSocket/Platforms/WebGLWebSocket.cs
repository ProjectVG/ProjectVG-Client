using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace ProjectVG.Infrastructure.Network.WebSocket.Platforms
{
    /// <summary>
    /// WebGL 플랫폼용 WebSocket 구현체
    /// UnityWebRequest.WebSocket을 사용합니다.
    /// </summary>
    public class WebGLWebSocket : INativeWebSocket
    {
        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
#pragma warning disable CS0067
        public event Action<string> OnMessageReceived;
        public event Action<byte[]> OnBinaryDataReceived;
#pragma warning restore CS0067

        private UnityWebRequest _webRequest;
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

            IsConnecting = true;

            try
            {
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                // UnityWebRequest.WebSocket 사용
                _webRequest = UnityWebRequest.Get(url);
                _webRequest.SetRequestHeader("Upgrade", "websocket");
                _webRequest.SetRequestHeader("Connection", "Upgrade");
                
                var operation = _webRequest.SendWebRequest();
                await operation.WithCancellation(combinedCancellationToken);

                if (_webRequest.result == UnityWebRequest.Result.Success)
                {
                    IsConnected = true;
                    IsConnecting = false;
                    OnConnected?.Invoke();
                    
                    // 메시지 수신 루프 시작
                    _ = ReceiveLoopAsync();
                    
                    return true;
                }
                else
                {
                    var error = $"WebGL WebSocket 연결 실패: {_webRequest.error}";
                    Debug.LogError(error);
                    OnError?.Invoke(error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                IsConnecting = false;
                var error = $"WebGL WebSocket 연결 중 예외 발생: {ex.Message}";
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
                
                _webRequest?.Abort();
                _webRequest?.Dispose();
                _webRequest = null;
                
                await UniTask.CompletedTask; // 비동기 작업 시뮬레이션
                
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebGL WebSocket 연결 해제 중 오류: {ex.Message}");
            }
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("WebGL WebSocket이 연결되지 않았습니다.");
                return false;
            }

            try
            {
                // TODO : WebGL에서는 WebSocket 메시지 전송을 위한 별도 구현 필요
                await UniTask.CompletedTask; 
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebGL WebSocket 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> SendBinaryAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("WebGL WebSocket이 연결되지 않았습니다.");
                return false;
            }

            try
            {
                await UniTask.CompletedTask; 
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebGL WebSocket 바이너리 전송 실패: {ex.Message}");
                return false;
            }
        }

        private async UniTask ReceiveLoopAsync()
        {
            try
            {
                while (IsConnected && !_isDisposed)
                {
                    // TODO : WebGL에서는 WebSocket 메시지 수신을 위한 별도 구현 필요
                    await UniTask.Delay(100); 
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"WebGL WebSocket 수신 루프 오류: {ex.Message}");
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
            _webRequest?.Dispose();
        }
    }
} 