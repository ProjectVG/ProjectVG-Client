using System;
using UnityEngine;
using ProjectVG.Core.Utils;
using ProjectVG.Core.Input;

namespace ProjectVG.Core.Settings
{
    public class PauseMenuManager : Singleton<PauseMenuManager>
    {
        [Header("UI References")]
        [SerializeField] private GameObject _pauseMenuUI;
        [SerializeField] private GameObject _settingsMenuUI;

        [Header("Settings")]
        [SerializeField] private bool _pauseTimeOnMenu = true;
        [SerializeField] private bool _enableOnWebGL = false;

        private bool _isPaused = false;
        private bool _isSettingsOpen = false;
        private float _previousTimeScale = 1f;
        
        // 입력 시스템
        private PlatformInputManager _inputManager;

        public bool IsPaused => _isPaused;
        public bool IsSettingsOpen => _isSettingsOpen;

        public event Action<bool> OnPauseStateChanged;
        public event Action<bool> OnSettingsStateChanged;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            Initialize();
            SetupInputSystem();
        }

        private void SetupInputSystem()
        {
            _inputManager = PlatformInputManager.Instance;
            
            if (_inputManager != null)
            {
                _inputManager.OnMenuTogglePressed += OnMenuToggleInput;
            }
            else
            {
                Debug.LogWarning("[PauseMenuManager] PlatformInputManager를 찾을 수 없습니다.");
            }
        }

        private void OnEnable()
        {
            if (_inputManager != null && ShouldEnableInput())
            {
                _inputManager.OnMenuTogglePressed += OnMenuToggleInput;
            }
        }

        private void OnDisable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnMenuTogglePressed -= OnMenuToggleInput;
            }
        }

        private void Initialize()
        {
            // WebGL에서는 필요하지 않으면 비활성화
            if (Application.platform == RuntimePlatform.WebGLPlayer && !_enableOnWebGL)
            {
                gameObject.SetActive(false);
                return;
            }

            // 초기 상태 설정
            if (_pauseMenuUI != null)
                _pauseMenuUI.SetActive(false);
            
            if (_settingsMenuUI != null)
                _settingsMenuUI.SetActive(false);

            Debug.Log("[PauseMenuManager] 초기화 완료");
        }

        private bool ShouldEnableInput()
        {
            // WebGL에서는 입력 비활성화 (옵션에 따라)
            if (Application.platform == RuntimePlatform.WebGLPlayer && !_enableOnWebGL)
                return false;

            return true;
        }

        private void OnMenuToggleInput()
        {
            if (!ShouldEnableInput()) return;

            // 설정 메뉴가 열려있으면 설정 메뉴 닫기
            if (_isSettingsOpen)
            {
                CloseSettings();
            }
            // 일시정지 상태 토글
            else
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (_isPaused) return;

            _isPaused = true;

            // 시간 정지
            if (_pauseTimeOnMenu)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            // 일시정지 메뉴 UI 표시
            if (_pauseMenuUI != null)
                _pauseMenuUI.SetActive(true);

            OnPauseStateChanged?.Invoke(_isPaused);
            
            Debug.Log("[PauseMenuManager] 게임 일시정지");
        }

        public void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;

            // 시간 복원
            if (_pauseTimeOnMenu)
            {
                Time.timeScale = _previousTimeScale;
            }

            // UI 숨기기
            if (_pauseMenuUI != null)
                _pauseMenuUI.SetActive(false);

            // 설정 메뉴도 함께 닫기
            if (_isSettingsOpen)
                CloseSettings();

            OnPauseStateChanged?.Invoke(_isPaused);
            
            Debug.Log("[PauseMenuManager] 게임 재개");
        }

        public void OpenSettings()
        {
            if (_isSettingsOpen) return;

            _isSettingsOpen = true;

            // 설정 메뉴 UI 표시
            if (_settingsMenuUI != null)
                _settingsMenuUI.SetActive(true);

            // 일시정지 메뉴 UI 숨기기 (설정이 최상단에 표시)
            if (_pauseMenuUI != null)
                _pauseMenuUI.SetActive(false);

            OnSettingsStateChanged?.Invoke(_isSettingsOpen);
            
            Debug.Log("[PauseMenuManager] 설정 메뉴 열기");
        }

        public void CloseSettings()
        {
            if (!_isSettingsOpen) return;

            _isSettingsOpen = false;

            // 설정 메뉴 UI 숨기기
            if (_settingsMenuUI != null)
                _settingsMenuUI.SetActive(false);

            // 아직 일시정지 상태라면 일시정지 메뉴 다시 표시
            if (_isPaused && _pauseMenuUI != null)
                _pauseMenuUI.SetActive(true);

            OnSettingsStateChanged?.Invoke(_isSettingsOpen);
            
            Debug.Log("[PauseMenuManager] 설정 메뉴 닫기");
        }

        public void QuitApplication()
        {
            Debug.Log("[PauseMenuManager] 애플리케이션 종료");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // UI 버튼에서 호출할 메서드들
        public void OnResumeButtonClick()
        {
            Resume();
        }

        public void OnSettingsButtonClick()
        {
            OpenSettings();
        }

        public void OnQuitButtonClick()
        {
            QuitApplication();
        }

        public void OnSettingsBackButtonClick()
        {
            CloseSettings();
        }

        // UI 참조 설정 메서드 (런타임에서 동적 할당용)
        public void SetPauseMenuUI(GameObject pauseMenuUI)
        {
            _pauseMenuUI = pauseMenuUI;
        }

        public void SetSettingsMenuUI(GameObject settingsMenuUI)
        {
            _settingsMenuUI = settingsMenuUI;
        }

        private void OnDestroy()
        {
            if (_inputManager != null)
            {
                _inputManager.OnMenuTogglePressed -= OnMenuToggleInput;
            }
        }

        // 개발자용 디버그 메서드
        [ContextMenu("Toggle Pause (Debug)")]
        private void DebugTogglePause()
        {
            TogglePause();
        }

        [ContextMenu("Open Settings (Debug)")]
        private void DebugOpenSettings()
        {
            if (!_isPaused) Pause();
            OpenSettings();
        }
    }
}