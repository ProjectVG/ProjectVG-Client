using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Network.WebSocket.Platforms
{
    /// <summary>
    /// Unity WebSocket 시뮬레이션 구현체
    /// 개발/테스트용으로만 사용
    /// </summary>
    public class UnityWebSocket : INativeWebSocket
    {
        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<string> OnMessageReceived;
        public event Action<byte[]> OnBinaryDataReceived;

        public async UniTask<bool> ConnectAsync(string url, CancellationToken cancellationToken = default)
        {
            if (IsConnected || IsConnecting)
            {
                return IsConnected;
            }

            IsConnecting = true;

            try
            {
                Debug.Log($"WebSocket 시뮬레이션 연결: {url}");
                await UniTask.Delay(100, cancellationToken: cancellationToken);
                
                IsConnected = true;
                IsConnecting = false;
                OnConnected?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                IsConnecting = false;
                OnError?.Invoke(ex.Message);
                return false;
            }
        }

        public async UniTask DisconnectAsync()
        {
            IsConnected = false;
            IsConnecting = false;
            OnDisconnected?.Invoke();
            await UniTask.CompletedTask;
        }

        public async UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return false;
            }

            Debug.Log($"WebSocket 시뮬레이션 메시지: {message}");
            return true;
        }

        public async UniTask<bool> SendBinaryAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return false;
            }

            Debug.Log($"WebSocket 시뮬레이션 바이너리: {data.Length} bytes");
            return true;
        }

        public void Dispose()
        {
            DisconnectAsync().Forget();
        }
    }
} 