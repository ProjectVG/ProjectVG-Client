#nullable enable
using UnityEngine;
using System;
using ProjectVG.Infrastructure.Network.WebSocket;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Infrastructure.Network.Http;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Loading;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Chat.Service;
using ProjectVG.Core.Utils;

namespace ProjectVG.Core.Managers
{
    public class SystemManager : Singleton<SystemManager>
    {
        [Header("Settings")]
        [SerializeField] private bool _autoUpdateCameraOnSceneChange = true;

        [Header("Camera Settings")]
        [SerializeField] private Camera? _camera;
        
        public event Action? OnCameraUpdated;

        protected override void Awake()
        {
            base.Awake();
            if (this != Instance)
            {
                return;
            }
            
            // 초기 카메라 설정
            UpdateCamera();
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
                
                // ScreenTapManager에 Camera 주입 (CoreManagerRegistry를 통해)
                var coreRegistry = CoreManagerRegistry.Instance;
                if (coreRegistry?.IsAllManagersInitialized == true)
                {
                    var screenTapManager = ScreenTapManager.Instance;
                    if (screenTapManager != null)
                    {
                        screenTapManager.UpdateCamera(_camera);
                    }
                }
                
                OnCameraUpdated?.Invoke();
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
            
            // ScreenTapManager에 Camera 주입 (CoreManagerRegistry를 통해)
            var coreRegistry = CoreManagerRegistry.Instance;
            if (coreRegistry?.IsAllManagersInitialized == true)
            {
                var screenTapManager = ScreenTapManager.Instance;
                if (screenTapManager != null)
                {
                    screenTapManager.UpdateCamera(_camera);
                }
            }
            
            OnCameraUpdated?.Invoke();
        }
        

        public void Shutdown()
        {
            // 매니저들의 셧다운은 CoreManagerRegistry에서 담당하도록 변경 가능
            Debug.Log("[SystemManager] 시스템 종료 완료");
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
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainScene");
                Debug.Log("[SystemManager] MainScene 전환 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SystemManager] 씬 전환 실패: {ex.Message}");
            }
        }
        
    }
    
    
} 