using System;
using System.Text;
using UnityEngine;
using ProjectVG.Infrastructure.Network.DTOs.WebSocket;

namespace ProjectVG.Infrastructure.Network.WebSocket.Processors
{
    /// <summary>
    /// 바이너리 메시지 처리기 (Bridge Pattern의 구현체)
    /// </summary>
    public class BinaryMessageProcessor : IMessageProcessor
    {
        public string MessageType => "binary";
        
        // 메시지 타입 상수
        private const byte MESSAGE_TYPE_INTEGRATED = 0x03;
        private const byte MESSAGE_TYPE_TEXT_ONLY = 0x01;
        private const byte MESSAGE_TYPE_AUDIO_ONLY = 0x02;
        
        public void ProcessMessage(string message, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            // 바이너리 프로세서는 문자열 메시지를 처리하지 않음
            Debug.LogWarning("바이너리 프로세서는 문자열 메시지를 처리하지 않습니다.");
        }
        
        public void ProcessBinaryMessage(byte[] data, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            try
            {
                Debug.Log($"바이너리 메시지 처리: {data.Length} bytes");
                
                // 바이너리 메시지 파싱 시도
                var integratedMessage = ParseBinaryMessage(data);
                if (integratedMessage != null)
                {
                    ProcessIntegratedMessage(integratedMessage, handlers);
                    return;
                }
                
                // 바이너리 파싱 실패 시 순수 오디오 데이터로 처리
                Debug.Log("바이너리 메시지 파싱 실패 - 순수 오디오 데이터로 처리");
                ProcessAudioData(data, handlers);
            }
            catch (Exception ex)
            {
                Debug.LogError($"바이너리 메시지 처리 실패: {ex.Message}");
            }
        }
        
        public string ExtractSessionId(string message)
        {
            // 바이너리 프로세서는 문자열에서 세션 ID를 추출하지 않음
            return null;
        }
        
        /// <summary>
        /// 바이너리 메시지 파싱
        /// </summary>
        private IntegratedMessage ParseBinaryMessage(byte[] data)
        {
            try
            {
                if (data.Length < 5) // 최소 길이 체크
                {
                    Debug.LogWarning("바이너리 메시지가 너무 짧습니다.");
                    return null;
                }

                int offset = 0;

                // 메시지 타입 확인 (1바이트)
                byte messageType = data[offset];
                offset += 1;

                if (messageType != MESSAGE_TYPE_INTEGRATED)
                {
                    Debug.LogWarning($"지원하지 않는 메시지 타입: {messageType}");
                    return null;
                }

                // 세션 ID 읽기
                if (offset + 4 > data.Length) return null;
                int sessionIdLength = BitConverter.ToInt32(data, offset);
                offset += 4;

                if (offset + sessionIdLength > data.Length) return null;
                string sessionId = Encoding.UTF8.GetString(data, offset, sessionIdLength);
                offset += sessionIdLength;

                // 텍스트 읽기
                if (offset + 4 > data.Length) return null;
                int textLength = BitConverter.ToInt32(data, offset);
                offset += 4;

                string text = null;
                if (textLength > 0)
                {
                    if (offset + textLength > data.Length) return null;
                    text = Encoding.UTF8.GetString(data, offset, textLength);
                    offset += textLength;
                }

                // 오디오 데이터 읽기
                if (offset + 4 > data.Length) return null;
                int audioLength = BitConverter.ToInt32(data, offset);
                offset += 4;

                byte[] audioData = null;
                if (audioLength > 0)
                {
                    if (offset + audioLength > data.Length) return null;
                    audioData = new byte[audioLength];
                    Array.Copy(data, offset, audioData, 0, audioLength);
                    offset += audioLength;
                }

                // 오디오 지속시간 읽기 (float)
                if (offset + 4 > data.Length) return null;
                float audioDuration = BitConverter.ToSingle(data, offset);

                return new IntegratedMessage
                {
                    sessionId = sessionId,
                    text = text,
                    audioData = audioData,
                    audioDuration = audioDuration
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"바이너리 메시지 파싱 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 통합 메시지 처리 (텍스트 + 오디오)
        /// </summary>
        private void ProcessIntegratedMessage(IntegratedMessage integratedMessage, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            try
            {
                Debug.Log($"통합 메시지 수신 - 텍스트: {integratedMessage.text?.Length ?? 0}자, 오디오: {integratedMessage.audioData?.Length ?? 0}바이트");
                
                // 이벤트 발생
                foreach (var handler in handlers)
                {
                    handler.OnIntegratedMessageReceived(integratedMessage);
                }

                // 텍스트가 있는 경우 텍스트 메시지로도 처리
                if (!string.IsNullOrEmpty(integratedMessage.text))
                {
                    var chatMessage = new ChatMessage
                    {
                        type = "chat",
                        sessionId = integratedMessage.sessionId,
                        message = integratedMessage.text,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    
                    foreach (var handler in handlers)
                    {
                        handler.OnChatMessageReceived(chatMessage);
                    }
                }

                // 오디오가 있는 경우 오디오 데이터로도 처리
                if (integratedMessage.audioData != null && integratedMessage.audioData.Length > 0)
                {
                    ProcessAudioData(integratedMessage.audioData, handlers);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"통합 메시지 처리 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 오디오 데이터 처리
        /// </summary>
        private void ProcessAudioData(byte[] audioData, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            try
            {
                Debug.Log($"오디오 데이터 수신: {audioData.Length} bytes");
                
                // 이벤트 발생
                foreach (var handler in handlers)
                {
                    handler.OnAudioDataReceived(audioData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"오디오 데이터 처리 실패: {ex.Message}");
            }
        }
    }
} 