using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public class IOSCallbackHandler : IOAuth2CallbackHandler
    {
        private bool _isListening = false;
        private string _expectedState = null;
        private float _timeoutTime = 0f;
        
        public bool IsHandlingCallback => _isListening;
        public string PlatformName => "iOS";
        
        public event Action<string, string> OnAuthorizationCodeReceived;
        public event Action<string> OnCallbackError;
        public event Action OnCallbackCancelled;
        
        #region External iOS Functions
        
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StartASWebAuthenticationSession(string authUrl, string callbackScheme, string objectName, string callbackMethod);
        
        [DllImport("__Internal")]
        private static extern void StopASWebAuthenticationSession();
        
        [DllImport("__Internal")]
        private static extern bool IsASWebAuthenticationSessionSupported();
#endif
        
        #endregion
        
        #region Public Methods
        
        public async UniTask<bool> StartListeningForCallbackAsync(string expectedState, TimeSpan timeout)
        {
            if (_isListening)
            {
                Debug.LogWarning("[IOSCallbackHandler] 이미 콜백을 수신 중입니다.");
                return false;
            }
            
            try
            {
                _expectedState = expectedState;
                _timeoutTime = Time.time + (float)timeout.TotalSeconds;
                _isListening = true;
                
                Debug.Log($"[IOSCallbackHandler] 콜백 수신 시작 - 타임아웃: {timeout.TotalSeconds}초");
                
#if UNITY_IOS && !UNITY_EDITOR
                StartIOSCallbackListener();
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
                    Debug.LogWarning("[IOSCallbackHandler] 콜백 수신 타임아웃");
                    StopListening();
                    OnCallbackError?.Invoke("타임아웃");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IOSCallbackHandler] 콜백 수신 시작 실패: {ex.Message}");
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
            
#if UNITY_IOS && !UNITY_EDITOR
            StopIOSCallbackListener();
#endif
            
            Debug.Log("[IOSCallbackHandler] 콜백 수신 중지");
        }
        
        public bool ValidateCallback(string code, string state, string expectedState)
        {
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[IOSCallbackHandler] Authorization Code가 비어있습니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(expectedState))
            {
                Debug.LogError("[IOSCallbackHandler] State 파라미터가 비어있습니다.");
                return false;
            }
            
            if (state != expectedState)
            {
                Debug.LogError("[IOSCallbackHandler] State 파라미터가 일치하지 않습니다.");
                return false;
            }
            
            Debug.Log("[IOSCallbackHandler] 콜백 검증 성공");
            return true;
        }
        
        public string GetCallbackUrl()
        {
            // iOS Universal Link 또는 Custom URL Scheme
            return "com.yourcompany.yourgame://auth/callback";
        }
        
        #endregion
        
        #region Private Methods
        
        private void StartIOSCallbackListener()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                // TODO: iOS Native Plugin을 통한 ASWebAuthenticationSession 시작
                // 1. ASWebAuthenticationSession 생성
                // 2. completionHandler에서 콜백 처리
                // 3. presentationContextProvider 설정
                
                if (!IsASWebAuthenticationSessionSupported())
                {
                    throw new NotSupportedException("ASWebAuthenticationSession이 지원되지 않습니다.");
                }
                
                var callbackScheme = GetCallbackScheme();
                // StartASWebAuthenticationSession(authUrl, callbackScheme, gameObject.name, "OnASWebAuthCallback");
                
                Debug.Log("[IOSCallbackHandler] iOS ASWebAuthenticationSession 시작");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IOSCallbackHandler] iOS 리스너 시작 실패: {ex.Message}");
                throw;
            }
#endif
        }
        
        private void StopIOSCallbackListener()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                StopASWebAuthenticationSession();
                Debug.Log("[IOSCallbackHandler] iOS ASWebAuthenticationSession 중지");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IOSCallbackHandler] iOS 리스너 중지 실패: {ex.Message}");
            }
#endif
        }
        
        private string GetCallbackScheme()
        {
            var callbackUrl = GetCallbackUrl();
            var uri = new Uri(callbackUrl);
            return uri.Scheme;
        }
        
        // iOS Native에서 호출되는 콜백 메서드
        public void OnASWebAuthCallback(string callbackUrl)
        {
            if (!_isListening)
            {
                return;
            }
            
            try
            {
                Debug.Log($"[IOSCallbackHandler] ASWebAuthenticationSession 콜백 수신: {callbackUrl}");
                
                if (string.IsNullOrEmpty(callbackUrl))
                {
                    Debug.LogError("[IOSCallbackHandler] 콜백 URL이 비어있습니다.");
                    StopListening();
                    OnCallbackError?.Invoke("콜백 URL이 비어있습니다.");
                    return;
                }
                
                // URL에서 파라미터 파싱
                var uri = new Uri(callbackUrl);
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
                    
                    Debug.LogError($"[IOSCallbackHandler] {errorMsg}");
                    StopListening();
                    OnCallbackError?.Invoke(errorMsg);
                    return;
                }
                
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
                {
                    if (ValidateCallback(code, state, _expectedState))
                    {
                        Debug.Log("[IOSCallbackHandler] Authorization Code 수신 성공");
                        StopListening();
                        OnAuthorizationCodeReceived?.Invoke(code, state);
                    }
                    else
                    {
                        Debug.LogError("[IOSCallbackHandler] 콜백 검증 실패");
                        StopListening();
                        OnCallbackError?.Invoke("콜백 검증 실패");
                    }
                }
                else
                {
                    Debug.LogError("[IOSCallbackHandler] 필수 파라미터 누락");
                    StopListening();
                    OnCallbackError?.Invoke("필수 파라미터 누락");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IOSCallbackHandler] 콜백 처리 실패: {ex.Message}");
                StopListening();
                OnCallbackError?.Invoke($"콜백 처리 실패: {ex.Message}");
            }
        }
        
        // ASWebAuthenticationSession이 취소된 경우 호출
        public void OnASWebAuthCancelled()
        {
            if (!_isListening)
            {
                return;
            }
            
            Debug.Log("[IOSCallbackHandler] 사용자가 ASWebAuthenticationSession을 취소했습니다.");
            StopListening();
            OnCallbackCancelled?.Invoke();
        }
        
        // ASWebAuthenticationSession 오류 발생 시 호출
        public void OnASWebAuthError(string error)
        {
            if (!_isListening)
            {
                return;
            }
            
            Debug.LogError($"[IOSCallbackHandler] ASWebAuthenticationSession 오류: {error}");
            StopListening();
            OnCallbackError?.Invoke($"ASWebAuthenticationSession 오류: {error}");
        }
        
        private async UniTask SimulateCallbackInEditor()
        {
            await UniTask.Delay(2000); // 2초 후 시뮬레이션
            
            if (_isListening)
            {
                Debug.Log("[IOSCallbackHandler] 에디터에서 콜백 시뮬레이션");
                var simulatedCode = "simulated_auth_code_ios_12345";
                var simulatedState = _expectedState;
                
                StopListening();
                OnAuthorizationCodeReceived?.Invoke(simulatedCode, simulatedState);
            }
        }
        
        #endregion
    }
}
