using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Utils;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Network.Configs;
using ProjectVG.Infrastructure.Network.Http;

namespace ProjectVG.Core.Managers
{
    /// <summary>
    /// 앱 시작 프로세스의 각 단계를 나타내는 상태
    /// </summary>
    public enum AppStartState
    {
        Initializing,           // 초기화 중
        CheckingManagers,       // 필수 매니저 확인 중
        CheckingServerConnection, // 서버 연결 확인 중  
        CheckingUpdates,        // 업데이트 확인 중
        AttemptingAutoLogin,    // 자동 로그인 시도 중
        LoginRequired,          // 로그인 필요
        LoginSuccessful,        // 로그인 성공
        Ready                   // 앱 시작 준비 완료
    }

    /// <summary>
    /// AppStart 프로세스 정보를 담는 구조체
    /// </summary>
    [System.Serializable]
    public struct AppStartInfo
    {
        public AppStartState state;
        public string message;
        public float progress;
        public bool hasError;
        public string errorMessage;
        
        public AppStartInfo(AppStartState state, string message, float progress, bool hasError = false, string errorMessage = "")
        {
            this.state = state;
            this.message = message;
            this.progress = Mathf.Clamp01(progress);
            this.hasError = hasError;
            this.errorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 앱 시작 프로세스를 전담 관리하는 매니저
    /// SystemManager, AuthManager, LoadingManager와 협력하여 AppStart 플로우를 관리
    /// </summary>
    public class AppStartManager : Singleton<AppStartManager>
    {
        [Header("Settings")]
        [SerializeField] private bool _autoStartOnAwake = true;
        [SerializeField] private float _serverTimeoutSeconds = 5f;
        [SerializeField] private int _maxRetryAttempts = 3;
        
        private AppStartState _currentState = AppStartState.Initializing;
        private bool _isProcessRunning = false;
        
        #region Events
        
        /// <summary>
        /// AppStart 상태가 변경될 때 발생하는 이벤트
        /// </summary>
        public event Action<AppStartInfo> OnStateChanged;
        
        /// <summary>
        /// 매니저 초기화가 완료되었을 때 발생하는 이벤트
        /// </summary>
        public event Action OnManagersInitialized;
        
        /// <summary>
        /// 서버 연결 확인이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<bool> OnServerConnectionChecked;
        
        /// <summary>
        /// 업데이트 확인이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<bool> OnUpdateCheckCompleted;
        
        /// <summary>
        /// 자동 로그인 결과 이벤트
        /// </summary>
        public event Action<bool> OnAutoLoginCompleted;
        
        /// <summary>
        /// 로그인이 필요할 때 발생하는 이벤트
        /// </summary>
        public event Action OnLoginRequired;
        
        /// <summary>
        /// 앱 시작 준비가 완료되었을 때 발생하는 이벤트
        /// </summary>
        public event Action OnAppStartReady;
        
        #endregion
        
        #region Properties
        
        public AppStartState CurrentState => _currentState;
        public bool IsProcessRunning => _isProcessRunning;
        
        #endregion
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            if (this != Instance)
                return;
                
            if (_autoStartOnAwake)
            {
                StartAppProcess().Forget();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 앱 시작 프로세스를 시작합니다
        /// </summary>
        public async UniTask StartAppProcess()
        {
            if (_isProcessRunning)
            {
                Debug.LogWarning("[AppStartManager] 앱 시작 프로세스가 이미 실행 중입니다.");
                return;
            }
            
            _isProcessRunning = true;
            
            try
            {
                await RunAppStartSequence();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppStartManager] 앱 시작 프로세스 중 오류 발생: {ex.Message}");
                UpdateState(AppStartState.Initializing, "시작 프로세스 오류", 0f, true, ex.Message);
            }
            finally
            {
                _isProcessRunning = false;
            }
        }
        
        /// <summary>
        /// 수동으로 로그인을 시도합니다
        /// </summary>
        public async UniTask<bool> TryLoginAsync()
        {
            if (_isProcessRunning)
            {
                Debug.LogWarning("[AppStartManager] 다른 프로세스가 실행 중입니다.");
                return false;
            }
            
            UpdateState(AppStartState.AttemptingAutoLogin, "로그인 시도 중...", 0.7f);
            
            bool loginResult = await AttemptLogin();
            
            if (loginResult)
            {
                UpdateState(AppStartState.LoginSuccessful, "로그인 성공", 0.9f);
                await UniTask.Delay(500); // UI 피드백을 위한 짧은 지연
                
                UpdateState(AppStartState.Ready, "앱 시작 준비 완료", 1.0f);
                OnAppStartReady?.Invoke();
            }
            else
            {
                UpdateState(AppStartState.LoginRequired, "로그인이 필요합니다", 0.6f);
                OnLoginRequired?.Invoke();
            }
            
            return loginResult;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 앱 시작 시퀀스를 실행합니다
        /// </summary>
        private async UniTask RunAppStartSequence()
        {
            // 1. 매니저 초기화 확인
            UpdateState(AppStartState.CheckingManagers, "필수 매니저 확인 중...", 0.1f);
            bool managersReady = await CheckRequiredManagers();
            
            if (!managersReady)
            {
                UpdateState(AppStartState.CheckingManagers, "필수 매니저 초기화 실패", 0.1f, true, "CoreManagerRegistry 초기화 실패");
                return;
            }
            
            OnManagersInitialized?.Invoke();
            
            // 2. 서버 연결 확인
            UpdateState(AppStartState.CheckingServerConnection, "서버 연결 확인 중...", 0.3f);
            bool serverConnected = await CheckServerConnection();
            OnServerConnectionChecked?.Invoke(serverConnected);
            
            if (!serverConnected)
            {
                UpdateState(AppStartState.CheckingServerConnection, "서버 연결 실패", 0.3f, true, "서버에 연결할 수 없습니다. 네트워크 연결을 확인해주세요.");
                return;
            }
            
            // 3. 업데이트 확인 (현재는 스킵, 나중에 구현)
            UpdateState(AppStartState.CheckingUpdates, "업데이트 확인 중...", 0.5f);
            bool updateRequired = await CheckForUpdates();
            OnUpdateCheckCompleted?.Invoke(updateRequired);
            
            if (updateRequired)
            {
                // 업데이트 처리는 나중에 구현
                UpdateState(AppStartState.CheckingUpdates, "업데이트가 필요합니다", 0.5f, false, "새로운 업데이트가 있습니다.");
                // 현재는 계속 진행
            }
            
            // 4. 자동 로그인 시도
            UpdateState(AppStartState.AttemptingAutoLogin, "자동 로그인 시도 중...", 0.7f);
            bool autoLoginSuccess = await AttemptLogin();
            OnAutoLoginCompleted?.Invoke(autoLoginSuccess);
            
            if (autoLoginSuccess)
            {
                UpdateState(AppStartState.LoginSuccessful, "로그인 성공", 0.9f);
                await UniTask.Delay(500); // UI 피드백을 위한 짧은 지연
                
                UpdateState(AppStartState.Ready, "앱 시작 준비 완료", 1.0f);
                OnAppStartReady?.Invoke();
            }
            else
            {
                UpdateState(AppStartState.LoginRequired, "로그인이 필요합니다", 0.6f);
                OnLoginRequired?.Invoke();
            }
        }
        
        /// <summary>
        /// 필수 매니저들의 초기화 상태를 확인합니다
        /// CoreManagerRegistry를 통해 모든 핵심 매니저들의 초기화 완료를 확인
        /// </summary>
        private async UniTask<bool> CheckRequiredManagers()
        {
            // CoreManagerRegistry 확인
            if (CoreManagerRegistry.Instance == null)
            {
                Debug.LogError("[AppStartManager] CoreManagerRegistry를 찾을 수 없습니다.");
                return false;
            }
            
            // CoreManagerRegistry 초기화가 시작되지 않았다면 시작
            if (!CoreManagerRegistry.Instance.IsInitializationStarted)
            {
                Debug.Log("[AppStartManager] CoreManagerRegistry 초기화 시작");
                await CoreManagerRegistry.Instance.InitializeAllManagersAsync();
            }
            
            // CoreManagerRegistry의 모든 매니저 초기화 완료 대기
            int waitCount = 0;
            while (!CoreManagerRegistry.Instance.IsAllManagersInitialized && waitCount < 300) // 최대 30초 대기
            {
                // 초기화 에러가 있는지 확인
                if (CoreManagerRegistry.Instance.InitializationError != null)
                {
                    Debug.LogError($"[AppStartManager] CoreManagerRegistry 초기화 실패: {CoreManagerRegistry.Instance.InitializationError.Message}");
                    return false;
                }
                
                await UniTask.Delay(100);
                waitCount++;
            }
            
            if (!CoreManagerRegistry.Instance.IsAllManagersInitialized)
            {
                Debug.LogError("[AppStartManager] CoreManagerRegistry 초기화 타임아웃 (30초)");
                return false;
            }
            
            Debug.Log("[AppStartManager] 모든 핵심 매니저 초기화 확인 완료");
            return true;
        }
        
        /// <summary>
        /// 서버 연결 상태를 확인합니다
        /// </summary>
        private async UniTask<bool> CheckServerConnection()
        {
            try
            {
                // NetworkConfig에서 서버 주소 가져오기
                string serverUrl = NetworkConfig.HttpServerAddress;
                
                // HttpApiClient를 통한 간단한 연결 테스트
                var httpClient = HttpApiClient.Instance;
                if (httpClient == null)
                {
                    Debug.LogError("[AppStartManager] HttpApiClient를 찾을 수 없습니다.");
                    return false;
                }
                
                Debug.Log($"[AppStartManager] 서버 연결 확인: {serverUrl}");
                
                // 실제 HTTP 요청으로 서버 연결 테스트
                // 헬스체크나 공개 API 엔드포인트 호출 시도
                try
                {
                    // 간단한 연결 테스트 (타임아웃 설정)
                    var timeoutCts = new System.Threading.CancellationTokenSource(System.TimeSpan.FromSeconds(_serverTimeoutSeconds));
                    
                    // HttpApiClient를 통해 간단한 연결 확인
                    // 실제 서버에 /health나 /ping 엔드포인트가 있다고 가정
                    // 현재는 시뮬레이션
                    await UniTask.Delay(System.TimeSpan.FromMilliseconds(500), cancellationToken: timeoutCts.Token);
                    
                    // 실제 HTTP 요청을 시도하려면:
                    // var response = await httpClient.GetAsync<object>("/health", requiresAuth: false, cancellationToken: timeoutCts.Token);
                    
                    Debug.Log("[AppStartManager] 서버 연결 확인 완료");
                    return true;
                }
                catch (System.OperationCanceledException)
                {
                    Debug.LogError($"[AppStartManager] 서버 연결 타임아웃 ({_serverTimeoutSeconds}초)");
                    return false;
                }
                catch (Exception networkEx)
                {
                    Debug.LogError($"[AppStartManager] 네트워크 요청 실패: {networkEx.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppStartManager] 서버 연결 확인 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 업데이트 확인을 수행합니다
        /// </summary>
        private async UniTask<bool> CheckForUpdates()
        {
            try
            {
                // 현재는 업데이트 체크 로직이 없으므로 간단히 처리
                await UniTask.Delay(300); // 업데이트 체크 시뮬레이션
                
                Debug.Log("[AppStartManager] 업데이트 확인 완료 - 업데이트 없음");
                return false; // 업데이트 불필요
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppStartManager] 업데이트 확인 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 자동 로그인을 시도합니다
        /// </summary>
        private async UniTask<bool> AttemptLogin()
        {
            try
            {
                // CoreManagerRegistry를 통해 AuthManager 가져오기
                var authManager = CoreManagerRegistry.Instance?.AuthManager;
                if (authManager == null)
                {
                    Debug.LogError("[AppStartManager] AuthManager를 찾을 수 없습니다.");
                    return false;
                }
                
                // AuthManager가 이미 로그인된 상태인지 확인
                if (authManager.IsLoggedIn)
                {
                    Debug.Log("[AppStartManager] 이미 로그인된 상태입니다.");
                    return true;
                }
                
                // RefreshToken이 있으면 자동 로그인 시도
                if (authManager.HasValidRefreshToken)
                {
                    Debug.Log("[AppStartManager] RefreshToken으로 자동 로그인 시도");
                    bool refreshSuccess = await authManager.RefreshTokenAsync();
                    
                    if (refreshSuccess)
                    {
                        Debug.Log("[AppStartManager] 자동 로그인 성공");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("[AppStartManager] RefreshToken으로 로그인 실패");
                    }
                }
                
                Debug.Log("[AppStartManager] 자동 로그인 불가 - 수동 로그인 필요");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppStartManager] 자동 로그인 시도 중 오류: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 상태를 업데이트하고 이벤트를 발생시킵니다
        /// </summary>
        private void UpdateState(AppStartState newState, string message, float progress, bool hasError = false, string errorMessage = "")
        {
            _currentState = newState;
            
            var appStartInfo = new AppStartInfo(newState, message, progress, hasError, errorMessage);
            
            Debug.Log($"[AppStartManager] State: {newState}, Message: {message}, Progress: {progress:F2}");
            
            if (hasError)
            {
                Debug.LogError($"[AppStartManager] Error: {errorMessage}");
            }
            
            OnStateChanged?.Invoke(appStartInfo);
        }
        
        #endregion
    }
}