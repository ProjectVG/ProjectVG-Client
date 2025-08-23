using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Core;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;

namespace ProjectVG.Infrastructure.Auth.Integration
{
    /// <summary>
    /// 인증 시스템 전체를 초기화하고 각 컴포넌트 간의 의존성을 연결하는 클래스
    /// </summary>
    public class AuthSystemInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool initializeOnStart = false;
        [SerializeField] private float initializationDelay = 0.5f;
        
        [Header("Component References")]
        [SerializeField] private AuthManager authManager;
        [SerializeField] private HttpClientAuthInterceptor authInterceptor;
        
        [Header("Initialization Status")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private bool initializationInProgress = false;
        
        private SessionManager _sessionManager;
        private HttpApiClient _httpClient;
        
        public bool IsInitialized => isInitialized;
        public bool IsInitializationInProgress => initializationInProgress;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (initializeOnAwake && !initializationInProgress)
            {
                _ = InitializeAuthSystemAsync();
            }
        }
        
        private async void Start()
        {
            if (initializeOnStart && !initializationInProgress && !isInitialized)
            {
                if (initializationDelay > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(initializationDelay));
                }
                
                await InitializeAuthSystemAsync();
            }
        }
        
        private void OnDestroy()
        {
            ShutdownAuthSystem();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 인증 시스템 수동 초기화
        /// </summary>
        public async UniTask<bool> InitializeAuthSystemAsync()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[AuthSystemInitializer] 이미 초기화되었습니다.");
                return true;
            }
            
            if (initializationInProgress)
            {
                Debug.LogWarning("[AuthSystemInitializer] 초기화가 이미 진행 중입니다.");
                return false;
            }
            
            initializationInProgress = true;
            
            try
            {
                Debug.Log("[AuthSystemInitializer] 인증 시스템 초기화 시작");
                
                // 1단계: 의존성 수집
                if (!CollectDependencies())
                {
                    Debug.LogError("[AuthSystemInitializer] 의존성 수집 실패");
                    return false;
                }
                
                // 2단계: AuthManager 초기화
                if (!await InitializeAuthManagerAsync())
                {
                    Debug.LogError("[AuthSystemInitializer] AuthManager 초기화 실패");
                    return false;
                }
                
                // 3단계: HTTP 클라이언트 인증 연동
                if (!InitializeHttpClientIntegration())
                {
                    Debug.LogError("[AuthSystemInitializer] HTTP 클라이언트 연동 실패");
                    return false;
                }
                
                // 4단계: SessionManager 연동
                if (!InitializeSessionManagerIntegration())
                {
                    Debug.LogError("[AuthSystemInitializer] SessionManager 연동 실패");
                    return false;
                }
                
                // 5단계: 인터셉터 초기화 (선택적)
                InitializeInterceptor();
                
                isInitialized = true;
                Debug.Log("[AuthSystemInitializer] 인증 시스템 초기화 완료");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthSystemInitializer] 초기화 중 오류 발생: {ex.Message}");
                Debug.LogError($"[AuthSystemInitializer] 스택 트레이스: {ex.StackTrace}");
                return false;
            }
            finally
            {
                initializationInProgress = false;
            }
        }
        
        /// <summary>
        /// 인증 시스템 종료
        /// </summary>
        public void ShutdownAuthSystem()
        {
            if (!isInitialized)
            {
                return;
            }
            
            try
            {
                Debug.Log("[AuthSystemInitializer] 인증 시스템 종료 시작");
                
                // 역순으로 종료
                SessionManagerExtension.Shutdown();
                HttpClientAuthExtension.Shutdown();
                
                if (authInterceptor != null)
                {
                    authInterceptor.SetInterceptionEnabled(false);
                }
                
                isInitialized = false;
                Debug.Log("[AuthSystemInitializer] 인증 시스템 종료 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthSystemInitializer] 종료 중 오류 발생: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 초기화 상태 확인
        /// </summary>
        public void ValidateInitialization()
        {
            Debug.Log($"[AuthSystemInitializer] 초기화 상태 검증:");
            Debug.Log($"  - 전체 초기화: {isInitialized}");
            Debug.Log($"  - AuthManager 초기화: {authManager?.IsInitialized ?? false}");
            Debug.Log($"  - AuthManager 인증 상태: {authManager?.IsAuthenticated ?? false}");
            Debug.Log($"  - SessionManager 초기화: {_sessionManager?.IsInitialized ?? false}");
            Debug.Log($"  - HttpClient 연결: {_httpClient != null}");
            Debug.Log($"  - 인터셉터 활성화: {authInterceptor?.IsEnabled ?? false}");
        }
        
        #endregion
        
        #region Private Methods
        
        private bool CollectDependencies()
        {
            Debug.Log("[AuthSystemInitializer] 의존성 수집 중...");
            
            // AuthManager 확인
            if (authManager == null)
            {
                authManager = AuthManager.Instance;
                if (authManager == null)
                {
                    Debug.LogError("[AuthSystemInitializer] AuthManager를 찾을 수 없습니다.");
                    return false;
                }
            }
            
            // SessionManager 확인
            _sessionManager = SessionManager.Instance;
            if (_sessionManager == null)
            {
                Debug.LogError("[AuthSystemInitializer] SessionManager를 찾을 수 없습니다.");
                return false;
            }
            
            // HttpApiClient 확인
            _httpClient = HttpApiClient.Instance;
            if (_httpClient == null)
            {
                Debug.LogError("[AuthSystemInitializer] HttpApiClient를 찾을 수 없습니다.");
                return false;
            }
            
            // AuthInterceptor 확인 (선택적)
            if (authInterceptor == null)
            {
                authInterceptor = FindAnyObjectByType<HttpClientAuthInterceptor>();
                if (authInterceptor == null)
                {
                    Debug.LogWarning("[AuthSystemInitializer] HttpClientAuthInterceptor를 찾을 수 없습니다. (선택적)");
                }
            }
            
            Debug.Log("[AuthSystemInitializer] 의존성 수집 완료");
            return true;
        }
        
        private async UniTask<bool> InitializeAuthManagerAsync()
        {
            try
            {
                Debug.Log("[AuthSystemInitializer] AuthManager 초기화 중...");
                
                if (authManager.IsInitialized)
                {
                    Debug.Log("[AuthSystemInitializer] AuthManager가 이미 초기화되어 있습니다.");
                    return true;
                }
                
                await authManager.InitializeAsync();
                
                if (!authManager.IsInitialized)
                {
                    Debug.LogError("[AuthSystemInitializer] AuthManager 초기화 실패");
                    return false;
                }
                
                Debug.Log("[AuthSystemInitializer] AuthManager 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthSystemInitializer] AuthManager 초기화 중 오류: {ex.Message}");
                return false;
            }
        }
        
        private bool InitializeHttpClientIntegration()
        {
            try
            {
                Debug.Log("[AuthSystemInitializer] HTTP 클라이언트 인증 연동 중...");
                
                HttpClientAuthExtension.Initialize(authManager);
                
                Debug.Log("[AuthSystemInitializer] HTTP 클라이언트 인증 연동 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthSystemInitializer] HTTP 클라이언트 연동 중 오류: {ex.Message}");
                return false;
            }
        }
        
        private bool InitializeSessionManagerIntegration()
        {
            try
            {
                Debug.Log("[AuthSystemInitializer] SessionManager 인증 연동 중...");
                
                SessionManagerExtension.Initialize(_sessionManager, authManager);
                
                Debug.Log("[AuthSystemInitializer] SessionManager 인증 연동 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthSystemInitializer] SessionManager 연동 중 오류: {ex.Message}");
                return false;
            }
        }
        
        private void InitializeInterceptor()
        {
            if (authInterceptor != null)
            {
                try
                {
                    Debug.Log("[AuthSystemInitializer] 인터셉터 초기화 중...");
                    
                    authInterceptor.Initialize(_httpClient, authManager);
                    authInterceptor.SetInterceptionEnabled(true);
                    
                    Debug.Log("[AuthSystemInitializer] 인터셉터 초기화 완료");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AuthSystemInitializer] 인터셉터 초기화 중 오류: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Editor Methods
        
#if UNITY_EDITOR
        [ContextMenu("Initialize Auth System")]
        private void TestInitializeAuthSystem()
        {
            if (Application.isPlaying)
            {
                _ = InitializeAuthSystemAsync();
            }
            else
            {
                Debug.LogWarning("[AuthSystemInitializer] 플레이 모드에서만 초기화할 수 있습니다.");
            }
        }
        
        [ContextMenu("Shutdown Auth System")]
        private void TestShutdownAuthSystem()
        {
            if (Application.isPlaying)
            {
                ShutdownAuthSystem();
            }
            else
            {
                Debug.LogWarning("[AuthSystemInitializer] 플레이 모드에서만 종료할 수 있습니다.");
            }
        }
        
        [ContextMenu("Validate Initialization")]
        private void TestValidateInitialization()
        {
            ValidateInitialization();
        }
#endif
        
        #endregion
    }
}
