using UnityEngine;
using ProjectVG.Infrastructure.Network.DTOs.WebSocket;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /// <summary>
    /// 기본 WebSocket 핸들러 구현체
    /// 기본적인 로깅과 이벤트 처리를 제공합니다.
    /// </summary>
    public class DefaultWebSocketHandler : MonoBehaviour, IWebSocketHandler
    {
        [Header("Handler Configuration")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool autoRegister = true;

        // 이벤트
        public System.Action OnConnectedEvent;
        public System.Action OnDisconnectedEvent;
        public System.Action<string> OnErrorEvent;
        public System.Action<WebSocketMessage> OnMessageReceivedEvent;
        public System.Action<ChatMessage> OnChatMessageReceivedEvent;
        public System.Action<SystemMessage> OnSystemMessageReceivedEvent;
        public System.Action<ConnectionMessage> OnConnectionMessageReceivedEvent;
        public System.Action<SessionIdMessage> OnSessionIdMessageReceivedEvent;
        public System.Action<byte[]> OnAudioDataReceivedEvent;
        public System.Action<IntegratedMessage> OnIntegratedMessageReceivedEvent;

        private void Start()
        {
            if (autoRegister)
            {
                RegisterToManager();
            }
        }

        private void OnDestroy()
        {
            UnregisterFromManager();
        }

        /// <summary>
        /// WebSocket 매니저에 등록
        /// </summary>
        public void RegisterToManager()
        {
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.RegisterHandler(this);
                if (enableLogging)
                {
                    Debug.Log("DefaultWebSocketHandler가 WebSocketManager에 등록되었습니다.");
                }
            }
        }

        /// <summary>
        /// WebSocket 매니저에서 해제
        /// </summary>
        public void UnregisterFromManager()
        {
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.UnregisterHandler(this);
                if (enableLogging)
                {
                    Debug.Log("DefaultWebSocketHandler가 WebSocketManager에서 해제되었습니다.");
                }
            }
        }

        #region IWebSocketHandler 구현

        public void OnConnected()
        {
            if (enableLogging)
            {
                Debug.Log("WebSocket 연결됨");
            }
            
            OnConnectedEvent?.Invoke();
        }

        public void OnDisconnected()
        {
            if (enableLogging)
            {
                Debug.Log("WebSocket 연결 해제됨");
            }
            
            OnDisconnectedEvent?.Invoke();
        }

        public void OnError(string error)
        {
            if (enableLogging)
            {
                Debug.LogError($"WebSocket 오류: {error}");
            }
            
            OnErrorEvent?.Invoke(error);
        }

        public void OnMessageReceived(WebSocketMessage message)
        {
            if (enableLogging)
            {
                Debug.Log($"WebSocket 메시지 수신: {message.type} - {message.data}");
            }
            
            OnMessageReceivedEvent?.Invoke(message);
        }

        public void OnChatMessageReceived(ChatMessage message)
        {
            if (enableLogging)
            {
                Debug.Log($"채팅 메시지 수신: {message.characterId} - {message.message}");
            }
            
            OnChatMessageReceivedEvent?.Invoke(message);
        }

        public void OnSystemMessageReceived(SystemMessage message)
        {
            if (enableLogging)
            {
                Debug.Log($"시스템 메시지 수신: {message.status} - {message.description}");
            }
            
            OnSystemMessageReceivedEvent?.Invoke(message);
        }

        public void OnConnectionMessageReceived(ConnectionMessage message)
        {
            if (enableLogging)
            {
                Debug.Log($"연결 상태 메시지 수신: {message.status} - {message.reason}");
            }
            
            OnConnectionMessageReceivedEvent?.Invoke(message);
        }

        public void OnSessionIdMessageReceived(SessionIdMessage message)
        {
            if (enableLogging)
            {
                Debug.Log($"세션 ID 메시지 수신: {message.session_id}");
            }
            
            OnSessionIdMessageReceivedEvent?.Invoke(message);
        }

        public void OnAudioDataReceived(byte[] audioData)
        {
            if (enableLogging)
            {
                Debug.Log($"오디오 데이터 수신: {audioData.Length} bytes");
            }
            
            OnAudioDataReceivedEvent?.Invoke(audioData);
        }

        public void OnIntegratedMessageReceived(IntegratedMessage message)
        {
            if (enableLogging)
            {
                Debug.Log($"통합 메시지 수신 - 텍스트: {message.text?.Length ?? 0}자, 오디오: {message.audioData?.Length ?? 0}바이트, 지속시간: {message.audioDuration:F2}초");
            }
            
            OnIntegratedMessageReceivedEvent?.Invoke(message);
        }

        #endregion
    }
} 