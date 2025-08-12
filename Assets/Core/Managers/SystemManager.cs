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
        [SerializeField] private bool _autoUpdateCameraOnSceneChange = true;

        [Header("Camera Settings")]
        [SerializeField] private Camera _camera;
        
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

        private void Start()
        {
            // 씬 전환 이벤트 구독
            if (_autoUpdateCameraOnSceneChange)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }
        
        private void OnDestroy()
        {
            if (this != Instance)
            {
                return;
            }
            
            // 이벤트 구독 해제
            if (_autoUpdateCameraOnSceneChange)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            }
            
            Shutdown();
        }

        /// <summary>
        /// 씬이 로드될 때 호출된다.
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[SystemManager] 씬 로드됨: {scene.name}");
            UpdateCamera();
        }

        /// <summary>
        /// 현재 씬의 Main Camera로 Camera를 업데이트한다.
        /// </summary>
        public void UpdateCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _camera = mainCamera;
                Debug.Log($"[SystemManager] Camera 업데이트: {mainCamera.name}");
                
                // ScreenTapManager에 Camera 주입
                if (ScreenTapManager.Instance != null)
                {
                    ScreenTapManager.Instance.UpdateCamera(_camera);
                }
            }
            else
            {
                Debug.LogWarning("[SystemManager] Main Camera를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 수동으로 Camera를 설정한다.
        /// </summary>
        public void SetCamera(Camera camera)
        {
            _camera = camera;
            Debug.Log($"[SystemManager] Camera 수동 설정: {(camera != null ? camera.name : "null")}");
            
            // ScreenTapManager에 Camera 주입
            if (ScreenTapManager.Instance != null)
            {
                ScreenTapManager.Instance.UpdateCamera(_camera);
            }
        }
        
        public async void InitializeGame()
        {
            if (_initializationKickoffDone && (IsInitialized || (_initializationManager?.IsInitializing ?? false)))
            {
                return;
            }
            _initializationKickoffDone = true;

            // Camera 업데이트 및 ScreenTapManager 초기화
            UpdateCamera();
            if (_camera != null)
            {
                ScreenTapManager.Instance.Initialize(_camera);
            }

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
            Debug.Log($"[SystemManager] Current Camera: {(_camera != null ? _camera.name : "null")}");
            _managerRegistry?.LogManagerStatus();
        }

        [ContextMenu("Update Camera")]
        public void UpdateCameraFromContextMenu()
        {
            UpdateCamera();
        }

        [ContextMenu("Set Main Camera")]
        public void SetMainCameraFromContextMenu()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                SetCamera(mainCamera);
            }
            else
            {
                Debug.LogWarning("[SystemManager] Main Camera를 찾을 수 없습니다.");
            }
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