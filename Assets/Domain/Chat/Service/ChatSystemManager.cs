#nullable enable
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Domain.Chat.View;
using ProjectVG.Domain.Character.Service;
using ProjectVG.Infrastructure.Network.DTOs.Chat;


namespace ProjectVG.Domain.Chat.Service
{
    /// <summary>
    /// 대화 메시지를 전송하고 받으면 처리한다.
    /// 대화 메시지를 처리할때 S시간 동안 액션과 Voice를 점유하고 다음 행동을 수행한다.
    /// </summary>
    public class ChatSystemManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private ChatBubblePanel? _chatBubblePanel;
        [SerializeField] private CharacterManager? _characterManager;

        [Header("Chat Settings")]
        [SerializeField] private string _characterId = "11111111-1111-1111-1111-111111111111";

        private WebSocketManager? _webSocketManager;
        private AudioManager? _audioManager;
        private ChatMessageQueue? _messageQueue;
        private ChatApiService? _chatApiService;

        private bool _isInitialized = false;
        private CancellationTokenSource _cts = new();
        public bool IsInitialized => _isInitialized;

        public event Action<string>? OnError;
        public event Action? OnConversationEnd;

        #region Unity Lifecycle

        private static ChatSystemManager? _instance;
        public static ChatSystemManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindAnyObjectByType<ChatSystemManager>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            StartCoroutine(InitializeWhenReady());
        }

        private IEnumerator InitializeWhenReady()
        {
            float timeout = 5f;
            float startTime = Time.realtimeSinceStartup;

            while (_chatBubblePanel == null && (Time.realtimeSinceStartup - startTime) < timeout) {
                yield return new WaitForSecondsRealtime(0.1f);
            }

            if (_chatBubblePanel == null) {
                Debug.LogError("[ChatSystemManager] ChatBubblePanel 초기화 타임아웃");
                yield break;
            }

            Initialize();
        }
        public void Initialize()
        {
            if (_isInitialized) return;

            try {
                _webSocketManager = WebSocketManager.Instance;
                _audioManager = AudioManager.Instance;
                _messageQueue = new ChatMessageQueue();
                _chatApiService = ApiServiceManager.Instance.Chat;

                if (_messageQueue != null) {
                    _messageQueue.OnError += (msg) => OnError?.Invoke(msg);
                }

                if (_webSocketManager != null) {
                    _webSocketManager.OnChatMessageReceived += ProcessCharacterMessage;
                }

                _isInitialized = true;
                Debug.Log("[ChatSystemManager] 초기화 완료");
            }
            catch (Exception ex) {
                Debug.LogError($"[ChatSystemManager] 초기화 실패: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
            if (_webSocketManager != null) {
                _webSocketManager.OnChatMessageReceived -= ProcessCharacterMessage;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 유저 메시지를 처리한다
        /// </summary>
        /// <param name="message">User 메시지</param>
        public async void SendUserMessage(string message)
        {
            if (!ValidateUserInput(message)) { return; }

            try {
                // 유저 메시지 전송 시 캐릭터를 Listen 상태로 변경
                if (_characterManager != null) {
                    var listenAction = new CharacterActionData(CharacterActionType.Listen);
                    _characterManager.PlayAction(listenAction);
                }

                if (_chatApiService != null) {
                    ChatRequest chatRequest = new() {
                        Message = message,
                        CharacterId = _characterId,
                        UseTTS = true,
                        RequestAt = DateTime.Now
                    };
                    var response = await _chatApiService.SendChatAsync(chatRequest);
                    if (response == null) {
                        Debug.LogWarning("[ChatSystemManager] 채팅 응답이 null입니다.");
                    }
                }
                if (_chatBubblePanel != null) {
                    _chatBubblePanel.CreateBubble(Actor.User, message);
                }
            }
            catch (Exception ex) {
                // todo : 메시지 실패 처리
                Debug.LogError($"[ChatSystemManager] 메시지 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 캐릭터 메시지를 처리한다
        /// </summary>
        /// <param name="chatMessage">캐릭터 메시지</param>
        public void ProcessCharacterMessage(ChatMessage chatMessage)
        {
            if (chatMessage == null) {
                Debug.LogWarning("[ChatSystemManager] 빈 채팅 메시지를 받았습니다.");
                return;
            }

            if (_messageQueue == null) {
                Debug.LogError("[ChatSystemManager] 메시지 큐가 초기화되지 않았습니다.");
                return;
            }

            if (_messageQueue.Enqueue(chatMessage)) {
                _messageQueue.ProcessQueueAsync(ProcessMessageAsync).Forget();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 메시지를 비동기적으로 처리한다
        /// </summary>
        /// <param name="chatMessage"></param>
        /// <returns></returns>
        private async UniTask ProcessMessageAsync(ChatMessage chatMessage)
        {
            try {
                if (_chatBubblePanel != null && !string.IsNullOrEmpty(chatMessage.Text)) {
                    _chatBubblePanel.CreateBubble(Actor.Character, chatMessage.Text);
                }

                if (_characterManager != null) {
                    _characterManager.PlayAction(chatMessage.ActionData);
                }


                if (chatMessage.VoiceData != null && _audioManager != null) {
                    _audioManager.PlayVoiceAsync(chatMessage.VoiceData).Forget();
                }

                float waitTime = CalculateConversationWaitTime(chatMessage);
                await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: _cts.Token);

                // 대화 종료 시 캐릭터를 Idle 상태로 변경
                if (_characterManager != null) {
                    var idleAction = new CharacterActionData(CharacterActionType.Idle);
                    _characterManager.PlayAction(idleAction);
                }

                OnConversationEnd?.Invoke();
            }
            catch (Exception ex) {
                Debug.LogError($"[ChatSystemManager] 캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 대화 대기 시간을 계산한다
        /// </summary>
        private float CalculateConversationWaitTime(ChatMessage chatMessage)
        {
            float baseTime = 0f;

            if (chatMessage.VoiceData != null && chatMessage.VoiceData.IsPlayable()) {
                baseTime = chatMessage.VoiceData.Length;
            }

            if (baseTime <= 0f) {
                baseTime = 2f;
            }

            return baseTime + 0.5f;
        }

        private bool ValidateUserInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) {
                Debug.LogWarning("[ChatSystemManager] 빈 메시지는 전송할 수 없습니다.");
                return false;
            }
            if (message.Length > 1000) {
                Debug.LogWarning("[ChatSystemManager] 메시지가 너무 깁니다. (최대 1000자)");
                return false;
            }
            return true;
        }

        #endregion
    }
}
