using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /// <summary>
    /// 플랫폼별 Native WebSocket 구현을 위한 인터페이스
    /// </summary>
    public interface INativeWebSocket : IDisposable
    {
        // 연결 상태
        bool IsConnected { get; }
        bool IsConnecting { get; }
        
        // 이벤트
        event Action OnConnected;
        event Action OnDisconnected;
        event Action<string> OnError;
        event Action<string> OnMessageReceived;
        event Action<byte[]> OnBinaryDataReceived;
        
        // 연결 관리
        UniTask<bool> ConnectAsync(string url, CancellationToken cancellationToken = default);
        UniTask DisconnectAsync();
        
        // 메시지 전송
        UniTask<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default);
        UniTask<bool> SendBinaryAsync(byte[] data, CancellationToken cancellationToken = default);
    }
} 