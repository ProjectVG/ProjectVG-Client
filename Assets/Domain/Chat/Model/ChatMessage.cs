#nullable enable
using System;
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
        public CharacterActionData? ActionData { get; set; }
        public CostInfo? CostInfo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public static ChatMessage FromChatResponse(ChatResponse response)
        {
            var chatMessage = new ChatMessage {
                SessionId = response.SessionId,
                Text = response.Text,
                Timestamp = response.Timestamp,
                ActionData = new CharacterActionData(response.Action)
            };
            
            if (!string.IsNullOrEmpty(response.AudioData))
            {
                chatMessage.VoiceData = VoiceData.FromBase64(response.AudioData, response.AudioFormat);
            }
            
            if ((response.UsedCost ?? 0) > 0 || (response.RemainingCost ?? 0) > 0)
            {
                chatMessage.CostInfo = new CostInfo(response.UsedCost ?? 0f, response.RemainingCost ?? 0f);
            }
            
            return chatMessage;
        }
        
        public bool HasVoiceData() => VoiceData != null && VoiceData.IsPlayable();
        
        public bool HasTextData() => !string.IsNullOrEmpty(Text);

        public bool HasActionData() => ActionData != null && ActionData.HasAction();
        
        public bool HasCostInfo() => CostInfo != null && CostInfo.HasCostInfo();
        
        public AudioClip? GetAudioClip() => VoiceData?.AudioClip;

    }
} 