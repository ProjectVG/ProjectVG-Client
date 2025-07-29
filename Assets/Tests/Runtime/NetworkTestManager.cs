using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Configs;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using ProjectVG.Domain.Chat;

namespace ProjectVG.Tests.Runtime
{
    /// <summary>
    /// WebSocket + HTTP 통합 테스트 매니저
    /// 더미 클라이언트와 동일한 방식으로 테스트합니다.
    /// </summary>
    public class NetworkTestManager : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private string testCharacterId = "44444444-4444-4444-4444-444444444444"; // 제로
        [SerializeField] private string testUserId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        [SerializeField] private string testMessage = "안녕하세요! 테스트 메시지입니다.";
        
        [Header("자동 테스트")]
        [SerializeField] private bool autoTest = false;
        [SerializeField] private float testInterval = 15f; // 더 긴 간격으로 변경
        
        // UI에서 접근할 수 있도록 public 프로퍼티 추가
        public bool AutoTest
        {
            get => autoTest;
            set => autoTest = value;
        }
        
        public float TestInterval
        {
            get => testInterval;
            set => testInterval = value;
        }
        
        private WebSocketManager _webSocketManager;
        private ApiServiceManager _apiServiceManager;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isTestRunning = false;
        private string _receivedSessionId = null;
        private bool _chatResponseReceived = false;
        private string _lastChatResponse = null;
        private int _reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 3;
        private bool _isIntentionalDisconnect = false; // 의도적인 연결 해제 여부

        private void Awake()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            // HTTP 연결 허용 설정
            #if UNITY_EDITOR || UNITY_STANDALONE
            UnityEngine.Networking.UnityWebRequest.ClearCookieCache();
            #endif
            
            // 매니저들이 없으면 생성
            EnsureManagersExist();
            
            InitializeManagers();
        }
        
        /// <summary>
        /// 필요한 매니저들이 존재하는지 확인하고 없으면 생성
        /// </summary>
        private void EnsureManagersExist()
        {
            // HttpApiClient가 없으면 생성
            if (HttpApiClient.Instance == null)
            {
                Debug.Log("HttpApiClient를 생성합니다...");
                var httpApiClientGO = new GameObject("HttpApiClient");
                httpApiClientGO.AddComponent<HttpApiClient>();
                DontDestroyOnLoad(httpApiClientGO);
            }
            
            // WebSocketManager가 없으면 생성
            if (WebSocketManager.Instance == null)
            {
                Debug.Log("WebSocketManager를 생성합니다...");
                var webSocketManagerGO = new GameObject("WebSocketManager");
                webSocketManagerGO.AddComponent<WebSocketManager>();
                DontDestroyOnLoad(webSocketManagerGO);
            }
            
            // ApiServiceManager가 없으면 생성
            if (ApiServiceManager.Instance == null)
            {
                Debug.Log("ApiServiceManager를 생성합니다...");
                var apiServiceManagerGO = new GameObject("ApiServiceManager");
                apiServiceManagerGO.AddComponent<ApiServiceManager>();
                DontDestroyOnLoad(apiServiceManagerGO);
            }
        }

        private void Start()
        {
            if (autoTest)
            {
                StartAutoTest().Forget();
            }
        }

        /// <summary>
        /// UI에서 자동 테스트를 시작할 수 있도록 public 메서드 제공
        /// </summary>
        public void StartAutoTestFromUI()
        {
            if (autoTest)
            {
                StartAutoTest().Forget();
            }
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private void InitializeManagers()
        {
            try
            {
                // NetworkConfig 초기화 (앱 시작 시 환경 설정)
                NetworkConfig.SetDevelopmentEnvironment();
                
                // WebSocket 매니저 초기화
                _webSocketManager = WebSocketManager.Instance;
                if (_webSocketManager == null)
                {
                    Debug.LogError("WebSocketManager.Instance가 null입니다. 매니저가 생성되지 않았습니다.");
                    return;
                }
                
                Debug.Log($"WebSocket 설정 적용: {NetworkConfig.GetWebSocketUrl()}");
                Debug.Log($"현재 환경: {NetworkConfig.CurrentEnvironment}");
                
                // HTTP API 클라이언트 설정
                if (HttpApiClient.Instance != null)
                {
                    Debug.Log($"API 설정 적용: {NetworkConfig.GetFullApiUrl("chat")}");
                }
                
                // API 서비스 매니저 초기화
                _apiServiceManager = ApiServiceManager.Instance;
                if (_apiServiceManager == null)
                {
                    Debug.LogError("ApiServiceManager.Instance가 null입니다. 매니저가 생성되지 않았습니다.");
                    return;
                }
                
                // WebSocket 이벤트 구독
                _webSocketManager.OnConnected += OnWebSocketConnected;
                _webSocketManager.OnDisconnected += OnWebSocketDisconnected;
                _webSocketManager.OnError += OnWebSocketError;
                _webSocketManager.OnSessionIdReceived += OnSessionIdReceived;
                _webSocketManager.OnChatMessageReceived += OnChatMessageReceived;
                
                Debug.Log("NetworkTestManager 초기화 완료");
                NetworkConfig.LogCurrentSettings();
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkTestManager 초기화 중 오류: {ex.Message}");
            }
        }

        #region 수동 테스트 메서드들

        [ContextMenu("1. WebSocket 연결 (더미 클라이언트 방식)")]
        public async void ConnectWebSocket()
        {
            if (_isTestRunning)
            {
                Debug.LogWarning("테스트가 이미 실행 중입니다.");
                return;
            }

            if (_webSocketManager == null)
            {
                Debug.LogError("WebSocketManager가 초기화되지 않았습니다.");
                return;
            }

            try
            {
                Debug.Log("=== WebSocket 연결 시작 (더미 클라이언트 방식) ===");
                
                // 현재 설정 정보 출력
                Debug.Log($"현재 환경: {NetworkConfig.CurrentEnvironment}");
                Debug.Log($"WebSocket 서버: {NetworkConfig.GetWebSocketUrl()}");
                
                _receivedSessionId = null; // 세션 ID 초기화
                _reconnectAttempts = 0;
                _isIntentionalDisconnect = false; // 의도적 해제 플래그 초기화
                
                // 더미 클라이언트처럼 세션 ID 없이 연결
                bool connected = await _webSocketManager.ConnectAsync();
                
                if (connected)
                {
                    Debug.Log("✅ WebSocket 연결 성공! 세션 ID 대기 중...");
                }
                else
                {
                    Debug.LogError("❌ WebSocket 연결 실패!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 연결 중 오류: {ex.Message}");
            }
        }

        [ContextMenu("2. HTTP 채팅 요청 (더미 클라이언트 방식)")]
        public async void SendChatRequest()
        {
            if (_webSocketManager == null || !_webSocketManager.IsConnected)
            {
                Debug.LogWarning("WebSocket이 연결되지 않았습니다. 먼저 연결해주세요.");
                return;
            }

            if (_apiServiceManager == null)
            {
                Debug.LogError("ApiServiceManager가 초기화되지 않았습니다.");
                return;
            }

            // 세션 ID가 아직 수신되지 않았으면 대기
            if (string.IsNullOrEmpty(_receivedSessionId))
            {
                Debug.LogError("세션 ID가 없습니다. WebSocket에서 세션 ID를 먼저 받아야 합니다.");
                return;
            }

            try
            {
                Debug.Log("=== HTTP 채팅 요청 시작 (더미 클라이언트 방식) ===");
                
                // 현재 설정 정보 출력
                Debug.Log($"현재 환경: {NetworkConfig.CurrentEnvironment}");
                Debug.Log($"API 서버: {NetworkConfig.GetFullApiUrl("chat")}");
                Debug.Log($"세션 ID: {_receivedSessionId}");
                
                var chatRequest = new ChatRequest
                {
                    message = testMessage,
                    characterId = testCharacterId,
                    userId = testUserId,
                    sessionId = _receivedSessionId, // WebSocket에서 받은 세션 ID 사용
                    actor = "web_user",
                    action = "chat", // 클라이언트와 동일하게 명시적으로 설정
                    requestedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var response = await _apiServiceManager.Chat.SendChatAsync(chatRequest);
                
                if (response != null)
                {
                    Debug.Log($"✅ HTTP 채팅 요청 성공!");
                    Debug.Log($"   - 세션 ID: {_receivedSessionId}");
                    Debug.Log($"   - 캐릭터 ID: {testCharacterId}");
                    Debug.Log($"   - 사용자 ID: {testUserId}");
                }
                else
                {
                    Debug.LogError($"❌ HTTP 채팅 요청 실패: 응답이 null입니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HTTP 채팅 요청 중 오류: {ex.Message}");
            }
        }

        [ContextMenu("3. HTTP 캐릭터 정보 요청")]
        public async void GetCharacterInfo()
        {
            try
            {
                Debug.Log("=== HTTP 캐릭터 정보 요청 시작 ===");
                
                var character = await _apiServiceManager.Character.GetCharacterAsync(testCharacterId);
                
                if (character != null)
                {
                    Debug.Log($"✅ 캐릭터 정보 조회 성공!");
                    Debug.Log($"   - ID: {character.id}");
                    Debug.Log($"   - 이름: {character.name}");
                    Debug.Log($"   - 설명: {character.description}");
                    Debug.Log($"   - 역할: {character.role}");
                    Debug.Log($"   - 활성화: {character.isActive}");
                }
                else
                {
                    Debug.LogError($"❌ 캐릭터 정보 조회 실패: 캐릭터를 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HTTP 캐릭터 정보 요청 중 오류: {ex.Message}");
            }
        }

        [ContextMenu("4. WebSocket 메시지 전송")]
        public async void SendWebSocketMessage()
        {
            if (!_webSocketManager.IsConnected)
            {
                Debug.LogWarning("WebSocket이 연결되지 않았습니다.");
                return;
            }

            try
            {
                Debug.Log("=== WebSocket 메시지 전송 시작 ===");
                Debug.LogWarning("WebSocket 메시지 전송 기능이 제거되었습니다. HTTP API를 사용하세요.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 메시지 전송 중 오류: {ex.Message}");
            }
        }

        [ContextMenu("5. WebSocket 연결 해제")]
        public async void DisconnectWebSocket()
        {
            try
            {
                Debug.Log("=== WebSocket 연결 해제 시작 ===");
                _isIntentionalDisconnect = true; // 의도적 해제 플래그 설정
                await _webSocketManager.DisconnectAsync();
                _receivedSessionId = null; // 세션 ID 초기화
                Debug.Log("✅ WebSocket 연결 해제 완료!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 연결 해제 중 오류: {ex.Message}");
            }
        }

        [ContextMenu("더미 클라이언트 방식 전체 테스트")]
        public async void RunDummyClientTest()
        {
            if (_isTestRunning)
            {
                Debug.LogWarning("테스트가 이미 실행 중입니다.");
                return;
            }

            _isTestRunning = true;
            
            try
            {
                Debug.Log("🚀 === 더미 클라이언트 방식 전체 테스트 시작 ===");
                
                // 현재 설정 정보 출력
                Debug.Log($"테스트 환경: {NetworkConfig.CurrentEnvironment}");
                Debug.Log($"API 서버: {NetworkConfig.GetFullApiUrl("")}");
                Debug.Log($"WebSocket 서버: {NetworkConfig.GetWebSocketUrl()}");
                
                // 0. 기존 연결이 있으면 해제
                if (_webSocketManager.IsConnected)
                {
                    Debug.Log("0️⃣ 기존 연결 해제 중...");
                    _isIntentionalDisconnect = true; // 의도적 해제 플래그 설정
                    await _webSocketManager.DisconnectAsync();
                    await UniTask.Delay(1000); // 연결 해제 완료 대기
                }
                
                // 1. WebSocket 연결 (세션 ID 없이)
                Debug.Log("1️⃣ WebSocket 연결 중...");
                bool connected = await _webSocketManager.ConnectAsync();
                if (!connected)
                {
                    Debug.LogError("WebSocket 연결 실패로 테스트 중단");
                    return;
                }
                
                // 2. 세션 ID 수신 대기 (최대 10초)
                Debug.Log("2️⃣ 세션 ID 수신 대기 중...");
                int waitCount = 0;
                while (string.IsNullOrEmpty(_receivedSessionId) && waitCount < 100) // 10초로 증가
                {
                    await UniTask.Delay(100);
                    waitCount++;
                    if (waitCount % 10 == 0) // 1초마다 로그
                    {
                        Debug.Log($"2️⃣ 세션 ID 대기 중... ({waitCount/10}초 경과)");
                    }
                }
                
                if (string.IsNullOrEmpty(_receivedSessionId))
                {
                    Debug.LogError("세션 ID를 받지 못했습니다. (10초 타임아웃)");
                    Debug.LogWarning("서버에서 세션 ID 메시지를 보내지 않았거나, 메시지 형식이 다를 수 있습니다.");
                    return;
                }
                
                Debug.Log($"✅ 세션 ID 수신: {_receivedSessionId}");
                
                await UniTask.Delay(1000); // 안정화 대기
                
                // 3. HTTP 채팅 요청 (세션 ID 포함)
                Debug.Log("3️⃣ HTTP 채팅 요청 중...");
                await SendChatRequestInternal();
                
                // 채팅 응답을 기다림
                await WaitForChatResponse(15); // 15초 타임아웃
                
                // 4. HTTP 캐릭터 정보 요청
                Debug.Log("4️⃣ HTTP 캐릭터 정보 요청 중...");
                await GetCharacterInfoInternal();
                
                await UniTask.Delay(1000);
                
                // 5. WebSocket 연결 해제
                Debug.Log("5️⃣ WebSocket 연결 해제 중...");
                _isIntentionalDisconnect = true; // 의도적 해제 플래그 설정
                await _webSocketManager.DisconnectAsync();
                _receivedSessionId = null;
                
                // 연결 해제 후 충분한 대기 시간
                await UniTask.Delay(2000);
                
                Debug.Log("✅ === 더미 클라이언트 방식 전체 테스트 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"더미 클라이언트 방식 전체 테스트 중 오류: {ex.Message}");
            }
            finally
            {
                _isTestRunning = false;
            }
        }

        [ContextMenu("전체 테스트 실행")]
        public async void RunFullTest()
        {
            if (_isTestRunning)
            {
                Debug.LogWarning("테스트가 이미 실행 중입니다.");
                return;
            }

            _isTestRunning = true;
            
            try
            {
                Debug.Log("🚀 === 전체 테스트 시작 ===");
                
                // 현재 설정 정보 출력
                Debug.Log($"테스트 환경: {NetworkConfig.CurrentEnvironment}");
                Debug.Log($"API 서버: {NetworkConfig.GetFullApiUrl("")}");
                Debug.Log($"WebSocket 서버: {NetworkConfig.GetWebSocketUrl()}");
                
                // 1. WebSocket 연결
                Debug.Log("1️⃣ WebSocket 연결 중...");
                bool connected = await _webSocketManager.ConnectAsync();
                if (!connected)
                {
                    Debug.LogError("WebSocket 연결 실패로 테스트 중단");
                    return;
                }
                
                // 2. 세션 ID 수신 대기 (최대 10초)
                Debug.Log("2️⃣ 세션 ID 수신 대기 중...");
                int waitCount = 0;
                while (string.IsNullOrEmpty(_receivedSessionId) && waitCount < 100)
                {
                    await UniTask.Delay(100);
                    waitCount++;
                    if (waitCount % 10 == 0) // 1초마다 로그
                    {
                        Debug.Log($"2️⃣ 세션 ID 대기 중... ({waitCount/10}초 경과)");
                    }
                }
                
                if (string.IsNullOrEmpty(_receivedSessionId))
                {
                    Debug.LogError("세션 ID를 받지 못했습니다. (10초 타임아웃)");
                    return;
                }
                
                Debug.Log($"✅ 세션 ID 수신: {_receivedSessionId}");
                await UniTask.Delay(1000); // 안정화 대기
                
                // 3. HTTP 채팅 요청
                Debug.Log("3️⃣ HTTP 채팅 요청 중...");
                await SendChatRequestInternal();
                
                // 채팅 응답을 기다림
                await WaitForChatResponse(15); // 15초 타임아웃
                
                // 4. HTTP 캐릭터 정보 요청
                Debug.Log("4️⃣ HTTP 캐릭터 정보 요청 중...");
                await GetCharacterInfoInternal();
                
                await UniTask.Delay(1000);
                
                // 5. WebSocket 메시지 전송 (기능 제거됨)
                Debug.Log("5️⃣ WebSocket 메시지 전송 기능이 제거되었습니다.");
                
                await UniTask.Delay(1000);
                
                // 6. WebSocket 연결 해제
                Debug.Log("6️⃣ WebSocket 연결 해제 중...");
                _isIntentionalDisconnect = true; // 의도적 해제 플래그 설정
                await _webSocketManager.DisconnectAsync();
                
                Debug.Log("✅ === 전체 테스트 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"전체 테스트 중 오류: {ex.Message}");
            }
            finally
            {
                _isTestRunning = false;
            }
        }

        [ContextMenu("현재 네트워크 설정 정보 출력")]
        public void LogCurrentNetworkConfig()
        {
            Debug.Log("=== 현재 네트워크 설정 정보 ===");
            NetworkConfig.LogCurrentSettings();
        }

        #endregion

        #region 자동 테스트

        private async UniTaskVoid StartAutoTest()
        {
            Debug.Log("🔄 자동 테스트 시작...");
            
            // 매니저 초기화 확인
            if (_webSocketManager == null || _apiServiceManager == null)
            {
                Debug.LogError("매니저가 초기화되지 않았습니다. 자동 테스트를 중단합니다.");
                return;
            }
            
            // HttpApiClient 확인
            if (HttpApiClient.Instance == null)
            {
                Debug.LogError("HttpApiClient가 초기화되지 않았습니다. 자동 테스트를 중단합니다.");
                return;
            }
            
            // 현재 설정 정보 출력
            Debug.Log($"자동 테스트 환경: {NetworkConfig.CurrentEnvironment}");
            Debug.Log($"API 서버: {NetworkConfig.GetFullApiUrl("")}");
            Debug.Log($"WebSocket 서버: {NetworkConfig.GetWebSocketUrl()}");
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await RunDummyClientTestInternal();
                    await UniTask.Delay(TimeSpan.FromSeconds(testInterval), cancellationToken: _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("자동 테스트가 취소되었습니다.");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"자동 테스트 중 오류: {ex.Message}");
                    Debug.LogError($"스택 트레이스: {ex.StackTrace}");
                    await UniTask.Delay(5000, cancellationToken: _cancellationTokenSource.Token);
                }
            }
            
            Debug.Log("🔄 자동 테스트 종료");
        }

        private async UniTask RunDummyClientTestInternal()
        {
            // 매니저 null 체크
            if (_webSocketManager == null)
            {
                Debug.LogError("WebSocketManager가 null입니다. 초기화를 확인해주세요.");
                return;
            }
            
            if (_apiServiceManager == null)
            {
                Debug.LogError("ApiServiceManager가 null입니다. 초기화를 확인해주세요.");
                return;
            }

            try
            {
                // 현재 설정 정보 출력
                Debug.Log($"자동 테스트 환경: {NetworkConfig.CurrentEnvironment}");
                
                // 0. 기존 연결이 있으면 해제
                if (_webSocketManager.IsConnected)
                {
                    Debug.Log("자동 테스트 - 기존 연결 해제 중...");
                    _isIntentionalDisconnect = true; // 의도적 해제 플래그 설정
                    await _webSocketManager.DisconnectAsync();
                    await UniTask.Delay(1000);
                }
                
                // 1. WebSocket 연결 (세션 ID 없이)
                Debug.Log("자동 테스트 - WebSocket 연결 중...");
                bool connected = await _webSocketManager.ConnectAsync();
                if (!connected)
                {
                    Debug.LogError("자동 테스트 - WebSocket 연결 실패");
                    return;
                }
                
                // 2. 세션 ID 수신 대기
                Debug.Log("자동 테스트 - 세션 ID 수신 대기 중...");
                int waitCount = 0;
                while (string.IsNullOrEmpty(_receivedSessionId) && waitCount < 100) // 10초로 증가
                {
                    await UniTask.Delay(100);
                    waitCount++;
                    if (waitCount % 10 == 0)
                    {
                        Debug.Log($"자동 테스트 - 세션 ID 대기 중... ({waitCount/10}초 경과)");
                    }
                }
                
                if (string.IsNullOrEmpty(_receivedSessionId))
                {
                    Debug.LogError("자동 테스트 - 세션 ID를 받지 못했습니다. (10초 타임아웃)");
                    Debug.LogWarning("서버에서 세션 ID 메시지를 보내지 않았거나, 메시지 형식이 다를 수 있습니다.");
                    return;
                }
                
                Debug.Log($"자동 테스트 - 세션 ID 수신: {_receivedSessionId}");
                await UniTask.Delay(500);
                
                // 3. HTTP 채팅 요청 (세션 ID 포함)
                Debug.Log("자동 테스트 - HTTP 채팅 요청 중...");
                await SendChatRequestInternal();
                Debug.Log("자동 테스트 - HTTP 요청 완료, 채팅 응답 대기 중...");
                
                // 채팅 응답을 기다림
                await WaitForChatResponse(15); // 15초 타임아웃
                
                // 4. HTTP 캐릭터 정보 요청
                Debug.Log("자동 테스트 - HTTP 캐릭터 정보 요청 중...");
                await GetCharacterInfoInternal();
                await UniTask.Delay(500);
                
                // 5. 연결 해제 (더 오래 기다린 후)
                Debug.Log("자동 테스트 - WebSocket 응답 대기 완료, 연결 해제 중...");
                _isIntentionalDisconnect = true; // 의도적 해제 플래그 설정
                await UniTask.Delay(2000); // 추가 대기 시간
                await _webSocketManager.DisconnectAsync();
                _receivedSessionId = null;
                
                // 연결 해제 후 충분한 대기 시간
                await UniTask.Delay(2000);
                
                Debug.Log("자동 테스트 - 더미 클라이언트 방식 테스트 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"자동 테스트 중 오류: {ex.Message}");
                throw; // 상위로 예외 전파
            }
        }

        private async UniTask SendChatRequestInternal()
        {
            if (_apiServiceManager?.Chat == null)
            {
                Debug.LogError("ChatApiService가 null입니다.");
                return;
            }

            // 세션 ID가 없으면 HTTP 요청을 보내지 않음
            if (string.IsNullOrEmpty(_receivedSessionId))
            {
                Debug.LogError("세션 ID가 없습니다. WebSocket에서 세션 ID를 먼저 받아야 합니다.");
                return;
            }

            var chatRequest = new ChatRequest
            {
                message = $"자동 테스트 메시지 - {DateTime.Now:HH:mm:ss}",
                characterId = testCharacterId,
                userId = testUserId,
                sessionId = _receivedSessionId, // 서버에서 받은 세션 ID 사용
                actor = "web_user",
                action = "chat", // 클라이언트와 동일하게 명시적으로 설정
                requestedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            Debug.Log($"HTTP 채팅 요청 전송: {JsonUtility.ToJson(chatRequest)}");
            
            // 채팅 응답 수신 상태 초기화
            _chatResponseReceived = false;
            _lastChatResponse = null;
            
            try
            {
                var response = await _apiServiceManager.Chat.SendChatAsync(chatRequest);
                Debug.Log($"HTTP 채팅 요청 성공: {JsonUtility.ToJson(response)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"HTTP 채팅 요청 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 채팅 응답을 기다리는 메서드
        /// </summary>
        private async UniTask WaitForChatResponse(int timeoutSeconds = 15)
        {
            Debug.Log($"채팅 응답 대기 시작 (타임아웃: {timeoutSeconds}초)");
            
            var startTime = DateTime.Now;
            while (!_chatResponseReceived && (DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                await UniTask.Delay(100);
                
                // WebSocket 연결 상태 확인
                if (_webSocketManager != null && !_webSocketManager.IsConnected)
                {
                    Debug.LogWarning("WebSocket 연결이 끊어졌습니다. 응답 대기 중단");
                    break;
                }
            }
            
            if (_chatResponseReceived)
            {
                Debug.Log($"✅ 채팅 응답 수신 완료: {_lastChatResponse}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 채팅 응답 타임아웃 ({timeoutSeconds}초)");
            }
        }

        private async UniTask GetCharacterInfoInternal()
        {
            if (_apiServiceManager?.Character == null)
            {
                Debug.LogError("CharacterApiService가 null입니다.");
                return;
            }

            var character = await _apiServiceManager.Character.GetCharacterAsync(testCharacterId);
            if (character != null)
            {
                Debug.Log($"자동 테스트 - 캐릭터 정보 조회 성공: {character.name}");
            }
        }

        private async UniTask SendWebSocketMessageInternal()
        {
            if (_webSocketManager == null)
            {
                Debug.LogError("WebSocketManager가 null입니다.");
                return;
            }

            Debug.LogWarning("WebSocket 메시지 전송 기능이 제거되었습니다. HTTP API를 사용하세요.");
        }

        #endregion

        #region WebSocket 이벤트 핸들러

        private void OnWebSocketConnected()
        {
            Debug.Log("🎉 WebSocket 연결됨!");
            _reconnectAttempts = 0; // 연결 성공 시 재연결 시도 횟수 초기화
        }

        private void OnWebSocketDisconnected()
        {
            Debug.Log("🔌 WebSocket 연결 해제됨!");
            
            // 의도적인 연결 해제가 아닌 경우에만 자동 재연결 시도
            if (!_isIntentionalDisconnect && _reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
            {
                _reconnectAttempts++;
                Debug.Log($"재연결 시도 {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS}");
                ConnectWebSocket();
            }
            else if (_isIntentionalDisconnect)
            {
                Debug.Log("의도적인 연결 해제로 재연결을 시도하지 않습니다.");
                _isIntentionalDisconnect = false; // 플래그 초기화
            }
            else
            {
                Debug.LogError("최대 재연결 시도 횟수 초과");
            }
        }

        private void OnWebSocketError(string error)
        {
            Debug.LogError($"❌ WebSocket 오류: {error}");
        }

        private void OnSessionIdReceived(string sessionId)
        {
            _receivedSessionId = sessionId;
            Debug.Log($"🆔 WebSocket 세션 ID 수신: {_receivedSessionId}");
            Debug.Log($"✅ 세션 ID가 성공적으로 저장되었습니다!");
        }

        private void OnChatMessageReceived(ChatMessage chatMessage)
        {
            Debug.Log($"💬 WebSocket 채팅 메시지 수신:");
            Debug.Log($"   - SessionId: {chatMessage.SessionId}");
            Debug.Log($"   - Text: {chatMessage.Text}");
            Debug.Log($"   - HasVoiceData: {chatMessage.HasVoiceData()}");
            Debug.Log($"   - Timestamp: {chatMessage.Timestamp}");
            
            _chatResponseReceived = true;
            _lastChatResponse = chatMessage.Text;
        }

        #endregion
    }
} 