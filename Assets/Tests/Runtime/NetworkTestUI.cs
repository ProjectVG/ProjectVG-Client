using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectVG.Tests.Runtime
{
    /// <summary>
    /// 네트워크 테스트를 위한 UI 매니저
    /// </summary>
    public class NetworkTestUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Button chatRequestButton;
        [SerializeField] private Button characterInfoButton;
        [SerializeField] private Button webSocketMessageButton;
        [SerializeField] private Button fullTestButton;
        [SerializeField] private Button autoTestButton;
        
        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private ScrollRect logScrollRect;
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField sessionIdInput;
        [SerializeField] private TMP_InputField characterIdInput;
        [SerializeField] private TMP_InputField userIdInput;
        [SerializeField] private TMP_InputField messageInput;
        
        private NetworkTestManager _testManager;
        private bool _isAutoTestRunning = false;

        private void Start()
        {
            _testManager = FindObjectOfType<NetworkTestManager>();
            if (_testManager == null)
            {
                Debug.LogError("NetworkTestManager를 찾을 수 없습니다!");
                return;
            }

            InitializeUI();
            UpdateStatus("대기 중...");
        }

        private void InitializeUI()
        {
            // 버튼 이벤트 연결
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnectButtonClicked);
            
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
            
            if (chatRequestButton != null)
                chatRequestButton.onClick.AddListener(OnChatRequestButtonClicked);
            
            if (characterInfoButton != null)
                characterInfoButton.onClick.AddListener(OnCharacterInfoButtonClicked);
            
            if (webSocketMessageButton != null)
                webSocketMessageButton.onClick.AddListener(OnWebSocketMessageButtonClicked);
            
            if (fullTestButton != null)
                fullTestButton.onClick.AddListener(OnFullTestButtonClicked);
            
            if (autoTestButton != null)
                autoTestButton.onClick.AddListener(OnAutoTestButtonClicked);

            // 초기값 설정
            if (sessionIdInput != null)
                sessionIdInput.text = "test-session-123";
            
            if (characterIdInput != null)
                characterIdInput.text = "test-character-456";
            
            if (userIdInput != null)
                userIdInput.text = "test-user-789";
            
            if (messageInput != null)
                messageInput.text = "안녕하세요! 테스트 메시지입니다.";

            // 초기 버튼 상태 설정
            UpdateButtonStates(false);
        }

        private void UpdateButtonStates(bool isConnected)
        {
            if (connectButton != null)
                connectButton.interactable = !isConnected;
            
            if (disconnectButton != null)
                disconnectButton.interactable = isConnected;
            
            if (chatRequestButton != null)
                chatRequestButton.interactable = isConnected;
            
            if (characterInfoButton != null)
                characterInfoButton.interactable = true; // HTTP 요청은 연결 없이도 가능
            
            if (webSocketMessageButton != null)
                webSocketMessageButton.interactable = isConnected;
            
            if (fullTestButton != null)
                fullTestButton.interactable = !_isAutoTestRunning;
            
            if (autoTestButton != null)
            {
                autoTestButton.interactable = !_isAutoTestRunning;
                autoTestButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                    _isAutoTestRunning ? "자동 테스트 중지" : "자동 테스트 시작";
            }
        }

        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = $"상태: {status}";
            }
        }

        private void AddLog(string message)
        {
            if (logText != null)
            {
                logText.text += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
                
                // 스크롤을 맨 아래로 이동
                if (logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        #region Button Event Handlers

        private void OnConnectButtonClicked()
        {
            AddLog("WebSocket 연결 시도...");
            UpdateStatus("연결 중...");
            _testManager.ConnectWebSocket();
        }

        private void OnDisconnectButtonClicked()
        {
            AddLog("WebSocket 연결 해제...");
            UpdateStatus("연결 해제 중...");
            _testManager.DisconnectWebSocket();
        }

        private void OnChatRequestButtonClicked()
        {
            AddLog("HTTP 채팅 요청 전송...");
            UpdateStatus("채팅 요청 중...");
            _testManager.SendChatRequest();
        }

        private void OnCharacterInfoButtonClicked()
        {
            AddLog("HTTP 캐릭터 정보 요청...");
            UpdateStatus("캐릭터 정보 요청 중...");
            _testManager.GetCharacterInfo();
        }

        private void OnWebSocketMessageButtonClicked()
        {
            string message = messageInput != null ? messageInput.text : "테스트 메시지";
            AddLog($"WebSocket 메시지 전송: {message}");
            UpdateStatus("WebSocket 메시지 전송 중...");
            _testManager.SendWebSocketMessage();
        }

        private void OnFullTestButtonClicked()
        {
            AddLog("전체 테스트 시작...");
            UpdateStatus("전체 테스트 실행 중...");
            _testManager.RunFullTest();
        }

        private void OnAutoTestButtonClicked()
        {
            if (!_isAutoTestRunning)
            {
                _isAutoTestRunning = true;
                AddLog("자동 테스트 시작...");
                UpdateStatus("자동 테스트 실행 중...");
                UpdateButtonStates(true);
                
                // 자동 테스트 시작
                _testManager.AutoTest = true;
                _testManager.StartAutoTestFromUI();
            }
            else
            {
                _isAutoTestRunning = false;
                AddLog("자동 테스트 중지...");
                UpdateStatus("대기 중...");
                UpdateButtonStates(false);
                
                // 자동 테스트 중지
                _testManager.AutoTest = false;
            }
        }

        #endregion

        #region Public Methods for External Updates

        public void OnWebSocketConnected()
        {
            UpdateStatus("WebSocket 연결됨");
            UpdateButtonStates(true);
            AddLog("✅ WebSocket 연결 성공!");
        }

        public void OnWebSocketDisconnected()
        {
            UpdateStatus("WebSocket 연결 해제됨");
            UpdateButtonStates(false);
            AddLog("🔌 WebSocket 연결 해제됨!");
        }

        public void OnWebSocketError(string error)
        {
            UpdateStatus("WebSocket 오류");
            AddLog($"❌ WebSocket 오류: {error}");
        }

        public void OnChatMessageReceived(string message)
        {
            AddLog($"💬 채팅 메시지 수신: {message}");
        }

        public void OnSystemMessageReceived(string message)
        {
            AddLog($"🔧 시스템 메시지 수신: {message}");
        }

        public void OnSessionIdMessageReceived(string sessionId)
        {
            AddLog($"🆔 세션 ID 수신: {sessionId}");
        }

        public void OnHttpRequestSuccess(string operation)
        {
            AddLog($"✅ HTTP {operation} 성공!");
        }

        public void OnHttpRequestFailed(string operation, string error)
        {
            AddLog($"❌ HTTP {operation} 실패: {error}");
        }

        #endregion

        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (connectButton != null)
                connectButton.onClick.RemoveListener(OnConnectButtonClicked);
            
            if (disconnectButton != null)
                disconnectButton.onClick.RemoveListener(OnDisconnectButtonClicked);
            
            if (chatRequestButton != null)
                chatRequestButton.onClick.RemoveListener(OnChatRequestButtonClicked);
            
            if (characterInfoButton != null)
                characterInfoButton.onClick.RemoveListener(OnCharacterInfoButtonClicked);
            
            if (webSocketMessageButton != null)
                webSocketMessageButton.onClick.RemoveListener(OnWebSocketMessageButtonClicked);
            
            if (fullTestButton != null)
                fullTestButton.onClick.RemoveListener(OnFullTestButtonClicked);
            
            if (autoTestButton != null)
                autoTestButton.onClick.RemoveListener(OnAutoTestButtonClicked);
        }
    }
} 