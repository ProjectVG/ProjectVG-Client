using System;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    /// <summary>
    /// 채팅 응답 DTO
    /// </summary>
    [Serializable]
    public class ChatResponse
    {
        public bool success;
        public string message;
        public string sessionId;
    }
} 