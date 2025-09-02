#nullable enable
using System;
using UnityEngine;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    [Serializable]
    public record ChatRequest
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("character_id")]
        public string CharacterId { get; set; } = string.Empty;

        [JsonProperty("action")]
        public string? Action { get; set; } 

        [JsonProperty("use_tts")]
        public bool UseTTS { get; set; } = true;

        [JsonProperty("request_at")]
        public DateTime RequestAt { get; set; }
    }
} 