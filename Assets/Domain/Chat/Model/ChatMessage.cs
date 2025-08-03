#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectVG.Infrastructure.Network.DTOs.Chat;

namespace ProjectVG.Domain.Chat.Model
{
    [Serializable]
    public class ChatMessage
    {
        public string SessionId { get; set; } = string.Empty;
        public string? Text { get; set; }
        public VoiceData? VoiceData { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }
        
        public static ChatMessage FromChatResponse(ChatResponse response)
        {
            var chatMessage = new ChatMessage
            {
                SessionId = response.SessionId,
                Text = response.Text,
                Timestamp = response.Timestamp,
                Metadata = response.Metadata
            };
            
            if (!string.IsNullOrEmpty(response.AudioData))
            {
                chatMessage.VoiceData = VoiceData.FromBase64(response.AudioData, response.AudioFormat);
            }
            
            return chatMessage;
        }
        
        public bool HasVoiceData() => VoiceData != null && VoiceData.IsPlayable();
        
        public bool HasTextData() => !string.IsNullOrEmpty(Text);
        
        public AudioClip? GetAudioClip() => VoiceData?.AudioClip;
    }
} 