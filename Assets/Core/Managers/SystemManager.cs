using UnityEngine;
using System;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Core.DI;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Managers
{
    public class SystemManager : Singleton<SystemManager>
    {
        [Header("Core Managers")]
        [SerializeField] private InitializationManager _initializationManager;
        [SerializeField] private ManagerRegistry _managerRegistry;
        [SerializeField] private DependencyManager _dependencyManager;
        
        [Header("Settings")]
        [SerializeField] private bool _autoInitializeOnStart = true;
        [SerializeField] private bool _createManagersIfNotExist = true;
        
        private bool _initializationKickoffDone = false;
        
        public bool IsInitialized => _initializationManager?.IsInitialized ?? false;
        
        public WebSocketManager WebSocketManager => _managerRegistry?.WebSocketManager;
        public SessionManager SessionManager => _managerRegistry?.SessionManager;
        
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

        protected override void Awake()
        {
            base.Awake();
            if (this != Instance)
            {
                return;
            }
            InitializeComponents();
            
            if (_autoInitializeOnStart && !_initializationKickoffDone && !IsInitialized)
            {
                _initializationKickoffDone = true;
                InitializeGame();
            }
        }
        
        private void OnDestroy()
        {
            if (this != Instance)
            {
                return;
            }
            Shutdown();
        }
        
        public async void InitializeGame()
        {
            if (_initializationKickoffDone && (IsInitialized || (_initializationManager?.IsInitializing ?? false)))
            {
                return;
            }
            _initializationKickoffDone = true;
            await InitializeGameAsync();
        }

        public async UniTask InitializeGameAsync()
        {
            if (_initializationManager == null)
            {
                Debug.LogError("[SystemManager] InitializationManager가 설정되지 않았습니다.");
                return;
            }
            
            if (_initializationManager.IsInitialized)
            {
                return;
            }

            if (_initializationManager.IsInitializing)
            {
                while (_initializationManager.IsInitializing && !_initializationManager.IsInitialized)
                {
                    await UniTask.Yield();
                }
                return;
            }
            
            await _initializationManager.InitializeAsync();
            if (IsInitialized)
            {
                Debug.Log("[SystemManager] 게임 시스템 준비 완료");
            }
        }
        
        public void Shutdown()
        {
            _managerRegistry?.ShutdownAllManagers();
            Debug.Log("[SystemManager] 시스템 종료 완료");
        }
        
        [ContextMenu("Log Manager Status")]
        public void LogManagerStatus()
        {
            Debug.Log($"[SystemManager] Initialized: {IsInitialized}");
            _managerRegistry?.LogManagerStatus();
        }

        public async UniTask TransitionToMainSceneAsync()
        {
            try
            {
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainSence");
                Debug.Log("[SystemManager] MainScene 전환 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SystemManager] 씬 전환 실패: {ex.Message}");
            }
        }
        
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
            }
            else
            {
                Debug.LogError("[SystemManager] 필수 매니저가 설정되지 않았습니다.");
            }
        }
    }
    
    public interface IManager
    {
        void Shutdown();
    }
} 