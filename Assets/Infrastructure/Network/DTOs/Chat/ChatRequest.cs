using System;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    /// <summary>
    /// 채팅 요청 DTO
    /// </summary>
    [Serializable]
    public class ChatRequest
    {
        public string sessionId;
        public string actor;
        public string message;
        public string action = "chat";
        public string character_id;
        public string user_id;
    }
} 