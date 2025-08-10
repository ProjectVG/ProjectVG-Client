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
    public class GameManager : Singleton<GameManager>
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
                Debug.LogError("[GameManager] InitializationManager가 설정되지 않았습니다.");
                return;
            }
            
            if (_initializationManager.IsInitialized)
            {
                Debug.Log("[GameManager] 이미 초기화가 완료되었습니다.");
                return;
            }

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
        
        public void Shutdown()
        {
            Debug.Log("[GameManager] 종료 처리");
            _managerRegistry?.ShutdownAllManagers();
        }
        
        [ContextMenu("Log Manager Status")]
        public void LogManagerStatus()
        {
            Debug.Log($"[GameManager] 초기화: {(IsInitialized ? "완료" : "미완료")}");
            _managerRegistry?.LogManagerStatus();
        }

        public async UniTask TransitionToMainSceneAsync()
        {
            Debug.Log("[GameManager] MainScene으로 전환 시작");
            
            try
            {
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainSence");
                Debug.Log("[GameManager] MainScene 전환 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] 씬 전환 실패: {ex.Message}");
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
                Debug.Log("[GameManager] 매니저 참조 설정 완료");
            }
            else
            {
                Debug.LogError("[GameManager] 필수 매니저가 설정되지 않았습니다.");
            }
        }
    }
    
    public interface IManager
    {
        void Shutdown();
    }
} 