using System.Collections.Generic;
using UnityEngine;

namespace ProjectVG.Infrastructure.Network.WebSocket.Processors
{
    /// <summary>
    /// 메시지 처리기 팩토리 (Factory Pattern)
    /// </summary>
    public static class MessageProcessorFactory
    {
        private static readonly Dictionary<string, IMessageProcessor> _processors = new Dictionary<string, IMessageProcessor>();
        
        static MessageProcessorFactory()
        {
            // 기본 프로세서 등록
            RegisterProcessor(new JsonMessageProcessor());
            RegisterProcessor(new BinaryMessageProcessor());
        }
        
        /// <summary>
        /// 프로세서 등록
        /// </summary>
        public static void RegisterProcessor(IMessageProcessor processor)
        {
            if (processor != null && !string.IsNullOrEmpty(processor.MessageType))
            {
                _processors[processor.MessageType.ToLower()] = processor;
                Debug.Log($"메시지 프로세서 등록: {processor.MessageType}");
            }
        }
        
        /// <summary>
        /// 메시지 타입에 따른 프로세서 생성
        /// </summary>
        public static IMessageProcessor CreateProcessor(string messageType)
        {
            if (string.IsNullOrEmpty(messageType))
            {
                Debug.LogWarning("메시지 타입이 null입니다. JSON 프로세서를 사용합니다.");
                return GetDefaultProcessor();
            }
            
            var key = messageType.ToLower();
            if (_processors.TryGetValue(key, out var processor))
            {
                Debug.Log($"메시지 프로세서 생성: {messageType}");
                return processor;
            }
            
            Debug.LogWarning($"지원하지 않는 메시지 타입: {messageType}. JSON 프로세서를 사용합니다.");
            return GetDefaultProcessor();
        }
        
        /// <summary>
        /// 기본 프로세서 (JSON) 반환
        /// </summary>
        public static IMessageProcessor GetDefaultProcessor()
        {
            return _processors["json"];
        }
        
        /// <summary>
        /// 등록된 모든 프로세서 타입 반환
        /// </summary>
        public static IEnumerable<string> GetAvailableProcessors()
        {
            return _processors.Keys;
        }
        
        /// <summary>
        /// 프로세서 존재 여부 확인
        /// </summary>
        public static bool HasProcessor(string messageType)
        {
            if (string.IsNullOrEmpty(messageType))
                return false;
                
            return _processors.ContainsKey(messageType.ToLower());
        }
    }
} 