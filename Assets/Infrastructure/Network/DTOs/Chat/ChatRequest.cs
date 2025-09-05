using System;
using UnityEngine;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    [Serializable]
    public class ChatRequest
    {
        
        [JsonProperty("message")]
        [SerializeField] public string message;
        
        [JsonProperty("character_id")]
        [SerializeField] public string characterId;
        
        [JsonProperty("action")]
        [SerializeField] public string action = "chat";
        
        [JsonProperty("requested_at")]
        [SerializeField] public string requestedAt;
    }
} 