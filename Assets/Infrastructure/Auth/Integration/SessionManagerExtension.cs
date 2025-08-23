using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Auth.Core;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.Integration
{
    /// <summary>
    /// SessionManager와 AuthManager를 연동하는 확장 클래스
    /// </summary>
    public static class SessionManagerExtension
    {
        private static SessionManager _sessionManager;
        private static IAuthManager _authManager;
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();
        
        // 인증 세션 상태
        private static bool _isAuthenticatedSession = false;
        private static string _lastAuthenticatedSessionId = null;
        
        public static bool IsAuthenticatedSession => _isAuthenticatedSession;
        public static string AuthenticatedSessionId => _lastAuthenticatedSessionId;
        
        /// <summary>
        /// SessionManager-AuthManager 연동 초기화
        /// </summary>
        public static void Initialize(SessionManager sessionManager, IAuthManager authManager)
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    Debug.LogWarning("[SessionManagerExtension] 이미 초기화되었습니다.");
                    return;
                }
                
                _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
                _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
                
                SetupEventHandlers();
                _isInitialized = true;
                
                Debug.Log("[SessionManagerExtension] SessionManager-AuthManager 연동 초기화 완료");
            }
        }
        
        /// <summary>
        /// 인증된 세션 연결 요청
        /// </summary>
        public static async UniTask<string> GetAuthenticatedSessionAsync(this SessionManager sessionManager)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("SessionManagerExtension이 초기화되지 않았습니다.");
            }
            
            if (!_authManager.IsAuthenticated)
            {
                Debug.LogWarning("[SessionManagerExtension] 인증되지 않은 상태에서 인증 세션 요청");
                return null;
            }
            
            try
            {
                // 기존 세션이 있고 인증된 세션이라면 재사용
                if (!string.IsNullOrEmpty(sessionManager.SessionId) && _isAuthenticatedSession)
                {
                    Debug.Log($"[SessionManagerExtension] 기존 인증 세션 재사용: {sessionManager.SessionId}");
                    return sessionManager.SessionId;
                }
                
                // 새로운 인증 세션 생성
                Debug.Log("[SessionManagerExtension] 새로운 인증 세션 요청");
                
                var sessionId = await sessionManager.GetSessionIdAsync();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await MarkSessionAsAuthenticated(sessionId);
                }
                
                return sessionId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManagerExtension] 인증 세션 요청 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 세션에 인증 토큰 주입
        /// </summary>
        public static async UniTask<bool> InjectAuthTokenToSessionAsync(this SessionManager sessionManager)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("SessionManagerExtension이 초기화되지 않았습니다.");
            }
            
            if (!_authManager.IsAuthenticated)
            {
                Debug.LogWarning("[SessionManagerExtension] 인증되지 않은 상태에서 토큰 주입 시도");
                return false;
            }
            
            try
            {
                var accessToken = await _authManager.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    Debug.LogError("[SessionManagerExtension] 유효한 Access 토큰을 얻을 수 없습니다.");
                    return false;
                }
                
                // WebSocket을 통해 인증 토큰 전송
                return await SendAuthTokenToWebSocket(accessToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManagerExtension] 토큰 주입 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 세션의 인증 상태 확인
        /// </summary>
        public static bool IsSessionAuthenticated(this SessionManager sessionManager)
        {
            if (!_isInitialized)
            {
                return false;
            }
            
            return sessionManager.IsSessionConnected && 
                   _isAuthenticatedSession && 
                   _authManager.IsAuthenticated &&
                   sessionManager.SessionId == _lastAuthenticatedSessionId;
        }
        
        /// <summary>
        /// 세션 인증 해제
        /// </summary>
        public static async UniTask ClearSessionAuthenticationAsync(this SessionManager sessionManager)
        {
            if (!_isInitialized)
            {
                return;
            }
            
            try
            {
                _isAuthenticatedSession = false;
                _lastAuthenticatedSessionId = null;
                
                // WebSocket을 통해 인증 해제 신호 전송
                await SendLogoutSignalToWebSocket();
                
                Debug.Log("[SessionManagerExtension] 세션 인증 해제 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManagerExtension] 세션 인증 해제 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 자동 재인증 (토큰 갱신 시)
        /// </summary>
        public static async UniTask<bool> ReauthenticateSessionAsync(this SessionManager sessionManager)
        {
            if (!_isInitialized)
            {
                return false;
            }
            
            if (!sessionManager.IsSessionConnected)
            {
                Debug.LogWarning("[SessionManagerExtension] 세션이 연결되지 않은 상태에서 재인증 시도");
                return false;
            }
            
            try
            {
                Debug.Log("[SessionManagerExtension] 세션 재인증 시작");
                
                // 새 토큰으로 재인증
                var success = await sessionManager.InjectAuthTokenToSessionAsync();
                
                if (success)
                {
                    Debug.Log("[SessionManagerExtension] 세션 재인증 성공");
                }
                else
                {
                    Debug.LogError("[SessionManagerExtension] 세션 재인증 실패");
                    await sessionManager.ClearSessionAuthenticationAsync();
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManagerExtension] 세션 재인증 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 종료 처리
        /// </summary>
        public static void Shutdown()
        {
            lock (_lockObject)
            {
                if (!_isInitialized)
                {
                    return;
                }
                
                ClearEventHandlers();
                
                _sessionManager = null;
                _authManager = null;
                _isAuthenticatedSession = false;
                _lastAuthenticatedSessionId = null;
                _isInitialized = false;
                
                Debug.Log("[SessionManagerExtension] 종료 처리 완료");
            }
        }
        
        #region Private Methods
        
        private static void SetupEventHandlers()
        {
            // AuthManager 이벤트 구독
            _authManager.OnAuthStateChanged += OnAuthStateChanged;
            
            // SessionManager 이벤트 구독
            _sessionManager.OnSessionStarted += OnSessionStarted;
            _sessionManager.OnSessionEnded += OnSessionEnded;
            _sessionManager.OnSessionError += OnSessionError;
            
            Debug.Log("[SessionManagerExtension] 이벤트 핸들러 설정 완료");
        }
        
        private static void ClearEventHandlers()
        {
            if (_authManager != null)
            {
                _authManager.OnAuthStateChanged -= OnAuthStateChanged;
            }
            
            if (_sessionManager != null)
            {
                _sessionManager.OnSessionStarted -= OnSessionStarted;
                _sessionManager.OnSessionEnded -= OnSessionEnded;
                _sessionManager.OnSessionError -= OnSessionError;
            }
        }
        
        private static async void OnAuthStateChanged(bool isAuthenticated)
        {
            Debug.Log($"[SessionManagerExtension] 인증 상태 변경: {isAuthenticated}");
            
            if (isAuthenticated)
            {
                // 인증 완료 시 세션에 토큰 주입
                if (_sessionManager.IsSessionConnected)
                {
                    await _sessionManager.InjectAuthTokenToSessionAsync();
                }
            }
            else
            {
                // 인증 해제 시 세션 인증 상태도 해제
                await _sessionManager.ClearSessionAuthenticationAsync();
            }
        }
        
        private static async void OnSessionStarted(string sessionId)
        {
            Debug.Log($"[SessionManagerExtension] 세션 시작: {sessionId}");
            
            // 인증된 상태라면 자동으로 토큰 주입
            if (_authManager.IsAuthenticated)
            {
                await _sessionManager.InjectAuthTokenToSessionAsync();
            }
        }
        
        private static void OnSessionEnded()
        {
            Debug.Log("[SessionManagerExtension] 세션 종료");
            
            _isAuthenticatedSession = false;
            _lastAuthenticatedSessionId = null;
        }
        
        private static void OnSessionError(string error)
        {
            Debug.LogError($"[SessionManagerExtension] 세션 오류: {error}");
            
            _isAuthenticatedSession = false;
            _lastAuthenticatedSessionId = null;
        }
        
        private static async UniTask MarkSessionAsAuthenticated(string sessionId)
        {
            _isAuthenticatedSession = true;
            _lastAuthenticatedSessionId = sessionId;
            
            Debug.Log($"[SessionManagerExtension] 세션을 인증됨으로 표시: {sessionId}");
            
            await UniTask.CompletedTask;
        }
        
        private static async UniTask<bool> SendAuthTokenToWebSocket(string accessToken)
        {
            try
            {
                // TODO: WebSocketManager를 통해 인증 토큰 전송
                // 예: { "type": "auth", "token": "Bearer ..." }
                
                Debug.Log("[SessionManagerExtension] WebSocket에 인증 토큰 전송");
                
                // 임시 지연 (실제 구현에서는 WebSocket 메시지 전송)
                await UniTask.Delay(100);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManagerExtension] WebSocket 토큰 전송 실패: {ex.Message}");
                return false;
            }
        }
        
        private static async UniTask SendLogoutSignalToWebSocket()
        {
            try
            {
                // TODO: WebSocketManager를 통해 로그아웃 신호 전송
                // 예: { "type": "logout" }
                
                Debug.Log("[SessionManagerExtension] WebSocket에 로그아웃 신호 전송");
                
                // 임시 지연 (실제 구현에서는 WebSocket 메시지 전송)
                await UniTask.Delay(100);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SessionManagerExtension] WebSocket 로그아웃 신호 전송 실패: {ex.Message}");
            }
        }
        
        #endregion
    }
}
