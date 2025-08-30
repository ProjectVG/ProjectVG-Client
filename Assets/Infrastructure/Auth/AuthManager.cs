using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Auth.Services;
using ProjectVG.Infrastructure.Auth.OAuth2;

namespace ProjectVG.Infrastructure.Auth
{
    /// <summary>
    /// 인증 시스템 전체를 관리하는 파사드/중재자 역할의 매니저
    /// Guest 로그인, OAuth2 로그인, 토큰 자동 갱신 등을 통합 관리
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static AuthManager _instance;
        public static AuthManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AuthManager");
                    _instance = go.AddComponent<AuthManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Private Fields
        
        private TokenManager _tokenManager;
        private TokenRefreshService _tokenRefreshService;
        private GuestAuthService _guestAuthService;
        private ServerOAuth2Provider _oauth2Provider;
        
        private bool _isInitialized = false;
        private bool _isAutoRefreshInProgress = false;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// 현재 로그인 상태 (Access Token이 존재하고 유효한 경우)
        /// </summary>
        public bool IsLoggedIn => _tokenManager?.HasValidTokens ?? false;
        
        /// <summary>
        /// Refresh Token이 존재하고 유효한 경우
        /// </summary>
        public bool HasValidRefreshToken => _tokenManager?.HasRefreshToken == true && 
                                           !_tokenManager.IsRefreshTokenExpired();
        
        /// <summary>
        /// 현재 사용자 ID
        /// </summary>
        public string CurrentUserId => _tokenManager?.CurrentUserId;
        
        /// <summary>
        /// 자동 토큰 갱신 진행 중 여부
        /// </summary>
        public bool IsAutoRefreshInProgress => _isAutoRefreshInProgress;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// 로그인 성공 시 발생하는 이벤트 (Guest, OAuth2 모두 포함)
        /// </summary>
        public event Action<TokenSet> OnLoginSuccess;
        
        /// <summary>
        /// 로그인 실패 시 발생하는 이벤트
        /// </summary>
        public event Action<string> OnLoginFailed;
        
        /// <summary>
        /// 로그아웃 시 발생하는 이벤트
        /// </summary>
        public event Action OnLoggedOut;
        
        /// <summary>
        /// 자동 토큰 갱신 성공 시 발생하는 이벤트
        /// </summary>
        public event Action<string> OnTokenAutoRefreshed;
        
        /// <summary>
        /// 토큰 갱신 실패로 재로그인이 필요한 경우 발생하는 이벤트
        /// </summary>
        public event Action<string> OnReLoginRequired;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAsync().Forget();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private async UniTaskVoid InitializeAsync()
        {
            try
            {
                Debug.Log("[AuthManager] 초기화 시작");
                
                // 의존성 초기화
                _tokenManager = TokenManager.Instance;
                _tokenRefreshService = TokenRefreshService.Instance;
                _guestAuthService = GuestAuthService.Instance;
                
                // TokenManager 이벤트 구독
                _tokenManager.OnTokensUpdated += HandleTokensUpdated;
                _tokenManager.OnTokensExpired += HandleTokensExpired;
                _tokenManager.OnTokensCleared += HandleTokensCleared;
                
                // TokenRefreshService 이벤트 구독
                _tokenRefreshService.OnTokenRefreshed += HandleTokenRefreshed;
                _tokenRefreshService.OnTokenRefreshFailed += HandleTokenRefreshFailed;
                
                // GuestAuthService 이벤트 구독
                _guestAuthService.OnGuestLoginSuccess += HandleGuestLoginSuccess;
                _guestAuthService.OnGuestLoginFailed += HandleGuestLoginFailed;
                
                _isInitialized = true;
                Debug.Log("[AuthManager] 초기화 완료");
                
                // 앱 시작 시 자동 로그인 시도
                await TryAutoLoginAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 초기화 실패: {ex.Message}");
            }
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_tokenManager != null)
            {
                _tokenManager.OnTokensUpdated -= HandleTokensUpdated;
                _tokenManager.OnTokensExpired -= HandleTokensExpired;
                _tokenManager.OnTokensCleared -= HandleTokensCleared;
            }
            
            if (_tokenRefreshService != null)
            {
                _tokenRefreshService.OnTokenRefreshed -= HandleTokenRefreshed;
                _tokenRefreshService.OnTokenRefreshFailed -= HandleTokenRefreshFailed;
            }
            
            if (_guestAuthService != null)
            {
                _guestAuthService.OnGuestLoginSuccess -= HandleGuestLoginSuccess;
                _guestAuthService.OnGuestLoginFailed -= HandleGuestLoginFailed;
            }
        }
        
        #endregion
        
        #region Public Methods - Login
        
        /// <summary>
        /// Guest 로그인 수행
        /// </summary>
        /// <returns>로그인 성공 여부</returns>
        public async UniTask<bool> LoginAsGuestAsync()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AuthManager] AuthManager가 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                Debug.Log("[AuthManager] Guest 로그인 시작");
                return await _guestAuthService.LoginAsGuestAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Guest 로그인 실패: {ex.Message}");
                OnLoginFailed?.Invoke(ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// OAuth2 로그인 수행
        /// </summary>
        /// <returns>로그인 성공 여부</returns>
        public async UniTask<bool> LoginWithOAuth2Async()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AuthManager] AuthManager가 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                Debug.Log("[AuthManager] OAuth2 로그인 시작");
                
                // OAuth2Provider 초기화 (필요한 경우)
                if (_oauth2Provider == null)
                {
                    var config = ProjectVG.Infrastructure.Auth.OAuth2.Config.ServerOAuth2Config.Instance;
                    if (config == null)
                    {
                        throw new InvalidOperationException("ServerOAuth2Config를 찾을 수 없습니다.");
                    }
                    _oauth2Provider = new ServerOAuth2Provider(config);
                }
                
                // OAuth2 로그인 수행
                var tokenSet = await _oauth2Provider.LoginWithServerOAuth2Async();
                
                if (tokenSet?.AccessToken != null)
                {
                    // 토큰 저장
                    _tokenManager.SaveTokens(tokenSet);
                    Debug.Log("[AuthManager] OAuth2 로그인 성공");
                    OnLoginSuccess?.Invoke(tokenSet);
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("OAuth2 로그인에서 유효한 토큰을 받지 못했습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] OAuth2 로그인 실패: {ex.Message}");
                OnLoginFailed?.Invoke(ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// 로그아웃 수행 (토큰 삭제)
        /// </summary>
        public void Logout()
        {
            try
            {
                Debug.Log("[AuthManager] 로그아웃 시작");
                _tokenManager.ClearTokens();
                Debug.Log("[AuthManager] 로그아웃 완료");
                OnLoggedOut?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 로그아웃 실패: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Public Methods - Token Management
        
        /// <summary>
        /// 수동으로 토큰 갱신 요청
        /// </summary>
        /// <returns>갱신 성공 여부</returns>
        public async UniTask<bool> RefreshTokenAsync()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AuthManager] AuthManager가 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                Debug.Log("[AuthManager] 수동 토큰 갱신 시작");
                return await _tokenRefreshService.RefreshAccessTokenAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 수동 토큰 갱신 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 토큰이 곧 만료되는지 확인하고 필요시 갱신
        /// </summary>
        /// <param name="minutesBeforeExpiry">만료 몇 분 전에 갱신할지</param>
        /// <returns>유효한 토큰 보장 여부</returns>
        public async UniTask<bool> EnsureValidTokenAsync(int minutesBeforeExpiry = 5)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AuthManager] AuthManager가 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                // 이미 유효한 토큰이 있고 만료가 임박하지 않은 경우
                /*
                var currentToken = _tokenManager.GetAccessToken();
                if (IsLoggedIn && currentToken != null && !currentToken.IsExpiringSoon(minutesBeforeExpiry))
                {
                    return true;
                }
                */
                
                // 토큰이 만료되었거나 곧 만료될 경우 갱신 시도
                if (HasValidRefreshToken)
                {
                    Debug.Log($"[AuthManager] 토큰이 {minutesBeforeExpiry}분 내에 만료 예정 - 갱신 시도");
                    return await _tokenRefreshService.RefreshAccessTokenAsync();
                }
                
                Debug.LogWarning("[AuthManager] 유효한 Refresh Token이 없습니다.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 토큰 유효성 보장 실패: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Private Methods - Auto Login
        
        /// <summary>
        /// 앱 시작 시 자동 로그인 시도
        /// </summary>
        private async UniTask TryAutoLoginAsync()
        {
            try
            {
                Debug.Log("[AuthManager] 자동 로그인 시도 시작");
                
                // 이미 유효한 Access Token이 있는 경우
                if (IsLoggedIn)
                {
                    Debug.Log("[AuthManager] 이미 유효한 Access Token이 존재합니다.");
                    var tokenSet = _tokenManager.LoadTokens();
                    OnLoginSuccess?.Invoke(tokenSet);
                    return;
                }
                
                // Refresh Token으로 Access Token 재발급 시도
                if (HasValidRefreshToken)
                {
                    Debug.Log("[AuthManager] Refresh Token으로 자동 로그인 시도");
                    _isAutoRefreshInProgress = true;
                    
                    bool refreshSuccess = await _tokenRefreshService.RefreshAccessTokenAsync();
                    
                    _isAutoRefreshInProgress = false;
                    
                    if (refreshSuccess)
                    {
                        Debug.Log("[AuthManager] 자동 로그인 성공");
                        var tokenSet = _tokenManager.LoadTokens();
                        OnLoginSuccess?.Invoke(tokenSet);
                        OnTokenAutoRefreshed?.Invoke("자동 로그인 성공");
                    }
                    else
                    {
                        Debug.LogWarning("[AuthManager] 자동 로그인 실패 - 재로그인 필요");
                        OnReLoginRequired?.Invoke("자동 로그인 실패");
                    }
                }
                else
                {
                    Debug.Log("[AuthManager] 저장된 유효한 토큰이 없습니다 - 로그인 필요");
                    OnReLoginRequired?.Invoke("토큰 없음");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 자동 로그인 시도 실패: {ex.Message}");
                OnReLoginRequired?.Invoke(ex.Message);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleTokensUpdated(TokenSet tokenSet)
        {
            Debug.Log("[AuthManager] 토큰 업데이트됨");
            // 로그인 성공 이벤트는 각 로그인 메서드에서 직접 발생
        }
        
        private void HandleTokensExpired()
        {
            Debug.LogWarning("[AuthManager] 토큰 만료됨 - 자동 갱신 시도");
            // TokenRefreshService가 자동으로 갱신 시도함
        }
        
        private void HandleTokensCleared()
        {
            Debug.Log("[AuthManager] 토큰이 삭제됨");
        }
        
        private void HandleTokenRefreshed(string newAccessToken)
        {
            Debug.Log("[AuthManager] 토큰 갱신 성공");
            OnTokenAutoRefreshed?.Invoke(newAccessToken);
        }
        
        private void HandleTokenRefreshFailed(string error)
        {
            Debug.LogError($"[AuthManager] 토큰 갱신 실패: {error}");
            OnReLoginRequired?.Invoke(error);
        }
        
        private void HandleGuestLoginSuccess(TokenSet tokenSet)
        {
            Debug.Log("[AuthManager] Guest 로그인 성공");
            OnLoginSuccess?.Invoke(tokenSet);
        }
        
        private void HandleGuestLoginFailed(string error)
        {
            Debug.LogError($"[AuthManager] Guest 로그인 실패: {error}");
            OnLoginFailed?.Invoke(error);
        }
        
        #endregion
        
        #region Public Methods - Utility
        
        /// <summary>
        /// 현재 Access Token 반환 (문자열)
        /// </summary>
        /// <returns>Access Token 문자열 (만료된 경우 null)</returns>
        public string GetAccessToken()
        {
            return _tokenManager?.GetAccessToken();
        }
        
        /// <summary>
        /// 현재 Access Token 객체 반환
        /// </summary>
        /// <returns>AccessToken 객체 (없거나 만료된 경우 null)</returns>
        public AccessToken GetCurrentAccessToken()
        {
            if (_tokenManager?.HasValidTokens == true)
            {
                var tokenSet = _tokenManager.LoadTokens();
                return tokenSet?.AccessToken;
            }
            return null;
        }
        
        /// <summary>
        /// Guest 로그인 가능 여부 확인
        /// </summary>
        /// <returns>Guest 로그인 가능 여부</returns>
        public bool CanLoginAsGuest()
        {
            return _guestAuthService?.CanLoginAsGuest() ?? false;
        }
        
        /// <summary>
        /// 디버그 정보 반환
        /// </summary>
        /// <returns>AuthManager 상태 정보</returns>
        public string GetDebugInfo()
        {
            var info = "=== AuthManager Debug Info ===\n";
            info += $"Is Initialized: {_isInitialized}\n";
            info += $"Is Logged In: {IsLoggedIn}\n";
            info += $"Has Valid Refresh Token: {HasValidRefreshToken}\n";
            info += $"Current User ID: {CurrentUserId ?? "None"}\n";
            info += $"Auto Refresh In Progress: {_isAutoRefreshInProgress}\n";
            info += $"Can Login As Guest: {CanLoginAsGuest()}\n";
            
            if (_tokenManager != null)
            {
                var accessToken = GetCurrentAccessToken();
                if (accessToken != null)
                {
                    info += $"Access Token Expires At: {accessToken.ExpiresAt}\n";
                    info += $"Access Token Expires Soon (5min): {accessToken.IsExpiringSoon(5)}\n";
                }
            }
            
            info += "===============================";
            return info;
        }
        
        #endregion
    }
}