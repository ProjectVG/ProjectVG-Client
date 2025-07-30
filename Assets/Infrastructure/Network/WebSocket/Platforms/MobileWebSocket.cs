using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Network.WebSocket.Platforms
{
    /// <summary>
    /// 모바일 플랫폼용 WebSocket 구현체
    /// iOS/Android 네이티브 WebSocket 라이브러리를 사용합니다.
    /// </summary>
    public class MobileWebSocket : INativeWebSocket
    {
        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
#pragma warning disable CS0067
        public event Action<string> OnMessageReceived;
#pragma warning restore CS0067

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed = false;

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

            try
            {
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                // TODO : 모바일에서는 네이티브 WebSocket 라이브러리 사용
                // TODO : 현재는 기본 구현만 제공 (실제 구현 시 네이티브 플러그인 필요)
                
                // TODO : 임시로 성공 시뮬레이션
                await UniTask.Delay(100, cancellationToken: combinedCancellationToken);
                
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
                var error = $"모바일 WebSocket 연결 중 예외 발생: {ex.Message}";
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
                
                // TODO : 네이티브 WebSocket 연결 해제
                // TODO : 실제 구현 시 네이티브 플러그인 호출
                await UniTask.CompletedTask;
                
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"모바일 WebSocket 연결 해제 중 오류: {ex.Message}");
            }
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("모바일 WebSocket이 연결되지 않았습니다.");
                return false;
            }

            try
            {
                // TODO : 네이티브 WebSocket 메시지 전송
                // TODO : 실제 구현 시 네이티브 플러그인 호출
                await UniTask.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"모바일 WebSocket 메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        private async UniTask ReceiveLoopAsync()
        {
            try
            {
                while (IsConnected && !_isDisposed)
                {
                    // TODO : 네이티브 WebSocket 메시지 수신
                    // TODO : 실제 구현 시 네이티브 플러그인에서 메시지 수신
                    await UniTask.Delay(100); 
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogError($"모바일 WebSocket 수신 루프 오류: {ex.Message}");
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
        }
    }
} 