using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;

namespace ProjectVG.Infrastructure.Network
{
    /// <summary>
    /// WebSocket + HTTP 통합 테스트 매니저
    /// WebSocket 연결 → HTTP 요청 → WebSocket으로 결과 수신
    /// </summary>
    public class NetworkTestManager : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private string testSessionId = "test-session-123";
        [SerializeField] private string testCharacterId = "test-character-456";
        [SerializeField] private string testUserId = "test-user-789";
        
        [Header("자동 테스트")]
        [SerializeField] private bool autoTest = false;
        [SerializeField] private float testInterval = 5f;
        
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
        private DefaultWebSocketHandler _webSocketHandler;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isTestRunning = false;

        private void Awake()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
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
                // WebSocket 매니저 초기화
                _webSocketManager = WebSocketManager.Instance;
                if (_webSocketManager == null)
                {
                    Debug.LogError("WebSocketManager.Instance가 null입니다. 매니저가 생성되지 않았습니다.");
                    return;
                }
                
                // API 서비스 매니저 초기화
                _apiServiceManager = ApiServiceManager.Instance;
                if (_apiServiceManager == null)
                {
                    Debug.LogError("ApiServiceManager.Instance가 null입니다. 매니저가 생성되지 않았습니다.");
                    return;
                }
                
                // WebSocket 핸들러 생성 및 등록
                _webSocketHandler = gameObject.AddComponent<DefaultWebSocketHandler>();
                if (_webSocketHandler == null)
                {
                    Debug.LogError("DefaultWebSocketHandler 생성에 실패했습니다.");
                    return;
                }
                
                _webSocketManager.RegisterHandler(_webSocketHandler);
                
                // 이벤트 구독
                _webSocketHandler.OnConnectedEvent += OnWebSocketConnected;
                _webSocketHandler.OnDisconnectedEvent += OnWebSocketDisconnected;
                _webSocketHandler.OnErrorEvent += OnWebSocketError;
                _webSocketHandler.OnChatMessageReceivedEvent += OnChatMessageReceived;
                _webSocketHandler.OnSystemMessageReceivedEvent += OnSystemMessageReceived;
                _webSocketHandler.OnSessionIdMessageReceivedEvent += OnSessionIdMessageReceived;
                
                Debug.Log("NetworkTestManager 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkTestManager 초기화 중 오류: {ex.Message}");
            }
        }

        #region 수동 테스트 메서드들

        [ContextMenu("1. WebSocket 연결")]
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
                Debug.Log("=== WebSocket 연결 시작 ===");
                bool connected = await _webSocketManager.ConnectAsync(testSessionId);
                
                if (connected)
                {
                    Debug.Log("✅ WebSocket 연결 성공!");
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

        [ContextMenu("2. HTTP 채팅 요청")]
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

            try
            {
                Debug.Log("=== HTTP 채팅 요청 시작 ===");
                
                var chatRequest = new DTOs.Chat.ChatRequest
                {
                    message = "안녕하세요! 테스트 메시지입니다.",
                    characterId = testCharacterId,
                    userId = testUserId,
                    sessionId = testSessionId,
                    actor = "web_user"
                };

                var response = await _apiServiceManager.Chat.SendChatAsync(chatRequest);
                
                if (response != null && response.success)
                {
                    Debug.Log($"✅ HTTP 채팅 요청 성공! 응답: {response.message}");
                }
                else
                {
                    Debug.LogError($"❌ HTTP 채팅 요청 실패: {response?.message ?? "알 수 없는 오류"}");
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
                
                bool sent = await _webSocketManager.SendChatMessageAsync(
                    message: "WebSocket으로 직접 전송하는 테스트 메시지",
                    characterId: testCharacterId,
                    userId: testUserId
                );
                
                if (sent)
                {
                    Debug.Log("✅ WebSocket 메시지 전송 성공!");
                }
                else
                {
                    Debug.LogError("❌ WebSocket 메시지 전송 실패!");
                }
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
                await _webSocketManager.DisconnectAsync();
                Debug.Log("✅ WebSocket 연결 해제 완료!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 연결 해제 중 오류: {ex.Message}");
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
                
                // 1. WebSocket 연결
                Debug.Log("1️⃣ WebSocket 연결 중...");
                bool connected = await _webSocketManager.ConnectAsync(testSessionId);
                if (!connected)
                {
                    Debug.LogError("WebSocket 연결 실패로 테스트 중단");
                    return;
                }
                
                await UniTask.Delay(1000); // 연결 안정화 대기
                
                // 2. HTTP 채팅 요청
                Debug.Log("2️⃣ HTTP 채팅 요청 중...");
                await SendChatRequestInternal();
                
                await UniTask.Delay(1000);
                
                // 3. HTTP 캐릭터 정보 요청
                Debug.Log("3️⃣ HTTP 캐릭터 정보 요청 중...");
                await GetCharacterInfoInternal();
                
                await UniTask.Delay(1000);
                
                // 4. WebSocket 메시지 전송
                Debug.Log("4️⃣ WebSocket 메시지 전송 중...");
                await SendWebSocketMessageInternal();
                
                await UniTask.Delay(1000);
                
                // 5. WebSocket 연결 해제
                Debug.Log("5️⃣ WebSocket 연결 해제 중...");
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
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await RunFullTestInternal();
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

        private async UniTask RunFullTestInternal()
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
                // 1. WebSocket 연결
                Debug.Log("자동 테스트 - WebSocket 연결 중...");
                await _webSocketManager.ConnectAsync(testSessionId);
                await UniTask.Delay(500);
                
                // 2. HTTP 요청들
                Debug.Log("자동 테스트 - HTTP 채팅 요청 중...");
                await SendChatRequestInternal();
                await UniTask.Delay(500);
                
                Debug.Log("자동 테스트 - HTTP 캐릭터 정보 요청 중...");
                await GetCharacterInfoInternal();
                await UniTask.Delay(500);
                
                // 3. WebSocket 메시지 전송
                Debug.Log("자동 테스트 - WebSocket 메시지 전송 중...");
                await SendWebSocketMessageInternal();
                await UniTask.Delay(500);
                
                // 4. 연결 해제
                Debug.Log("자동 테스트 - WebSocket 연결 해제 중...");
                await _webSocketManager.DisconnectAsync();
                
                Debug.Log("자동 테스트 - 전체 테스트 완료");
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

            var chatRequest = new DTOs.Chat.ChatRequest
            {
                message = $"자동 테스트 메시지 - {DateTime.Now:HH:mm:ss}",
                characterId = testCharacterId,
                userId = testUserId,
                sessionId = testSessionId,
                actor = "web_user"
            };

            await _apiServiceManager.Chat.SendChatAsync(chatRequest);
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

            await _webSocketManager.SendChatMessageAsync(
                message: $"자동 WebSocket 메시지 - {DateTime.Now:HH:mm:ss}",
                characterId: testCharacterId,
                userId: testUserId
            );
        }

        #endregion

        #region WebSocket 이벤트 핸들러

        private void OnWebSocketConnected()
        {
            Debug.Log("🎉 WebSocket 연결됨!");
        }

        private void OnWebSocketDisconnected()
        {
            Debug.Log("🔌 WebSocket 연결 해제됨!");
        }

        private void OnWebSocketError(string error)
        {
            Debug.LogError($"❌ WebSocket 오류: {error}");
        }

        private void OnChatMessageReceived(DTOs.WebSocket.ChatMessage message)
        {
            Debug.Log($"💬 WebSocket 채팅 메시지 수신: {message.message}");
        }

        private void OnSystemMessageReceived(DTOs.WebSocket.SystemMessage message)
        {
            Debug.Log($"🔧 WebSocket 시스템 메시지 수신: {message.description}");
        }

        private void OnSessionIdMessageReceived(DTOs.WebSocket.SessionIdMessage message)
        {
            Debug.Log($"🆔 WebSocket 세션 ID 수신: {message.session_id}");
        }

        #endregion
    }
} 