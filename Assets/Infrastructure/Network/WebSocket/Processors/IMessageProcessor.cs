using System;
using ProjectVG.Infrastructure.Network.DTOs.WebSocket;

namespace ProjectVG.Infrastructure.Network.WebSocket.Processors
{
    /// <summary>
    /// 메시지 처리기 인터페이스 (Bridge Pattern의 추상화)
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// 메시지 타입
        /// </summary>
        string MessageType { get; }
        
        /// <summary>
        /// 문자열 메시지 처리
        /// </summary>
        /// <param name="message">수신된 메시지</param>
        /// <param name="handlers">핸들러 목록</param>
        void ProcessMessage(string message, System.Collections.Generic.List<IWebSocketHandler> handlers);
        
        /// <summary>
        /// 바이너리 메시지 처리
        /// </summary>
        /// <param name="data">수신된 바이너리 데이터</param>
        /// <param name="handlers">핸들러 목록</param>
        void ProcessBinaryMessage(byte[] data, System.Collections.Generic.List<IWebSocketHandler> handlers);
        
        /// <summary>
        /// 세션 ID 추출
        /// </summary>
        /// <param name="message">메시지</param>
        /// <returns>세션 ID</returns>
        string ExtractSessionId(string message);
    }
} 