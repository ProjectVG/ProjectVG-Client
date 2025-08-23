using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public class OAuth2Client : IOAuth2Client
    {
        private OAuth2Config _config;
        private PKCEHelper _pkceHelper;
        
        public bool IsConfigured => _config != null;
        
        public OAuth2Client()
        {
            _pkceHelper = new PKCEHelper();
            LoadConfiguration();
        }
        
        #region Public Methods
        
        public async UniTask<bool> StartAuthorizationAsync()
        {
            if (!IsConfigured)
            {
                Debug.LogError("[OAuth2Client] OAuth2 설정이 없습니다.");
                return false;
            }
            
            try
            {
                var state = GenerateState();
                var codeChallenge = _pkceHelper.GenerateCodeChallenge();
                var authUrl = GenerateAuthorizationUrl(state, codeChallenge);
                
                return await OpenBrowserAsync(authUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OAuth2Client] 인증 시작 실패: {ex.Message}");
                return false;
            }
        }
        
        public async UniTask<TokenSet> ExchangeCodeForTokensAsync(string code, string state)
        {
            if (!IsConfigured)
            {
                Debug.LogError("[OAuth2Client] OAuth2 설정이 없습니다.");
                return null;
            }
            
            try
            {
                Debug.Log($"[OAuth2Client] 코드 교환 시작: {code}");
                
                // TODO: HTTP 요청으로 서버에 코드 교환 요청
                await UniTask.Delay(1000); // 임시 지연
                
                // 임시 토큰 생성 (실제로는 서버 응답에서 파싱)
                var accessToken = new AccessToken("temp_access_token", 900); // 15분
                var refreshToken = new RefreshToken("temp_refresh_token", 2592000); // 30일
                
                return new TokenSet(accessToken, refreshToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OAuth2Client] 코드 교환 실패: {ex.Message}");
                return null;
            }
        }
        
        public async UniTask<TokenSet> RefreshTokenAsync(RefreshToken refreshToken)
        {
            if (!IsConfigured)
            {
                Debug.LogError("[OAuth2Client] OAuth2 설정이 없습니다.");
                return null;
            }
            
            try
            {
                Debug.Log("[OAuth2Client] 토큰 갱신 시작");
                
                // TODO: HTTP 요청으로 서버에 토큰 갱신 요청
                await UniTask.Delay(500); // 임시 지연
                
                // 임시 토큰 생성 (실제로는 서버 응답에서 파싱)
                var newAccessToken = new AccessToken("refreshed_access_token", 900); // 15분
                var newRefreshToken = new RefreshToken("new_refresh_token", 2592000); // 30일 (로테이션)
                
                return new TokenSet(newAccessToken, newRefreshToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OAuth2Client] 토큰 갱신 실패: {ex.Message}");
                return null;
            }
        }
        
        public string GenerateAuthorizationUrl(string state, string codeChallenge)
        {
            if (!IsConfigured) return null;
            
            var parameters = new System.Text.StringBuilder();
            parameters.Append($"response_type=code");
            parameters.Append($"&client_id={_config.ClientId}");
            parameters.Append($"&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}");
            parameters.Append($"&scope={Uri.EscapeDataString(_config.Scope)}");
            parameters.Append($"&state={state}");
            parameters.Append($"&code_challenge={codeChallenge}");
            parameters.Append($"&code_challenge_method=S256");
            
            return $"{_config.AuthorizationEndpoint}?{parameters}";
        }
        
        public bool ValidateState(string receivedState, string expectedState)
        {
            return !string.IsNullOrEmpty(receivedState) && 
                   !string.IsNullOrEmpty(expectedState) && 
                   receivedState == expectedState;
        }
        
        #endregion
        
        #region Private Methods
        
        private void LoadConfiguration()
        {
            // TODO: Resources나 Config에서 OAuth2 설정 로드
            _config = new OAuth2Config
            {
                ClientId = "your_client_id",
                AuthorizationEndpoint = "https://your-auth-server.com/oauth2/authorize",
                TokenEndpoint = "https://your-auth-server.com/oauth2/token",
                RedirectUri = GetPlatformRedirectUri(),
                Scope = "openid profile offline_access"
            };
        }
        
        private string GetPlatformRedirectUri()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return "https://your-game.com/auth/callback";
#elif UNITY_STANDALONE && !UNITY_EDITOR
            return "http://127.0.0.1:8080/callback";
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return "com.yourcompany.yourgame://auth/callback";
#else
            return "http://127.0.0.1:8080/callback"; // 에디터용
#endif
        }
        
        private async UniTask<bool> OpenBrowserAsync(string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return await OpenWebGLBrowserAsync(url);
#elif UNITY_STANDALONE && !UNITY_EDITOR
            return await OpenDesktopBrowserAsync(url);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return await OpenMobileBrowserAsync(url);
#else
            // 에디터에서는 시스템 브라우저로 열기
            Application.OpenURL(url);
            Debug.Log($"[OAuth2Client] 에디터에서 브라우저 열기: {url}");
            return true;
#endif
        }
        
        private async UniTask<bool> OpenWebGLBrowserAsync(string url)
        {
            try
            {
                Application.OpenURL(url);
                await UniTask.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OAuth2Client] WebGL 브라우저 열기 실패: {ex.Message}");
                return false;
            }
        }
        
        private async UniTask<bool> OpenDesktopBrowserAsync(string url)
        {
            try
            {
                Application.OpenURL(url);
                await UniTask.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OAuth2Client] 데스크톱 브라우저 열기 실패: {ex.Message}");
                return false;
            }
        }
        
        private async UniTask<bool> OpenMobileBrowserAsync(string url)
        {
            try
            {
                Application.OpenURL(url);
                await UniTask.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OAuth2Client] 모바일 브라우저 열기 실패: {ex.Message}");
                return false;
            }
        }
        
        private string GenerateState()
        {
            return Guid.NewGuid().ToString("N");
        }
        
        #endregion
    }
}
