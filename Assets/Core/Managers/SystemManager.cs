using UnityEngine;
using System;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Loading;

namespace ProjectVG.Core.Managers
{
    public class SystemManager : Singleton<SystemManager>
    {
        [Header("Core Managers")]
        [SerializeField] private WebSocketManager _webSocketManager;
        [SerializeField] private SessionManager _sessionManager;
        [SerializeField] private HttpApiClient _httpApiClient;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private LoadingManager _loadingManager;

        
        [Header("Settings")]
        [SerializeField] private bool _autoInitializeOnStart = true;
        [SerializeField] private bool _createManagersIfNotExist = true;
        [SerializeField] private bool _autoUpdateCameraOnSceneChange = true;

        [Header("Camera Settings")]
        [SerializeField] private Camera _camera;
        
        private bool _initializationKickoffDone = false;
        public bool IsInitialized { get; private set; }

        public WebSocketManager WebSocketManager => _webSocketManager;
        public SessionManager SessionManager => _sessionManager;
        public AudioManager AudioManager => _audioManager;
        public LoadingManager LoadingManager => _loadingManager;

        public event Action OnGameInitialized;
        public event Action<string> OnInitializationError;

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
            if (_initializationKickoffDone && IsInitialized)
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
            try
            {
                await InitializeManagersAsync();
                IsInitialized = true;
                Debug.Log("[SystemManager] 게임 시스템 준비 완료");
                OnGameInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                Debug.LogError($"[SystemManager] 초기화 실패: {ex.Message}");
                OnInitializationError?.Invoke(ex.Message);
            }
        }
        
        public void Shutdown()
        {
            try { _httpApiClient?.Shutdown(); } catch {}
            try { _sessionManager?.Shutdown(); } catch {}
            try { _webSocketManager?.Shutdown(); } catch {}
            Debug.Log("[SystemManager] 시스템 종료 완료");
        }
        
        [ContextMenu("Log Manager Status")]
        public void LogManagerStatus()
        {
            Debug.Log($"[SystemManager] Initialized: {IsInitialized}");
            Debug.Log($"[SystemManager] Current Camera: {(_camera != null ? _camera.name : "null")}");
            Debug.Log($"[SystemManager] WS: {(WebSocketManager != null ? "OK" : "null")}, Session: {(SessionManager != null ? "OK" : "null")}, Audio: {(AudioManager != null ? "OK" : "null")}, Loading: {(LoadingManager != null ? "OK" : "null")}");
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
        }
        
        private void CreateManagersIfNotExist()
        {
            if (_createManagersIfNotExist)
            {
                _webSocketManager = WebSocketManager.Instance;
                _sessionManager = SessionManager.Instance;
                _httpApiClient = HttpApiClient.Instance;
                _audioManager = AudioManager.Instance;
                _loadingManager = LoadingManager.Instance;
            }
        }

        private async UniTask InitializeManagersAsync()
        {
            if (_webSocketManager == null || _sessionManager == null || _httpApiClient == null)
            {
                throw new InvalidOperationException("필수 매니저 인스턴스를 찾을 수 없습니다.");
            }
            _loadingManager?.BeginLoadingUI();
            _audioManager?.Initialize();
            _webSocketManager.Initialize();
            _sessionManager.Initialize(_webSocketManager);
            _httpApiClient.Initialize(_sessionManager);

            bool connected = await _sessionManager.EnsureConnectionAsync();
            if (!connected)
            {
                throw new InvalidOperationException("세션 연결 실패");
            }
        }
    }
    
    
} 