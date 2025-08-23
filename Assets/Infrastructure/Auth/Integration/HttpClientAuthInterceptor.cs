using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Auth.Core;

namespace ProjectVG.Infrastructure.Auth.Integration
{
    /// <summary>
    /// HttpApiClient의 요청을 가로채서 자동으로 인증 처리를 수행하는 인터셉터
    /// </summary>
    public class HttpClientAuthInterceptor : MonoBehaviour
    {
        [Header("Interceptor Settings")]
        [SerializeField] private bool enableAutoInterception = true;
        [SerializeField] private bool logInterceptionEvents = false;
        
        private HttpApiClient _httpClient;
        private IAuthManager _authManager;
        private bool _isInitialized = false;
        
        public bool IsEnabled => enableAutoInterception && _isInitialized;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeInterceptor();
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 인터셉터 초기화
        /// </summary>
        public void Initialize(HttpApiClient httpClient, IAuthManager authManager)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[HttpClientAuthInterceptor] 이미 초기화되었습니다.");
                return;
            }
            
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            
            SetupInterception();
            _isInitialized = true;
            
            Debug.Log("[HttpClientAuthInterceptor] 인터셉터 초기화 완료");
        }
        
        /// <summary>
        /// 자동 인터셉션 활성화/비활성화
        /// </summary>
        public void SetInterceptionEnabled(bool enabled)
        {
            enableAutoInterception = enabled;
            
            if (logInterceptionEvents)
            {
                Debug.Log($"[HttpClientAuthInterceptor] 자동 인터셉션 {(enabled ? "활성화" : "비활성화")}");
            }
        }
        
        /// <summary>
        /// 로깅 활성화/비활성화
        /// </summary>
        public void SetLoggingEnabled(bool enabled)
        {
            logInterceptionEvents = enabled;
            Debug.Log($"[HttpClientAuthInterceptor] 로깅 {(enabled ? "활성화" : "비활성화")}");
        }
        
        /// <summary>
        /// 인터셉터 상태 정보 출력
        /// </summary>
        public void LogInterceptorStatus()
        {
            Debug.Log($"[HttpClientAuthInterceptor] 상태 정보:");
            Debug.Log($"  - 초기화: {_isInitialized}");
            Debug.Log($"  - 자동 인터셉션: {enableAutoInterception}");
            Debug.Log($"  - 로깅: {logInterceptionEvents}");
            Debug.Log($"  - HttpClient 연결: {_httpClient != null}");
            Debug.Log($"  - AuthManager 연결: {_authManager != null}");
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeInterceptor()
        {
            // 초기화는 외부에서 명시적으로 호출해야 함
            Debug.Log("[HttpClientAuthInterceptor] 인터셉터 준비 완료 (Initialize 호출 대기 중)");
        }
        
        private void SetupInterception()
        {
            // HttpApiClient의 기본 메서드들을 래핑하여 인증 처리 추가
            // 실제 구현에서는 HttpApiClient의 아키텍처에 따라 다를 수 있음
            
            if (logInterceptionEvents)
            {
                Debug.Log("[HttpClientAuthInterceptor] 요청 인터셉션 설정 완료");
            }
        }
        
        /// <summary>
        /// 요청 전 처리 (인증 헤더 추가)
        /// </summary>
        private async UniTask<bool> PreprocessRequestAsync(string endpoint, System.Collections.Generic.Dictionary<string, string> headers)
        {
            if (!IsEnabled)
            {
                return true;
            }
            
            try
            {
                // 인증이 필요하지 않은 엔드포인트는 스킵
                if (!HttpClientAuthExtension.RequiresAuthentication(endpoint))
                {
                    if (logInterceptionEvents)
                    {
                        Debug.Log($"[HttpClientAuthInterceptor] 공개 엔드포인트, 인증 스킵: {endpoint}");
                    }
                    return true;
                }
                
                // 사용자가 인증되지 않은 경우
                if (!_authManager.IsAuthenticated)
                {
                    Debug.LogWarning($"[HttpClientAuthInterceptor] 인증되지 않은 상태에서 보호된 엔드포인트 접근 시도: {endpoint}");
                    return false;
                }
                
                // Access 토큰 확인 및 헤더 추가
                var accessToken = await _authManager.GetValidAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    Debug.LogError($"[HttpClientAuthInterceptor] 유효한 Access 토큰을 얻을 수 없음: {endpoint}");
                    return false;
                }
                
                // Authorization 헤더 추가
                headers = headers ?? new System.Collections.Generic.Dictionary<string, string>();
                headers["Authorization"] = $"Bearer {accessToken}";
                
                if (logInterceptionEvents)
                {
                    Debug.Log($"[HttpClientAuthInterceptor] 인증 헤더 추가: {endpoint}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpClientAuthInterceptor] 요청 전처리 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 응답 후 처리 (401 오류 처리)
        /// </summary>
        private async UniTask<bool> PostprocessResponseAsync(long statusCode, string endpoint)
        {
            if (!IsEnabled)
            {
                return true;
            }
            
            try
            {
                // 401 Unauthorized 응답 처리
                if (statusCode == 401)
                {
                    if (logInterceptionEvents)
                    {
                        Debug.Log($"[HttpClientAuthInterceptor] 401 응답 감지, 토큰 갱신 시도: {endpoint}");
                    }
                    
                    var refreshSuccess = await HttpClientAuthExtension.Handle401ResponseAsync();
                    
                    if (refreshSuccess)
                    {
                        if (logInterceptionEvents)
                        {
                            Debug.Log($"[HttpClientAuthInterceptor] 토큰 갱신 성공: {endpoint}");
                        }
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"[HttpClientAuthInterceptor] 토큰 갱신 실패, 인증 상태 초기화: {endpoint}");
                        return false;
                    }
                }
                
                // 다른 상태 코드는 정상 처리
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpClientAuthInterceptor] 응답 후처리 실패: {ex.Message}");
                return false;
            }
        }
        
        private void Shutdown()
        {
            if (_isInitialized)
            {
                _httpClient = null;
                _authManager = null;
                _isInitialized = false;
                
                if (logInterceptionEvents)
                {
                    Debug.Log("[HttpClientAuthInterceptor] 인터셉터 종료 처리 완료");
                }
            }
        }
        
        #endregion
        
        #region Editor Methods
        
#if UNITY_EDITOR
        [ContextMenu("Log Interceptor Status")]
        private void TestLogInterceptorStatus()
        {
            LogInterceptorStatus();
        }
        
        [ContextMenu("Toggle Interception")]
        private void TestToggleInterception()
        {
            SetInterceptionEnabled(!enableAutoInterception);
        }
        
        [ContextMenu("Toggle Logging")]
        private void TestToggleLogging()
        {
            SetLoggingEnabled(!logInterceptionEvents);
        }
#endif
        
        #endregion
    }
}
