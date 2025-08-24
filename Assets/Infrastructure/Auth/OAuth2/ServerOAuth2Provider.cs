using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Auth.OAuth2.Config;
using ProjectVG.Infrastructure.Auth.OAuth2.Models;
using ProjectVG.Infrastructure.Auth.OAuth2.Utils;
using ProjectVG.Infrastructure.Auth.OAuth2.Handlers;
using ProjectVG.Infrastructure.Network.Http;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    /// <summary>
    /// 서버 OAuth2 제공자
    /// 새로운 서버 OAuth2 정책에 따른 구현
    /// </summary>
    public class ServerOAuth2Provider : IServerOAuth2Client
    {
        private readonly ServerOAuth2Config _config;
        private readonly HttpApiClient _httpClient;
        private readonly PKCEParameters _currentPKCE;
        private IOAuth2CallbackHandler _callbackHandler;
        
        public ServerOAuth2Provider(ServerOAuth2Config config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = HttpApiClient.Instance;
            _currentPKCE = null;
            
            // HttpApiClient 초기화 상태 확인
            if (_httpClient == null)
            {
                Debug.LogError("[ServerOAuth2Provider] HttpApiClient.Instance가 null입니다.");
                throw new InvalidOperationException("HttpApiClient가 초기화되지 않았습니다.");
            }
            
            Debug.Log($"[ServerOAuth2Provider] HttpApiClient 초기화 상태: {_httpClient.IsInitialized}");
            
            // 플랫폼별 콜백 핸들러 생성
            _callbackHandler = OAuth2CallbackHandlerFactory.CreateHandler();
            Debug.Log($"[ServerOAuth2Provider] {_callbackHandler.PlatformName} 콜백 핸들러 생성됨");
        }
        
        #region IServerOAuth2Client Properties
        
        /// <summary>
        /// OAuth2 설정이 유효한지 확인
        /// </summary>
        public bool IsConfigured => _config != null && _config.IsValid();
        
        #endregion
        
        #region IServerOAuth2Client Implementation
        
        /// <summary>
        /// PKCE 파라미터 생성 (Code Verifier, Code Challenge, State)
        /// </summary>
        public async Task<PKCEParameters> GeneratePKCEAsync()
        {
            try
            {
                Debug.Log("[ServerOAuth2Provider] PKCE 파라미터 생성 시작");
                
                var pkce = await PKCEGenerator.GeneratePKCEAsync(
                    _config.PKCECodeVerifierLength,
                    _config.StateLength
                );
                
                if (!PKCEGenerator.ValidatePKCE(pkce))
                {
                    throw new InvalidOperationException("생성된 PKCE 파라미터가 유효하지 않습니다.");
                }
                
                Debug.Log($"[ServerOAuth2Provider] PKCE 생성 완료 - State: {pkce.State}");
                return pkce;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerOAuth2Provider] PKCE 생성 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 서버 OAuth2 인증 시작
        /// </summary>
        /// <param name="pkce">PKCE 파라미터</param>
        /// <param name="scope">OAuth2 스코프</param>
        /// <returns>Google OAuth2 URL</returns>
        public async Task<string> StartServerOAuth2Async(PKCEParameters pkce, string scope)
        {
            if (pkce == null || !pkce.IsValid())
            {
                throw new ArgumentException("유효하지 않은 PKCE 파라미터입니다.", nameof(pkce));
            }
            
            if (string.IsNullOrEmpty(scope))
            {
                scope = _config.Scope;
            }
            
            try
            {
                Debug.Log("[ServerOAuth2Provider] 서버 OAuth2 인증 시작");
                Debug.Log($"[ServerOAuth2Provider] HttpApiClient 초기화 상태: {_httpClient.IsInitialized}");
                
                // 1. JavaScript와 동일한 방식으로 쿼리 파라미터 생성
                var queryParams = new Dictionary<string, string>
                {
                    { "client_id", _config.ClientId },
                    { "redirect_uri", _config.GetCurrentPlatformRedirectUri() },
                    { "response_type", "code" },
                    { "scope", scope },
                    { "state", pkce.State },
                    { "code_challenge", pkce.CodeChallenge },
                    { "code_challenge_method", "S256" },
                    { "code_verifier", pkce.CodeVerifier },
                    { "client_redirect_uri", GetClientRedirectUri() }
                };
                
                Debug.Log($"[ServerOAuth2Provider] 쿼리 파라미터: {JsonConvert.SerializeObject(queryParams)}");
                
                // 2. 쿼리 파라미터를 URL에 추가
                var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var authorizeUrl = $"{_config.ServerUrl}/auth/oauth2/authorize?{queryString}";
                
                Debug.Log($"[ServerOAuth2Provider] 최종 URL: {authorizeUrl}");
                
                // 3. 서버 API 호출 (GET 요청) - 인증 불필요
                var response = await _httpClient.GetAsync<ServerOAuth2AuthorizeResponse>(authorizeUrl, requiresAuth: false);
                
                if (response == null)
                {
                    throw new InvalidOperationException("서버 응답이 null입니다.");
                }
                
                if (!response.Success)
                {
                    throw new InvalidOperationException($"서버 OAuth2 인증 실패: {response.Message}");
                }
                
                if (string.IsNullOrEmpty(response.AuthUrl))
                {
                    throw new InvalidOperationException("서버에서 반환된 인증 URL이 비어있습니다.");
                }
                
                Debug.Log($"[ServerOAuth2Provider] Google OAuth2 URL 생성 완료: {response.AuthUrl}");
                return response.AuthUrl;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerOAuth2Provider] 서버 OAuth2 인증 시작 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// OAuth2 콜백 처리 (redirect URL에서 state 추출)
        /// </summary>
        /// <param name="callbackUrl">콜백 URL</param>
        /// <returns>성공 여부와 state 값</returns>
        public async Task<(bool success, string state)> HandleOAuth2CallbackAsync(string callbackUrl)
        {
            if (string.IsNullOrEmpty(callbackUrl))
            {
                Debug.LogError("[ServerOAuth2Provider] 콜백 URL이 비어있습니다.");
                return (false, null);
            }
            
            try
            {
                Debug.Log($"[ServerOAuth2Provider] OAuth2 콜백 처리: {callbackUrl}");
                
                // URL 파싱
                OAuth2CallbackResult callbackResult;
                
                if (OAuth2CallbackParser.IsCustomScheme(callbackUrl))
                {
                    // 커스텀 스킴 URL (모바일 앱)
                    callbackResult = OAuth2CallbackParser.ParseSchemeUrl(callbackUrl);
                }
                else
                {
                    // 일반 URL (WebGL, 데스크톱)
                    callbackResult = OAuth2CallbackParser.ParseCallbackUrl(callbackUrl);
                }
                
                if (!callbackResult.Success)
                {
                    Debug.LogError($"[ServerOAuth2Provider] 콜백 파싱 실패: {callbackResult.Error}");
                    return (false, null);
                }
                
                if (string.IsNullOrEmpty(callbackResult.State))
                {
                    Debug.LogError("[ServerOAuth2Provider] State 파라미터가 비어있습니다.");
                    return (false, null);
                }
                
                Debug.Log($"[ServerOAuth2Provider] 콜백 처리 성공 - State: {callbackResult.State}");
                return (true, callbackResult.State);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerOAuth2Provider] 콜백 처리 실패: {ex.Message}");
                return (false, null);
            }
        }
        
        /// <summary>
        /// 서버에서 토큰 요청
        /// </summary>
        /// <param name="state">OAuth2 state 값</param>
        /// <returns>JWT 토큰 세트</returns>
        public async Task<TokenSet> RequestTokenAsync(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                throw new ArgumentException("State 파라미터가 비어있습니다.", nameof(state));
            }
            
            try
            {
                Debug.Log($"[ServerOAuth2Provider] 토큰 요청 시작 - State: {state}");
                
                // 1. 서버 토큰 API 호출 (HTTP 헤더 포함) - 인증 불필요
                var tokenUrl = $"{_config.ServerUrl}/auth/oauth2/token?state={Uri.EscapeDataString(state)}";
                var (response, headers) = await _httpClient.GetWithHeadersAsync<ServerOAuth2TokenResponse>(tokenUrl, requiresAuth: false);
                
                if (response == null)
                {
                    throw new InvalidOperationException("토큰 응답이 null입니다.");
                }
                
                if (!response.Success)
                {
                    throw new InvalidOperationException($"토큰 요청 실패: {response.Message}");
                }
                
                // 2. HTTP 헤더에서 토큰 정보 추출 (JavaScript와 동일한 방식)
                var accessToken = headers.ContainsKey("X-Access-Token") ? headers["X-Access-Token"] : null;
                var refreshToken = headers.ContainsKey("X-Refresh-Token") ? headers["X-Refresh-Token"] : null;
                var expiresInStr = headers.ContainsKey("X-Expires-In") ? headers["X-Expires-In"] : null;
                var userId = headers.ContainsKey("X-User-Id") ? headers["X-User-Id"] : null;
                
                // 헤더에서 값을 가져올 수 없는 경우 응답 본문에서 가져오기 (폴백)
                if (string.IsNullOrEmpty(accessToken))
                    accessToken = response.AccessToken;
                if (string.IsNullOrEmpty(refreshToken))
                    refreshToken = response.RefreshToken;
                if (string.IsNullOrEmpty(userId))
                    userId = response.UserId;
                    
                var expiresIn = 3600; // 기본값 1시간
                if (!string.IsNullOrEmpty(expiresInStr) && int.TryParse(expiresInStr, out var parsedExpiresIn))
                    expiresIn = parsedExpiresIn;
                else if (response.ExpiresIn > 0)
                    expiresIn = response.ExpiresIn;
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("Access Token이 비어있습니다.");
                }
                
                // 3. TokenSet 생성
                var accessTokenModel = new AccessToken(accessToken, expiresIn, "Bearer", "oauth2");
                var refreshTokenModel = !string.IsNullOrEmpty(refreshToken) 
                    ? new RefreshToken(refreshToken, expiresIn * 2, userId)  // userId를 DeviceId로 사용
                    : null;
                
                var tokenSet = new TokenSet(accessTokenModel, refreshTokenModel);
                
                Debug.Log($"[ServerOAuth2Provider] 토큰 요청 성공 - UserId: {userId}");
                
                // 토큰 수신 확인 로그
                Debug.Log("=== 🔐 서버에서 토큰 수신 완료 ===");
                Debug.Log($"Access Token: {accessToken?.Substring(0, Math.Min(20, accessToken?.Length ?? 0))}...");
                Debug.Log($"Refresh Token: {(string.IsNullOrEmpty(refreshToken) ? "없음" : refreshToken.Substring(0, Math.Min(20, refreshToken.Length)) + "...")}");
                Debug.Log($"Expires In: {expiresIn}초");
                Debug.Log($"User ID: {userId}");
                Debug.Log("=== 토큰 수신 완료 ===");
                
                // TokenManager에 토큰 저장
                try
                {
                    Debug.Log("=== 🔐 ServerOAuth2Provider TokenManager 저장 시작 ===");
                    var tokenManager = TokenManager.Instance;
                    tokenManager.SaveTokens(tokenSet);
                    Debug.Log("[ServerOAuth2Provider] TokenManager에 토큰 저장 완료");
                    Debug.Log("=== TokenManager 저장 완료 ===");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServerOAuth2Provider] TokenManager 토큰 저장 실패: {ex.Message}");
                    // 토큰 저장 실패해도 토큰은 반환 (사용자가 직접 저장할 수 있도록)
                }
                
                return tokenSet;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerOAuth2Provider] 토큰 요청 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 전체 OAuth2 로그인 플로우 (편의 메서드)
        /// </summary>
        /// <param name="scope">OAuth2 스코프</param>
        /// <returns>JWT 토큰 세트</returns>
        public async Task<TokenSet> LoginWithServerOAuth2Async(string scope)
        {
            try
            {
                Debug.Log("[ServerOAuth2Provider] 전체 OAuth2 로그인 플로우 시작");
                
                // 1. PKCE 파라미터 생성
                var pkce = await GeneratePKCEAsync();
                
                // 2. 서버 OAuth2 인증 시작
                var authUrl = await StartServerOAuth2Async(pkce, scope);
                
                // 3. 브라우저에서 OAuth2 로그인 진행
                var browserResult = await OpenOAuth2BrowserAsync(authUrl);
                if (!browserResult.Success)
                {
                    throw new InvalidOperationException($"브라우저 열기 실패: {browserResult.Error}");
                }
                
                // 4. 콜백 대기 및 처리
                var callbackResult = await WaitForOAuth2CallbackAsync(pkce.State);
                if (!callbackResult.success)
                {
                    throw new InvalidOperationException("OAuth2 콜백 처리 실패");
                }
                
                // 5. 토큰 요청
                var tokenSet = await RequestTokenAsync(callbackResult.state);
                
                Debug.Log("[ServerOAuth2Provider] 전체 OAuth2 로그인 플로우 완료");
                return tokenSet;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerOAuth2Provider] 전체 OAuth2 로그인 플로우 실패: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 클라이언트 리다이렉트 URI 생성
        /// </summary>
        /// <returns>클라이언트 리다이렉트 URI</returns>
        private string GetClientRedirectUri()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL에서는 현재 페이지 URL 사용
            return Application.absoluteURL;
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Android에서는 커스텀 스킴 사용
            return _config.GetCurrentPlatformRedirectUri();
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS에서는 커스텀 스킴 사용
            return _config.GetCurrentPlatformRedirectUri();
#else
            // 데스크톱에서는 로컬 서버 URL 사용
            return _config.GetCurrentPlatformRedirectUri();
#endif
        }
        
        /// <summary>
        /// OAuth2 브라우저 열기
        /// </summary>
        /// <param name="authUrl">인증 URL</param>
        /// <returns>브라우저 열기 결과</returns>
        private async Task<OAuth2BrowserResult> OpenOAuth2BrowserAsync(string authUrl)
        {
            try
            {
                Debug.Log($"[ServerOAuth2Provider] OAuth2 브라우저 열기: {authUrl}");
                
                var platform = _config.GetCurrentPlatformName();
                string browserType;
                
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL에서는 현재 탭에서 리다이렉트
                Application.ExternalEval($"window.location.href = '{authUrl}';");
                browserType = "Current Tab";
#elif UNITY_ANDROID && !UNITY_EDITOR
                // Android에서는 기본 브라우저 사용
                Application.OpenURL(authUrl);
                browserType = "Default Browser";
#elif UNITY_IOS && !UNITY_EDITOR
                // iOS에서는 기본 브라우저 사용
                Application.OpenURL(authUrl);
                browserType = "Default Browser";
#else
                // 데스크톱에서는 기본 브라우저 사용
                Application.OpenURL(authUrl);
                browserType = "Default Browser";
#endif
                
                // 브라우저 열기 대기
                await UniTask.Delay(1000);
                
                return OAuth2BrowserResult.SuccessResult(authUrl, platform, browserType);
            }
            catch (Exception ex)
            {
                var platform = _config.GetCurrentPlatformName();
                return OAuth2BrowserResult.ErrorResult(ex.Message, platform);
            }
        }
        
        /// <summary>
        /// OAuth2 콜백 대기
        /// </summary>
        /// <param name="expectedState">예상되는 State 값</param>
        /// <returns>콜백 처리 결과</returns>
        private async Task<(bool success, string state)> WaitForOAuth2CallbackAsync(string expectedState)
        {
            try
            {
                Debug.Log($"[ServerOAuth2Provider] OAuth2 콜백 대기 시작 - State: {expectedState}");
                
                // 콜백 핸들러 초기화
                await _callbackHandler.InitializeAsync(expectedState, _config.TimeoutSeconds);
                
                // 콜백 대기
                var callbackUrl = await _callbackHandler.WaitForCallbackAsync();
                
                if (!string.IsNullOrEmpty(callbackUrl))
                {
                    var result = await HandleOAuth2CallbackAsync(callbackUrl);
                    if (result.success && result.state == expectedState)
                    {
                        Debug.Log("[ServerOAuth2Provider] OAuth2 콜백 수신 완료");
                        return result;
                    }
                }
                
                Debug.LogError("[ServerOAuth2Provider] OAuth2 콜백 타임아웃 또는 실패");
                return (false, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerOAuth2Provider] OAuth2 콜백 대기 중 오류: {ex.Message}");
                return (false, null);
            }
            finally
            {
                // 콜백 핸들러 정리
                _callbackHandler?.Cleanup();
            }
        }
        
        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        /// <returns>디버그 정보</returns>
        public string GetDebugInfo()
        {
            var info = $"ServerOAuth2Provider Debug Info:\n";
            info += $"Is Configured: {IsConfigured}\n";
            info += $"Server URL: {_config?.ServerUrl}\n";
            info += $"Client ID: {_config?.ClientId}\n";
            info += $"Scope: {_config?.Scope}\n";
            info += $"Platform: {_config?.GetCurrentPlatformName()}\n";
            info += $"Redirect URI: {_config?.GetCurrentPlatformRedirectUri()}\n";
            info += $"Client Redirect URI: {GetClientRedirectUri()}\n";
            
            return info;
        }
        
        #endregion
    }
}
