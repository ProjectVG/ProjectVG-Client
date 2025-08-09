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
        
        public InitializationPhase CurrentPhase => _currentPhase;
        public float InitializationProgress => _initializationProgress;
        public bool IsInitialized => _isInitialized;
        
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
            Debug.Log("[InitializationManager] 초기화 시작");
            
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
                
                Debug.Log("[InitializationManager] 초기화 완료");
                OnInitializationCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                string error = $"초기화 실패: {ex.Message}";
                Debug.LogError($"[InitializationManager] {error}");
                OnInitializationError?.Invoke(error);
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
            Debug.Log("[InitializationManager] 매니저 초기화 시작");
            
            if (_managerRegistry == null)
                throw new InvalidOperationException("ManagerRegistry가 설정되지 않았습니다.");
                
            _managerRegistry.InitializeAllManagers();
            _dependencyManager?.SetupDependencies(_managerRegistry);
            
            Debug.Log("[InitializationManager] 매니저 초기화 완료");
        }
        
        private async UniTask ConnectToServerAsync()
        {
            Debug.Log("[InitializationManager] 서버 연결 시작");
            
            if (_managerRegistry == null)
                throw new InvalidOperationException("ManagerRegistry가 설정되지 않았습니다.");
                
            bool connected = await _managerRegistry.TryConnectSessionAsync();
            if (!connected)
            {
                throw new InvalidOperationException("서버 연결에 실패했습니다.");
            }
            
            Debug.Log("[InitializationManager] 서버 연결 완료");
        }
        
        private async UniTask LoadResourcesAsync()
        {
            Debug.Log("[InitializationManager] 리소스 로딩 시작");
            
            await UniTask.Delay(500);
            
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
