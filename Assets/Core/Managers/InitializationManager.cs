using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Managers
{
    public enum InitializationPhase
    {
        NotStarted,
        InitializingManagers,
        ConnectingToServer,
        LoadingResources,
        Completed
    }
    
    /** 
     * 초기화 과정을 전담하는 매니저
     * 단계별 초기화, 진행률 추적, 이벤트 발생을 담당
     */
    public class InitializationManager : MonoBehaviour
    {
        private InitializationPhase _currentPhase = InitializationPhase.NotStarted;
        private bool _isInitialized = false;
        private bool _isInitializing = false;
        
        public InitializationPhase CurrentPhase => _currentPhase;
        public bool IsInitialized => _isInitialized;
        public bool IsInitializing => _isInitializing;
        
        public event Action OnInitializationCompleted;
        public event Action<string> OnInitializationError;
        public event Action<string, string, float> OnProgressUpdated;
        
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
            if (_isInitialized)
            {
                return;
            }
            
            if (_isInitializing)
            {
                while (_isInitializing && !_isInitialized)
                {
                    await UniTask.Yield();
                }
                return;
            }
            
            _isInitializing = true;
            
            try
            {
                _currentPhase = InitializationPhase.InitializingManagers;
                await InitializeManagersAsync();

                _currentPhase = InitializationPhase.ConnectingToServer;
                await ConnectToServerAsync();

                _currentPhase = InitializationPhase.LoadingResources;
                await LoadResourcesAsync();

                _currentPhase = InitializationPhase.Completed;
                UpdateLoadingProgress("INITIALIZATION", "게임 준비 완료", 1.0f);
                
                _isInitialized = true;
                Debug.Log("[InitializationManager] 초기화 완료");
                OnInitializationCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                string error = $"[InitializationManager] 초기화 실패: {ex.Message}";
                Debug.LogError(error);
                OnInitializationError?.Invoke(error);
            }
            finally
            {
                _isInitializing = false;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask InitializeManagersAsync()
        {
            if (_managerRegistry == null)
                throw new InvalidOperationException("ManagerRegistry가 설정되지 않았습니다.");
            
            UpdateLoadingProgress("INITIALIZATION", "시스템 매니저 초기화", 0.05f);
            
            _managerRegistry.InitializeAllManagers();
            
            UpdateLoadingProgress("INITIALIZATION", "매니저 등록", 0.12f);
            
            _dependencyManager?.SetupDependencies(_managerRegistry);

            UpdateLoadingProgress("INITIALIZATION", "의존성 주입", 0.20f);
            
            if (_managerRegistry.SessionManager != null)
            {
                _managerRegistry.SessionManager.Initialize();
            }
        }
        
        private async UniTask ConnectToServerAsync()
        {
            if (_managerRegistry == null)
                throw new InvalidOperationException("ManagerRegistry가 설정되지 않았습니다.");
            
            var sessionManager = _managerRegistry.SessionManager;
            if (sessionManager == null)
                throw new InvalidOperationException("SessionManager가 초기화되지 않았습니다.");
            
            UpdateLoadingProgress("INITIALIZATION", "네트워크 연결", 0.30f);
            UpdateLoadingProgress("INITIALIZATION", "세션 생성", 0.45f);
            
            bool connected = await sessionManager.EnsureConnectionAsync();
            if (!connected)
            {
                throw new InvalidOperationException("세션 연결에 실패했습니다.");
            }
            
            UpdateLoadingProgress("INITIALIZATION", "세션 완료", 0.60f);
        }
        
        private async UniTask LoadResourcesAsync()
        {
            UpdateLoadingProgress("INITIALIZATION", "리소스 스캔", 0.65f);
            await UniTask.Delay(100);
            UpdateLoadingProgress("INITIALIZATION", "필수 에셋 로딩", 0.75f);
            await UniTask.Delay(100);
            UpdateLoadingProgress("INITIALIZATION", "리소스 로딩 완료", 0.90f);
        }
        
        private void UpdateLoadingProgress(string taskName, string description, float progress)
        {
            OnProgressUpdated?.Invoke(taskName, description, progress);
        }
        
        #endregion
    }
}
