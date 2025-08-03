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
        
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            try
            {
                if (_webSocketManager == null)
                {
                    _webSocketManager = WebSocketManager.Instance;
                    if (_webSocketManager == null)
                    {
                        throw new InvalidOperationException("WebSocketManager.Instance가 null입니다. WebSocketManager가 먼저 초기화되어야 합니다.");
                    }
                }
                
                _webSocketManager.OnConnected += OnWebSocketConnected;
                _webSocketManager.OnDisconnected += OnWebSocketDisconnected;
                _webSocketManager.OnError += OnWebSocketError;
                
                _isInitialized = true;
                Debug.Log("SessionManager 초기화 완료 - WebSocketManager와 강하게 결합됨");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SessionManager 초기화 실패: {ex.Message}");
                OnSessionError?.Invoke($"SessionManager 초기화 실패: {ex.Message}");
            }
        }
        
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
                Debug.LogWarning("WebSocket이 연결되지 않았습니다. 연결을 시도합니다.");
                await _webSocketManager.ConnectAsync();
            }
            
            if (_webSocketManager.IsConnected)
            {
                Debug.Log("WebSocket 연결 완료 - 세션 ID 자동 수신 대기 중");
            }
            else
            {
                string error = "WebSocket 연결 실패";
                Debug.LogError(error);
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
                
                Debug.Log($"세션 종료: {oldSessionId}");
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
                    
                    Debug.Log($"[Session] 시작: {_sessionId}");
                    OnSessionStarted?.Invoke(_sessionId);
                }
                else
                {
                    string error = $"세션 응답 데이터가 유효하지 않습니다.";
                    Debug.LogError(error);
                    OnSessionError?.Invoke(error);
                }
            }
            catch (Exception ex)
            {
                string error = $"세션 메시지 처리 중 오류: {ex.Message}";
                Debug.LogError(error);
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
        
        private void OnWebSocketConnected()
        {
            Debug.Log("WebSocket 연결됨 - 세션 요청 준비");
        }
        
        private void OnWebSocketDisconnected()
        {
            _isSessionConnected = false;
            Debug.Log("WebSocket 연결 해제됨");
        }
        
        private void OnWebSocketError(string error)
        {
            Debug.LogError($"WebSocket 에러: {error}");
            OnSessionError?.Invoke($"WebSocket 에러: {error}");
        }
        
        #endregion
    }
} 