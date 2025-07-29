using System;
using UnityEngine;
using ProjectVG.Infrastructure.Network.DTOs.WebSocket;

namespace ProjectVG.Infrastructure.Network.WebSocket.Processors
{
    /// <summary>
    /// JSON 메시지 처리기 (Bridge Pattern의 구현체)
    /// </summary>
    public class JsonMessageProcessor : IMessageProcessor
    {
        public string MessageType => "json";
        
        public void ProcessMessage(string message, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            try
            {
                Debug.Log($"JSON 메시지 처리: {message}");
                
                // 세션 ID 메시지 특별 처리
                if (message.Contains("\"type\":\"session_id\""))
                {
                    ProcessSessionIdMessage(message, handlers);
                    return;
                }
                
                // JSON 메시지 파싱 및 처리
                var baseMessage = ParseJsonMessage(message);
                if (baseMessage != null)
                {
                    ProcessReceivedMessage(baseMessage, handlers);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON 메시지 처리 실패: {ex.Message}");
            }
        }
        
        public void ProcessBinaryMessage(byte[] data, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            // JSON 프로세서는 바이너리 메시지를 처리하지 않음
            Debug.LogWarning("JSON 프로세서는 바이너리 메시지를 처리하지 않습니다.");
        }
        
        public string ExtractSessionId(string message)
        {
            try
            {
                if (message.Contains("\"type\":\"session_id\""))
                {
                    int sessionIdStart = message.IndexOf("\"session_id\":\"") + 14;
                    int sessionIdEnd = message.IndexOf("\"", sessionIdStart);
                    if (sessionIdStart > 13 && sessionIdEnd > sessionIdStart)
                    {
                        return message.Substring(sessionIdStart, sessionIdEnd - sessionIdStart);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"세션 ID 추출 실패: {ex.Message}");
                return null;
            }
        }
        
        private void ProcessSessionIdMessage(string message, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            Debug.Log("세션 ID 메시지 감지됨");
            
            var sessionId = ExtractSessionId(message);
            if (!string.IsNullOrEmpty(sessionId))
            {
                Debug.Log($"세션 ID 저장됨: {sessionId}");
                
                // 핸들러들에게 세션 ID 메시지 전달
                var sessionMessage = new SessionIdMessage { session_id = sessionId };
                foreach (var handler in handlers)
                {
                    Debug.Log($"핸들러에게 세션 ID 전달: {handler.GetType().Name}");
                    handler.OnSessionIdMessageReceived(sessionMessage);
                }
            }
            else
            {
                Debug.LogError("세션 ID 추출 실패 - JSON 형식 확인 필요");
            }
        }
        
        private WebSocketMessage ParseJsonMessage(string message)
        {
            try
            {
                return JsonUtility.FromJson<WebSocketMessage>(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON 메시지 파싱 실패: {ex.Message}");
                return null;
            }
        }
        
        private void ProcessReceivedMessage(WebSocketMessage baseMessage, System.Collections.Generic.List<IWebSocketHandler> handlers)
        {
            try
            {
                Debug.Log($"메시지 수신: {baseMessage.type} - {baseMessage.data}");
                
                // 메시지 타입에 따른 처리
                switch (baseMessage.type?.ToLower())
                {
                    case "session_id":
                        Debug.Log("세션 ID 메시지 처리 중...");
                        var sessionMessage = JsonUtility.FromJson<SessionIdMessage>(JsonUtility.ToJson(baseMessage));
                        foreach (var handler in handlers)
                        {
                            handler.OnSessionIdMessageReceived(sessionMessage);
                        }
                        break;
                        
                    case "chat":
                        var chatMessage = JsonUtility.FromJson<ChatMessage>(JsonUtility.ToJson(baseMessage));
                        foreach (var handler in handlers)
                        {
                            handler.OnChatMessageReceived(chatMessage);
                        }
                        break;
                        
                    case "system":
                        var systemMessage = JsonUtility.FromJson<SystemMessage>(JsonUtility.ToJson(baseMessage));
                        foreach (var handler in handlers)
                        {
                            handler.OnSystemMessageReceived(systemMessage);
                        }
                        break;
                        
                    case "connection":
                        var connectionMessage = JsonUtility.FromJson<ConnectionMessage>(JsonUtility.ToJson(baseMessage));
                        foreach (var handler in handlers)
                        {
                            handler.OnConnectionMessageReceived(connectionMessage);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 처리 실패: {ex.Message}");
            }
        }
    }
} 