using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectVG.Tests.Runtime
{
    /// <summary>
    /// 네트워크 테스트를 위한 UI 매니저
    /// 더미 클라이언트 방식 테스트를 지원합니다.
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
        [SerializeField] private Button dummyClientTestButton;
        [SerializeField] private Button autoTestButton;
        
        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private ScrollRect logScrollRect;
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField characterIdInput;
        [SerializeField] private TMP_InputField userIdInput;
        [SerializeField] private TMP_InputField messageInput;
        
        [Header("Test Settings")]
        [SerializeField] private Toggle autoTestToggle;
        [SerializeField] private Slider testIntervalSlider;
        [SerializeField] private TextMeshProUGUI intervalText;
        
        private NetworkTestManager _testManager;
        private bool _isAutoTestRunning = false;

        private void Start()
        {
            _testManager = FindFirstObjectByType<NetworkTestManager>();
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
            
            if (dummyClientTestButton != null)
                dummyClientTestButton.onClick.AddListener(OnDummyClientTestButtonClicked);
            
            if (autoTestButton != null)
                autoTestButton.onClick.AddListener(OnAutoTestButtonClicked);

            // 초기값 설정
            if (characterIdInput != null)
                characterIdInput.text = "44444444-4444-4444-4444-444444444444"; // 제로
            
            if (userIdInput != null)
                userIdInput.text = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
            
            if (messageInput != null)
                messageInput.text = "안녕하세요! 테스트 메시지입니다.";

            // 자동 테스트 설정
            if (autoTestToggle != null)
            {
                autoTestToggle.isOn = _testManager.AutoTest;
                autoTestToggle.onValueChanged.AddListener(OnAutoTestToggleChanged);
            }
            
            if (testIntervalSlider != null)
            {
                testIntervalSlider.minValue = 5f;
                testIntervalSlider.maxValue = 30f;
                testIntervalSlider.value = _testManager.TestInterval;
                testIntervalSlider.onValueChanged.AddListener(OnTestIntervalChanged);
                UpdateIntervalText();
            }

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
            
            if (dummyClientTestButton != null)
                dummyClientTestButton.interactable = !_isAutoTestRunning;
            
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

        private void UpdateIntervalText()
        {
            if (intervalText != null && testIntervalSlider != null)
            {
                intervalText.text = $"테스트 간격: {testIntervalSlider.value:F1}초";
            }
        }

        #region Button Event Handlers

        private void OnConnectButtonClicked()
        {
            AddLog("WebSocket 연결 시도 (더미 클라이언트 방식)...");
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
            AddLog("HTTP 채팅 요청 전송 (더미 클라이언트 방식)...");
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

        private void OnDummyClientTestButtonClicked()
        {
            AddLog("더미 클라이언트 방식 전체 테스트 시작...");
            UpdateStatus("더미 클라이언트 테스트 실행 중...");
            _testManager.RunDummyClientTest();
        }

        private void OnAutoTestButtonClicked()
        {
            if (!_isAutoTestRunning)
            {
                _isAutoTestRunning = true;
                AddLog("자동 테스트 시작 (더미 클라이언트 방식)...");
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

        private void OnAutoTestToggleChanged(bool isOn)
        {
            _testManager.AutoTest = isOn;
            AddLog($"자동 테스트 설정: {(isOn ? "활성화" : "비활성화")}");
        }

        private void OnTestIntervalChanged(float value)
        {
            _testManager.TestInterval = value;
            UpdateIntervalText();
            AddLog($"테스트 간격 변경: {value:F1}초");
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

        public void OnReconnectAttempt(int attempt, int maxAttempts)
        {
            AddLog($"🔄 재연결 시도 {attempt}/{maxAttempts}");
            UpdateStatus($"재연결 시도 중... ({attempt}/{maxAttempts})");
        }

        public void OnReconnectFailed()
        {
            AddLog("❌ 최대 재연결 시도 횟수 초과");
            UpdateStatus("재연결 실패");
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
            
            if (dummyClientTestButton != null)
                dummyClientTestButton.onClick.RemoveListener(OnDummyClientTestButtonClicked);
            
            if (autoTestButton != null)
                autoTestButton.onClick.RemoveListener(OnAutoTestButtonClicked);
            
            if (autoTestToggle != null)
                autoTestToggle.onValueChanged.RemoveListener(OnAutoTestToggleChanged);
            
            if (testIntervalSlider != null)
                testIntervalSlider.onValueChanged.RemoveListener(OnTestIntervalChanged);
        }
    }
} 