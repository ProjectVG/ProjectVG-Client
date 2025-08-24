using System;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.OAuth2;
using ProjectVG.Infrastructure.Auth.OAuth2.Config;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Auth.OAuth2.Models;

namespace ProjectVG.Infrastructure.Auth
{
    /// <summary>
    /// 토큰 갱신 서비스
    /// Refresh Token을 사용하여 Access Token을 자동으로 갱신
    /// </summary>
    public class TokenRefreshService : MonoBehaviour
    {
        private static TokenRefreshService _instance;
        public static TokenRefreshService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TokenRefreshService");
                    _instance = go.AddComponent<TokenRefreshService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private TokenManager _tokenManager;
        private ServerOAuth2Provider _oauth2Provider;
        private bool _isRefreshing = false;
        
        public event Action<string> OnTokenRefreshed;
        public event Action<string> OnTokenRefreshFailed;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            _tokenManager = TokenManager.Instance;
            
            // TokenManager 이벤트 구독
            _tokenManager.OnTokensExpired += HandleTokensExpired;
            
            Debug.Log("[TokenRefreshService] 초기화 완료");
        }
        
        /// <summary>
        /// 토큰 만료 시 자동 갱신 시도
        /// </summary>
        private async void HandleTokensExpired()
        {
            Debug.Log("[TokenRefreshService] 토큰 만료 감지 - 갱신 시도");
            await RefreshAccessTokenAsync();
        }
        
        /// <summary>
        /// Access Token 갱신
        /// </summary>
        public async UniTask<bool> RefreshAccessTokenAsync()
        {
            if (_isRefreshing)
            {
                Debug.Log("[TokenRefreshService] 이미 토큰 갱신 중입니다.");
                return false;
            }
            
            if (_tokenManager.IsRefreshTokenExpired())
            {
                Debug.LogError("[TokenRefreshService] Refresh Token이 만료되었습니다. 재로그인이 필요합니다.");
                OnTokenRefreshFailed?.Invoke("Refresh Token이 만료되었습니다.");
                return false;
            }
            
            _isRefreshing = true;
            
            try
            {
                Debug.Log("[TokenRefreshService] Access Token 갱신 시작");
                
                // OAuth2 Provider 초기화 (필요한 경우)
                if (_oauth2Provider == null)
                {
                    var config = ServerOAuth2Config.Instance;
                    if (config == null)
                    {
                        throw new InvalidOperationException("ServerOAuth2Config를 찾을 수 없습니다.");
                    }
                    _oauth2Provider = new ServerOAuth2Provider(config);
                }
                
                // Refresh Token으로 새로운 Access Token 요청
                var refreshToken = _tokenManager.GetRefreshToken();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    throw new InvalidOperationException("Refresh Token이 없습니다.");
                }
                
                // 서버에 토큰 갱신 요청
                var newTokenSet = await RequestTokenRefreshAsync(refreshToken);
                
                if (newTokenSet?.AccessToken != null)
                {
                    // 새로운 토큰 저장
                    _tokenManager.SaveTokens(newTokenSet);
                    
                    Debug.Log("[TokenRefreshService] Access Token 갱신 성공");
                    OnTokenRefreshed?.Invoke(newTokenSet.AccessToken.Token);
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("서버에서 새로운 토큰을 받지 못했습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenRefreshService] Access Token 갱신 실패: {ex.Message}");
                OnTokenRefreshFailed?.Invoke(ex.Message);
                return false;
            }
            finally
            {
                _isRefreshing = false;
            }
        }
        
        /// <summary>
        /// 서버에 토큰 갱신 요청
        /// </summary>
        private async UniTask<TokenSet> RequestTokenRefreshAsync(string refreshToken)
        {
            try
            {
                var httpClient = HttpApiClient.Instance;
                
                // 서버 토큰 갱신 엔드포인트 호출
                var refreshRequest = new
                {
                    refresh_token = refreshToken,
                    grant_type = "refresh_token"
                };
                
                var response = await httpClient.PostAsync<ServerOAuth2TokenResponse>(
                    "/auth/oauth2/refresh",
                    refreshRequest,
                    requiresAuth: false  // 토큰 갱신은 인증 불필요
                );
                
                if (response == null || !response.Success)
                {
                    throw new InvalidOperationException($"토큰 갱신 실패: {response?.Message ?? "응답이 null입니다."}");
                }
                
                // 새로운 토큰 생성
                var accessToken = new AccessToken(
                    response.AccessToken,
                    response.ExpiresIn,
                    "Bearer",
                    "oauth2"
                );
                
                var newRefreshToken = !string.IsNullOrEmpty(response.RefreshToken)
                    ? new RefreshToken(response.RefreshToken, response.ExpiresIn * 2, response.UserId)  // UserId를 DeviceId로 사용
                    : null;
                
                return new TokenSet(accessToken, newRefreshToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenRefreshService] 서버 토큰 갱신 요청 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 토큰 갱신 상태 확인
        /// </summary>
        public bool IsRefreshing => _isRefreshing;
        
        /// <summary>
        /// 강제 토큰 갱신 (사용자가 직접 호출)
        /// </summary>
        public async UniTask<bool> ForceRefreshAsync()
        {
            Debug.Log("[TokenRefreshService] 강제 토큰 갱신 시작");
            return await RefreshAccessTokenAsync();
        }
        
        /// <summary>
        /// 토큰 상태 확인 및 필요시 갱신
        /// </summary>
        public async UniTask<bool> EnsureValidTokenAsync()
        {
            if (_tokenManager.HasValidTokens)
            {
                return true;
            }
            
            if (_tokenManager.HasRefreshToken && !_tokenManager.IsRefreshTokenExpired())
            {
                return await RefreshAccessTokenAsync();
            }
            
            Debug.LogWarning("[TokenRefreshService] 유효한 토큰이 없고 Refresh Token도 만료되었습니다.");
            return false;
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            var info = "TokenRefreshService Debug Info:\n";
            info += $"Is Refreshing: {_isRefreshing}\n";
            info += $"Has Valid Tokens: {_tokenManager.HasValidTokens}\n";
            info += $"Has Refresh Token: {_tokenManager.HasRefreshToken}\n";
            info += $"Access Token Expired: {_tokenManager.IsAccessTokenExpired()}\n";
            info += $"Refresh Token Expired: {_tokenManager.IsRefreshTokenExpired()}\n";
            return info;
        }
        
        private void OnDestroy()
        {
            if (_tokenManager != null)
            {
                _tokenManager.OnTokensExpired -= HandleTokensExpired;
            }
        }
    }
}
