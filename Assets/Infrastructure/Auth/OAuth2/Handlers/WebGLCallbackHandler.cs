using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public class WebGLCallbackHandler : IOAuth2CallbackHandler
    {
        private bool _isListening = false;
        private string _expectedState = null;
        private float _timeoutTime = 0f;
        
        public bool IsHandlingCallback => _isListening;
        public string PlatformName => "WebGL";
        
        public event Action<string, string> OnAuthorizationCodeReceived;
        public event Action<string> OnCallbackError;
        public event Action OnCallbackCancelled;
        
        #region External JavaScript Functions
        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StartOAuth2CallbackListener(string callbackObjectName, string callbackMethodName);
        
        [DllImport("__Internal")]
        private static extern void StopOAuth2CallbackListener();
        
        [DllImport("__Internal")]
        private static extern string GetCurrentUrl();
        
        [DllImport("__Internal")]
        private static extern void RegisterUrlChangeListener(string objectName, string methodName);
        
        [DllImport("__Internal")]
        private static extern void UnregisterUrlChangeListener();
#endif
        
        #endregion
        
        #region Public Methods
        
        public async UniTask<bool> StartListeningForCallbackAsync(string expectedState, TimeSpan timeout)
        {
            if (_isListening)
            {
                Debug.LogWarning("[WebGLCallbackHandler] 이미 콜백을 수신 중입니다.");
                return false;
            }
            
            try
            {
                _expectedState = expectedState;
                _timeoutTime = Time.time + (float)timeout.TotalSeconds;
                _isListening = true;
                
                Debug.Log($"[WebGLCallbackHandler] 콜백 수신 시작 - 타임아웃: {timeout.TotalSeconds}초");
                
#if UNITY_WEBGL && !UNITY_EDITOR
                StartWebGLCallbackListener();
#else
                // 에디터에서는 시뮬레이션
                _ = SimulateCallbackInEditor();
#endif
                
                // 타임아웃까지 대기
                while (_isListening && Time.time < _timeoutTime)
                {
                    await UniTask.Delay(100);
                }
                
                if (_isListening)
                {
                    Debug.LogWarning("[WebGLCallbackHandler] 콜백 수신 타임아웃");
                    StopListening();
                    OnCallbackError?.Invoke("타임아웃");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCallbackHandler] 콜백 수신 시작 실패: {ex.Message}");
                StopListening();
                OnCallbackError?.Invoke($"시작 실패: {ex.Message}");
                return false;
            }
        }
        
        public void StopListening()
        {
            if (!_isListening)
            {
                return;
            }
            
            _isListening = false;
            _expectedState = null;
            
#if UNITY_WEBGL && !UNITY_EDITOR
            StopWebGLCallbackListener();
#endif
            
            Debug.Log("[WebGLCallbackHandler] 콜백 수신 중지");
        }
        
        public bool ValidateCallback(string code, string state, string expectedState)
        {
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[WebGLCallbackHandler] Authorization Code가 비어있습니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(expectedState))
            {
                Debug.LogError("[WebGLCallbackHandler] State 파라미터가 비어있습니다.");
                return false;
            }
            
            if (state != expectedState)
            {
                Debug.LogError("[WebGLCallbackHandler] State 파라미터가 일치하지 않습니다.");
                return false;
            }
            
            Debug.Log("[WebGLCallbackHandler] 콜백 검증 성공");
            return true;
        }
        
        public string GetCallbackUrl()
        {
            // WebGL에서는 현재 페이지 URL이 콜백 URL
            return Application.absoluteURL;
        }
        
        #endregion
        
        #region Private Methods
        
        private void StartWebGLCallbackListener()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // JavaScript에서 URL 변경을 감지하도록 설정
                RegisterUrlChangeListener(gameObject.name, "OnUrlChanged");
                
                Debug.Log("[WebGLCallbackHandler] WebGL 콜백 리스너 시작");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCallbackHandler] WebGL 리스너 시작 실패: {ex.Message}");
                throw;
            }
#endif
        }
        
        private void StopWebGLCallbackListener()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                UnregisterUrlChangeListener();
                Debug.Log("[WebGLCallbackHandler] WebGL 콜백 리스너 중지");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCallbackHandler] WebGL 리스너 중지 실패: {ex.Message}");
            }
#endif
        }
        
        // JavaScript에서 호출되는 콜백 메서드
        public void OnUrlChanged(string url)
        {
            if (!_isListening)
            {
                return;
            }
            
            try
            {
                Debug.Log($"[WebGLCallbackHandler] URL 변경 감지: {url}");
                
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                
                var code = query["code"];
                var state = query["state"];
                var error = query["error"];
                var errorDescription = query["error_description"];
                
                if (!string.IsNullOrEmpty(error))
                {
                    var errorMsg = $"OAuth2 오류: {error}";
                    if (!string.IsNullOrEmpty(errorDescription))
                    {
                        errorMsg += $" - {errorDescription}";
                    }
                    
                    Debug.LogError($"[WebGLCallbackHandler] {errorMsg}");
                    StopListening();
                    OnCallbackError?.Invoke(errorMsg);
                    return;
                }
                
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
                {
                    if (ValidateCallback(code, state, _expectedState))
                    {
                        Debug.Log("[WebGLCallbackHandler] Authorization Code 수신 성공");
                        StopListening();
                        OnAuthorizationCodeReceived?.Invoke(code, state);
                    }
                    else
                    {
                        Debug.LogError("[WebGLCallbackHandler] 콜백 검증 실패");
                        StopListening();
                        OnCallbackError?.Invoke("콜백 검증 실패");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLCallbackHandler] URL 변경 처리 실패: {ex.Message}");
                StopListening();
                OnCallbackError?.Invoke($"URL 처리 실패: {ex.Message}");
            }
        }
        
        private async UniTask SimulateCallbackInEditor()
        {
            await UniTask.Delay(2000); // 2초 후 시뮬레이션
            
            if (_isListening)
            {
                Debug.Log("[WebGLCallbackHandler] 에디터에서 콜백 시뮬레이션");
                var simulatedCode = "simulated_auth_code_12345";
                var simulatedState = _expectedState;
                
                StopListening();
                OnAuthorizationCodeReceived?.Invoke(simulatedCode, simulatedState);
            }
        }
        
        #endregion
    }
}
