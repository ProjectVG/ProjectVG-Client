using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Core.Managers;
using ProjectVG.Core.Attributes;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// 새로운 이벤트 기반 SessionManager
    /// WebSocketManager의 연결/해제 상태를 모니터링하고 세션 ID를 관리
    /// </summary>
    public class SessionManager : Singleton<SessionManager>, IManager
    {
        [Header("Session Info")]
        [SerializeField] private string _currentSessionId = "";
        [SerializeField] private bool _isInitialized = false;
        
        [Inject] private WebSocketManager _webSocketManager;
        
        // 공개 속성
        public string SessionId => _currentSessionId;
        public bool IsSessionConnected => !string.IsNullOrEmpty(_currentSessionId) && _webSocketManager?.IsConnected == true;
        public bool IsWebSocketConnected => _webSocketManager?.IsConnected == true;
        public bool IsWebSocketConnecting => _webSocketManager?.IsConnecting == true;
        public bool IsInitialized => _isInitialized;
        
        // 이벤트
        public event Action<string> OnSessionStarted;
        public event Action OnSessionEnded;
        public event Action<string> OnSessionError;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        private void Start()
        {
            // DI 완료 후 ManagerRegistry에서 Initialize 호출됨
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 세션 ID를 요청합니다. 연결되지 않았다면 연결을 시도하고 기다립니다.
        /// </summary>
        public async UniTask<string> GetSessionIdAsync()
        {
            if (IsSessionConnected)
            {
                Debug.Log($"[SessionManager] 현재 세션 ID 반환: {_currentSessionId}");
                return _currentSessionId;
            }
            
            Debug.Log("[SessionManager] 세션이 없거나 연결되지 않음. 연결을 시도합니다.");
            bool connected = await EnsureConnectionAsync();
            
            if (connected)
            {
                return _currentSessionId;
            }
            else
            {
                Debug.LogError("[SessionManager] 세션 연결에 실패했습니다.");
                return null;
            }
        }
        
        /// <summary>
        /// 세션 연결을 보장합니다. 이미 연결되어 있으면 즉시 반환하고, 그렇지 않으면 연결을 시도합니다.
        /// </summary>
        public async UniTask<bool> EnsureConnectionAsync()
        {
            Debug.Log($"[SessionManager] EnsureConnectionAsync 호출 - 초기화 상태: {_isInitialized}, WebSocketManager: {(_webSocketManager != null ? "존재" : "null")}");
            
            if (IsSessionConnected)
            {
                Debug.Log("[SessionManager] 이미 세션이 연결되어 있습니다.");
                return true;
            }
            
            // DI로 주입받은 WebSocketManager 확인
            if (_webSocketManager == null)
            {
                Debug.LogError("[SessionManager] WebSocketManager가 DI로 주입되지 않았습니다. DependencyManager 설정을 확인하세요.");
                return false;
            }
            
            return await RequestConnectionAsync();
        }
        
        /// <summary>
        /// 새로운 연결 요청 로직 - 폴링 방식
        /// </summary>
        private async UniTask<bool> RequestConnectionAsync()
        {
            try
            {
                // DI로 주입받은 WebSocketManager 사용
                if (_webSocketManager == null)
                {
                    Debug.LogError("[SessionManager] WebSocketManager가 DI로 주입되지 않았습니다.");
                    return false;
                }
                
                // 1. WebSocket 연결 상태 확인 및 연결 요청
                if (!IsWebSocketConnected)
                {
                    if (IsWebSocketConnecting)
                    {
                        Debug.Log("[SessionManager] WebSocket이 이미 연결 중입니다. 연결 완료를 기다립니다.");
                    }
                    else
                    {
                        Debug.Log("[SessionManager] WebSocket 연결을 요청합니다.");
                        bool connected = await _webSocketManager.ConnectAsync();
                        if (!connected)
                        {
                            Debug.LogError("[SessionManager] WebSocket 연결에 실패했습니다.");
                            return false;
                        }
                    }
                }
                
                // 2. 연결 완료 대기 (폴링)
                return await WaitForSessionConnection();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] 연결 요청 실패: {ex.Message}");
                Debug.LogError($"[SessionManager] 스택 트레이스: {ex.StackTrace}");
                OnSessionError?.Invoke($"연결 요청 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 세션 연결 완료를 폴링으로 대기
        /// </summary>
        private async UniTask<bool> WaitForSessionConnection()
        {
            Debug.Log("[SessionManager] 세션 연결 완료 대기 중...");
            
            const int timeoutSeconds = 10;
            const int pollIntervalMs = 100; // 100ms마다 체크
            int elapsedMs = 0;
            
            while (elapsedMs < timeoutSeconds * 1000)
            {
                // 세션이 연결되었는지 확인
                if (IsSessionConnected)
                {
                    Debug.Log($"[SessionManager] 세션 연결 완료: {_currentSessionId}");
                    return true;
                }
                
                // WebSocket 연결이 끊어졌다면 실패
                if (!IsWebSocketConnected && !IsWebSocketConnecting)
                {
                    Debug.LogError("[SessionManager] WebSocket 연결이 끊어졌습니다.");
                    return false;
                }
                
                await UniTask.Delay(pollIntervalMs);
                elapsedMs += pollIntervalMs;
            }
            
            Debug.LogError($"[SessionManager] 세션 연결 타임아웃 ({timeoutSeconds}초)");
            return false;
        }
        
        /// <summary>
        /// 세션 해제
        /// </summary>
        public void EndSession()
        {
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                string oldSessionId = _currentSessionId;
                _currentSessionId = "";
                
                Debug.Log($"[SessionManager] 세션 종료: {oldSessionId}");
                OnSessionEnded?.Invoke();
            }
        }
        
        #endregion
        
        #region Private Methods - 초기화 및 이벤트 핸들링
        
        public void Initialize()
        {
            try
            {
                Debug.Log("[SessionManager] 초기화 시작");
                
                // DI로 주입받은 WebSocketManager 확인
                if (_webSocketManager == null)
                {
                    Debug.LogError("[SessionManager] WebSocketManager가 DI로 주입되지 않았습니다.");
                    return;
                }
                
                // 이벤트 구독
                SubscribeToWebSocketEvents();
                
                _isInitialized = true;
                Debug.Log("[SessionManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManager] 초기화 실패: {ex.Message}");
                Debug.LogError($"[SessionManager] 스택 트레이스: {ex.StackTrace}");
            }
        }
        
        private void SubscribeToWebSocketEvents()
        {
            if (_webSocketManager == null) return;
            
            // 기존 구독 해제 (중복 방지)
            UnsubscribeFromWebSocketEvents();
            
            // 새로운 이벤트 구독
            _webSocketManager.OnSessionConnected += OnWebSocketSessionConnected;
            _webSocketManager.OnSessionDisconnected += OnWebSocketSessionDisconnected;
            _webSocketManager.OnDisconnected += OnWebSocketDisconnected;
            _webSocketManager.OnError += OnWebSocketError;
            
            Debug.Log("[SessionManager] WebSocket 이벤트 구독 완료");
        }
        
        private void UnsubscribeFromWebSocketEvents()
        {
            if (_webSocketManager == null) return;
            
            _webSocketManager.OnSessionConnected -= OnWebSocketSessionConnected;
            _webSocketManager.OnSessionDisconnected -= OnWebSocketSessionDisconnected;
            _webSocketManager.OnDisconnected -= OnWebSocketDisconnected;
            _webSocketManager.OnError -= OnWebSocketError;
        }
        
        #endregion
        
        #region WebSocket 이벤트 핸들러
        
        private void OnWebSocketSessionConnected(string sessionId)
        {
            Debug.Log($"[SessionManager] WebSocket 세션 연결됨: {sessionId}");
            
            _currentSessionId = sessionId;
            OnSessionStarted?.Invoke(sessionId);
        }
        
        private void OnWebSocketSessionDisconnected()
        {
            Debug.Log("[SessionManager] WebSocket 세션 연결 해제됨");
            
            _currentSessionId = "";
            OnSessionEnded?.Invoke();
        }
        
        private void OnWebSocketDisconnected()
        {
            Debug.Log("[SessionManager] WebSocket 연결 해제됨");
            
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                _currentSessionId = "";
                OnSessionEnded?.Invoke();
            }
        }
        
        private void OnWebSocketError(string error)
        {
            Debug.LogError($"[SessionManager] WebSocket 오류: {error}");
            OnSessionError?.Invoke(error);
        }
        
        #endregion
        
        #region IManager 구현
        
        public void Shutdown()
        {
            Debug.Log("[SessionManager] 종료 중...");
            
            // 이벤트 구독 해제
            UnsubscribeFromWebSocketEvents();
            
            // 세션 종료
            EndSession();
            
            _isInitialized = false;
            Debug.Log("[SessionManager] 종료 완료");
        }
        
        #endregion
    }
}