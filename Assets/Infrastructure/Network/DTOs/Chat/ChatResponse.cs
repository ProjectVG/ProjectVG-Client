#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.DTOs.Chat
{
    [Serializable]
    public class WebSocketResponse
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "chat";
        
        [JsonProperty("message_type")]
        public string MessageType { get; set; } = "json";
        
        [JsonProperty("data")]
        public ChatData? Data { get; set; }
    }
    
    [Serializable]
    public class ChatData
    {
        [JsonProperty("text")]
        public string? Text { get; set; }
        
        [JsonProperty("emotion")]
        public string? Emotion { get; set; }
        
        [JsonProperty("actions")]
        public string[]? Actions { get; set; }
        
        [JsonProperty("order")]
        public int Order { get; set; }
        
        [JsonProperty("request_id")]
        public string RequestId { get; set; } = string.Empty;
        
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = string.Empty;
        
        [JsonProperty("audio_data")]
        public string? AudioData { get; set; }
        
        [JsonProperty("audio_format")]
        public string? AudioFormat { get; set; } = "wav";
        
        [JsonProperty("audio_length")]
        public float? AudioLength { get; set; }
    }
} 