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
        private bool _isInitialized = false;
        private bool _isInitializing = false;
        
        public InitializationPhase CurrentPhase => _currentPhase;
        public bool IsInitialized => _isInitialized;
        public bool IsInitializing => _isInitializing;
        
        // 필요한 이벤트만 유지 (외부에서 구독 가능)
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
            
            Debug.Log("[InitializationManager] 초기화 완료");
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
                _currentPhase = InitializationPhase.InitializingManagers;
                await InitializeManagersAsync();

                _currentPhase = InitializationPhase.ConnectingToServer;
                await ConnectToServerAsync();

                _currentPhase = InitializationPhase.LoadingResources;
                await LoadResourcesAsync();

                _currentPhase = InitializationPhase.Completed;
                UpdateLoadingProgress("INITIALIZATION", "게임 준비 완료", 1.0f);
                
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
        
        // GetStatus 제거됨 - public 프로퍼티로 직접 접근
        
        #endregion
        
        #region Private Methods
        
        private async UniTask InitializeManagersAsync()
        {
            if (_managerRegistry == null)
                throw new InvalidOperationException("ManagerRegistry가 설정되지 않았습니다.");
            
            // 전체 진행률: 0~20% (매니저 초기화 + 의존성 주입)
            UpdateLoadingProgress("INITIALIZATION", "시스템 매니저 초기화 중...", 0.05f);
            
            _managerRegistry.InitializeAllManagers();
            
            UpdateLoadingProgress("INITIALIZATION", "매니저 등록 중...", 0.12f);
            
            _dependencyManager?.SetupDependencies(_managerRegistry);

            UpdateLoadingProgress("INITIALIZATION", "의존성 주입 완료", 0.20f);
            
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
            
            // 전체 진행률: 20~60% (네트워크 연결 + 세션 생성)
            UpdateLoadingProgress("INITIALIZATION", "네트워크 연결 중...", 0.30f);
            
            // 세션 연결 시도
            UpdateLoadingProgress("INITIALIZATION", "세션 생성 중...", 0.45f);
            
            bool connected = await sessionManager.EnsureConnectionAsync();
            if (!connected)
            {
                throw new InvalidOperationException("세션 연결에 실패했습니다.");
            }
            
            UpdateLoadingProgress("INITIALIZATION", "세션 생성 완료", 0.60f);
            Debug.Log("[InitializationManager] 서버 연결 완료");
        }
        
        private async UniTask LoadResourcesAsync()
        {
            // 전체 진행률: 60~90% (리소스 로딩)
            UpdateLoadingProgress("INITIALIZATION", "리소스 스캔 중...", 0.65f);
            
            // 시뮬레이션: 실제 리소스 로딩
            await UniTask.Delay(100);
            UpdateLoadingProgress("INITIALIZATION", "필수 에셋 로딩 중...", 0.75f);
            
            await UniTask.Delay(100);
            UpdateLoadingProgress("INITIALIZATION", "리소스 로딩 완료", 0.90f);
            
            Debug.Log("[InitializationManager] 리소스 로딩 완료");
        }
        
        // SetPhase, UpdateProgress 제거됨 - LoadingManager가 UI 업데이트 담당
        
        
        /// <summary>
        /// 로딩 진행상황을 업데이트하는 메서드 (이벤트 기반)
        /// </summary>
        private void UpdateLoadingProgress(string taskName, string description, float progress)
        {
            Debug.Log($"[InitializationManager] {taskName}: {description} ({Mathf.RoundToInt(progress * 100)}%)");
            OnProgressUpdated?.Invoke(taskName, description, progress);
        }
        
        #endregion
    }
}
