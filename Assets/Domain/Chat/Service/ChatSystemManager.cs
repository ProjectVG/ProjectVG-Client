#nullable enable
using System;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Domain.Chat.View;
using ProjectVG.Domain.Character.Service;


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
        [SerializeField] private CharacterManager? _chracterManager;

        [Header("Chat Settings")]
        [SerializeField] private string _characterId = "44444444-4444-4444-4444-444444444444";
        [SerializeField] private string _userId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        private WebSocketManager? _webSocketManager;
        private AudioManager? _audioManager;
        private ChatMessageQueue? _messageQueue;
        private ChatApiService? _chatApiService;

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        public event Action<string>? OnError;

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
                if (_chatApiService != null) {
                    var response = await _chatApiService.SendChatAsync(
                        message: message,
                        characterId: _characterId,
                        userId: _userId
                    );
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

                // TODO : 캐릭터 반응을 전달한다. 

                if (chatMessage.VoiceData != null && _audioManager != null) {
                    await _audioManager.PlayVoiceAsync(chatMessage.VoiceData);
                }
            }
            catch (Exception ex) {
                Debug.LogError($"[ChatSystemManager] 캐릭터 메시지 처리 실패: {ex.Message}");
                OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
            }
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
