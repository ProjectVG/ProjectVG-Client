using System;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Handlers
{
    /// <summary>
    /// WebGL OAuth2 콜백 핸들러
    /// URL 파라미터를 통해 OAuth2 콜백을 처리
    /// </summary>
    public class WebGLCallbackHandler : IOAuth2CallbackHandler
    {
        private string _expectedState;
        private float _timeoutSeconds;
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        
        public string PlatformName => "WebGL";
        public bool IsSupported => true;
        
        public async Task InitializeAsync(string expectedState, float timeoutSeconds)
        {
            _expectedState = expectedState;
            _timeoutSeconds = timeoutSeconds;
            _isInitialized = true;
            
            Debug.Log($"[WebGLCallbackHandler] 초기화 완료 - State: {expectedState}, Timeout: {timeoutSeconds}초");
            await UniTask.CompletedTask;
        }
        
        public async Task<string> WaitForCallbackAsync()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("WebGL 콜백 핸들러가 초기화되지 않았습니다.");
            }
            
            Debug.Log("[WebGLCallbackHandler] OAuth2 콜백 대기 시작");
            
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            
            while (DateTime.UtcNow - startTime < timeout && !_isDisposed)
            {
                // WebGL에서 URL 파라미터 확인
                var callbackUrl = CheckUrlParameters();
                
                if (!string.IsNullOrEmpty(callbackUrl))
                {
                    Debug.Log($"[WebGLCallbackHandler] OAuth2 콜백 수신: {callbackUrl}");
                    return callbackUrl;
                }
                
                // 100ms 대기
                await UniTask.Delay(100);
            }
            
            Debug.LogWarning("[WebGLCallbackHandler] OAuth2 콜백 타임아웃");
            return null;
        }
        
        public void Cleanup()
        {
            _isDisposed = true;
            Debug.Log("[WebGLCallbackHandler] 정리 완료");
        }
        
        /// <summary>
        /// URL 파라미터에서 OAuth2 콜백 확인
        /// </summary>
        private string CheckUrlParameters()
        {
            try
            {
                // WebGL에서 현재 URL 가져오기
                var currentUrl = Application.absoluteURL;
                
                if (string.IsNullOrEmpty(currentUrl))
                    return null;
                
                // URL에 OAuth2 콜백 파라미터가 있는지 확인
                if (currentUrl.Contains("success=") && currentUrl.Contains("state="))
                {
                    // URL에서 state 파라미터 추출
                    var stateParam = ExtractParameter(currentUrl, "state");
                    
                    if (!string.IsNullOrEmpty(stateParam) && stateParam == _expectedState)
                    {
                        return currentUrl;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCallbackHandler] URL 파라미터 확인 중 오류: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// URL에서 파라미터 추출
        /// </summary>
        private string ExtractParameter(string url, string parameterName)
        {
            try
            {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return query[parameterName];
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCallbackHandler] 파라미터 추출 중 오류: {ex.Message}");
                return null;
            }
        }
    }
}
