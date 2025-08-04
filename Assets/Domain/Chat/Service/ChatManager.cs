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
using ProjectVG.Domain.Chat.Service;

namespace ProjectVG.Domain.Chat.Service
{
    public class ChatManager : Singleton<ChatManager>
    {
        [Header("Components")]
        [SerializeField] private WebSocketManager _webSocketManager;
        [SerializeField] private VoiceManager _voiceManager;
        [SerializeField] private ChatBubbleManager _chatBubbleManager;
        
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
                    
                if (_chatBubbleManager == null)
                    _chatBubbleManager = FindObjectOfType<ChatBubbleManager>();
                
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
                Debug.Log("[ChatManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 초기화 실패: {ex.Message}");
                OnError?.Invoke($"초기화 실패: {ex.Message}");
            }
        }
        
        public async void SendUserMessage(string message)
        {
            if (!ValidateUserInput(message))
                return;
                
            try
            {
                if (_chatBubbleManager != null)
                {
                    _chatBubbleManager.CreateBubble(Actor.User, message);
                }
                
                var chatService = ApiServiceManager.Instance.Chat;
                var response = await chatService.SendChatAsync(
                    message: message,
                    characterId: _characterId,
                    userId: _userId
                );

                if (response == null)
                {
                    Debug.LogWarning("[ChatManager] 채팅 응답이 null입니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 메시지 전송 실패: {ex.Message}");
                OnError?.Invoke($"메시지 전송 실패: {ex.Message}");
            }
        }
        
        public void ProcessCharacterMessage(ChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                Debug.LogWarning("[ChatManager] 빈 채팅 메시지를 받았습니다.");
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
                    Debug.LogWarning($"[ChatManager] 메시지 큐가 가득 찼습니다. (최대: {_maxQueueSize})");
                    return;
                }
                
                _messageQueue.Enqueue(chatMessage);
            }
            
            ProcessMessageQueueAsync().Forget();
        }
        
        public void ClearMessageQueue()
        {
            lock (_queueLock)
            {
                _messageQueue.Clear();
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
                Debug.LogError($"[ChatManager] 메시지 큐 처리 중 오류: {ex.Message}");
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
                
                // 캐릭터 메시지를 버블로 표시
                if (_chatBubbleManager != null && !string.IsNullOrEmpty(chatMessage.Text))
                {
                    _chatBubbleManager.CreateBubble(Actor.Character, chatMessage.Text);
                }
                
                if (chatMessage.VoiceData != null && _voiceManager != null)
                {
                    _voiceManager.PlayVoice(chatMessage.VoiceData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
        }
        
        private async UniTask ProcessMessageImmediatelyAsync(ChatMessage chatMessage)
        {
            try
            {
                OnChatMessageReceived?.Invoke(chatMessage);
                
                if (_chatBubbleManager != null && !string.IsNullOrEmpty(chatMessage.Text))
                {
                    _chatBubbleManager.CreateBubble(Actor.Character, chatMessage.Text);
                }
                
                if (chatMessage.VoiceData != null && _voiceManager != null)
                {
                    await _voiceManager.PlayVoiceAsync(chatMessage.VoiceData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
        }
        
        private bool ValidateUserInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("[ChatManager] 빈 메시지는 전송할 수 없습니다.");
                return false;
            }
            
            if (message.Length > 1000)
            {
                Debug.LogWarning("[ChatManager] 메시지가 너무 깁니다. (최대 1000자)");
                return false;
            }
            
            return true;
        }
        
        private void OnVoiceFinished()
        {
        }
        
        private void HandleChatMessageReceived(ChatMessage chatMessage)
        {
            ProcessCharacterMessage(chatMessage);
        }
        
        #endregion
    }
} 