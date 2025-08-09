using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Managers
{
    /** 
     * 초기화 과정을 전담하는 매니저
     * 단계별 초기화, 진행률 추적, 이벤트 발생을 담당
     */
    public class InitializationManager : MonoBehaviour
    {
        private InitializationPhase _currentPhase = InitializationPhase.NotStarted;
        private float _initializationProgress = 0f;
        private bool _isInitialized = false;
        private bool _isInitializing = false;
        
        public InitializationPhase CurrentPhase => _currentPhase;
        public float InitializationProgress => _initializationProgress;
        public bool IsInitialized => _isInitialized;
        public bool IsInitializing => _isInitializing;
        
        public event Action<InitializationPhase> OnPhaseChanged;
        public event Action<float> OnProgressChanged;
        public event Action OnInitializationCompleted;
        public event Action<string> OnInitializationError;
        
        private ManagerRegistry _managerRegistry;
        private DependencyManager _dependencyManager;
        
        #region Public Methods
        
        public void Initialize(ManagerRegistry managerRegistry, DependencyManager dependencyManager)
        {
            _managerRegistry = managerRegistry;
            _dependencyManager = dependencyManager;
        }
        
        public async UniTask InitializeAsync()
        {
            // 중복 초기화 방지
            if (_isInitialized)
            {
                Debug.Log("[InitializationManager] 이미 초기화가 완료되었습니다.");
                return;
            }
            
            if (_isInitializing)
            {
                Debug.Log("[InitializationManager] 이미 초기화가 진행 중입니다. 대기합니다.");
                
                // 초기화 완료까지 대기
                while (_isInitializing && !_isInitialized)
                {
                    await UniTask.Yield();
                }
                return;
            }
            
            _isInitializing = true;
            
            try
            {
                SetPhase(InitializationPhase.InitializingManagers);
                UpdateProgress(0f);

                await InitializeManagersAsync();
                UpdateProgress(0.4f);

                SetPhase(InitializationPhase.ConnectingToServer);
                await ConnectToServerAsync();
                UpdateProgress(0.8f);

                SetPhase(InitializationPhase.LoadingResources);
                await LoadResourcesAsync();
                UpdateProgress(1f);

                SetPhase(InitializationPhase.Completed);
                _isInitialized = true;
                
                OnInitializationCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                string error = $"초기화 실패: {ex.Message}";
                Debug.LogError($"[InitializationManager] {error}");
                OnInitializationError?.Invoke(error);
            }
            finally
            {
                _isInitializing = false;
            }
        }
        
        public InitializationStatus GetStatus()
        {
            return new InitializationStatus
            {
                IsManagersInitialized = _isInitialized,
                IsServerConnected = _managerRegistry?.IsSessionConnected() ?? false,
                CurrentPhase = _currentPhase,
                Progress = _initializationProgress
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask InitializeManagersAsync()
        {
            if (_managerRegistry == null)
                throw new InvalidOperationException("ManagerRegistry가 설정되지 않았습니다.");
                
            _managerRegistry.InitializeAllManagers();
            _dependencyManager?.SetupDependencies(_managerRegistry);
            
            // DI 완료 후 SessionManager 초기화
            if (_managerRegistry.SessionManager != null)
            {
                _managerRegistry.SessionManager.Initialize();
                Debug.Log("[InitializationManager] SessionManager 초기화 완료");
            }
        }
        
        private async UniTask ConnectToServerAsync()
        {
            if (_managerRegistry == null)
                throw new InvalidOperationException("ManagerRegistry가 설정되지 않았습니다.");
            
            var sessionManager = _managerRegistry.SessionManager;
            if (sessionManager == null)
                throw new InvalidOperationException("SessionManager가 초기화되지 않았습니다.");
                
            bool connected = await sessionManager.EnsureConnectionAsync();
            if (!connected)
            {
                throw new InvalidOperationException("세션 연결에 실패했습니다.");
            }
            
            Debug.Log("[InitializationManager] 서버 연결 완료");
        }
        
        private async UniTask LoadResourcesAsync()
        {
            
            //await UniTask.Delay(500);
            Debug.Log("[InitializationManager] 리소스 로딩 완료");
        }
        
        private void SetPhase(InitializationPhase phase)
        {
            _currentPhase = phase;
            Debug.Log($"[InitializationManager] 초기화 단계: {phase}");
            OnPhaseChanged?.Invoke(phase);
        }

        private void UpdateProgress(float progress)
        {
            _initializationProgress = progress;
            OnProgressChanged?.Invoke(progress);
        }
        
        #endregion
    }
}
