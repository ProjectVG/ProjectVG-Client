using ProjectVG.Infrastructure.Network.DTOs.WebSocket;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /// <summary>
    /// WebSocket 이벤트 핸들러 인터페이스
    /// </summary>
    public interface IWebSocketHandler
    {
        /// <summary>
        /// 연결 성공 시 호출
        /// </summary>
        void OnConnected();

        /// <summary>
        /// 연결 해제 시 호출
        /// </summary>
        void OnDisconnected();

        /// <summary>
        /// 연결 오류 시 호출
        /// </summary>
        /// <param name="error">오류 메시지</param>
        void OnError(string error);

        /// <summary>
        /// 메시지 수신 시 호출
        /// </summary>
        /// <param name="message">수신된 메시지</param>
        void OnMessageReceived(WebSocketMessage message);

        /// <summary>
        /// 채팅 메시지 수신 시 호출
        /// </summary>
        /// <param name="message">채팅 메시지</param>
        void OnChatMessageReceived(ChatMessage message);

        /// <summary>
        /// 시스템 메시지 수신 시 호출
        /// </summary>
        /// <param name="message">시스템 메시지</param>
        void OnSystemMessageReceived(SystemMessage message);

        /// <summary>
        /// 연결 상태 메시지 수신 시 호출
        /// </summary>
        /// <param name="message">연결 상태 메시지</param>
        void OnConnectionMessageReceived(ConnectionMessage message);

        /// <summary>
        /// 세션 ID 메시지 수신 시 호출
        /// </summary>
        /// <param name="message">세션 ID 메시지</param>
        void OnSessionIdMessageReceived(SessionIdMessage message);

        /// <summary>
        /// 오디오 데이터 수신 시 호출
        /// </summary>
        /// <param name="audioData">오디오 바이트 데이터</param>
        void OnAudioDataReceived(byte[] audioData);
    }
} 