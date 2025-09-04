#nullable enable
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Utils;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Chat.Service;
using ProjectVG.Core.Input;

namespace ProjectVG.Core.Managers
{
    /// <summary>
    /// 모든 핵심 매니저들의 싱글톤 인스턴스를 중앙 관리하고 순차적으로 초기화하는 레지스트리
    /// AppStartManager가 이 클래스의 초기화 완료를 기다린 후 앱 시작 프로세스를 진행
    /// </summary>
    public class CoreManagerRegistry : Singleton<CoreManagerRegistry>
    {
        [Header("Core Managers")]
        [SerializeField] private bool _autoInitializeOnAwake = true;
        [SerializeField] private float _initializationTimeoutSeconds = 30f;
        
        private bool _isInitialized = false;
        private bool _initializationStarted = false;
        private Exception? _initializationError = null;
        
        #region Manager References
        
        private AuthManager? _authManager;
        private AudioManager? _audioManager;
        private WebSocketManager? _webSocketManager;
        private HttpApiClient? _httpApiClient;
        private ChatSystemManager? _chatSystemManager;
        private ScreenTapManager? _screenTapManager;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 모든 매니저들의 초기화가 완료되었는지 여부
        /// </summary>
        public bool IsAllManagersInitialized => _isInitialized;
        
        /// <summary>
        /// 초기화 시작 여부
        /// </summary>
        public bool IsInitializationStarted => _initializationStarted;
        
        /// <summary>
        /// 초기화 중 발생한 오류
        /// </summary>
        public Exception? InitializationError => _initializationError;
        
        /// <summary>
        /// AuthManager 인스턴스
        /// </summary>
        public AuthManager? AuthManager => _authManager;
        
        /// <summary>
        /// AudioManager 인스턴스
        /// </summary>
        public AudioManager? AudioManager => _audioManager;
        
        /// <summary>
        /// WebSocketManager 인스턴스
        /// </summary>
        public WebSocketManager? WebSocketManager => _webSocketManager;
        
        /// <summary>
        /// HttpApiClient 인스턴스
        /// </summary>
        public HttpApiClient? HttpApiClient => _httpApiClient;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// 모든 매니저 초기화가 완료되었을 때 발생하는 이벤트
        /// </summary>
        public event Action? OnAllManagersInitialized;
        
        /// <summary>
        /// 매니저 초기화 실패 시 발생하는 이벤트
        /// </summary>
        public event Action<Exception>? OnInitializationFailed;
        
        /// <summary>
        /// 개별 매니저 초기화 완료 시 발생하는 이벤트 (진행률 표시용)
        /// </summary>
        public event Action<string, int, int>? OnManagerInitialized; // managerName, current, total
        
        #endregion
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            if (this != Instance)
                return;
                
            if (_autoInitializeOnAwake && !_initializationStarted)
            {
                InitializeAllManagersAsync().Forget();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 모든 매니저들을 순차적으로 초기화합니다
        /// </summary>
        public async UniTask InitializeAllManagersAsync()
        {
            if (_initializationStarted)
            {
                Debug.LogWarning("[CoreManagerRegistry] 이미 초기화가 시작되었습니다.");
                return;
            }
            
            _initializationStarted = true;
            _initializationError = null;
            
            try
            {
                Debug.Log("[CoreManagerRegistry] 핵심 매니저들 초기화 시작");
                
                // 타임아웃 설정
                var timeoutCts = new System.Threading.CancellationTokenSource(System.TimeSpan.FromSeconds(_initializationTimeoutSeconds));
                
                await InitializeManagersInOrder(timeoutCts.Token);
                
                _isInitialized = true;
                Debug.Log("[CoreManagerRegistry] 모든 핵심 매니저 초기화 완료");
                OnAllManagersInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                _initializationError = ex;
                Debug.LogError($"[CoreManagerRegistry] 매니저 초기화 실패: {ex.Message}");
                OnInitializationFailed?.Invoke(ex);
                throw;
            }
        }
        
        /// <summary>
        /// 초기화 상태를 리셋합니다 (테스트용)
        /// </summary>
        public void ResetInitializationState()
        {
            _isInitialized = false;
            _initializationStarted = false;
            _initializationError = null;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 매니저들을 의존성 순서에 따라 초기화합니다
        /// </summary>
        private async UniTask InitializeManagersInOrder(System.Threading.CancellationToken cancellationToken)
        {
            const int totalManagers = 6;
            int currentManager = 0;
            
            // 1. HttpApiClient (네트워크 통신 기반)
            currentManager++;
            Debug.Log($"[CoreManagerRegistry] HttpApiClient 초기화 중... ({currentManager}/{totalManagers})");
            _httpApiClient = HttpApiClient.Instance;
            _httpApiClient.Initialize();
            OnManagerInitialized?.Invoke("HttpApiClient", currentManager, totalManagers);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            
            // 2. AuthManager (HTTP 클라이언트 의존)
            currentManager++;
            Debug.Log($"[CoreManagerRegistry] AuthManager 초기화 중... ({currentManager}/{totalManagers})");
            _authManager = AuthManager.Instance;
            await WaitForAuthManagerInitialization(cancellationToken);
            OnManagerInitialized?.Invoke("AuthManager", currentManager, totalManagers);
            
            // 3. WebSocketManager (HTTP 클라이언트와 독립적)
            currentManager++;
            Debug.Log($"[CoreManagerRegistry] WebSocketManager 초기화 중... ({currentManager}/{totalManagers})");
            _webSocketManager = WebSocketManager.Instance;
            _webSocketManager.Initialize();
            OnManagerInitialized?.Invoke("WebSocketManager", currentManager, totalManagers);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            
            // 4. AudioManager (독립적)
            currentManager++;
            Debug.Log($"[CoreManagerRegistry] AudioManager 초기화 중... ({currentManager}/{totalManagers})");
            _audioManager = AudioManager.Instance;
            _audioManager.Initialize();
            OnManagerInitialized?.Invoke("AudioManager", currentManager, totalManagers);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            
            // 5. ChatSystemManager (WebSocket과 Auth 의존)
            currentManager++;
            Debug.Log($"[CoreManagerRegistry] ChatSystemManager 초기화 중... ({currentManager}/{totalManagers})");
            _chatSystemManager = ChatSystemManager.Instance;
            OnManagerInitialized?.Invoke("ChatSystemManager", currentManager, totalManagers);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            
            // 6. ScreenTapManager (독립적)
            currentManager++;
            Debug.Log($"[CoreManagerRegistry] ScreenTapManager 초기화 중... ({currentManager}/{totalManagers})");
            _screenTapManager = ScreenTapManager.Instance;
            OnManagerInitialized?.Invoke("ScreenTapManager", currentManager, totalManagers);
            await UniTask.Delay(100, cancellationToken: cancellationToken);
            
            Debug.Log($"[CoreManagerRegistry] 총 {totalManagers}개 매니저 초기화 완료");
        }
        
        /// <summary>
        /// AuthManager의 비동기 초기화 완료를 기다립니다
        /// </summary>
        private async UniTask WaitForAuthManagerInitialization(System.Threading.CancellationToken cancellationToken)
        {
            if (_authManager == null)
                return;
                
            // AuthManager는 Start()에서 InitializeAsync()를 호출하므로 잠시 대기
            await UniTask.Delay(500, cancellationToken: cancellationToken);
            
            // AuthManager의 초기화가 완료될 때까지 대기 (최대 10초)
            int maxWaitCycles = 100; // 10초 (100ms * 100)
            int waitCycles = 0;
            
            while (waitCycles < maxWaitCycles && !cancellationToken.IsCancellationRequested)
            {
                // AuthManager가 초기화되었는지 확인하는 방법이 없으므로
                // 일정 시간 대기 후 진행 (AuthManager 내부에서 초기화 완료 이벤트나 플래그 추가 필요)
                await UniTask.Delay(100, cancellationToken: cancellationToken);
                waitCycles++;
                
                // 임시로 5초 대기 후 진행
                if (waitCycles >= 50) // 5초
                {
                    Debug.Log("[CoreManagerRegistry] AuthManager 초기화 대기 완료 (5초 경과)");
                    break;
                }
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("AuthManager 초기화 대기가 취소되었습니다.", cancellationToken);
            }
        }
        
        #endregion
        
        #region Debug Methods
        
        /// <summary>
        /// 현재 매니저들의 상태를 반환합니다 (디버그용)
        /// </summary>
        public string GetManagerStatus()
        {
            var status = "=== CoreManagerRegistry Status ===\n";
            status += $"Initialization Started: {_initializationStarted}\n";
            status += $"All Managers Initialized: {_isInitialized}\n";
            status += $"Initialization Error: {_initializationError?.Message ?? "None"}\n\n";
            
            status += "Manager Instances:\n";
            status += $"- AuthManager: {(_authManager != null ? "✓" : "✗")}\n";
            status += $"- AudioManager: {(_audioManager != null ? "✓" : "✗")}\n";
            status += $"- WebSocketManager: {(_webSocketManager != null ? "✓" : "✗")}\n";
            status += $"- HttpApiClient: {(_httpApiClient != null ? "✓" : "✗")}\n";
            status += $"- ChatSystemManager: {(_chatSystemManager != null ? "✓" : "✗")}\n";
            status += $"- ScreenTapManager: {(_screenTapManager != null ? "✓" : "✗")}\n";
            
            status += "===================================";
            return status;
        }
        
        #endregion
    }
}