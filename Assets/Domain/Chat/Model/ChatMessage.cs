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
        
        // 새로운 API 필드들
        public string? Emotion { get; set; }
        public int Order { get; set; } = 0;
        public string RequestId { get; set; } = string.Empty;
        
        /// <summary>
        /// 새로운 API ChatData에서 ChatMessage로 변환
        /// </summary>
        /// <param name="chatData">새 API 채팅 데이터</param>
        /// <returns>ChatMessage 인스턴스</returns>
        public static ChatMessage FromChatData(ChatData chatData)
        {
            var chatMessage = new ChatMessage {
                Text = chatData.Text,
                Emotion = chatData.Emotion,
                Order = chatData.Order,
                RequestId = chatData.RequestId,
                ActionData = new CharacterActionData(chatData.Actions, chatData.Emotion)
            };
            
            // 타임스탬프 파싱 (새 API는 string 형태)
            if (!string.IsNullOrEmpty(chatData.Timestamp))
            {
                if (DateTime.TryParse(chatData.Timestamp, out var parsedTimestamp))
                {
                    chatMessage.Timestamp = parsedTimestamp;
                }
            }
            
            // 오디오 데이터 처리
            if (!string.IsNullOrEmpty(chatData.AudioData))
            {
                chatMessage.VoiceData = VoiceData.FromBase64(chatData.AudioData, chatData.AudioFormat);
            }
            
            return chatMessage;
        }
        
        public bool HasVoiceData() => VoiceData != null && VoiceData.IsPlayable();
        
        public bool HasTextData() => !string.IsNullOrEmpty(Text);

        public bool HasActionData() => ActionData != null && ActionData.HasAction();
        
        public bool HasCostInfo() => CostInfo != null && CostInfo.HasCostInfo();
        
        /// <summary>
        /// 감정 데이터가 있는지 확인
        /// </summary>
        /// <returns>감정 데이터가 있으면 true</returns>
        public bool HasEmotionData() => !string.IsNullOrEmpty(Emotion);
        
        /// <summary>
        /// 복수의 액션이 있는지 확인
        /// </summary>
        /// <returns>복수 액션이 있으면 true</returns>
        public bool HasMultipleActions() => ActionData != null && ActionData.HasMultipleActions();
        
        /// <summary>
        /// 요청 ID가 설정되어 있는지 확인
        /// </summary>
        /// <returns>요청 ID가 있으면 true</returns>
        public bool HasRequestId() => !string.IsNullOrEmpty(RequestId);
        
        public AudioClip? GetAudioClip() => VoiceData?.AudioClip;

    }
} 