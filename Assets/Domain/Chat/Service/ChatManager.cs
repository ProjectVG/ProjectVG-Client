#nullable enable
using System;
using UnityEngine;
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
        [SerializeField] private bool _autoConnect = true;
        [SerializeField] private string _characterId = "test-character";
        [SerializeField] private string _userId = "test-user";
        
        private string _sessionId = string.Empty;
        private bool _isConnected = false;
        private bool _isInitialized = false;
        
        public bool IsConnected => _isConnected;
        public bool IsInitialized => _isInitialized;
        public string SessionId => _sessionId;

        public event Action<string>? OnSessionStarted;
        public event Action<string>? OnSessionEnded;
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
                    _webSocketManager = FindFirstObjectByType<WebSocketManager>();
                    
                if (_voiceManager == null)
                    _voiceManager = FindFirstObjectByType<VoiceManager>();
                
                // 이벤트 구독
                if (_webSocketManager != null)
                {
                    _webSocketManager.OnSessionIdReceived += ProcessSessionIdMessage;
                    _webSocketManager.OnChatMessageReceived += HandleChatMessageReceived;
                }
                
                if (_voiceManager != null)
                {
                    _voiceManager.OnVoiceFinished += OnVoiceFinished;
                }
                
                _isInitialized = true;
                Debug.Log("ChatManager 초기화 완료");
                
                if (_autoConnect)
                {
                    StartNewSession();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ChatManager 초기화 실패: {ex.Message}");
                OnError?.Invoke($"초기화 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 새로운 세션 시작
        /// </summary>
        public async void StartNewSession()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("ChatManager가 초기화되지 않았습니다.");
                return;
            }
            
            try
            {
                if (_webSocketManager != null)
                {
                    await _webSocketManager.ConnectAsync();
                    _isConnected = true;
                    Debug.Log("새로운 채팅 세션 시작 - 세션 ID 대기 중");
                }
                else
                {
                    Debug.LogError("WebSocketManager가 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"세션 시작 실패: {ex.Message}");
                OnError?.Invoke($"세션 시작 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 세션 종료
        /// </summary>
        public async void EndSession()
        {
            try
            {
                if (_webSocketManager != null)
                {
                    await _webSocketManager.DisconnectAsync();
                    _isConnected = false;
                    _sessionId = string.Empty;
                    
                    OnSessionEnded?.Invoke(_sessionId);
                    Debug.Log("채팅 세션 종료");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"세션 종료 실패: {ex.Message}");
                OnError?.Invoke($"세션 종료 실패: {ex.Message}");
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
                
            if (string.IsNullOrEmpty(_sessionId))
            {
                Debug.LogWarning("세션 ID가 없습니다. WebSocket 연결을 먼저 완료해주세요.");
                OnError?.Invoke("세션 ID가 없습니다. WebSocket 연결을 먼저 완료해주세요.");
                return;
            }
                
            try
            {
                Debug.Log($"사용자 메시지 전송: {message} (세션 ID: {_sessionId})");
                
                // ChatApiService를 통해 HTTP API 호출
                var chatService = ApiServiceManager.Instance.Chat;
                var response = await chatService.SendChatAsync(
                    message: message,
                    characterId: _characterId,
                    userId: _userId,
                    sessionId: _sessionId
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
        /// 세션 ID 메시지 처리
        /// </summary>
        /// <param name="sessionId">수신된 세션 ID</param>
        public void ProcessSessionIdMessage(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                Debug.LogWarning("빈 세션 ID를 받았습니다.");
                return;
            }
            
            _sessionId = sessionId;
            OnSessionStarted?.Invoke(sessionId);
            
            Debug.Log($"세션 ID 수신: {sessionId}");
        }
        
        /// <summary>
        /// 채팅 메시지 처리
        /// </summary>
        /// <param name="chatMessage">수신된 채팅 메시지</param>
        public void ProcessCharacterMessage(ChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                Debug.LogWarning("빈 채팅 메시지를 받았습니다.");
                return;
            }
            
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
                _webSocketManager.OnSessionIdReceived -= ProcessSessionIdMessage;
                _webSocketManager.OnChatMessageReceived -= HandleChatMessageReceived;
            }
            
            if (_voiceManager != null)
            {
                _voiceManager.OnVoiceFinished -= OnVoiceFinished;
            }
            
            EndSession();
        }
    }
} 