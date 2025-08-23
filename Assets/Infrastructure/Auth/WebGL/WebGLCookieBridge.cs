using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Network.Http;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.WebGL
{
    public class WebGLCookieBridge : IWebGLCookieBridge
    {
        private const string REFRESH_TOKEN_COOKIE_NAME = "auth_refresh_token";
        private const string CSRF_TOKEN_COOKIE_NAME = "csrf_token";
        private const string SESSION_COOKIE_NAME = "auth_session";
        
        public bool IsAvailable { get; private set; }
        public bool IsBFFModeEnabled { get; private set; }
        
        public event Action<string> OnCookieError;
        
        public WebGLCookieBridge()
        {
            InitializeWebGLCookieBridge();
        }
        
        #region External JavaScript Functions
        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SetHttpOnlyCookie(string name, string value, int maxAgeSeconds, string sameSite);
        
        [DllImport("__Internal")]
        private static extern string GetCookieValue(string name);
        
        [DllImport("__Internal")]
        private static extern void DeleteCookie(string name);
        
        [DllImport("__Internal")]
        private static extern string GenerateCSRFToken();
        
        [DllImport("__Internal")]
        private static extern bool IsCookieSupported();
        
        [DllImport("__Internal")]
        private static extern void ClearAllAuthCookies();
#endif
        
        #endregion
        
        #region Public Methods
        
        public async UniTask<bool> SetRefreshTokenCookieAsync(RefreshToken token)
        {
            if (!IsAvailable)
            {
                OnCookieError?.Invoke("WebGL 쿠키를 사용할 수 없습니다.");
                return false;
            }
            
            if (token == null)
            {
                OnCookieError?.Invoke("null 토큰을 쿠키에 설정할 수 없습니다.");
                return false;
            }
            
            try
            {
                if (IsBFFModeEnabled)
                {
                    return await SetRefreshTokenViaBFFAsync(token);
                }
                else
                {
                    return await SetRefreshTokenViaJSAsync(token);
                }
            }
            catch (Exception ex)
            {
                var error = $"Refresh 토큰 쿠키 설정 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return false;
            }
        }
        
        public async UniTask<RefreshToken> GetRefreshTokenFromCookieAsync()
        {
            if (!IsAvailable)
            {
                OnCookieError?.Invoke("WebGL 쿠키를 사용할 수 없습니다.");
                return null;
            }
            
            try
            {
                if (IsBFFModeEnabled)
                {
                    return await GetRefreshTokenViaBFFAsync();
                }
                else
                {
                    return await GetRefreshTokenViaJSAsync();
                }
            }
            catch (Exception ex)
            {
                var error = $"Refresh 토큰 쿠키 읽기 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return null;
            }
        }
        
        public async UniTask<bool> ClearRefreshTokenCookieAsync()
        {
            if (!IsAvailable)
            {
                return false;
            }
            
            try
            {
                if (IsBFFModeEnabled)
                {
                    return await ClearRefreshTokenViaBFFAsync();
                }
                else
                {
                    return await ClearRefreshTokenViaJSAsync();
                }
            }
            catch (Exception ex)
            {
                var error = $"Refresh 토큰 쿠키 삭제 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return false;
            }
        }
        
        public async UniTask<string> GetCSRFTokenAsync()
        {
            if (!IsAvailable)
            {
                return null;
            }
            
            try
            {
                if (IsBFFModeEnabled)
                {
                    return await GetCSRFTokenViaBFFAsync();
                }
                else
                {
                    return await GetCSRFTokenViaJSAsync();
                }
            }
            catch (Exception ex)
            {
                var error = $"CSRF 토큰 읽기 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return null;
            }
        }
        
        public async UniTask<bool> ValidateCSRFTokenAsync(string token)
        {
            if (!IsAvailable || string.IsNullOrEmpty(token))
            {
                return false;
            }
            
            try
            {
                var currentToken = await GetCSRFTokenAsync();
                return !string.IsNullOrEmpty(currentToken) && currentToken == token;
            }
            catch (Exception ex)
            {
                var error = $"CSRF 토큰 검증 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return false;
            }
        }
        
        public async UniTask<bool> SetAuthenticationCookieAsync(string sessionId, TimeSpan? maxAge = null)
        {
            if (!IsAvailable || string.IsNullOrEmpty(sessionId))
            {
                return false;
            }
            
            try
            {
                var maxAgeSeconds = (int)(maxAge?.TotalSeconds ?? 3600); // 기본 1시간
                
                if (IsBFFModeEnabled)
                {
                    return await SetSessionViaBFFAsync(sessionId, maxAgeSeconds);
                }
                else
                {
                    return await SetSessionViaJSAsync(sessionId, maxAgeSeconds);
                }
            }
            catch (Exception ex)
            {
                var error = $"인증 쿠키 설정 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return false;
            }
        }
        
        public async UniTask<bool> ClearAuthenticationCookieAsync()
        {
            if (!IsAvailable)
            {
                return false;
            }
            
            try
            {
                if (IsBFFModeEnabled)
                {
                    return await ClearSessionViaBFFAsync();
                }
                else
                {
                    return await ClearSessionViaJSAsync();
                }
            }
            catch (Exception ex)
            {
                var error = $"인증 쿠키 삭제 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return false;
            }
        }
        
        public async UniTask<bool> TestCookieAccessibilityAsync()
        {
            if (!IsAvailable)
            {
                return false;
            }
            
            try
            {
                var testKey = "test_cookie";
                var testValue = "test_value";
                
#if UNITY_WEBGL && !UNITY_EDITOR
                SetHttpOnlyCookie(testKey, testValue, 60, "Lax");
                await UniTask.Delay(100);
                
                var retrievedValue = GetCookieValue(testKey);
                var success = retrievedValue == testValue;
                
                DeleteCookie(testKey);
                
                Debug.Log($"[WebGLCookieBridge] 쿠키 접근성 테스트: {(success ? "성공" : "실패")}");
                return success;
#else
                await UniTask.Delay(100);
                Debug.Log("[WebGLCookieBridge] 에디터에서는 쿠키 테스트를 수행할 수 없습니다.");
                return true; // 에디터에서는 항상 성공으로 처리
#endif
            }
            catch (Exception ex)
            {
                var error = $"쿠키 접근성 테스트 실패: {ex.Message}";
                Debug.LogError($"[WebGLCookieBridge] {error}");
                OnCookieError?.Invoke(error);
                return false;
            }
        }
        
        public void EnableBFFMode(bool enabled)
        {
            IsBFFModeEnabled = enabled;
            Debug.Log($"[WebGLCookieBridge] BFF 모드: {(enabled ? "활성화" : "비활성화")}");
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeWebGLCookieBridge()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                IsAvailable = IsCookieSupported();
#else
                IsAvailable = false;
                Debug.LogWarning("[WebGLCookieBridge] WebGL 플랫폼이 아닙니다.");
#endif
                
                IsBFFModeEnabled = false; // 기본적으로 BFF 모드 비활성화
                
                if (IsAvailable)
                {
                    Debug.Log("[WebGLCookieBridge] WebGL 쿠키 브리지 초기화 성공");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCookieBridge] 초기화 실패: {ex.Message}");
                IsAvailable = false;
            }
        }
        
        // BFF 모드 메서드들
        private async UniTask<bool> SetRefreshTokenViaBFFAsync(RefreshToken token)
        {
            var apiClient = HttpApiClient.Instance;
            if (apiClient == null)
            {
                throw new InvalidOperationException("HttpApiClient를 사용할 수 없습니다.");
            }
            
            var request = new { refresh_token = token.Token, expires_in = (int)token.TimeUntilExpiry.TotalSeconds };
            var response = await apiClient.PostAsync<object>("/auth/set-refresh-cookie", request);
            
            return response != null;
        }
        
        private async UniTask<RefreshToken> GetRefreshTokenViaBFFAsync()
        {
            var apiClient = HttpApiClient.Instance;
            if (apiClient == null)
            {
                throw new InvalidOperationException("HttpApiClient를 사용할 수 없습니다.");
            }
            
            var response = await apiClient.GetAsync<dynamic>("/auth/validate-refresh-cookie");
            
            if (response?.is_valid == true)
            {
                // BFF에서는 토큰 값 자체를 반환하지 않고 유효성만 확인
                return new RefreshToken("bff_token", 3600); // 임시 토큰
            }
            
            return null;
        }
        
        private async UniTask<bool> ClearRefreshTokenViaBFFAsync()
        {
            var apiClient = HttpApiClient.Instance;
            if (apiClient == null)
            {
                throw new InvalidOperationException("HttpApiClient를 사용할 수 없습니다.");
            }
            
            var response = await apiClient.PostAsync<object>("/auth/clear-refresh-cookie", null);
            return response != null;
        }
        
        private async UniTask<string> GetCSRFTokenViaBFFAsync()
        {
            var apiClient = HttpApiClient.Instance;
            if (apiClient == null)
            {
                throw new InvalidOperationException("HttpApiClient를 사용할 수 없습니다.");
            }
            
            var response = await apiClient.GetAsync<dynamic>("/auth/csrf-token");
            return response?.token?.ToString();
        }
        
        private async UniTask<bool> SetSessionViaBFFAsync(string sessionId, int maxAgeSeconds)
        {
            var apiClient = HttpApiClient.Instance;
            if (apiClient == null)
            {
                throw new InvalidOperationException("HttpApiClient를 사용할 수 없습니다.");
            }
            
            var request = new { session_id = sessionId, max_age = maxAgeSeconds };
            var response = await apiClient.PostAsync<object>("/auth/set-session-cookie", request);
            
            return response != null;
        }
        
        private async UniTask<bool> ClearSessionViaBFFAsync()
        {
            var apiClient = HttpApiClient.Instance;
            if (apiClient == null)
            {
                throw new InvalidOperationException("HttpApiClient를 사용할 수 없습니다.");
            }
            
            var response = await apiClient.PostAsync<object>("/auth/clear-session-cookie", null);
            return response != null;
        }
        
        // JavaScript 직접 호출 메서드들
        private async UniTask<bool> SetRefreshTokenViaJSAsync(RefreshToken token)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var tokenJson = JsonConvert.SerializeObject(token);
            var maxAgeSeconds = (int)token.TimeUntilExpiry.TotalSeconds;
            
            SetHttpOnlyCookie(REFRESH_TOKEN_COOKIE_NAME, tokenJson, maxAgeSeconds, "Lax");
            await UniTask.Delay(10);
            
            return true;
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }
        
        private async UniTask<RefreshToken> GetRefreshTokenViaJSAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var tokenJson = GetCookieValue(REFRESH_TOKEN_COOKIE_NAME);
            await UniTask.Delay(10);
            
            if (string.IsNullOrEmpty(tokenJson))
            {
                return null;
            }
            
            try
            {
                return JsonConvert.DeserializeObject<RefreshToken>(tokenJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCookieBridge] 토큰 JSON 파싱 실패: {ex.Message}");
                return null;
            }
#else
            await UniTask.CompletedTask;
            return null;
#endif
        }
        
        private async UniTask<bool> ClearRefreshTokenViaJSAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DeleteCookie(REFRESH_TOKEN_COOKIE_NAME);
            await UniTask.Delay(10);
            return true;
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }
        
        private async UniTask<string> GetCSRFTokenViaJSAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var token = GenerateCSRFToken();
            await UniTask.Delay(10);
            return token;
#else
            await UniTask.CompletedTask;
            return Guid.NewGuid().ToString("N"); // 에디터용 임시 토큰
#endif
        }
        
        private async UniTask<bool> SetSessionViaJSAsync(string sessionId, int maxAgeSeconds)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetHttpOnlyCookie(SESSION_COOKIE_NAME, sessionId, maxAgeSeconds, "Lax");
            await UniTask.Delay(10);
            return true;
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }
        
        private async UniTask<bool> ClearSessionViaJSAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DeleteCookie(SESSION_COOKIE_NAME);
            await UniTask.Delay(10);
            return true;
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }
        
        #endregion
    }
}
