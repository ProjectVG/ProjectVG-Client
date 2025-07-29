using UnityEngine;
using ProjectVG.Domain.Chat.Service;
using ProjectVG.Domain.Chat.Model;

namespace ProjectVG.Tests.Runtime
{
    public class ChatTestManager : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private ChatManager _chatManager;
        [SerializeField] private string _testMessage = "안녕하세요!";
        [SerializeField] private string _characterId = "test-character";
        [SerializeField] private string _userId = "test-user";
        
        [Header("Test Results")]
        [SerializeField] private bool _isConnected = false;
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private string _sessionId = "";
        
        private void Start()
        {
            if (_chatManager == null)
            {
                _chatManager = FindFirstObjectByType<ChatManager>();
            }
            
            if (_chatManager != null)
            {
                _chatManager.OnSessionStarted += OnSessionStarted;
                _chatManager.OnSessionEnded += OnSessionEnded;
                _chatManager.OnChatMessageReceived += OnChatMessageReceived;
                _chatManager.OnError += OnError;
            }
        }
        
        private void Update()
        {
            if (_chatManager != null)
            {
                _isConnected = _chatManager.IsConnected;
                _isInitialized = _chatManager.IsInitialized;
                _sessionId = _chatManager.SessionId;
            }
        }
        
        [ContextMenu("1. ChatManager 초기화")]
        public void InitializeChatManager()
        {
            if (_chatManager != null)
            {
                _chatManager.Initialize();
            }
            else
            {
                Debug.LogError("ChatManager가 없습니다.");
            }
        }
        
        [ContextMenu("2. 새 세션 시작")]
        public void StartNewSession()
        {
            if (_chatManager != null)
            {
                _chatManager.StartNewSession();
            }
            else
            {
                Debug.LogError("ChatManager가 없습니다.");
            }
        }
        
        [ContextMenu("3. 세션 종료")]
        public void EndSession()
        {
            if (_chatManager != null)
            {
                _chatManager.EndSession();
            }
            else
            {
                Debug.LogError("ChatManager가 없습니다.");
            }
        }
        
        [ContextMenu("4. 테스트 메시지 전송")]
        public void SendTestMessage()
        {
            if (_chatManager != null)
            {
                Debug.Log($"테스트 메시지 전송: {_testMessage}");
                Debug.Log($"캐릭터 ID: {_characterId}, 사용자 ID: {_userId}");
                _chatManager.SendUserMessage(_testMessage);
            }
            else
            {
                Debug.LogError("ChatManager가 없습니다.");
            }
        }
        
        [ContextMenu("5. 빈 메시지 전송 (테스트)")]
        public void SendEmptyMessage()
        {
            if (_chatManager != null)
            {
                _chatManager.SendUserMessage("");
            }
            else
            {
                Debug.LogError("ChatManager가 없습니다.");
            }
        }
        
        [ContextMenu("6. 긴 메시지 전송 (테스트)")]
        public void SendLongMessage()
        {
            if (_chatManager != null)
            {
                string longMessage = new string('A', 1500);
                _chatManager.SendUserMessage(longMessage);
            }
            else
            {
                Debug.LogError("ChatManager가 없습니다.");
            }
        }
        
        private void OnSessionStarted(string sessionId)
        {
            Debug.Log($"세션 시작됨: {sessionId}");
        }
        
        private void OnSessionEnded(string sessionId)
        {
            Debug.Log($"세션 종료됨: {sessionId}");
        }
        
        private void OnChatMessageReceived(ChatMessage chatMessage)
        {
            Debug.Log($"채팅 메시지 수신: {chatMessage.Text}");
            if (chatMessage.HasVoiceData())
            {
                Debug.Log($"음성 데이터 포함: {chatMessage.VoiceData.Format}, 길이: {chatMessage.VoiceData.Length:F2}초");
            }
        }
        
        private void OnError(string error)
        {
            Debug.LogError($"ChatManager 에러: {error}");
        }
        
        private void OnDestroy()
        {
            if (_chatManager != null)
            {
                _chatManager.OnSessionStarted -= OnSessionStarted;
                _chatManager.OnSessionEnded -= OnSessionEnded;
                _chatManager.OnChatMessageReceived -= OnChatMessageReceived;
                _chatManager.OnError -= OnError;
            }
        }
    }
} 