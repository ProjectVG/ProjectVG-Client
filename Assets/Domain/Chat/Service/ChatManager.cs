#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.DTOs.Chat;

namespace ProjectVG.Domain.Chat.Service
{
    /// <summary>
    /// 채팅 시스템의 메인 매니저
    /// WebSocket 연결, 메시지 송수신, 음성 재생을 관리합니다.
    /// </summary>
    public class ChatManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private WebSocketManager _webSocketManager;
        [SerializeField] private VoiceManager _voiceManager;
        
        [Header("Chat Settings")]
        [SerializeField] private string _characterId = "44444444-4444-4444-4444-444444444444";
        [SerializeField] private string _userId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        
        [Header("Message Queue Settings")]
        [SerializeField] private bool _enableMessageQueue = true;
        [SerializeField] private int _maxQueueSize = 100;
        
        private bool _isConnected = false;
        private bool _isInitialized = false;
        private bool _isProcessing = false;
        
        // 메시지 큐 관리
        private readonly Queue<ChatMessage> _messageQueue = new Queue<ChatMessage>();
        private readonly object _queueLock = new object();
        
        public bool IsConnected => _isConnected;
        public bool IsInitialized => _isInitialized;
        public int QueueCount => _messageQueue.Count;

        public event Action<ChatMessage>? OnChatMessageReceived;
        public event Action<string>? OnError;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// ChatManager 초기화
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            try
            {
                // 컴포넌트 자동 찾기
                if (_webSocketManager == null)
                    _webSocketManager = WebSocketManager.Instance;
                    
                if (_voiceManager == null)
                    _voiceManager = VoiceManager.Instance;
                
                // 이벤트 구독
                if (_webSocketManager != null)
                {
                    _webSocketManager.OnChatMessageReceived += HandleChatMessageReceived;
                }
                
                if (_voiceManager != null)
                {
                    _voiceManager.OnVoiceFinished += OnVoiceFinished;
                }
                
                _isInitialized = true;
                _isConnected = true;
                Debug.Log("ChatManager 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ChatManager 초기화 실패: {ex.Message}");
                OnError?.Invoke($"초기화 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 사용자 메시지 전송
        /// </summary>
        /// <param name="message">전송할 메시지</param>
        public async void SendUserMessage(string message)
        {
            if (!ValidateUserInput(message))
                return;
                
            try
            {
                Debug.Log($"사용자 메시지 전송: {message}");
                
                // ChatApiService를 통해 HTTP API 호출
                var chatService = ApiServiceManager.Instance.Chat;
                var response = await chatService.SendChatAsync(
                    message: message,
                    characterId: _characterId,
                    userId: _userId
                );

                // TODO : 메시지 출력
                
                if (response != null)
                {
                    Debug.Log($"채팅 응답 수신: {response.Text}");
                }
                else
                {
                    Debug.LogWarning("채팅 응답이 null입니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 전송 실패: {ex.Message}");
                OnError?.Invoke($"메시지 전송 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 채팅 메시지 처리 (큐 방식)
        /// </summary>
        /// <param name="chatMessage">수신된 채팅 메시지</param>
        public void ProcessCharacterMessage(ChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                Debug.LogWarning("빈 채팅 메시지를 받았습니다.");
                return;
            }
            
            if (!_enableMessageQueue)
            {
                // 큐 비활성화 시 즉시 처리
                ProcessMessageImmediately(chatMessage);
                return;
            }
            
            // 큐에 메시지 추가
            lock (_queueLock)
            {
                if (_messageQueue.Count >= _maxQueueSize)
                {
                    Debug.LogWarning($"메시지 큐가 가득 찼습니다. (최대: {_maxQueueSize})");
                    return;
                }
                
                _messageQueue.Enqueue(chatMessage);
                Debug.Log($"메시지 큐에 추가됨: {chatMessage.Text} (큐 크기: {_messageQueue.Count})");
            }
            
            // 큐 처리 시작
            ProcessMessageQueueAsync().Forget();
        }
        
        /// <summary>
        /// 메시지 큐 비동기 처리
        /// </summary>
        private async UniTaskVoid ProcessMessageQueueAsync()
        {
            if (_isProcessing)
                return;
                
            _isProcessing = true;
            
            try
            {
                while (true)
                {
                    ChatMessage message = null;
                    
                    lock (_queueLock)
                    {
                        if (_messageQueue.Count == 0)
                        {
                            break;
                        }
                        message = _messageQueue.Dequeue();
                    }
                    
                    if (message != null)
                    {
                        await ProcessMessageImmediatelyAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 큐 처리 중 오류: {ex.Message}");
                OnError?.Invoke($"메시지 큐 처리 실패: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }
        
        /// <summary>
        /// 메시지 즉시 처리 (큐 비활성화 시 사용)
        /// </summary>
        /// <param name="chatMessage">처리할 메시지</param>
        private void ProcessMessageImmediately(ChatMessage chatMessage)
        {
            try
            {
                // 이벤트 발생
                OnChatMessageReceived?.Invoke(chatMessage);
                
                // 음성 데이터가 있으면 재생
                if (chatMessage.VoiceData != null && _voiceManager != null)
                {
                    _voiceManager.PlayVoice(chatMessage.VoiceData);
                }

                // TODO : 메시지 출력
                
                Debug.Log($"캐릭터 메시지 처리: {chatMessage.Text}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 메시지 비동기 처리 (큐 활성화 시 사용)
        /// </summary>
        /// <param name="chatMessage">처리할 메시지</param>
        private async UniTask ProcessMessageImmediatelyAsync(ChatMessage chatMessage)
        {
            try
            {
                // 이벤트 발생
                OnChatMessageReceived?.Invoke(chatMessage);

                // TODO : 메시지 출력
                Debug.Log($"캐릭터 메시지 처리 시작: {chatMessage.Text}");
                
                // 음성 데이터가 있으면 재생 (재생 완료까지 대기)
                if (chatMessage.VoiceData != null && _voiceManager != null)
                {
                    Debug.Log($"음성 재생 시작: {chatMessage.Text}");
                    await _voiceManager.PlayVoiceAsync(chatMessage.VoiceData);
                    Debug.Log($"음성 재생 완료: {chatMessage.Text}");
                }
                else
                {
                    // 음성이 없는 경우 기본 대기 시간 (텍스트 표시 시간)
                    await UniTask.Delay(2000); // 2초 대기
                }

                Debug.Log($"캐릭터 메시지 처리 완료: {chatMessage.Text}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 메시지 큐 초기화
        /// </summary>
        public void ClearMessageQueue()
        {
            lock (_queueLock)
            {
                _messageQueue.Clear();
                Debug.Log("메시지 큐가 초기화되었습니다.");
            }
        }
        
        /// <summary>
        /// 사용자 입력 검증
        /// </summary>
        /// <param name="message">검증할 메시지</param>
        /// <returns>유효성 여부</returns>
        private bool ValidateUserInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("빈 메시지는 전송할 수 없습니다.");
                return false;
            }
            
            if (message.Length > 1000)
            {
                Debug.LogWarning("메시지가 너무 깁니다. (최대 1000자)");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 음성 재생 완료 처리
        /// </summary>
        private void OnVoiceFinished()
        {
            Debug.Log("음성 재생 완료");
        }
        
        /// <summary>
        /// WebSocket에서 채팅 메시지 수신 처리
        /// </summary>
        /// <param name="chatMessage">수신된 채팅 메시지</param>
        private void HandleChatMessageReceived(ChatMessage chatMessage)
        {
            ProcessCharacterMessage(chatMessage);
        }

        private void OnDestroy()
        {
            if (_webSocketManager != null)
            {
                _webSocketManager.OnChatMessageReceived -= HandleChatMessageReceived;
            }
            
            if (_voiceManager != null)
            {
                _voiceManager.OnVoiceFinished -= OnVoiceFinished;
            }
        }
    }
} 