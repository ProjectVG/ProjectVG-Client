using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Core.DI;
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

    public class GameManager : Singleton<GameManager>
    {
        [Header("Core Managers")]
        [SerializeField] private InitializationManager _initializationManager;
        [SerializeField] private ManagerRegistry _managerRegistry;
        [SerializeField] private DependencyManager _dependencyManager;
        
        [Header("Settings")]
        [SerializeField] private bool _autoInitializeOnStart = true;
        [SerializeField] private bool _createManagersIfNotExist = true;
        
        // 핵심 상태만 노출
        public bool IsInitialized => _initializationManager?.IsInitialized ?? false;
        public InitializationPhase CurrentPhase => _initializationManager?.CurrentPhase ?? InitializationPhase.NotStarted;
        
        // 매니저 접근 (필요시에만)
        public WebSocketManager WebSocketManager => _managerRegistry?.WebSocketManager;
        public SessionManager SessionManager => _managerRegistry?.SessionManager;
        
        // 필요한 이벤트만 전달
        public event Action OnGameInitialized
        {
            add => _initializationManager.OnInitializationCompleted += value;
            remove => _initializationManager.OnInitializationCompleted -= value;
        }
        public event Action<string> OnInitializationError
        {
            add => _initializationManager.OnInitializationError += value;
            remove => _initializationManager.OnInitializationError -= value;
        }
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
            
            if (_autoInitializeOnStart) 
            {
                InitializeGame();
            }
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        public async void InitializeGame()
        {
            await InitializeGameAsync();
        }

        public async UniTask InitializeGameAsync()
        {
            if (_initializationManager == null)
            {
                Debug.LogError("[GameManager] InitializationManager가 설정되지 않았습니다.");
                return;
            }
            
            // 이미 초기화 완료되었으면 반환
            if (_initializationManager.IsInitialized)
            {
                Debug.Log("[GameManager] 이미 초기화가 완료되었습니다.");
                return;
            }
            
            // 초기화 중이면 대기
            if (_initializationManager.IsInitializing)
            {
                Debug.Log("[GameManager] 초기화가 진행 중입니다. 완료까지 대기합니다.");
                while (_initializationManager.IsInitializing && !_initializationManager.IsInitialized)
                {
                    await UniTask.Yield();
                }
                return;
            }
            
            Debug.Log("[GameManager] 초기화 시작");
            await _initializationManager.InitializeAsync();
        }
        
        // TryConnectSessionAsync 제거됨 - InitializationManager에서 처리
        
        public void Shutdown()
        {
            Debug.Log("[GameManager] 종료 처리");
            _managerRegistry?.ShutdownAllManagers();
        }
        
        // AreManagersReady, IsSessionConnected 제거됨 - 불필요한 래퍼
        
        [ContextMenu("Log Manager Status")]
        public void LogManagerStatus()
        {
            Debug.Log($"[GameManager] 초기화: {(IsInitialized ? "완료" : "미완료")}, 단계: {CurrentPhase}");
            _managerRegistry?.LogManagerStatus();
        }
        
        // GetInitializationStatus 제거됨 - InitializationManager에서 직접 접근

        #endregion
        
        #region Private Methods
        
        private void InitializeComponents()
        {
            CreateManagersIfNotExist();
            SetupManagerReferences();
        }
        
        private void CreateManagersIfNotExist()
        {
            if (_createManagersIfNotExist)
            {
                if (_initializationManager == null)
                {
                    var initObj = new GameObject("InitializationManager");
                    initObj.transform.SetParent(transform);
                    _initializationManager = initObj.AddComponent<InitializationManager>();
                }
                
                if (_managerRegistry == null)
                {
                    var registryObj = new GameObject("ManagerRegistry");
                    registryObj.transform.SetParent(transform);
                    _managerRegistry = registryObj.AddComponent<ManagerRegistry>();
                }
                
                if (_dependencyManager == null)
                {
                    var depObj = new GameObject("DependencyManager");
                    depObj.transform.SetParent(transform);
                    _dependencyManager = depObj.AddComponent<DependencyManager>();
                }
            }
        }
        
        private void SetupManagerReferences()
        {
            if (_initializationManager != null && _managerRegistry != null && _dependencyManager != null)
            {
                _initializationManager.Initialize(_managerRegistry, _dependencyManager);
                Debug.Log("[GameManager] 매니저 참조 설정 완료");
            }
            else
            {
                Debug.LogError("[GameManager] 필수 매니저가 설정되지 않았습니다.");
            }
        }
        
        #endregion
    }
    
    public interface IManager
    {
        void Shutdown();
    }
} 