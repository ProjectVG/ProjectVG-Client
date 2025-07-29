using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    [Serializable]
    public class ChatResponse
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "chat";
        
        [JsonProperty("message_type")]
        public string MessageType { get; set; } = "json";
        
        [JsonProperty("session_id")]
        public string SessionId { get; set; } = string.Empty;
        
        [JsonProperty("text")]
        public string? Text { get; set; }
        
        [JsonProperty("audio_data")]
        public string? AudioData { get; set; }
        
        [JsonProperty("audio_format")]
        public string? AudioFormat { get; set; } = "wav";
        
        [JsonProperty("audio_length")]
        public float? AudioLength { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }
} 