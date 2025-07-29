using System;
using UnityEngine;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    [Serializable]
    public class ChatRequest
    {
        [JsonProperty("session_id")]
        [SerializeField] public string sessionId;
        
        [JsonProperty("message")]
        [SerializeField] public string message;
        
        [JsonProperty("character_id")]
        [SerializeField] public string characterId;
        
        [JsonProperty("user_id")]
        [SerializeField] public string userId;
        
        [JsonProperty("action")]
        [SerializeField] public string action = "chat";
        
        [JsonProperty("actor")]
        [SerializeField] public string actor;
        
        [JsonProperty("instruction")]
        [SerializeField] public string instruction;
        
        [JsonProperty("requested_at")]
        [SerializeField] public string requestedAt;
    }
} 