using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Core.DI;

namespace ProjectVG.Core.Managers
{
    /// <summary>
    /// 게임의 핵심 매니저들을 관리하는 GameManager
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Manager References")]
        [SerializeField] private WebSocketManager _webSocketManager;
        [SerializeField] private SessionManager _sessionManager;
        [SerializeField] private HttpApiClient _httpApiClient;
        
        [Header("Initialization Settings")]
        [SerializeField] private bool _autoInitializeOnStart = true;
        [SerializeField] private bool _createManagersIfNotExist = true;
        
        private bool _isInitialized = false;
        private readonly List<IManager> _managers = new List<IManager>();
        
        public bool IsInitialized => _isInitialized;
        public WebSocketManager WebSocketManager => _webSocketManager;
        public SessionManager SessionManager => _sessionManager;
        public HttpApiClient HttpApiClient => _httpApiClient;
        
        public event Action OnGameInitialized;
        public event Action<string> OnInitializationError;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void Start()
        {
            if (_autoInitializeOnStart)
            {
                InitializeGame();
            }
        }
        
        private void OnDestroy()
        {
            ShutdownGame();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 게임 초기화
        /// </summary>
        public void InitializeGame()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("게임이 이미 초기화되었습니다.");
                return;
            }
            
            Debug.Log("게임 초기화 시작...");
            
            try
            {
                // 1. 필수 매니저들 초기화
                InitializeManagers();
                
                // 2. 매니저들 간의 의존성 설정
                SetupDependencies();
                
                // 3. 초기화 완료
                _isInitialized = true;
                Debug.Log("게임 초기화 완료");
                OnGameInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                string error = $"게임 초기화 실패: {ex.Message}";
                Debug.LogError(error);
                OnInitializationError?.Invoke(error);
            }
        }
        
        private void InitializeManagers()
        {
            // WebSocketManager 초기화 (가장 먼저)
            InitializeWebSocketManager();
            
            // SessionManager 초기화 (WebSocketManager에 의존)
            InitializeSessionManager();
            
            // HttpApiClient 초기화
            InitializeHttpApiClient();
        }
        
        private void InitializeWebSocketManager()
        {
            if (_webSocketManager == null && _createManagersIfNotExist)
            {
                var webSocketObj = new GameObject("WebSocketManager");
                webSocketObj.transform.SetParent(transform);
                _webSocketManager = webSocketObj.AddComponent<WebSocketManager>();
            }
            
            if (_webSocketManager != null)
            {
                _managers.Add(_webSocketManager);
                Debug.Log("WebSocketManager 초기화 완료");
            }
            else
            {
                throw new InvalidOperationException("WebSocketManager를 초기화할 수 없습니다.");
            }
        }
        
        private void InitializeSessionManager()
        {
            if (_sessionManager == null && _createManagersIfNotExist)
            {
                var sessionObj = new GameObject("SessionManager");
                sessionObj.transform.SetParent(transform);
                _sessionManager = sessionObj.AddComponent<SessionManager>();
            }
            
            if (_sessionManager != null)
            {
                _managers.Add(_sessionManager);
                Debug.Log("SessionManager 초기화 완료");
            }
            else
            {
                throw new InvalidOperationException("SessionManager를 초기화할 수 없습니다.");
            }
        }
        
        private void InitializeHttpApiClient()
        {
            if (_httpApiClient == null && _createManagersIfNotExist)
            {
                var httpObj = new GameObject("HttpApiClient");
                httpObj.transform.SetParent(transform);
                _httpApiClient = httpObj.AddComponent<HttpApiClient>();
            }
            
            if (_httpApiClient != null)
            {
                _managers.Add(_httpApiClient);
                Debug.Log("HttpApiClient 초기화 완료");
            }
            else
            {
                throw new InvalidOperationException("HttpApiClient를 초기화할 수 없습니다.");
            }
        }
        
        private void SetupDependencies()
        {
            // DI 컨테이너에 서비스 등록
            var container = DIContainer.Instance;
            container.Register<SessionManager>(_sessionManager);
            
            // SessionManager가 WebSocketManager에 의존하므로 초기화 순서 보장
            if (_sessionManager != null)
            {
                _sessionManager.Initialize();
            }
            
            // HttpApiClient에 의존성 주입
            if (_httpApiClient != null)
            {
                container.InjectDependencies(_httpApiClient);
                Debug.Log("HttpApiClient에 의존성 주입 완료");
            }
            
            // WebSocketManager에 의존성 주입
            if (_webSocketManager != null)
            {
                container.InjectDependencies(_webSocketManager);
                Debug.Log("WebSocketManager에 의존성 주입 완료");
            }
        }
        
        #endregion
        
        #region Shutdown
        
        /// <summary>
        /// 게임 종료 처리
        /// </summary>
        public void ShutdownGame()
        {
            if (!_isInitialized) return;
            
            Debug.Log("게임 종료 처리 시작...");
            
            // 매니저들을 역순으로 종료
            for (int i = _managers.Count - 1; i >= 0; i--)
            {
                try
                {
                    _managers[i]?.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"매니저 종료 중 오류: {ex.Message}");
                }
            }
            
            _managers.Clear();
            _isInitialized = false;
            
            Debug.Log("게임 종료 처리 완료");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 모든 매니저가 준비되었는지 확인
        /// </summary>
        public bool AreManagersReady()
        {
            return _webSocketManager != null && 
                   _sessionManager != null && 
                   _httpApiClient != null;
        }
        
        /// <summary>
        /// 매니저 상태 로그 출력
        /// </summary>
        [ContextMenu("Log Manager Status")]
        public void LogManagerStatus()
        {
            Debug.Log("=== 매니저 상태 ===");
            Debug.Log($"GameManager 초기화: {_isInitialized}");
            Debug.Log($"WebSocketManager: {(_webSocketManager != null ? "존재함" : "없음")}");
            Debug.Log($"SessionManager: {(_sessionManager != null ? "존재함" : "없음")}");
            Debug.Log($"HttpApiClient: {(_httpApiClient != null ? "존재함" : "없음")}");
            Debug.Log($"매니저 준비 상태: {(AreManagersReady() ? "준비됨" : "미준비")}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 매니저 인터페이스
    /// </summary>
    public interface IManager
    {
        void Shutdown();
    }
} 