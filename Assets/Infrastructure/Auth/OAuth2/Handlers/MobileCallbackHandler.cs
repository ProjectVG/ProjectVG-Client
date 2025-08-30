using System;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Handlers
{
    /// <summary>
    /// 모바일 OAuth2 콜백 핸들러
    /// 커스텀 스킴을 통해 OAuth2 콜백을 처리
    /// </summary>
    public class MobileCallbackHandler : IOAuth2CallbackHandler
    {
        private string _expectedState;
        private float _timeoutSeconds;
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private string _lastCustomUrl;
        
        public string PlatformName => "Mobile";
        public bool IsSupported => true;
        
        public async Task InitializeAsync(string expectedState, float timeoutSeconds)
        {
            _expectedState = expectedState;
            _timeoutSeconds = timeoutSeconds;
            _isInitialized = true;
            
            Debug.Log($"[MobileCallbackHandler] 초기화 완료 - State: {expectedState}, Timeout: {timeoutSeconds}초");
            await UniTask.CompletedTask;
        }
        
        public async Task<string> WaitForCallbackAsync()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("모바일 콜백 핸들러가 초기화되지 않았습니다.");
            }
            
            Debug.Log("[MobileCallbackHandler] OAuth2 콜백 대기 시작");
            
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            
            while (DateTime.UtcNow - startTime < timeout && !_isDisposed)
            {
                // 커스텀 스킴 URL 확인
                var callbackUrl = CheckCustomSchemeUrl();
                
                if (!string.IsNullOrEmpty(callbackUrl))
                {
                    Debug.Log($"[MobileCallbackHandler] OAuth2 콜백 수신: {callbackUrl}");
                    return callbackUrl;
                }
                
                // 100ms 대기
                await UniTask.Delay(100);
            }
            
            Debug.LogWarning("[MobileCallbackHandler] OAuth2 콜백 타임아웃");
            return null;
        }
        
        public void Cleanup()
        {
            _isDisposed = true;
            Debug.Log("[MobileCallbackHandler] 정리 완료");
        }
        
        /// <summary>
        /// 커스텀 스킴 URL 확인
        /// </summary>
        private string CheckCustomSchemeUrl()
        {
            try
            {
                // Unity에서 커스텀 스킴 URL을 받는 방법
                // 실제로는 플랫폼별 네이티브 플러그인이 필요할 수 있음
                
                // 임시로 시뮬레이션 (실제 구현에서는 네이티브 플러그인 사용)
                if (Application.isFocused && !string.IsNullOrEmpty(_lastCustomUrl))
                {
                    var url = _lastCustomUrl;
                    _lastCustomUrl = null; // 한 번만 사용
                    return url;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MobileCallbackHandler] 커스텀 스킴 URL 확인 중 오류: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 커스텀 스킴 URL 설정 (네이티브 플러그인에서 호출)
        /// </summary>
        public void SetCustomSchemeUrl(string url)
        {
            if (!string.IsNullOrEmpty(url) && url.Contains("auth/callback"))
            {
                _lastCustomUrl = url;
                Debug.Log($"[MobileCallbackHandler] 커스텀 스킴 URL 설정: {url}");
            }
        }
        
        /// <summary>
        /// 앱 포커스 변경 시 호출 (Unity에서 자동 호출)
        /// </summary>
        public void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Debug.Log("[MobileCallbackHandler] 앱 포커스 획득");
                // 여기서 커스텀 스킴 URL을 확인할 수 있음
                // 실제로는 네이티브 플러그인을 통해 URL을 받아야 함
            }
        }
        
        /// <summary>
        /// 앱 일시정지 시 호출 (Unity에서 자동 호출)
        /// </summary>
        public void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) // 앱 재개
            {
                Debug.Log("[MobileCallbackHandler] 앱 재개");
                // 여기서 커스텀 스킴 URL을 확인할 수 있음
            }
        }
    }
}
