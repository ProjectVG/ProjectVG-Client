using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Core.DI;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Managers
{
    public class GameManager : Singleton<GameManager>
    {
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
        
        protected override void Awake()
        {
            base.Awake();
            if (_autoInitializeOnStart) {
                InitializeGame();
            }
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        public async void InitializeGame()
        {
            
            Debug.Log("[GameManager] 초기화 시작");
            
            try
            {
                Initialize();
                SetupDependencies();
                await TryConnectSessionAsync();
                
                _isInitialized = true;
                Debug.Log("[GameManager] 초기화 완료");
                OnGameInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                string error = $"[GameManager] 초기화 실패: {ex.Message}";
                Debug.LogError(error);
                OnInitializationError?.Invoke(error);
            }
        }
        
        public async UniTask<bool> TryConnectSessionAsync()
        {
            if (_sessionManager == null)
            {
                Debug.LogError("[GameManager] SessionManager가 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                Debug.Log("[GameManager] 세션 연결 시도");
                
                if (_webSocketManager != null && !_webSocketManager.IsConnected)
                {
                    bool webSocketConnected = await _webSocketManager.ConnectAsync();
                    if (!webSocketConnected)
                    {
                        Debug.LogError("[GameManager] WebSocket 연결 실패");
                        return false;
                    }
                }
                
                Debug.Log("[GameManager] WebSocket 연결 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameManager] 세션 연결 오류: {ex.Message}");
                return false;
            }
        }
        
        public void Shutdown()
        {
            if (!_isInitialized) return;
            
            Debug.Log("[GameManager] 종료 처리 시작");
            
            for (int i = _managers.Count - 1; i >= 0; i--)
            {
                try
                {
                    _managers[i]?.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameManager] 매니저 종료 오류: {ex.Message}");
                }
            }
            
            _managers.Clear();
            _isInitialized = false;
            
            Debug.Log("[GameManager] 종료 처리 완료");
        }
        
        public bool AreManagersReady()
        {
            return _webSocketManager != null && 
                   _sessionManager != null && 
                   _httpApiClient != null;
        }
        
        public bool IsSessionConnected()
        {
            return _sessionManager != null && _sessionManager.IsSessionConnected;
        }
        
        [ContextMenu("Log Manager Status")]
        public void LogManagerStatus()
        {
            Debug.Log("[GameManager] === 매니저 상태 ===");
            Debug.Log($"[GameManager] 초기화: {(_isInitialized ? "완료" : "미완료")}");
            Debug.Log($"[GameManager] WebSocketManager: {(_webSocketManager != null ? "준비됨" : "없음")}");
            Debug.Log($"[GameManager] SessionManager: {(_sessionManager != null ? "준비됨" : "없음")}");
            Debug.Log($"[GameManager] HttpApiClient: {(_httpApiClient != null ? "준비됨" : "없음")}");
            Debug.Log($"[GameManager] 전체 준비: {(AreManagersReady() ? "완료" : "미완료")}");
            Debug.Log($"[GameManager] 세션 연결: {(IsSessionConnected() ? "연결됨" : "미연결")}");
        }
        
        #endregion
        
        #region Private Methods
        
        private void Initialize()
        {
            InitializeWebSocketManager();
            InitializeSessionManager();
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
                Debug.Log("[GameManager] WebSocketManager 초기화 완료");
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
                Debug.Log("[GameManager] SessionManager 초기화 완료");
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
                Debug.Log("[GameManager] HttpApiClient 초기화 완료");
            }
            else
            {
                throw new InvalidOperationException("HttpApiClient를 초기화할 수 없습니다.");
            }
        }
        
        private void SetupDependencies()
        {
            var container = DIContainer.Instance;
            container.Register<SessionManager>(_sessionManager);
            
            if (_sessionManager != null)
            {

            }
            
            if (_httpApiClient != null)
            {
                container.InjectDependencies(_httpApiClient);
                Debug.Log("[GameManager] HttpApiClient 의존성 주입 완료");
            }
            
            if (_webSocketManager != null)
            {
                container.InjectDependencies(_webSocketManager);
                Debug.Log("[GameManager] WebSocketManager 의존성 주입 완료");
            }
        }
        
        #endregion
    }
    
    public interface IManager
    {
        void Shutdown();
    }
} 