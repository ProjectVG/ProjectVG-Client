using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public class AndroidCallbackHandler : IOAuth2CallbackHandler
    {
        private bool _isListening = false;
        private string _expectedState = null;
        private float _timeoutTime = 0f;
        
        public bool IsHandlingCallback => _isListening;
        public string PlatformName => "Android";
        
        public event Action<string, string> OnAuthorizationCodeReceived;
        public event Action<string> OnCallbackError;
        public event Action OnCallbackCancelled;
        
        #region Public Methods
        
        public async UniTask<bool> StartListeningForCallbackAsync(string expectedState, TimeSpan timeout)
        {
            if (_isListening)
            {
                Debug.LogWarning("[AndroidCallbackHandler] 이미 콜백을 수신 중입니다.");
                return false;
            }
            
            try
            {
                _expectedState = expectedState;
                _timeoutTime = Time.time + (float)timeout.TotalSeconds;
                _isListening = true;
                
                Debug.Log($"[AndroidCallbackHandler] 콜백 수신 시작 - 타임아웃: {timeout.TotalSeconds}초");
                
#if UNITY_ANDROID && !UNITY_EDITOR
                StartAndroidCallbackListener();
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
                    Debug.LogWarning("[AndroidCallbackHandler] 콜백 수신 타임아웃");
                    StopListening();
                    OnCallbackError?.Invoke("타임아웃");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AndroidCallbackHandler] 콜백 수신 시작 실패: {ex.Message}");
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
            
#if UNITY_ANDROID && !UNITY_EDITOR
            StopAndroidCallbackListener();
#endif
            
            Debug.Log("[AndroidCallbackHandler] 콜백 수신 중지");
        }
        
        public bool ValidateCallback(string code, string state, string expectedState)
        {
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[AndroidCallbackHandler] Authorization Code가 비어있습니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(expectedState))
            {
                Debug.LogError("[AndroidCallbackHandler] State 파라미터가 비어있습니다.");
                return false;
            }
            
            if (state != expectedState)
            {
                Debug.LogError("[AndroidCallbackHandler] State 파라미터가 일치하지 않습니다.");
                return false;
            }
            
            Debug.Log("[AndroidCallbackHandler] 콜백 검증 성공");
            return true;
        }
        
        public string GetCallbackUrl()
        {
            // Android 앱 스킴 기반 콜백 URL
            return "com.yourcompany.yourgame://auth/callback";
        }
        
        #endregion
        
        #region Private Methods
        
        private void StartAndroidCallbackListener()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // TODO: Android Native Plugin을 통한 콜백 리스너 시작
                // 1. Intent Filter 등록 (앱 스킴 처리)
                // 2. Activity에서 Intent 수신 대기
                // 3. CustomTabs 또는 Chrome Custom Tabs 연동
                
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // 현재 Activity에서 Intent 리스너 등록
                    // currentActivity.Call("startOAuth2CallbackListener", gameObject.name, "OnCallbackReceived");
                }
                
                Debug.Log("[AndroidCallbackHandler] Android 콜백 리스너 시작");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AndroidCallbackHandler] Android 리스너 시작 실패: {ex.Message}");
                throw;
            }
#endif
        }
        
        private void StopAndroidCallbackListener()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // TODO: Android Native Plugin을 통한 콜백 리스너 중지
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // currentActivity.Call("stopOAuth2CallbackListener");
                }
                
                Debug.Log("[AndroidCallbackHandler] Android 콜백 리스너 중지");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AndroidCallbackHandler] Android 리스너 중지 실패: {ex.Message}");
            }
#endif
        }
        
        // Android Native에서 호출되는 콜백 메서드
        public void OnCallbackReceived(string intentData)
        {
            if (!_isListening)
            {
                return;
            }
            
            try
            {
                Debug.Log($"[AndroidCallbackHandler] Intent 콜백 수신: {intentData}");
                
                // Intent 데이터에서 파라미터 파싱
                var uri = new Uri(intentData);
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
                    
                    Debug.LogError($"[AndroidCallbackHandler] {errorMsg}");
                    StopListening();
                    OnCallbackError?.Invoke(errorMsg);
                    return;
                }
                
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
                {
                    if (ValidateCallback(code, state, _expectedState))
                    {
                        Debug.Log("[AndroidCallbackHandler] Authorization Code 수신 성공");
                        StopListening();
                        OnAuthorizationCodeReceived?.Invoke(code, state);
                    }
                    else
                    {
                        Debug.LogError("[AndroidCallbackHandler] 콜백 검증 실패");
                        StopListening();
                        OnCallbackError?.Invoke("콜백 검증 실패");
                    }
                }
                else
                {
                    Debug.LogError("[AndroidCallbackHandler] 필수 파라미터 누락");
                    StopListening();
                    OnCallbackError?.Invoke("필수 파라미터 누락");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AndroidCallbackHandler] 콜백 처리 실패: {ex.Message}");
                StopListening();
                OnCallbackError?.Invoke($"콜백 처리 실패: {ex.Message}");
            }
        }
        
        // 사용자가 브라우저에서 취소한 경우 호출되는 메서드
        public void HandleCallbackCancellation()
        {
            if (!_isListening)
            {
                return;
            }
            
            Debug.Log("[AndroidCallbackHandler] 사용자가 인증을 취소했습니다.");
            StopListening();
            OnCallbackCancelled?.Invoke();
        }
        
        private async UniTask SimulateCallbackInEditor()
        {
            await UniTask.Delay(2000); // 2초 후 시뮬레이션
            
            if (_isListening)
            {
                Debug.Log("[AndroidCallbackHandler] 에디터에서 콜백 시뮬레이션");
                var simulatedCode = "simulated_auth_code_android_12345";
                var simulatedState = _expectedState;
                
                StopListening();
                OnAuthorizationCodeReceived?.Invoke(simulatedCode, simulatedState);
            }
        }
        
        #endregion
    }
}