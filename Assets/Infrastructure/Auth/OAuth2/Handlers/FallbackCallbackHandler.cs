using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public class FallbackCallbackHandler : IOAuth2CallbackHandler
    {
        private bool _isListening = false;
        private string _expectedState = null;
        
        public bool IsHandlingCallback => _isListening;
        public string PlatformName => "Fallback";
        
        public event Action<string, string> OnAuthorizationCodeReceived;
        public event Action<string> OnCallbackError;
        public event Action OnCallbackCancelled;
        
        #region Public Methods
        
        public async UniTask<bool> StartListeningForCallbackAsync(string expectedState, TimeSpan timeout)
        {
            if (_isListening)
            {
                Debug.LogWarning("[FallbackCallbackHandler] 이미 콜백을 수신 중입니다.");
                return false;
            }
            
            try
            {
                _expectedState = expectedState;
                _isListening = true;
                
                Debug.LogWarning($"[FallbackCallbackHandler] Fallback 콜백 핸들러 사용 - 타임아웃: {timeout.TotalSeconds}초");
                Debug.LogWarning("[FallbackCallbackHandler] 실제 OAuth2 인증은 지원되지 않으며, 시뮬레이션 모드로 동작합니다.");
                
                // 시뮬레이션 모드
                _ = SimulateCallback();
                
                // 타임아웃까지 대기
                var timeoutTime = Time.time + (float)timeout.TotalSeconds;
                while (_isListening && Time.time < timeoutTime)
                {
                    await UniTask.Delay(100);
                }
                
                if (_isListening)
                {
                    Debug.LogWarning("[FallbackCallbackHandler] 콜백 수신 타임아웃");
                    StopListening();
                    OnCallbackError?.Invoke("타임아웃");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FallbackCallbackHandler] 콜백 수신 시작 실패: {ex.Message}");
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
            
            Debug.Log("[FallbackCallbackHandler] 콜백 수신 중지");
        }
        
        public bool ValidateCallback(string code, string state, string expectedState)
        {
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[FallbackCallbackHandler] Authorization Code가 비어있습니다.");
                return false;
            }
            
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(expectedState))
            {
                Debug.LogError("[FallbackCallbackHandler] State 파라미터가 비어있습니다.");
                return false;
            }
            
            if (state != expectedState)
            {
                Debug.LogError("[FallbackCallbackHandler] State 파라미터가 일치하지 않습니다.");
                return false;
            }
            
            Debug.Log("[FallbackCallbackHandler] 콜백 검증 성공");
            return true;
        }
        
        public string GetCallbackUrl()
        {
            return "fallback://auth/callback";
        }
        
        /// <summary>
        /// 수동으로 Authorization Code 주입 (테스트용)
        /// </summary>
        public void InjectAuthorizationCode(string code, string state)
        {
            if (!_isListening)
            {
                Debug.LogWarning("[FallbackCallbackHandler] 콜백을 수신 중이지 않습니다.");
                return;
            }
            
            Debug.Log($"[FallbackCallbackHandler] 수동 Authorization Code 주입: {code}");
            
            if (ValidateCallback(code, state, _expectedState))
            {
                StopListening();
                OnAuthorizationCodeReceived?.Invoke(code, state);
            }
            else
            {
                StopListening();
                OnCallbackError?.Invoke("수동 주입된 코드 검증 실패");
            }
        }
        
        /// <summary>
        /// 수동으로 오류 주입 (테스트용)
        /// </summary>
        public void InjectError(string error)
        {
            if (!_isListening)
            {
                Debug.LogWarning("[FallbackCallbackHandler] 콜백을 수신 중이지 않습니다.");
                return;
            }
            
            Debug.LogError($"[FallbackCallbackHandler] 수동 오류 주입: {error}");
            StopListening();
            OnCallbackError?.Invoke(error);
        }
        
        /// <summary>
        /// 수동으로 취소 주입 (테스트용)
        /// </summary>
        public void InjectCancellation()
        {
            if (!_isListening)
            {
                Debug.LogWarning("[FallbackCallbackHandler] 콜백을 수신 중이지 않습니다.");
                return;
            }
            
            Debug.Log("[FallbackCallbackHandler] 수동 취소 주입");
            StopListening();
            OnCallbackCancelled?.Invoke();
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask SimulateCallback()
        {
            await UniTask.Delay(3000); // 3초 후 시뮬레이션
            
            if (_isListening)
            {
                Debug.Log("[FallbackCallbackHandler] 시뮬레이션 콜백 실행");
                
                // 80% 확률로 성공, 20% 확률로 실패 시뮬레이션
                var random = UnityEngine.Random.Range(0f, 1f);
                
                if (random < 0.8f)
                {
                    // 성공 시뮬레이션
                    var simulatedCode = "simulated_fallback_auth_code_" + UnityEngine.Random.Range(1000, 9999);
                    var simulatedState = _expectedState;
                    
                    Debug.Log($"[FallbackCallbackHandler] 성공 시뮬레이션: {simulatedCode}");
                    StopListening();
                    OnAuthorizationCodeReceived?.Invoke(simulatedCode, simulatedState);
                }
                else if (random < 0.9f)
                {
                    // 오류 시뮬레이션
                    Debug.LogWarning("[FallbackCallbackHandler] 오류 시뮬레이션");
                    StopListening();
                    OnCallbackError?.Invoke("시뮬레이션된 인증 오류");
                }
                else
                {
                    // 취소 시뮬레이션
                    Debug.LogWarning("[FallbackCallbackHandler] 취소 시뮬레이션");
                    StopListening();
                    OnCallbackCancelled?.Invoke();
                }
            }
        }
        
        #endregion
        
        #region Editor Methods
        
#if UNITY_EDITOR
        [ContextMenu("Inject Success Code")]
        private void TestInjectSuccessCode()
        {
            if (Application.isPlaying)
            {
                InjectAuthorizationCode("test_code_12345", _expectedState ?? "test_state");
            }
            else
            {
                Debug.LogWarning("[FallbackCallbackHandler] 플레이 모드에서만 테스트할 수 있습니다.");
            }
        }
        
        [ContextMenu("Inject Error")]
        private void TestInjectError()
        {
            if (Application.isPlaying)
            {
                InjectError("access_denied");
            }
            else
            {
                Debug.LogWarning("[FallbackCallbackHandler] 플레이 모드에서만 테스트할 수 있습니다.");
            }
        }
        
        [ContextMenu("Inject Cancellation")]
        private void TestInjectCancellation()
        {
            if (Application.isPlaying)
            {
                InjectCancellation();
            }
            else
            {
                Debug.LogWarning("[FallbackCallbackHandler] 플레이 모드에서만 테스트할 수 있습니다.");
            }
        }
#endif
        
        #endregion
    }
}
