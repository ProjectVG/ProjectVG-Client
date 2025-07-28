using System;
using UnityEngine;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    /// <summary>
    /// 채팅 요청 DTO
    /// </summary>
    [Serializable]
    public class ChatRequest
    {
        [SerializeField] public string sessionId;
        [SerializeField] public string actor;
        [SerializeField] public string message;
        [SerializeField] public string action = "chat";
        [SerializeField] public string characterId;
        [SerializeField] public string userId;
    }
} 