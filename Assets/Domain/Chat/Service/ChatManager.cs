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
    public class ChatManager : Singleton<ChatManager>
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
        
        private readonly Queue<ChatMessage> _messageQueue = new Queue<ChatMessage>();
        private readonly object _queueLock = new object();
        
        public bool IsConnected => _isConnected;
        public bool IsInitialized => _isInitialized;
        public int QueueCount => _messageQueue.Count;

        public event Action<ChatMessage>? OnChatMessageReceived;
        public event Action<string>? OnError;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        private void Start()
        {
            Initialize();
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
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            try
            {
                if (_webSocketManager == null)
                    _webSocketManager = WebSocketManager.Instance;
                    
                if (_voiceManager == null)
                    _voiceManager = VoiceManager.Instance;
                
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
        
        public async void SendUserMessage(string message)
        {
            if (!ValidateUserInput(message))
                return;
                
            try
            {
                Debug.Log($"사용자 메시지 전송: {message}");
                
                var chatService = ApiServiceManager.Instance.Chat;
                var response = await chatService.SendChatAsync(
                    message: message,
                    characterId: _characterId,
                    userId: _userId
                );

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
        
        public void ProcessCharacterMessage(ChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                Debug.LogWarning("빈 채팅 메시지를 받았습니다.");
                return;
            }
            
            if (!_enableMessageQueue)
            {
                ProcessMessageImmediately(chatMessage);
                return;
            }
            
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
            
            ProcessMessageQueueAsync().Forget();
        }
        
        public void ClearMessageQueue()
        {
            lock (_queueLock)
            {
                _messageQueue.Clear();
                Debug.Log("메시지 큐가 초기화되었습니다.");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTaskVoid ProcessMessageQueueAsync()
        {
            if (_isProcessing)
                return;
                
            _isProcessing = true;
            
            try
            {
                while (true)
                {
                    ChatMessage message;
                    
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
        
        private void ProcessMessageImmediately(ChatMessage chatMessage)
        {
            try
            {
                OnChatMessageReceived?.Invoke(chatMessage);
                
                if (chatMessage.VoiceData != null && _voiceManager != null)
                {
                    _voiceManager.PlayVoice(chatMessage.VoiceData);
                }

                Debug.Log($"캐릭터 메시지 처리: {chatMessage.Text}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
        }
        
        private async UniTask ProcessMessageImmediatelyAsync(ChatMessage chatMessage)
        {
            try
            {
                OnChatMessageReceived?.Invoke(chatMessage);

                Debug.Log($"캐릭터 메시지 처리 시작: {chatMessage.Text}");
                
                if (chatMessage.VoiceData != null && _voiceManager != null)
                {
                    await _voiceManager.PlayVoiceAsync(chatMessage.VoiceData);
                }

                Debug.Log($"캐릭터 메시지 처리 완료: {chatMessage.Text}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
        }
        
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
        
        private void OnVoiceFinished()
        {
            Debug.Log("음성 재생 완료");
        }
        
        private void HandleChatMessageReceived(ChatMessage chatMessage)
        {
            ProcessCharacterMessage(chatMessage);
        }
        
        #endregion
    }
} 