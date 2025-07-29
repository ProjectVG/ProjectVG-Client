using System;

namespace ProjectVG.Infrastructure.Network.DTOs.WebSocket
{
    /// <summary>
    /// 통합 메시지 (텍스트 + 오디오)
    /// 바이너리 형식으로 전송되는 통합 메시지를 위한 DTO
    /// </summary>
    [System.Serializable]
    public class IntegratedMessage
    {
        /// <summary>
        /// 세션 ID
        /// </summary>
        public string sessionId;
        
        /// <summary>
        /// 텍스트 메시지
        /// </summary>
        public string text;
        
        /// <summary>
        /// 오디오 바이너리 데이터
        /// </summary>
        public byte[] audioData;
        
        /// <summary>
        /// 오디오 지속시간 (초)
        /// </summary>
        public float audioDuration;
        
        /// <summary>
        /// 텍스트가 있는지 확인
        /// </summary>
        public bool HasText => !string.IsNullOrEmpty(text);
        
        /// <summary>
        /// 오디오가 있는지 확인
        /// </summary>
        public bool HasAudio => audioData != null && audioData.Length > 0;
        
        /// <summary>
        /// 메시지 정보를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"IntegratedMessage[SessionId: {sessionId}, Text: {text?.Length ?? 0} chars, Audio: {audioData?.Length ?? 0} bytes, Duration: {audioDuration:F2}s]";
        }
    }
} 