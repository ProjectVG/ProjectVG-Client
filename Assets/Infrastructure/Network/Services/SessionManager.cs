using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Core.Managers;
using Newtonsoft.Json.Linq;

namespace ProjectVG.Infrastructure.Network.Services
{
    public class SessionManager : Singleton<SessionManager>, IManager
    {
        [Header("Session Info")]
        [SerializeField] private string _sessionId = "";
        [SerializeField] private bool _isSessionConnected = false;
        [SerializeField] private bool _isInitialized = false;
        
        private WebSocketManager _webSocketManager;
        
        public string SessionId => _sessionId;
        public bool IsSessionConnected => _isSessionConnected;
        public bool IsInitialized => _isInitialized;
        
        public event Action<string> OnSessionStarted;
        public event Action<string> OnSessionEnded;
        public event Action<string> OnSessionError;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        public async UniTask<string> GetSessionIdAsync()
        {
            if (string.IsNullOrEmpty(_sessionId) || !_isSessionConnected)
            {
                await RequestNewSessionAsync();
            }
            
            return _sessionId;
        }
        
        public async UniTask RequestNewSessionAsync()
        {
            if (_webSocketManager == null || !_webSocketManager.IsConnected)
            {
                Debug.LogWarning("[SessionManager] WebSocket이 연결되지 않았습니다. 연결을 시도합니다.");
                await _webSocketManager.ConnectAsync();
            }
            
            if (!_webSocketManager.IsConnected)
            {
                string error = "WebSocket 연결 실패";
                Debug.LogError($"[SessionManager] {error}");
                OnSessionError?.Invoke(error);
            }
        }
        
        public void EndSession()
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                string oldSessionId = _sessionId;
                _sessionId = "";
                _isSessionConnected = false;
                
                OnSessionEnded?.Invoke(oldSessionId);
            }
        }
        
        public void HandleSessionMessage(string data)
        {
            try
            {
                var jsonObject = JObject.Parse(data);
                string sessionId = jsonObject["session_id"]?.ToString();
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _sessionId = sessionId;
                    _isSessionConnected = true;
                    
                    OnSessionStarted?.Invoke(_sessionId);
                }
                else
                {
                    string error = $"세션 응답 데이터가 유효하지 않습니다.";
                    Debug.LogError($"[SessionManager] {error}");
                    OnSessionError?.Invoke(error);
                }
            }
            catch (Exception ex)
            {
                string error = $"세션 메시지 처리 중 오류: {ex.Message}";
                Debug.LogError($"[SessionManager] {error}");
                OnSessionError?.Invoke(error);
            }
        }
        
        public void Shutdown()
        {
            if (_webSocketManager != null)
            {
                _webSocketManager.OnConnected -= OnWebSocketConnected;
                _webSocketManager.OnDisconnected -= OnWebSocketDisconnected;
                _webSocketManager.OnError -= OnWebSocketError;
            }
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            if (_isInitialized)
                return;

            try {
                if (_webSocketManager == null) {
                    _webSocketManager = WebSocketManager.Instance;
                    if (_webSocketManager == null) {
                        throw new InvalidOperationException("[SessionManager] WebSocketManager Instance가 null입니다. WebSocketManager가 먼저 초기화되어야 합니다.");
                    }
                }

                _webSocketManager.OnConnected += OnWebSocketConnected;
                _webSocketManager.OnDisconnected += OnWebSocketDisconnected;
                _webSocketManager.OnError += OnWebSocketError;

                _isInitialized = true;
            }
            catch (Exception ex) {
                Debug.LogError($"[SessionManager] SessionManager 초기화 실패: {ex.Message}");
                OnSessionError?.Invoke($"[SessionManager] SessionManager 초기화 실패: {ex.Message}");
            }
        }

        private void OnWebSocketConnected()
        {
        }
        
        private void OnWebSocketDisconnected()
        {
            _isSessionConnected = false;
        }
        
        private void OnWebSocketError(string error)
        {
            Debug.LogError($"[SessionManager] WebSocket 에러: {error}");
            OnSessionError?.Invoke($"WebSocket 에러: {error}");
        }
        
        #endregion
    }
} 