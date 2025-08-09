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
        
        public bool IsInitialized => _initializationManager?.IsInitialized ?? false;
        public InitializationPhase CurrentPhase => _initializationManager?.CurrentPhase ?? InitializationPhase.NotStarted;
        public float InitializationProgress => _initializationManager?.InitializationProgress ?? 0f;
        public WebSocketManager WebSocketManager => _managerRegistry?.WebSocketManager;
        public SessionManager SessionManager => _managerRegistry?.SessionManager;
        public HttpApiClient HttpApiClient => _managerRegistry?.HttpApiClient;
        public AudioManager AudioManager => _managerRegistry?.AudioManager;
        
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
        public event Action<InitializationPhase> OnPhaseChanged
        {
            add => _initializationManager.OnPhaseChanged += value;
            remove => _initializationManager.OnPhaseChanged -= value;
        }
        public event Action<float> OnProgressChanged
        {
            add => _initializationManager.OnProgressChanged += value;
            remove => _initializationManager.OnProgressChanged -= value;
        }
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
            
            if (_autoInitializeOnStart) {
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
            Debug.Log("[GameManager] 초기화 시작");
            
            if (_initializationManager == null)
            {
                Debug.LogError("[GameManager] InitializationManager가 설정되지 않았습니다.");
                return;
            }
            
            await _initializationManager.InitializeAsync();
        }
        
        public async UniTask<bool> TryConnectSessionAsync()
        {
            if (_managerRegistry == null)
            {
                Debug.LogError("[GameManager] ManagerRegistry가 설정되지 않았습니다.");
                return false;
            }
            
            return await _managerRegistry.TryConnectSessionAsync();
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[GameManager] 종료 처리 시작");
            
            if (_managerRegistry != null)
            {
                _managerRegistry.ShutdownAllManagers();
            }
            
            Debug.Log("[GameManager] 종료 처리 완료");
        }
        
        public bool AreManagersReady()
        {
            return _managerRegistry?.AreManagersReady() ?? false;
        }
        
        public bool IsSessionConnected()
        {
            return _managerRegistry?.IsSessionConnected() ?? false;
        }
        
        [ContextMenu("Log Manager Status")]
        public void LogManagerStatus()
        {
            Debug.Log("[GameManager] === 매니저 상태 ===");
            Debug.Log($"[GameManager] 초기화: {(IsInitialized ? "완료" : "미완료")}");
            
            if (_managerRegistry != null)
            {
                _managerRegistry.LogManagerStatus();
            }
            else
            {
                Debug.LogWarning("[GameManager] ManagerRegistry가 설정되지 않았습니다.");
            }
        }
        
        public InitializationStatus GetInitializationStatus()
        {
            return _initializationManager?.GetStatus() ?? new InitializationStatus
            {
                IsManagersInitialized = false,
                IsServerConnected = false,
                CurrentPhase = InitializationPhase.NotStarted,
                Progress = 0f
            };
        }

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

    public class InitializationStatus
    {
        public bool IsManagersInitialized { get; set; }
        public bool IsServerConnected { get; set; }
        public InitializationPhase CurrentPhase { get; set; }
        public float Progress { get; set; }
    }
    
    public interface IManager
    {
        void Shutdown();
    }
} 