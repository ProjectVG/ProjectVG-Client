using System;

namespace ProjectVG.Infrastructure.Network.DTOs.WebSocket
{
    /// <summary>
    /// WebSocket 메시지 기본 구조
    /// </summary>
    [Serializable]
    public class WebSocketMessage
    {
        public string type;
        public string sessionId;
        public long timestamp;
        public string data;
    }

    /// <summary>
    /// 세션 ID 메시지 (더미 클라이언트와 동일)
    /// </summary>
    [Serializable]
    public class SessionIdMessage : WebSocketMessage
    {
        public string session_id;
    }

    /// <summary>
    /// 채팅 메시지 타입
    /// </summary>
    [Serializable]
    public class ChatMessage : WebSocketMessage
    {
        public string characterId;
        public string userId;
        public string message;
        public string actor;
    }

    /// <summary>
    /// 시스템 메시지 타입
    /// </summary>
    [Serializable]
    public class SystemMessage : WebSocketMessage
    {
        public string status;
        public string description;
    }

    /// <summary>
    /// 연결 상태 메시지 타입
    /// </summary>
    [Serializable]
    public class ConnectionMessage : WebSocketMessage
    {
        public string status; // "connected", "disconnected", "error"
        public string reason;
    }
} 